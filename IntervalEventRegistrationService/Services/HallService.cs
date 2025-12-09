using System.Text.Json;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Hall;
using IntervalEventRegistrationService.DTOs.Response.Hall;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntervalEventRegistrationService.Services;

public class HallService : IHallService
{
    private readonly IHallRepository _hallRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IEventRepository _eventRepository;

    public HallService(
        IHallRepository hallRepository,
        ISeatRepository seatRepository,
        IEventRepository eventRepository)
    {
        _hallRepository = hallRepository;
        _seatRepository = seatRepository;
        _eventRepository = eventRepository;
    }

    public async Task<PagedResponse<HallListItemDto>> GetAllHallsAsync(HallFilterRequest request)
    {
        var (halls, totalCount) = await _hallRepository.GetAllAsync(
            request.PageNumber,
            request.PageSize,
            request.Search,
            request.Status,
            request.MinCapacity,
            request.MaxCapacity);

        var hallDtos = new List<HallListItemDto>();
        foreach (var hall in halls)
        {
            var totalSeats = await _seatRepository.CountByHallIdAsync(hall.HallId);
            hallDtos.Add(new HallListItemDto
            {
                HallId = hall.HallId,
                Name = hall.Name,
                Location = hall.Address,
                Capacity = hall.Capacity,
                Status = hall.Status,
                TotalSeats = totalSeats,
                CreatedAt = hall.CreatedAt
            });
        }

        return new PagedResponse<HallListItemDto>
        {
            Success = true,
            Message = "Lấy danh sách hội trường thành công",
            Data = hallDtos,
            Pagination = new PaginationMeta
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            }
        };
    }

    public async Task<ApiResponse<HallDetailDto>> GetHallByIdAsync(string hallId)
    {
        var hall = await _hallRepository.GetByIdAsync(hallId);

        if (hall == null)
        {
            return ApiResponse<HallDetailDto>.FailureResponse("Không tìm thấy hội trường");
        }

        var dto = await MapToDetailDto(hall);
        return ApiResponse<HallDetailDto>.SuccessResponse(dto, "Lấy thông tin hội trường thành công");
    }

    public async Task<ApiResponse<HallDetailDto>> CreateHallAsync(CreateHallRequestDto request, string organizerId)
    {
        var hall = new Hall
        {
            HallId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Address = request.Location,
            Capacity = request.Capacity,
            Facilities = request.Facilities,
            Status = "active",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _hallRepository.CreateAsync(hall);

        var createdHall = await _hallRepository.GetByIdAsync(hall.HallId);
        var dto = await MapToDetailDto(createdHall!);

        return ApiResponse<HallDetailDto>.SuccessResponse(dto, "Tạo hội trường thành công");
    }

    public async Task<ApiResponse<HallDetailDto>> UpdateHallAsync(
        string hallId,
        UpdateHallRequestDto request,
        string currentUserId,
        string currentUserRole)
    {
        var hall = await _hallRepository.GetByIdAsync(hallId);

        if (hall == null)
        {
            return ApiResponse<HallDetailDto>.FailureResponse("Không tìm thấy hội trường");
        }

        // Check if reducing capacity
        if (request.Capacity < hall.Capacity)
        {
            var totalSeats = await _seatRepository.CountByHallIdAsync(hallId);
            if (request.Capacity < totalSeats)
            {
                return ApiResponse<HallDetailDto>.FailureResponse(
                    $"Không thể giảm sức chứa xuống dưới {totalSeats} (số ghế đã tạo)");
            }
        }

        hall.Name = request.Name;
        hall.Address = request.Location;
        hall.Capacity = request.Capacity;
        hall.Facilities = request.Facilities;
        hall.Status = request.Status;

        await _hallRepository.UpdateAsync(hall);

        var updatedHall = await _hallRepository.GetByIdAsync(hallId);
        var dto = await MapToDetailDto(updatedHall!);

        return ApiResponse<HallDetailDto>.SuccessResponse(dto, "Cập nhật hội trường thành công");
    }

    public async Task<ApiResponse<bool>> DeleteHallAsync(
        string hallId,
        string currentUserId,
        string currentUserRole)
    {
        var hall = await _hallRepository.GetByIdAsync(hallId);

        if (hall == null)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy hội trường");
        }

        var activeEventsCount = await _hallRepository.GetActiveEventsCountAsync(hallId);
        if (activeEventsCount > 0)
        {
            return ApiResponse<bool>.FailureResponse(
                $"Không thể xóa hội trường vì có {activeEventsCount} sự kiện đang sử dụng");
        }

        var result = await _hallRepository.DeleteAsync(hallId);

        if (!result)
        {
            return ApiResponse<bool>.FailureResponse("Xóa hội trường thất bại");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa hội trường thành công");
    }

    public async Task<ApiResponse<List<SeatDto>>> GetHallSeatsAsync(
        string hallId, 
        string? seatType = null, 
        bool? isActive = null)
    {
        try
        {
            var hall = await _hallRepository.GetByIdAsync(hallId);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<List<SeatDto>>.FailureResponse("Không tìm thấy hội trường");
            }

            var seats = await _seatRepository.GetByHallIdAsync(hallId);

            // Filter by status if provided
            if (!string.IsNullOrEmpty(seatType))
            {
                seats = seats.Where(s => s.Status == seatType).ToList();
            }

            var seatDtos = seats.Select(s => new SeatDto
            {
                SeatId = s.SeatId,
                HallId = s.HallId,
                SeatNumber = s.SeatNumber,
                RowLabel = s.RowLabel,
                Section = s.Section,
                Status = s.Status
            }).ToList();

            return ApiResponse<List<SeatDto>>.SuccessResponse(
                seatDtos,
                $"Lấy danh sách {seatDtos.Count} ghế thành công"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<List<SeatDto>>.FailureResponse(
                "Lỗi khi lấy danh sách ghế",
                new List<string> { ex.Message }
            );
        }
    }

    /// <summary>
    /// Generate seats automatically for a hall
    /// </summary>
    public async Task<ApiResponse<List<SeatDto>>> GenerateSeatsAsync(
        string hallId,
        GenerateSeatsRequestDto request,
        string currentUserId,
        string currentUserRole)
    {
        try
        {
            // 1. Validate Hall exists
            var hall = await _hallRepository.GetByIdAsync(hallId);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<List<SeatDto>>.FailureResponse("Không tìm thấy hội trường");
            }

            // 2. Check if seats already exist
            var existingSeats = await _seatRepository.GetByHallIdAsync(hallId);
            if (existingSeats.Any())
            {
                return ApiResponse<List<SeatDto>>.FailureResponse(
                    $"Hội trường đã có {existingSeats.Count} ghế. Vui lòng xóa ghế cũ trước khi tạo mới"
                );
            }

            // 3. Validate capacity
            int totalSeats = request.Rows * request.SeatsPerRow;
            if (totalSeats > hall.Capacity)
            {
                return ApiResponse<List<SeatDto>>.FailureResponse(
                    $"Tổng số ghế ({totalSeats}) vượt quá sức chứa hội trường ({hall.Capacity})"
                );
            }

            // 4. Generate seats
            var seats = new List<Seat>();
            var rowLabels = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            for (int row = 0; row < request.Rows; row++)
            {
                // Handle rows beyond Z (AA, AB, AC...)
                string rowLabel = row < 26
                    ? rowLabels[row].ToString()
                    : $"{rowLabels[row / 26 - 1]}{rowLabels[row % 26]}";

                for (int seatNum = 1; seatNum <= request.SeatsPerRow; seatNum++)
                {
                    var seat = new Seat
                    {
                        SeatId = Guid.NewGuid().ToString(),
                        HallId = hallId,
                        SeatNumber = $"{rowLabel}{seatNum}", // A1, A2, B1...
                        RowLabel = rowLabel,                 // A, B, C...
                        Section = "main",                    // Default: main section
                        Status = "available",                // Default: available
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    seats.Add(seat);
                }
            }

            // 5. Save to database
            await _seatRepository.AddRangeAsync(seats);
            await _seatRepository.SaveChangesAsync();

            // 6. Map to DTO
            var seatDtos = seats.Select(s => new SeatDto
            {
                SeatId = s.SeatId,
                HallId = s.HallId,
                SeatNumber = s.SeatNumber,
                RowLabel = s.RowLabel,
                Section = s.Section,
                Status = s.Status
            }).ToList();

            return ApiResponse<List<SeatDto>>.SuccessResponse(
                seatDtos,
                $"Tạo thành công {totalSeats} ghế cho hội trường"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<List<SeatDto>>.FailureResponse(
                "Lỗi khi tạo ghế tự động",
                new List<string> { ex.Message }
            );
        }
    }

    /// <summary>
    /// Check if hall is available (simplified - no date/time check)
    /// </summary>
    public async Task<ApiResponse<HallAvailabilityDto>> CheckAvailabilityAsync(string hallId)
    {
        try
        {
            // 1. Validate Hall exists
            var hall = await _hallRepository.GetByIdAsync(hallId);
            if (hall == null || hall.IsDeleted)
            {
                return ApiResponse<HallAvailabilityDto>.FailureResponse("Không tìm thấy hội trường");
            }

            // 2. Check current status
            bool isAvailable = hall.Status == "available" && hall.IsActive;

            // 3. Count total seats
            var totalSeats = await _seatRepository.CountByHallIdAsync(hallId);

            // 4. Count available seats
            var availableSeats = await _seatRepository.CountAvailableSeatsAsync(hallId);

            // 5. Get active events using this hall (có thể null)
            List<Event>? activeEvents = null;
            try
            {
                activeEvents = await _eventRepository.GetActiveEventsByHallIdAsync(hallId);
            }
            catch
            {
                // Ignore if method not found
            }

            var dto = new HallAvailabilityDto
            {
                HallId = hall.HallId,
                HallName = hall.Name,
                Status = hall.Status,
                IsAvailable = isAvailable,
                TotalCapacity = hall.Capacity,
                TotalSeats = totalSeats,
                AvailableSeats = availableSeats,
                OccupiedSeats = totalSeats - availableSeats,
                ActiveEventsCount = activeEvents?.Count ?? 0
            };

            return ApiResponse<HallAvailabilityDto>.SuccessResponse(
                dto,
                isAvailable ? "Hội trường đang trống" : "Hội trường đang được sử dụng"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<HallAvailabilityDto>.FailureResponse(
                "Lỗi khi kiểm tra tình trạng hội trường",
                new List<string> { ex.Message }
            );
        }
    }

    private async Task<HallDetailDto> MapToDetailDto(Hall hall)
    {
        var totalSeats = await _seatRepository.CountByHallIdAsync(hall.HallId);
        var activeEventsCount = await _hallRepository.GetActiveEventsCountAsync(hall.HallId);

        FacilitiesDto? facilitiesParsed = null;
        if (!string.IsNullOrWhiteSpace(hall.Facilities))
        {
            try
            {
                facilitiesParsed = JsonSerializer.Deserialize<FacilitiesDto>(hall.Facilities);
            }
            catch
            {
                // Ignore parse error
            }
        }

        return new HallDetailDto
        {
            HallId = hall.HallId,
            Name = hall.Name,
            Location = hall.Address,
            Capacity = hall.Capacity,
            Status = hall.Status,
            TotalSeats = totalSeats,
            CreatedAt = hall.CreatedAt,
            Facilities = hall.Facilities,
            FacilitiesParsed = facilitiesParsed,
            Seats = hall.Seats?.Select(s => new SeatDto
            {
                SeatId = s.SeatId,
                HallId = s.HallId,
                SeatNumber = s.SeatNumber,
                RowLabel = s.RowLabel,
                Section = s.Section,
                Status = s.Status
            }).ToList() ?? new List<SeatDto>(),
            ActiveEventsCount = activeEventsCount,
            UpdatedAt = hall.UpdatedAt
        };
    }
}

using System.Text.Json;
using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request.Hall;
using IntervalEventRegistrationService.DTOs.Response.Hall;
using IntervalEventRegistrationService.Interfaces;

namespace IntervalEventRegistrationService.Services;

public class HallService : IHallService
{
    private readonly IHallRepository _hallRepository;
    private readonly ISeatRepository _seatRepository;

    public HallService(IHallRepository hallRepository, ISeatRepository seatRepository)
    {
        _hallRepository = hallRepository;
        _seatRepository = seatRepository;
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
            var totalSeats = await _seatRepository.GetTotalSeatsCountAsync(hall.HallId);
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
        var hall = await _hallRepository.GetByIdAsync(hallId, includeRelations: true);

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

        var createdHall = await _hallRepository.GetByIdAsync(hall.HallId, includeRelations: true);
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
            var totalSeats = await _seatRepository.GetTotalSeatsCountAsync(hallId);
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

        var updatedHall = await _hallRepository.GetByIdAsync(hallId, includeRelations: true);
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
        var hallExists = await _hallRepository.ExistsAsync(hallId);
        if (!hallExists)
        {
            return ApiResponse<List<SeatDto>>.FailureResponse("Không tìm thấy hội trường");
        }

        var seats = await _seatRepository.GetSeatsByHallIdAsync(hallId, seatType, isActive);

        var seatDtos = seats.Select(s => new SeatDto
        {
            SeatId = s.SeatId,
            SeatCode = s.SeatNumber,
            SeatRow = s.RowLabel,
            SeatNumber = int.TryParse(s.SeatNumber, out var num) ? num : (int?)null,
            SeatType = s.Section ?? "regular",
            IsActive = !s.IsDeleted && s.Status == "available"
        }).ToList();

        return ApiResponse<List<SeatDto>>.SuccessResponse(
            seatDtos,
            $"Lấy danh sách {seatDtos.Count} ghế thành công");
    }

    public async Task<ApiResponse<List<SeatDto>>> GenerateSeatsAsync(
        string hallId,
        GenerateSeatsRequestDto request,
        string currentUserId,
        string currentUserRole)
    {
        var hall = await _hallRepository.GetByIdAsync(hallId);

        if (hall == null)
        {
            return ApiResponse<List<SeatDto>>.FailureResponse("Không tìm thấy hội trường");
        }

        var totalSeats = request.Rows * request.SeatsPerRow;
        if (totalSeats > hall.Capacity)
        {
            return ApiResponse<List<SeatDto>>.FailureResponse(
                $"Tổng số ghế ({totalSeats}) vượt quá sức chứa ({hall.Capacity})");
        }

        // Delete existing seats
        await _seatRepository.DeleteSeatsByHallIdAsync(hallId);

        // Generate new seats
        var seats = new List<Seat>();
        for (int row = 0; row < request.Rows; row++)
        {
            var rowLetter = ((char)('A' + row)).ToString();

            for (int seatNum = 1; seatNum <= request.SeatsPerRow; seatNum++)
            {
                var seatCode = $"{request.Prefix}{rowLetter}{seatNum}";

                seats.Add(new Seat
                {
                    SeatId = Guid.NewGuid().ToString(),
                    HallId = hallId,
                    SeatNumber = seatCode,
                    RowLabel = rowLetter,
                    Section = request.SeatType,
                    Status = "available",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _seatRepository.CreateBulkAsync(seats);

        var seatDtos = seats.Select(s => new SeatDto
        {
            SeatId = s.SeatId,
            SeatCode = s.SeatNumber,
            SeatRow = s.RowLabel,
            SeatNumber = int.TryParse(s.SeatNumber.Replace(request.Prefix, "").Substring(1), out var num) ? num : (int?)null,
            SeatType = s.Section ?? "regular",
            IsActive = !s.IsDeleted && s.Status == "available"
        }).ToList();

        return ApiResponse<List<SeatDto>>.SuccessResponse(
            seatDtos,
            $"Tạo thành công {seatDtos.Count} ghế");
    }

    public async Task<ApiResponse<HallAvailabilityDto>> CheckAvailabilityAsync(
        string hallId,
        CheckAvailabilityRequestDto request)
    {
        var hall = await _hallRepository.GetByIdAsync(hallId);

        if (hall == null)
        {
            return ApiResponse<HallAvailabilityDto>.FailureResponse("Không tìm thấy hội trường");
        }

        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<HallAvailabilityDto>.FailureResponse(
                "Thời gian kết thúc phải sau thời gian bắt đầu");
        }

        var conflictingEvents = await _hallRepository.GetConflictingEventsAsync(
            hallId,
            request.Date,
            request.StartTime,
            request.EndTime);

        var isAvailable = !conflictingEvents.Any();

        var dto = new HallAvailabilityDto
        {
            HallId = hallId,
            HallName = hall.Name,
            IsAvailable = isAvailable,
            ConflictingEvents = conflictingEvents.Select(e => new ConflictingEventDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Date = e.Date,
                StartTime = e.StartTime,
                EndTime = e.EndTime
            }).ToList(),
            Message = isAvailable
                ? "Hội trường còn trống"
                : $"Hội trường đã được đặt bởi {conflictingEvents.Count} sự kiện"
        };

        return ApiResponse<HallAvailabilityDto>.SuccessResponse(dto);
    }

    private async Task<HallDetailDto> MapToDetailDto(Hall hall)
    {
        var totalSeats = await _seatRepository.GetTotalSeatsCountAsync(hall.HallId);
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
                SeatCode = s.SeatNumber,
                SeatRow = s.RowLabel,
                SeatNumber = int.TryParse(s.SeatNumber, out var num) ? num : (int?)null,
                SeatType = s.Section ?? "regular",
                IsActive = !s.IsDeleted && s.Status == "available"
            }).ToList() ?? new List<SeatDto>(),
            ActiveEventsCount = activeEventsCount,
            UpdatedAt = hall.UpdatedAt
        };
    }
}

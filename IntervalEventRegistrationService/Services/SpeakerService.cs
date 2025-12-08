using IntervalEventRegistrationRepo.Entities;
using IntervalEventRegistrationRepo.Interfaces;
using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IntervalEventRegistrationService.Services;

public class SpeakerService : ISpeakerService
{
    private readonly ISpeakerRepository _speakerRepository;
    private readonly ICloudinaryService _cloudinaryService;

    public SpeakerService(ISpeakerRepository speakerRepository, ICloudinaryService cloudinaryService)
    {
        _speakerRepository = speakerRepository;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<PagedResponse<SpeakerResponseDto>> GetAllSpeakersAsync(PaginationRequest request)
    {
        var speakers = await _speakerRepository.GetAllAsync(request.PageNumber, request.PageSize, request.Search);
        var totalItems = await _speakerRepository.GetTotalCountAsync(request.Search);

        var speakerDtos = speakers.Select(s => MapToResponseDto(s)).ToList();

        return new PagedResponse<SpeakerResponseDto>
        {
            Success = true,
            Message = "Lấy danh sách speakers thành công",
            Data = speakerDtos,
            Pagination = new PaginationMeta
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize)
            }
        };
    }

    public async Task<ApiResponse<SpeakerDetailResponseDto>> GetSpeakerByIdAsync(string speakerId)
    {
        var speaker = await _speakerRepository.GetByIdAsync(speakerId);

        if (speaker == null)
        {
            return ApiResponse<SpeakerDetailResponseDto>.FailureResponse("Không tìm thấy speaker");
        }

        var totalEvents = await _speakerRepository.GetEventsCountBySpeakerIdAsync(speakerId);

        var dto = new SpeakerDetailResponseDto
        {
            SpeakerId = speaker.SpeakerId,
            Name = speaker.Name,
            Title = speaker.Title,
            Company = speaker.Company,
            Bio = speaker.Bio,
            Email = speaker.Email,
            Phone = speaker.Phone,
            LinkedinUrl = speaker.LinkedinUrl,
            AvatarUrl = speaker.AvatarUrl,
            CreatedAt = speaker.CreatedAt,
            UpdatedAt = speaker.UpdatedAt,
            TotalEvents = totalEvents
        };

        return ApiResponse<SpeakerDetailResponseDto>.SuccessResponse(dto, "Lấy thông tin speaker thành công");
    }

    public async Task<ApiResponse<SpeakerResponseDto>> CreateSpeakerAsync(CreateSpeakerRequest request)
    {
        // Kiểm tra email trùng lặp nếu được cung cấp
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingSpeaker = await _speakerRepository.GetByEmailAsync(request.Email);
            if (existingSpeaker != null)
            {
                return ApiResponse<SpeakerResponseDto>.FailureResponse("Email đã được sử dụng cho speaker khác");
            }
        }

        // Biến lưu URL ảnh sau upload
        string? avatarUrl = null;

        // Nếu có file ảnh được upload, tiến hành upload lên Cloudinary
        if (request.AvatarFile is not null && request.AvatarFile.Length > 0)
        {
            try
            {
                // Upload ảnh lên Cloudinary trong thư mục "speakers"
                avatarUrl = await _cloudinaryService.UploadAsync(request.AvatarFile, "speakers");
            }
            catch (Exception ex)
            {
                return ApiResponse<SpeakerResponseDto>.FailureResponse($"Lỗi upload ảnh: {ex.Message}");
            }
        }

        var speaker = new Speaker
        {
            Name = request.Name,
            Title = request.Title,
            Company = request.Company,
            Bio = request.Bio,
            Email = request.Email,
            Phone = request.Phone,
            LinkedinUrl = request.LinkedinUrl,
            // Sử dụng URL từ Cloudinary hoặc để trống nếu không có ảnh
            AvatarUrl = avatarUrl
        };

        var createdSpeaker = await _speakerRepository.CreateAsync(speaker);

        return ApiResponse<SpeakerResponseDto>.SuccessResponse(
            MapToResponseDto(createdSpeaker), 
            "Tạo speaker thành công");
    }

    public async Task<ApiResponse<SpeakerResponseDto>> UpdateSpeakerAsync(string speakerId, UpdateSpeakerRequest request)
    {
        var speaker = await _speakerRepository.GetByIdAsync(speakerId);

        if (speaker == null)
        {
            return ApiResponse<SpeakerResponseDto>.FailureResponse("Không tìm thấy speaker");
        }

        // Kiểm tra email trùng lặp nếu email được thay đổi
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != speaker.Email)
        {
            var existingSpeaker = await _speakerRepository.GetByEmailAsync(request.Email);
            if (existingSpeaker != null && existingSpeaker.SpeakerId != speakerId)
            {
                return ApiResponse<SpeakerResponseDto>.FailureResponse("Email đã được sử dụng cho speaker khác");
            }
        }

        // Nếu có file ảnh mới được upload, tiến hành upload lên Cloudinary
        if (request.AvatarFile is not null && request.AvatarFile.Length > 0)
        {
            try
            {
                // Upload ảnh mới lên Cloudinary trong thư mục "speakers"
                speaker.AvatarUrl = await _cloudinaryService.UploadAsync(request.AvatarFile, "speakers");
            }
            catch (Exception ex)
            {
                return ApiResponse<SpeakerResponseDto>.FailureResponse($"Lỗi upload ảnh: {ex.Message}");
            }
        }
        // Nếu không có file ảnh mới, giữ nguyên ảnh cũ

        // Cập nhật các trường thông tin
        speaker.Name = request.Name;
        speaker.Title = request.Title;
        speaker.Company = request.Company;
        speaker.Bio = request.Bio;
        speaker.Email = request.Email;
        speaker.Phone = request.Phone;
        speaker.LinkedinUrl = request.LinkedinUrl;

        var updatedSpeaker = await _speakerRepository.UpdateAsync(speaker);

        return ApiResponse<SpeakerResponseDto>.SuccessResponse(
            MapToResponseDto(updatedSpeaker), 
            "Cập nhật speaker thành công");
    }

    public async Task<ApiResponse<bool>> DeleteSpeakerAsync(string speakerId)
    {
        var exists = await _speakerRepository.ExistsAsync(speakerId);

        if (!exists)
        {
            return ApiResponse<bool>.FailureResponse("Không tìm thấy speaker");
        }

        var result = await _speakerRepository.DeleteAsync(speakerId);

        if (!result)
        {
            return ApiResponse<bool>.FailureResponse("Xóa speaker thất bại");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa speaker thành công");
    }

    public async Task<PagedResponse<SpeakerEventResponseDto>> GetSpeakerEventsAsync(string speakerId, PaginationRequest request)
    {
        var exists = await _speakerRepository.ExistsAsync(speakerId);

        if (!exists)
        {
            return new PagedResponse<SpeakerEventResponseDto>
            {
                Success = false,
                Message = "Không tìm thấy speaker",
                Data = new List<SpeakerEventResponseDto>()
            };
        }

        var events = await _speakerRepository.GetEventsBySpeakerIdAsync(speakerId, request.PageNumber, request.PageSize);
        var totalItems = await _speakerRepository.GetEventsCountBySpeakerIdAsync(speakerId);

        var eventDtos = events.Select(e => new SpeakerEventResponseDto
        {
            EventId = e.EventId,
            Title = e.Title,
            Description = e.Description,
            Date = e.Date,
            StartTime = e.StartTime,
            EndTime = e.EndTime,
            Location = e.Location,
            ImageUrl = e.ImageUrl,
            Status = e.Status,
            TotalSeats = e.TotalSeats,
            RegisteredCount = e.RegisteredCount
        }).ToList();

        return new PagedResponse<SpeakerEventResponseDto>
        {
            Success = true,
            Message = "Lấy danh sách sự kiện của speaker thành công",
            Data = eventDtos,
            Pagination = new PaginationMeta
            {
                CurrentPage = request.PageNumber,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize)
            }
        };
    }

    private static SpeakerResponseDto MapToResponseDto(Speaker speaker)
    {
        return new SpeakerResponseDto
        {
            SpeakerId = speaker.SpeakerId,
            Name = speaker.Name,
            Title = speaker.Title,
            Company = speaker.Company,
            Bio = speaker.Bio,
            Email = speaker.Email,
            Phone = speaker.Phone,
            LinkedinUrl = speaker.LinkedinUrl,
            AvatarUrl = speaker.AvatarUrl,
            CreatedAt = speaker.CreatedAt,
            UpdatedAt = speaker.UpdatedAt
        };
    }
}

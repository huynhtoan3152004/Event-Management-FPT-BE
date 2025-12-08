using IntervalEventRegistrationService.DTOs.Common;
using IntervalEventRegistrationService.DTOs.Request;
using IntervalEventRegistrationService.DTOs.Response;

namespace IntervalEventRegistrationService.Interfaces;

public interface ISpeakerService
{
    Task<PagedResponse<SpeakerResponseDto>> GetAllSpeakersAsync(PaginationRequest request);
    Task<ApiResponse<SpeakerDetailResponseDto>> GetSpeakerByIdAsync(string speakerId);
    Task<ApiResponse<SpeakerResponseDto>> CreateSpeakerAsync(CreateSpeakerRequest request);
    Task<ApiResponse<SpeakerResponseDto>> UpdateSpeakerAsync(string speakerId, UpdateSpeakerRequest request);
    Task<ApiResponse<bool>> DeleteSpeakerAsync(string speakerId);
    Task<PagedResponse<SpeakerEventResponseDto>> GetSpeakerEventsAsync(string speakerId, PaginationRequest request);
}

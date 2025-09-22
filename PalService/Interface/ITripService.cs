using PalService.DTOs;

namespace PalService.Interface
{
    public interface ITripService
    {
        Task<ResponseDto<TripDto>> CreateTripAsync(CreateTripDto dto, int driverId);
        Task<ResponseDto<List<TripDto>>> SearchTripsAsync(SearchTripsDto dto);
        Task<ResponseDto<List<TripDto>>> GetDriverTripsAsync(int driverId);
        Task<ResponseDto<TripDto>> GetTripByIdAsync(int tripId);
        Task<ResponseDto<bool>> CancelTripAsync(int tripId, int driverId);
        Task<ResponseDto<bool>> CompleteTripAsync(int tripId, int driverId);

        // Search history
        Task<ResponseDto<List<SearchHistoryItemDto>>> GetSearchHistoryAsync(int userId, int limit = 5);
        Task<ResponseDto<bool>> SaveSearchHistoryAsync(int userId, SearchTripsDto dto);
    }
}


using PalService.DTOs;
using PalRepository.Models;

namespace PalService.Interface
{
    public interface ITripService
    {
        Task<ResponseDto<TripDto>> CreateTripAsync(CreateTripDto dto, int driverId);
        Task<ResponseDto<List<TripDto>>> SearchTripsAsync(SearchTripsDto dto);
        Task<ResponseDto<List<TripDto>>> SearchPassengerRequestsAsync(SearchTripsDto dto);
        Task<ResponseDto<List<TripDto>>> SearchPassengerRequestsFilteredAsync(SearchTripsDto dto);
        Task<ResponseDto<List<TripDto>>> GetDriverTripsAsync(int driverId);
        Task<ResponseDto<TripDto>> GetTripByIdAsync(int tripId);
        Task<ResponseDto<bool>> CancelTripAsync(int tripId, int driverId);
        Task<ResponseDto<bool>> CompleteTripAsync(int tripId, int driverId);
        Task<ResponseDto<PriceRangeDto>> GetPriceRangeAsync(string pickup, string dropoff);
        Task<ResponseDto<VehicleDto>> AddVehicleAsync(int userId, AddVehicleDto dto);
        Task<ResponseDto<bool>> VerifyVehicleAsync(int vehicleId, bool verified);
        Task<ResponseDto<List<VehicleDto>>> GetMyVehiclesAsync(int userId);
        Task<ResponseDto<VehicleDto>> UpdateVehicleAsync(int userId, int vehicleId, UpdateVehicleDto dto);
        Task<ResponseDto<TripDto>> CreatePassengerRequestAsync(int userId, CreatePassengerRequestDto dto);
        Task<ResponseDto<bool>> WithdrawPassengerRequestAsync(int userId, int tripId);
    }
}

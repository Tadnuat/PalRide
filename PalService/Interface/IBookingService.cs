using PalRepository.DTOs.PalRide.API.Models.DTOs;

namespace PalService.Interface
{
    public interface IBookingService
    {
        Task<ResponseDto<BookingDto>> CreateBookingAsync(CreateBookingDto dto, int passengerId);
        Task<ResponseDto<BookingDto>> AcceptBookingAsync(int bookingId, int driverId);
        Task<ResponseDto<BookingDto>> CancelBookingAsync(int bookingId, int userId);
        Task<ResponseDto<List<BookingDto>>> GetUserBookingsAsync(int userId);
        Task<ResponseDto<List<BookingDto>>> GetTripBookingsAsync(int tripId, int driverId);
    }
}

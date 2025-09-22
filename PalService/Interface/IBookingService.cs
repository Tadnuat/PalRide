using PalService.DTOs;

namespace PalService.Interface
{
    public interface IBookingService
    {
        // Removed: CreateBookingAsync replaced by ConfirmBookingAsync
        Task<ResponseDto<BookingDto>> AcceptBookingAsync(int bookingId, int driverId);
        Task<ResponseDto<BookingDto>> CancelBookingAsync(int bookingId, int userId);
        Task<ResponseDto<List<BookingDto>>> GetUserBookingsAsync(int userId);
        Task<ResponseDto<List<BookingDto>>> GetTripBookingsAsync(int tripId, int driverId);

        // Pre-booking flow
        Task<ResponseDto<List<VoucherPreviewDto>>> GetApplicableVouchersAsync(int userId, int tripId, int seatCount, bool fullRide);
        Task<ResponseDto<BookingQuoteDto>> GetBookingQuoteAsync(int userId, BookingQuoteRequestDto dto);
        Task<ResponseDto<BookingDto>> ConfirmBookingAsync(int userId, ConfirmBookingDto dto);
    }
}

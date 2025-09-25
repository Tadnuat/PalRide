using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;
using PalService.DTOs;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // Pre-booking flow
        [HttpGet("trip/{tripId}/vouchers")]
        public async Task<IActionResult> GetApplicableVouchers(int tripId, [FromQuery] int seatCount = 1, [FromQuery] bool fullRide = false)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetApplicableVouchersAsync(userId, tripId, seatCount, fullRide);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("quote")]
        public async Task<IActionResult> GetQuote([FromBody] BookingQuoteRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetBookingQuoteAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmBookingDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.ConfirmBookingAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // Old CreateBooking removed in favor of quote + confirm

        [HttpPut("{bookingId}/accept")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.AcceptBookingAsync(bookingId, driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPut("{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.CancelBookingAsync(bookingId, userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetUserBookingsAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("my-bookings/history")]
        public async Task<IActionResult> GetMyBookingHistory()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetUserBookingHistoryAsync(userId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("trip/{tripId}/bookings")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetTripBookings(int tripId)
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _bookingService.GetTripBookingsAsync(tripId, driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }
    }
}

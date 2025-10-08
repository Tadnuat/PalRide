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
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.GetApplicableVouchersAsync(userId, tripId, seatCount, fullRide);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving applicable vouchers. Please try again later." });
            }
        }

        [HttpPost("quote")]
        public async Task<IActionResult> GetQuote([FromBody] BookingQuoteRequestDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.GetBookingQuoteAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while getting booking quote. Please try again later." });
            }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> Confirm([FromBody] ConfirmBookingDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.ConfirmBookingAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while confirming booking. Please try again later." });
            }
        }

        // Old CreateBooking removed in favor of quote + confirm

        [HttpPut("{bookingId}/accept")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> AcceptBooking(int bookingId)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.AcceptBookingAsync(bookingId, driverId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while accepting booking. Please try again later." });
            }
        }

        [HttpPut("{bookingId}/cancel")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.CancelBookingAsync(bookingId, userId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while canceling booking. Please try again later." });
            }
        }

        [HttpGet("my-bookings")]
        public async Task<IActionResult> GetMyBookings()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.GetUserBookingsAsync(userId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your bookings. Please try again later." });
            }
        }

        [HttpGet("my-bookings/history")]
        public async Task<IActionResult> GetMyBookingHistory()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.GetUserBookingHistoryAsync(userId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your booking history. Please try again later." });
            }
        }

        [HttpGet("trip/{tripId}/bookings")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetTripBookings(int tripId)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _bookingService.GetTripBookingsAsync(tripId, driverId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { isSuccess = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { isSuccess = false, message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving trip bookings. Please try again later." });
            }
        }
    }
}

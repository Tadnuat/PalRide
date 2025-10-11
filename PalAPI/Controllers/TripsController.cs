using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripsController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpPost]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CreateTrip(CreateTripDto dto)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.CreateTripAsync(dto, driverId);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while creating trip. Please try again later." });
            }
        }


        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTrips([FromQuery] SearchTripsDto dto)
        {
            try
            {
                var result = await _tripService.SearchTripsAsync(dto);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while searching trips. Please try again later." });
            }
        }

        [HttpGet("requests")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPassengerRequests()
        {
            try
            {
                var result = await _tripService.SearchPassengerRequestsAsync(new SearchTripsDto());
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving passenger requests. Please try again later." });
            }
        }

        [HttpGet("requests/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPassengerRequests([FromQuery] SearchTripsDto dto)
        {
            try
            {
                var result = await _tripService.SearchPassengerRequestsFilteredAsync(dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while searching passenger requests. Please try again later." });
            }
        }

        [HttpPost("accept-request")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> AcceptPassengerRequest(AcceptPassengerRequestDto dto)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.AcceptPassengerRequestAsync(driverId, dto);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while accepting passenger request. Please try again later." });
            }
        }

        [HttpPut("{tripId}")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> UpdateTrip(int tripId, UpdateTripDto dto)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.UpdateTripAsync(tripId, driverId, dto);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating trip. Please try again later." });
            }
        }

        [HttpGet("price-range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPriceRange([FromQuery] string pickup, [FromQuery] string dropoff)
        {
            try
            {
                var result = await _tripService.GetPriceRangeAsync(pickup, dropoff);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while getting price range. Please try again later." });
            }
        }


        [HttpPost("request")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> CreatePassengerRequest(CreatePassengerRequestDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.CreatePassengerRequestAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while creating passenger request. Please try again later." });
            }
        }

        [HttpPut("request/{tripId}/withdraw")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> WithdrawPassengerRequest(int tripId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.WithdrawPassengerRequestAsync(userId, tripId);
                if (!result.IsSuccess) return BadRequest(result);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while withdrawing passenger request. Please try again later." });
            }
        }

        [HttpGet("my-requests")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> GetMyPassengerRequests()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.GetMyPassengerRequestsAsync(userId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your passenger requests. Please try again later." });
            }
        }

        [HttpPut("request/{tripId}")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> UpdatePassengerRequest(int tripId, [FromBody] UpdatePassengerRequestDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.UpdatePassengerRequestAsync(userId, tripId, dto);
                if (!result.IsSuccess) return BadRequest(result);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating passenger request. Please try again later." });
            }
        }

        [HttpGet("my-trips")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetMyTrips()
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.GetDriverTripsAsync(driverId);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your trips. Please try again later." });
            }
        }

        [HttpGet("my-trips/history")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetMyTripHistory()
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.GetDriverTripHistoryAsync(driverId);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your trip history. Please try again later." });
            }
        }

        [HttpGet("{tripId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrip(int tripId)
        {
            try
            {
                var result = await _tripService.GetTripByIdAsync(tripId);
                
                if (!result.IsSuccess)
                    return BadRequest(result);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving trip details. Please try again later." });
            }
        }

        [HttpPut("{tripId}/cancel")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CancelTrip(int tripId)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.CancelTripAsync(tripId, driverId);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while canceling trip. Please try again later." });
            }
        }

        [HttpPut("{tripId}/complete")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CompleteTrip(int tripId)
        {
            try
            {
                var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.CompleteTripAsync(tripId, driverId);
                
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while completing trip. Please try again later." });
            }
        }
    }
}

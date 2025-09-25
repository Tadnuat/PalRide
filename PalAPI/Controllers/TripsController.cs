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
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.CreateTripAsync(dto, driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPost("sell")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CreateSellTrip(CreateSellTripDto dto)
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.CreateSellTripAsync(dto, driverId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTrips([FromQuery] SearchTripsDto dto)
        {
            var result = await _tripService.SearchTripsAsync(dto);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("requests")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPassengerRequests()
        {
            var result = await _tripService.SearchPassengerRequestsAsync(new SearchTripsDto());
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("requests/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPassengerRequests([FromQuery] SearchTripsDto dto)
        {
            var result = await _tripService.SearchPassengerRequestsFilteredAsync(dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("price-range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPriceRange([FromQuery] string pickup, [FromQuery] string dropoff)
        {
            var result = await _tripService.GetPriceRangeAsync(pickup, dropoff);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }


        [HttpPost("request")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> CreatePassengerRequest(CreatePassengerRequestDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.CreatePassengerRequestAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("request/{tripId}/withdraw")]
        [Authorize(Roles = "Passenger,Both")]
        public async Task<IActionResult> WithdrawPassengerRequest(int tripId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.WithdrawPassengerRequestAsync(userId, tripId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("my-trips")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetMyTrips()
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.GetDriverTripsAsync(driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("my-trips/history")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetMyTripHistory()
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.GetDriverTripHistoryAsync(driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpGet("{tripId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTrip(int tripId)
        {
            var result = await _tripService.GetTripByIdAsync(tripId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPut("{tripId}/cancel")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CancelTrip(int tripId)
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.CancelTripAsync(tripId, driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        [HttpPut("{tripId}/complete")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> CompleteTrip(int tripId)
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.CompleteTripAsync(tripId, driverId);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }
    }
}

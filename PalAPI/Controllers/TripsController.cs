using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using PalService.DTOs;
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

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchTrips([FromQuery] SearchTripsDto dto)
        {
            var result = await _tripService.SearchTripsAsync(dto);
            
            if (!result.IsSuccess)
                return BadRequest(result);
            
            return Ok(result);
        }

        // Search history APIs
        [HttpGet("search/history")]
        public async Task<IActionResult> GetSearchHistory([FromQuery] int limit = 5)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.GetSearchHistoryAsync(userId, limit);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("search/history")]
        public async Task<IActionResult> SaveSearchHistory([FromBody] SearchTripsDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.SaveSearchHistoryAsync(userId, dto);
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

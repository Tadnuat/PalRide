using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Driver,Both")] // only drivers can add vehicles
    public class VehiclesController : ControllerBase
    {
        private readonly ITripService _tripService;

        public VehiclesController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpPost]
        public async Task<IActionResult> AddVehicle(AddVehicleDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.AddVehicleAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.GetMyVehiclesAsync(userId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{vehicleId}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, UpdateVehicleDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _tripService.UpdateVehicleAsync(userId, vehicleId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{vehicleId}/verify")]
        [Authorize(Roles = "Admin")] // admin verifies vehicles
        public async Task<IActionResult> VerifyVehicle(int vehicleId, [FromBody] VerifyVehicleDto dto)
        {
            var result = await _tripService.VerifyVehicleAsync(vehicleId, dto.Verified);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}



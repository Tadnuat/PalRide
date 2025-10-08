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
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.AddVehicleAsync(userId, dto);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while adding vehicle. Please try again later." });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyVehicles()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.GetMyVehiclesAsync(userId);
                if (!result.IsSuccess) return BadRequest(result);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { isSuccess = false, message = $"Invalid input data: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while retrieving your vehicles. Please try again later." });
            }
        }

        [HttpPut("{vehicleId}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, UpdateVehicleDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _tripService.UpdateVehicleAsync(userId, vehicleId, dto);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while updating vehicle. Please try again later." });
            }
        }

        [HttpPut("{vehicleId}/verify")]
        [Authorize(Roles = "Admin")] // admin verifies vehicles
        public async Task<IActionResult> VerifyVehicle(int vehicleId, [FromBody] VerifyVehicleDto dto)
        {
            try
            {
                var result = await _tripService.VerifyVehicleAsync(vehicleId, dto.Verified);
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
                return StatusCode(500, new { isSuccess = false, message = "An unexpected error occurred while verifying vehicle. Please try again later." });
            }
        }
    }
}



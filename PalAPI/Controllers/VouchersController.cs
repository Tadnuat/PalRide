using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VouchersController : ControllerBase
    {
        private readonly IVoucherService _voucherService;

        public VouchersController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Require admin to create
        public async Task<IActionResult> Create([FromBody] CreateVoucherDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _voucherService.CreateVoucherAsync(dto, adminId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        // Removed grant endpoint; distribution occurs during creation based on dto flags
    }
}



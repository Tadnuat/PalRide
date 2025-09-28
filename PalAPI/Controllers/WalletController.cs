using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("driver/summary")]
        [Authorize(Roles = "Driver,Both")]
        public async Task<IActionResult> GetDriverSummary()
        {
            var driverId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _walletService.GetDriverWalletSummaryAsync(driverId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }
    }
}










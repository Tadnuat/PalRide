using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PalService.DTOs;
using PalService.Interface;
using System.Security.Claims;

namespace PalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Tạo đánh giá mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _reviewService.CreateReviewAsync(userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật đánh giá
        /// </summary>
        [HttpPut("{reviewId}")]
        public async Task<IActionResult> UpdateReview(int reviewId, [FromBody] UpdateReviewDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _reviewService.UpdateReviewAsync(reviewId, userId, dto);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Xóa đánh giá
        /// </summary>
        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = GetCurrentUserId();
            var result = await _reviewService.DeleteReviewAsync(reviewId, userId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin đánh giá theo ID
        /// </summary>
        [HttpGet("{reviewId}")]
        public async Task<IActionResult> GetReviewById(int reviewId)
        {
            var result = await _reviewService.GetReviewByIdAsync(reviewId);
            if (!result.IsSuccess) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy tóm tắt đánh giá của user (theo screen)
        /// </summary>
        [HttpGet("summary/{userId}")]
        public async Task<IActionResult> GetUserReviewSummary(int userId)
        {
            var result = await _reviewService.GetUserReviewSummaryAsync(userId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đánh giá của user với filter
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserReviews(int userId, [FromQuery] ReviewFilterDto filter)
        {
            var result = await _reviewService.GetUserReviewsAsync(userId, filter);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thống kê đánh giá của user
        /// </summary>
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetReviewStats(int userId)
        {
            var result = await _reviewService.GetReviewStatsAsync(userId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Lấy đánh giá của một chuyến đi
        /// </summary>
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripReviews(int tripId)
        {
            var result = await _reviewService.GetTripReviewsAsync(tripId);
            if (!result.IsSuccess) return BadRequest(result);
            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID");
            }
            return userId;
        }
    }
}

using PalService.DTOs;

namespace PalService.Interface
{
    public interface IReviewService
    {
        Task<ResponseDto<ReviewDto>> CreateReviewAsync(int fromUserId, CreateReviewDto dto);
        Task<ResponseDto<ReviewDto>> UpdateReviewAsync(int reviewId, int userId, UpdateReviewDto dto);
        Task<ResponseDto<bool>> DeleteReviewAsync(int reviewId, int userId);
        Task<ResponseDto<ReviewDto>> GetReviewByIdAsync(int reviewId);
        Task<ResponseDto<ReviewSummaryDto>> GetUserReviewSummaryAsync(int userId);
        Task<ResponseDto<List<ReviewDto>>> GetUserReviewsAsync(int userId, ReviewFilterDto filter);
        Task<ResponseDto<ReviewStatsDto>> GetReviewStatsAsync(int userId);
        Task<ResponseDto<List<ReviewDto>>> GetTripReviewsAsync(int tripId);
    }
}

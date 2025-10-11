using System.ComponentModel.DataAnnotations;

namespace PalService.DTOs
{
    public class CreateReviewDto
    {
        [Required]
        public int TripId { get; set; }

        [Required]
        public int ToUserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public byte Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class UpdateReviewDto
    {
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public byte? Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }
    }

    public class ReviewDto
    {
        public int ReviewId { get; set; }
        public int? TripId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FromUserName { get; set; } = string.Empty;
        public string ToUserName { get; set; } = string.Empty;
        public string? TripInfo { get; set; }
    }

    public class ReviewSummaryDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<ReviewDto> RecentReviews { get; set; } = new List<ReviewDto>();
    }

    public class ReviewFilterDto
    {
        public int? Rating { get; set; } // 1-5, null for all
        public string? Tag { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReviewStatsDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
        public List<TagCountDto> TagCounts { get; set; } = new List<TagCountDto>();
    }

    public class TagCountDto
    {
        public string Tag { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}





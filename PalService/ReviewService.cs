using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.DTOs;
using PalService.Interface;

namespace PalService
{
    public class ReviewService : IReviewService
    {
        private readonly PalRideContext _context;
        private readonly GenericRepository<Review> _reviewRepo;

        public ReviewService(PalRideContext context, GenericRepository<Review> reviewRepo)
        {
            _context = context;
            _reviewRepo = reviewRepo;
        }

        public async Task<ResponseDto<ReviewDto>> CreateReviewAsync(int fromUserId, CreateReviewDto dto)
        {
            var response = new ResponseDto<ReviewDto>();
            try
            {
                // Validate trip exists and user participated
                var trip = await _context.Trips
                    .Include(t => t.Driver)
                    .Include(t => t.Bookings)
                    .FirstOrDefaultAsync(t => t.TripId == dto.TripId);

                if (trip == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Trip not found";
                    return response;
                }

                // Check if user participated in the trip
                var userParticipated = trip.DriverId == fromUserId || 
                                     trip.Bookings.Any(b => b.PassengerId == fromUserId);

                if (!userParticipated)
                {
                    response.IsSuccess = false;
                    response.Message = "You can only review trips you participated in";
                    return response;
                }

                // Check if user already reviewed this person for this trip
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.TripId == dto.TripId && 
                                            r.FromUserId == fromUserId && 
                                            r.ToUserId == dto.ToUserId);

                if (existingReview != null)
                {
                    response.IsSuccess = false;
                    response.Message = "You have already reviewed this person for this trip";
                    return response;
                }

                // Validate toUserId is in the trip
                var toUserInTrip = trip.DriverId == dto.ToUserId || 
                                 trip.Bookings.Any(b => b.PassengerId == dto.ToUserId);

                if (!toUserInTrip)
                {
                    response.IsSuccess = false;
                    response.Message = "The person you're reviewing must be in the same trip";
                    return response;
                }

                var review = new Review
                {
                    TripId = dto.TripId,
                    FromUserId = fromUserId,
                    ToUserId = dto.ToUserId,
                    Rating = dto.Rating,
                    Comment = dto.Comment?.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                await _reviewRepo.CreateAsync(review);

                // Load user information
                var fromUser = await _context.Users.FindAsync(fromUserId);
                var toUser = await _context.Users.FindAsync(dto.ToUserId);

                response.Result = new ReviewDto
                {
                    ReviewId = review.ReviewId,
                    TripId = review.TripId,
                    FromUserId = review.FromUserId,
                    ToUserId = review.ToUserId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    FromUserName = fromUser?.FullName ?? "Unknown",
                    ToUserName = toUser?.FullName ?? "Unknown",
                    TripInfo = $"{trip.PickupLocation} → {trip.DropoffLocation}"
                };
                response.Message = "Review created successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReviewDto>> UpdateReviewAsync(int reviewId, int userId, UpdateReviewDto dto)
        {
            var response = new ResponseDto<ReviewDto>();
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.Trip)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.FromUserId == userId);

                if (review == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Review not found or you don't have permission to update it";
                    return response;
                }

                // Check if review is too old to update (e.g., more than 24 hours)
                var timeDiff = DateTime.UtcNow - review.CreatedAt;
                if (timeDiff.TotalHours > 24)
                {
                    response.IsSuccess = false;
                    response.Message = "Review cannot be updated after 24 hours";
                    return response;
                }

                if (dto.Rating.HasValue)
                    review.Rating = dto.Rating.Value;

                if (dto.Comment != null)
                    review.Comment = dto.Comment.Trim();

                await _reviewRepo.UpdateAsync(review);

                // Load user information
                var fromUser = await _context.Users.FindAsync(review.FromUserId);
                var toUser = await _context.Users.FindAsync(review.ToUserId);

                response.Result = new ReviewDto
                {
                    ReviewId = review.ReviewId,
                    TripId = review.TripId,
                    FromUserId = review.FromUserId,
                    ToUserId = review.ToUserId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    FromUserName = fromUser?.FullName ?? "Unknown",
                    ToUserName = toUser?.FullName ?? "Unknown",
                    TripInfo = review.Trip != null ? $"{review.Trip.PickupLocation} → {review.Trip.DropoffLocation}" : null
                };
                response.Message = "Review updated successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> DeleteReviewAsync(int reviewId, int userId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.FromUserId == userId);

                if (review == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Review not found or you don't have permission to delete it";
                    return response;
                }

                // Check if review is too old to delete (e.g., more than 24 hours)
                var timeDiff = DateTime.UtcNow - review.CreatedAt;
                if (timeDiff.TotalHours > 24)
                {
                    response.IsSuccess = false;
                    response.Message = "Review cannot be deleted after 24 hours";
                    return response;
                }

                await _reviewRepo.RemoveAsync(review);
                response.Result = true;
                response.Message = "Review deleted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReviewDto>> GetReviewByIdAsync(int reviewId)
        {
            var response = new ResponseDto<ReviewDto>();
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.FromUser)
                    .Include(r => r.ToUser)
                    .Include(r => r.Trip)
                    .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

                if (review == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Review not found";
                    return response;
                }

                response.Result = new ReviewDto
                {
                    ReviewId = review.ReviewId,
                    TripId = review.TripId,
                    FromUserId = review.FromUserId,
                    ToUserId = review.ToUserId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    FromUserName = review.FromUser?.FullName ?? "Unknown",
                    ToUserName = review.ToUser?.FullName ?? "Unknown",
                    TripInfo = review.Trip != null ? $"{review.Trip.PickupLocation} → {review.Trip.DropoffLocation}" : null
                };
                response.Message = "Review found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReviewSummaryDto>> GetUserReviewSummaryAsync(int userId)
        {
            var response = new ResponseDto<ReviewSummaryDto>();
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found";
                    return response;
                }

                var reviews = await _context.Reviews
                    .Include(r => r.FromUser)
                    .Include(r => r.Trip)
                    .Where(r => r.ToUserId == userId)
                    .ToListAsync();

                var totalReviews = reviews.Count;
                if (totalReviews == 0)
                {
                    response.Result = new ReviewSummaryDto
                    {
                        UserId = userId,
                        UserName = user.FullName,
                        AverageRating = 0,
                        TotalReviews = 0,
                        FiveStarCount = 0,
                        FourStarCount = 0,
                        ThreeStarCount = 0,
                        TwoStarCount = 0,
                        OneStarCount = 0,
                        Tags = new List<string>(),
                        RecentReviews = new List<ReviewDto>()
                    };
                    response.Message = "No reviews found";
                    return response;
                }

                var averageRating = reviews.Average(r => r.Rating);
                var ratingDistribution = reviews.GroupBy(r => r.Rating)
                    .ToDictionary(g => (int)g.Key, g => g.Count());

                var fiveStarCount = ratingDistribution.GetValueOrDefault(5, 0);
                var fourStarCount = ratingDistribution.GetValueOrDefault(4, 0);
                var threeStarCount = ratingDistribution.GetValueOrDefault(3, 0);
                var twoStarCount = ratingDistribution.GetValueOrDefault(2, 0);
                var oneStarCount = ratingDistribution.GetValueOrDefault(1, 0);

                // Generate tags based on comments (simplified)
                var tags = GenerateTagsFromReviews(reviews);

                // Get recent reviews
                var recentReviews = reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new ReviewDto
                    {
                        ReviewId = r.ReviewId,
                        TripId = r.TripId,
                        FromUserId = r.FromUserId,
                        ToUserId = r.ToUserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        FromUserName = r.FromUser?.FullName ?? "Unknown",
                        ToUserName = user.FullName,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null
                    })
                    .ToList();

                response.Result = new ReviewSummaryDto
                {
                    UserId = userId,
                    UserName = user.FullName,
                    AverageRating = Math.Round(averageRating, 1),
                    TotalReviews = totalReviews,
                    FiveStarCount = fiveStarCount,
                    FourStarCount = fourStarCount,
                    ThreeStarCount = threeStarCount,
                    TwoStarCount = twoStarCount,
                    OneStarCount = oneStarCount,
                    Tags = tags,
                    RecentReviews = recentReviews
                };
                response.Message = "Review summary retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<ReviewDto>>> GetUserReviewsAsync(int userId, ReviewFilterDto filter)
        {
            var response = new ResponseDto<List<ReviewDto>>();
            try
            {
                var query = _context.Reviews
                    .Include(r => r.FromUser)
                    .Include(r => r.Trip)
                    .Where(r => r.ToUserId == userId);

                // Apply rating filter
                if (filter.Rating.HasValue)
                {
                    query = query.Where(r => r.Rating == filter.Rating.Value);
                }

                // Apply tag filter (simplified - would need more sophisticated tag matching)
                if (!string.IsNullOrWhiteSpace(filter.Tag))
                {
                    query = query.Where(r => r.Comment != null && r.Comment.Contains(filter.Tag));
                }

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(r => new ReviewDto
                    {
                        ReviewId = r.ReviewId,
                        TripId = r.TripId,
                        FromUserId = r.FromUserId,
                        ToUserId = r.ToUserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        FromUserName = r.FromUser.FullName,
                        ToUserName = r.ToUser.FullName,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null
                    })
                    .ToListAsync();

                response.Result = reviews;
                response.Message = $"Found {reviews.Count} reviews";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReviewStatsDto>> GetReviewStatsAsync(int userId)
        {
            var response = new ResponseDto<ReviewStatsDto>();
            try
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ToUserId == userId)
                    .ToListAsync();

                var totalReviews = reviews.Count;
                if (totalReviews == 0)
                {
                    response.Result = new ReviewStatsDto
                    {
                        AverageRating = 0,
                        TotalReviews = 0,
                        RatingDistribution = new Dictionary<int, int>(),
                        TagCounts = new List<TagCountDto>()
                    };
                    response.Message = "No reviews found";
                    return response;
                }

                var averageRating = reviews.Average(r => r.Rating);
                var ratingDistribution = reviews.GroupBy(r => r.Rating)
                    .ToDictionary(g => (int)g.Key, g => g.Count());

                // Generate tag counts
                var tagCounts = GenerateTagCountsFromReviews(reviews);

                response.Result = new ReviewStatsDto
                {
                    AverageRating = Math.Round(averageRating, 1),
                    TotalReviews = totalReviews,
                    RatingDistribution = ratingDistribution,
                    TagCounts = tagCounts
                };
                response.Message = "Review stats retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<ReviewDto>>> GetTripReviewsAsync(int tripId)
        {
            var response = new ResponseDto<List<ReviewDto>>();
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.FromUser)
                    .Include(r => r.ToUser)
                    .Include(r => r.Trip)
                    .Where(r => r.TripId == tripId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReviewDto
                    {
                        ReviewId = r.ReviewId,
                        TripId = r.TripId,
                        FromUserId = r.FromUserId,
                        ToUserId = r.ToUserId,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        FromUserName = r.FromUser.FullName,
                        ToUserName = r.ToUser.FullName,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null
                    })
                    .ToListAsync();

                response.Result = reviews;
                response.Message = $"Found {reviews.Count} reviews for trip";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        private List<string> GenerateTagsFromReviews(List<Review> reviews)
        {
            // Simplified tag generation based on common keywords
            var tagKeywords = new Dictionary<string, List<string>>
            {
                ["Sạch sẽ"] = new List<string> { "sạch", "clean", "gọn gàng" },
                ["Biệt dương"] = new List<string> { "riêng tư", "private", "biệt dương" },
                ["Thân thiện"] = new List<string> { "thân thiện", "friendly", "dễ thương" },
                ["Cái mới"] = new List<string> { "mới", "new", "hiện đại" },
                ["Xe mới"] = new List<string> { "xe mới", "new car", "xe đẹp" },
                ["Nhiệt tình"] = new List<string> { "nhiệt tình", "enthusiastic", "tích cực" },
                ["Vui vẻ"] = new List<string> { "vui vẻ", "cheerful", "hài hước" },
                ["Lịch sự"] = new List<string> { "lịch sự", "polite", "có văn hóa" },
                ["Ít nói"] = new List<string> { "ít nói", "quiet", "im lặng" }
            };

            var tagCounts = new Dictionary<string, int>();
            var comments = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                                 .Select(r => r.Comment.ToLower())
                                 .ToList();

            foreach (var tag in tagKeywords)
            {
                var count = comments.Count(comment => 
                    tag.Value.Any(keyword => comment.Contains(keyword.ToLower())));
                if (count > 0)
                {
                    tagCounts[tag.Key] = count;
                }
            }

            return tagCounts.OrderByDescending(t => t.Value)
                           .Select(t => t.Key)
                           .ToList();
        }

        private List<TagCountDto> GenerateTagCountsFromReviews(List<Review> reviews)
        {
            var tagKeywords = new Dictionary<string, List<string>>
            {
                ["Sạch sẽ"] = new List<string> { "sạch", "clean", "gọn gàng" },
                ["Biệt dương"] = new List<string> { "riêng tư", "private", "biệt dương" },
                ["Thân thiện"] = new List<string> { "thân thiện", "friendly", "dễ thương" },
                ["Cái mới"] = new List<string> { "mới", "new", "hiện đại" },
                ["Xe mới"] = new List<string> { "xe mới", "new car", "xe đẹp" },
                ["Nhiệt tình"] = new List<string> { "nhiệt tình", "enthusiastic", "tích cực" },
                ["Vui vẻ"] = new List<string> { "vui vẻ", "cheerful", "hài hước" },
                ["Lịch sự"] = new List<string> { "lịch sự", "polite", "có văn hóa" },
                ["Ít nói"] = new List<string> { "ít nói", "quiet", "im lặng" }
            };

            var tagCounts = new List<TagCountDto>();
            var comments = reviews.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                                 .Select(r => r.Comment.ToLower())
                                 .ToList();

            foreach (var tag in tagKeywords)
            {
                var count = comments.Count(comment => 
                    tag.Value.Any(keyword => comment.Contains(keyword.ToLower())));
                if (count > 0)
                {
                    tagCounts.Add(new TagCountDto
                    {
                        Tag = tag.Key,
                        Count = count
                    });
                }
            }

            return tagCounts.OrderByDescending(t => t.Count).ToList();
        }
    }
}

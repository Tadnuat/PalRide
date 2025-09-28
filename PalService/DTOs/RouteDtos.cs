using System.ComponentModel.DataAnnotations;

namespace PalService.DTOs
{
    public class CreateRouteDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string PickupLocation { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string DropoffLocation { get; set; } = string.Empty;

        public bool IsRoundTrip { get; set; }
    }

    public class UpdateRouteDto
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string PickupLocation { get; set; } = string.Empty;

        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string DropoffLocation { get; set; } = string.Empty;

        public bool IsRoundTrip { get; set; }
    }

    public class RouteDto
    {
        public int RouteId { get; set; }
        public int UserId { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public bool IsRoundTrip { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    public class RouteListDto
    {
        public int RouteId { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public bool IsRoundTrip { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TripCount { get; set; }
    }
}
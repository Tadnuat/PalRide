using System;

namespace PalService.DTOs
{
    public class CreateRouteDto
    {
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public bool IsRoundTrip { get; set; } = false;
    }

    public class RouteDto
    {
        public int RouteId { get; set; }
        public string PickupLocation { get; set; } = string.Empty;
        public string DropoffLocation { get; set; } = string.Empty;
        public bool IsRoundTrip { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}




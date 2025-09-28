using System;

namespace PalService.DTOs
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public int? UserId { get; set; }
        public string? UserRole { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Important" or "Other"
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedEntityType { get; set; } // "Trip", "Booking", "Voucher", etc.
        public int? RelatedEntityId { get; set; }
    }

    public class CreateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Other"; // "Important" or "Other"
        public string? TargetRole { get; set; } // "Driver", "Passenger", "Both", null for all
        public int? TargetUserId { get; set; } // null for broadcast
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }

    public class CreateBulkNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Other"; // "Important" or "Other"
        public List<int> TargetUserIds { get; set; } = new List<int>(); // Danh sách UserId
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }

    public class SignalRNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}


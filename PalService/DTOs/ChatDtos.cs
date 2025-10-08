using System.ComponentModel.DataAnnotations;

namespace PalService.DTOs
{
    public class SendMessageDto
    {
        [Required]
        public int TripId { get; set; }
        
        [Required]
        public int ToUserId { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string MessageText { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public int MessageId { get; set; }
        public int TripId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string FromUserName { get; set; } = string.Empty;
        public string ToUserName { get; set; } = string.Empty;
    }

    public class ChatHistoryDto
    {
        public int TripId { get; set; }
        public string TripInfo { get; set; } = string.Empty; // "Suối Tiên, Q. 9 → ĐH FPT, Q. 9. 12/08 20:00"
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserRole { get; set; } = string.Empty;
    }

    public class ChatListDto
    {
        public int TripId { get; set; }
        public string TripInfo { get; set; } = string.Empty;
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserRole { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public DateTime? LastMessageTime { get; set; }
        public bool HasUnreadMessages { get; set; }
        public int UnreadCount { get; set; }
    }

    public class MarkAsReadDto
    {
        [Required]
        public int TripId { get; set; }
        
        [Required]
        public int FromUserId { get; set; }
    }
}




using PalService.DTOs;

namespace PalService.Interface
{
    public interface IChatService
    {
        Task<MessageDto> SendMessageAsync(int fromUserId, SendMessageDto sendMessageDto);
        Task<List<MessageDto>> GetChatHistoryAsync(int userId, int tripId);
        Task<List<ChatListDto>> GetChatListAsync(int userId);
        Task MarkMessagesAsReadAsync(int userId, MarkAsReadDto markAsReadDto);
        Task<int> GetUnreadMessageCountAsync(int userId);
        Task<bool> CanUserChatAsync(int userId, int tripId);
    }
}




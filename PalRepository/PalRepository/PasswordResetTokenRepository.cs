using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PalRepository.PalRepository
{
    public class PasswordResetTokenRepository
    {
        private readonly PalRideContext _context;
        public PasswordResetTokenRepository(PalRideContext context)
        {
            _context = context;
        }

        public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token)
        {
            _context.PasswordResetTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<PasswordResetToken> GetActiveByUserAndTokenAsync(int userId, string token)
        {
            return await _context.PasswordResetTokens
                .Where(t => t.UserId == userId && t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> MarkUsedAsync(PasswordResetToken token)
        {
            token.Used = true;
            _context.PasswordResetTokens.Update(token);
            return await _context.SaveChangesAsync();
        }
    }
}



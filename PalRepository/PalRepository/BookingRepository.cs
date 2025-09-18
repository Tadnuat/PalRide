using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PalRepository.PalRepository
{
    public class BookingRepository : GenericRepository<Booking>
    {
        private readonly PalRideContext _context;

        public BookingRepository(PalRideContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Booking>> GetBookingsByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Where(b => b.PassengerId == userId)
                .OrderByDescending(b => b.BookingTime)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetBookingsByTripIdAsync(int tripId)
        {
            return await _context.Bookings
                .Where(b => b.TripId == tripId)
                .OrderByDescending(b => b.BookingTime)
                .ToListAsync();
        }

        public async Task<Booking> GetBookingByUserAndTripAsync(int userId, int tripId)
        {
            return await _context.Bookings
                .FirstOrDefaultAsync(b => b.PassengerId == userId && b.TripId == tripId);
        }

        public async Task<List<Booking>> GetPendingBookingsByTripIdAsync(int tripId)
        {
            return await _context.Bookings
                .Where(b => b.TripId == tripId && b.Status == "Pending")
                .ToListAsync();
        }
    }
}

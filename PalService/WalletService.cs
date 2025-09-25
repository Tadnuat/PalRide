using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalService.DTOs;
using PalService.Interface;

namespace PalService
{
    public class WalletService : IWalletService
    {
        private readonly PalRideContext _context;

        public WalletService(PalRideContext context)
        {
            _context = context;
        }

        public async Task<ResponseDto<DriverWalletSummaryDto>> GetDriverWalletSummaryAsync(int driverId)
        {
            var response = new ResponseDto<DriverWalletSummaryDto>();
            try
            {
                var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == driverId);
                var balance = wallet?.Balance ?? 0m;

                var now = DateTime.UtcNow;
                var firstDay = new DateTime(now.Year, now.Month, 1);
                var nextMonth = firstDay.AddMonths(1);

                // Monthly income: sum of TotalPrice for bookings that were accepted this month and trip completed
                var monthlyIncome = await _context.Bookings
                    .Where(b => b.Status == "Accepted" && b.UpdatedAt >= firstDay && b.UpdatedAt < nextMonth)
                    .Join(_context.Trips,
                        b => b.TripId,
                        t => t.TripId,
                        (b, t) => new { b.TotalPrice, t.DriverId, t.Status })
                    .Where(x => x.DriverId == driverId && x.Status == "Completed")
                    .SumAsync(x => (decimal?)x.TotalPrice) ?? 0m;

                // Monthly trips count: number of distinct trips for accepted bookings this month by this driver with trip completed
                var monthlyTrips = await _context.Bookings
                    .Where(b => b.Status == "Accepted" && b.UpdatedAt >= firstDay && b.UpdatedAt < nextMonth)
                    .Join(_context.Trips,
                        b => b.TripId,
                        t => t.TripId,
                        (b, t) => new { b.TripId, t.DriverId, t.Status })
                    .Where(x => x.DriverId == driverId && x.Status == "Completed")
                    .Select(x => x.TripId)
                    .Distinct()
                    .CountAsync();

                response.Result = new DriverWalletSummaryDto
                {
                    Balance = balance,
                    MonthlyIncome = monthlyIncome,
                    MonthlyTrips = monthlyTrips
                };
                response.Message = "Driver wallet summary";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}




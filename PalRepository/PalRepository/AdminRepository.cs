using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.UnitOfWork;
using System.Threading.Tasks;

namespace PalRepository.PalRepository
{
    public class AdminRepository : GenericRepository<Admin>
    {
        public AdminRepository(PalRideContext context) : base(context) { }

        public Admin? GetByEmail(string email)
        {
            return _context.Admins.FirstOrDefault(a => a.Email == email);
        }

        public async Task<Admin?> GetByEmailAsync(string email)
        {
            return await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
        }
    }
}







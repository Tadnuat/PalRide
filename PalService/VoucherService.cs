using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.DTOs;
using PalService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PalService
{
    public class VoucherService : IVoucherService
    {
        private readonly PalRideContext _context;
        private readonly GenericRepository<Voucher> _voucherRepo;
        private readonly GenericRepository<UserVoucher> _userVoucherRepo;
        private readonly UserRepository _userRepo;

        public VoucherService(PalRideContext context, GenericRepository<Voucher> voucherRepo, GenericRepository<UserVoucher> userVoucherRepo, UserRepository userRepo)
        {
            _context = context;
            _voucherRepo = voucherRepo;
            _userVoucherRepo = userVoucherRepo;
            _userRepo = userRepo;
        }

        public async Task<ResponseDto<int>> CreateVoucherAsync(CreateVoucherDto dto, int adminId)
        {
            var response = new ResponseDto<int>();
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Code))
                    throw new InvalidOperationException("Code is required");

                // Create voucher
                var voucher = new Voucher
                {
                    Code = dto.Code.Trim(),
                    Description = dto.Description,
                    DiscountType = dto.DiscountType,
                    DiscountValue = dto.DiscountValue,
                    MinOrderValue = dto.MinOrderValue,
                    ExpiryDate = dto.ExpiryDate.HasValue ? DateOnly.FromDateTime(dto.ExpiryDate.Value.Date) : null,
                    UsageLimit = dto.UsageLimit,
                    CreatedBy = adminId,
                    CreatedAt = DateTime.UtcNow
                };

                await _voucherRepo.CreateAsync(voucher);

                // Build distribution targets
                var users = _context.Users.Where(u => u.IsActive);
                if (dto.OnlyVip)
                {
                    users = users.Where(u => u.IsVip);
                }
                if (dto.ToPassengers && !dto.ToDrivers)
                {
                    users = users.Where(u => u.Role == "Passenger" || u.Role == "Both");
                }
                else if (dto.ToDrivers && !dto.ToPassengers)
                {
                    users = users.Where(u => u.Role == "Driver" || u.Role == "Both");
                }
                // if both true or both false → no extra role filter (send to all, possibly restricted by OnlyVip)

                var targetIds = await users.Select(u => u.UserId).ToListAsync();

                if (targetIds.Count == 0)
                {
                    response.Result = 0;
                    response.Message = "Voucher created; no matching users to distribute";
                    return response;
                }

                // Avoid duplicate grants for same voucher
                var existingUserIds = await _context.UserVouchers
                    .Where(uv => uv.VoucherId == voucher.VoucherId && targetIds.Contains(uv.UserId))
                    .Select(uv => uv.UserId)
                    .ToListAsync();

                var newIds = targetIds.Except(existingUserIds).ToList();
                var now = DateTime.UtcNow;
                var grants = newIds.Select(uid => new UserVoucher
                {
                    UserId = uid,
                    VoucherId = voucher.VoucherId,
                    IsUsed = false,
                    GrantedAt = now
                }).ToList();

                if (grants.Count > 0)
                {
                    await _context.AddRangeAsync(grants);
                    await _context.SaveChangesAsync();
                }

                response.Result = grants.Count;
                response.Message = $"Voucher created and distributed to {grants.Count} users";
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



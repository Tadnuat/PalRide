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
        private readonly INotificationService _notificationService;

        public VoucherService(PalRideContext context, GenericRepository<Voucher> voucherRepo, GenericRepository<UserVoucher> userVoucherRepo, UserRepository userRepo, INotificationService notificationService)
        {
            _context = context;
            _voucherRepo = voucherRepo;
            _userVoucherRepo = userVoucherRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
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

                // Send notification about new voucher
                if (!string.IsNullOrEmpty(dto.Message))
                {
                    // Send to appropriate role
                    if (dto.ToPassengers && !dto.ToDrivers)
                    {
                        await _notificationService.SendAndSaveNotificationToRoleAsync(
                            "Passenger", 
                            "Voucher mới", 
                            dto.Message, 
                            "Other", 
                            "Voucher", 
                            voucher.VoucherId);
                    }
                    else if (dto.ToDrivers && !dto.ToPassengers)
                    {
                        await _notificationService.SendAndSaveNotificationToRoleAsync(
                            "Driver", 
                            "Voucher mới", 
                            dto.Message, 
                            "Other", 
                            "Voucher", 
                            voucher.VoucherId);
                    }
                    else
                    {
                        // Send to all users (Both role)
                        await _notificationService.SendAndSaveNotificationToRoleAsync(
                            "Both", 
                            "Voucher mới", 
                            dto.Message, 
                            "Other", 
                            "Voucher", 
                            voucher.VoucherId);
                    }
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

        public async Task<ResponseDto<VoucherDto>> UpdateVoucherAsync(int voucherId, UpdateVoucherDto dto, int adminId)
        {
            var response = new ResponseDto<VoucherDto>();
            try
            {
                var voucher = await _context.Vouchers
                    .Include(v => v.CreatedByNavigation)
                    .FirstOrDefaultAsync(v => v.VoucherId == voucherId);

                if (voucher == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Voucher not found";
                    return response;
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(dto.Description))
                    voucher.Description = dto.Description;

                if (!string.IsNullOrWhiteSpace(dto.DiscountType))
                    voucher.DiscountType = dto.DiscountType;

                if (dto.DiscountValue.HasValue)
                    voucher.DiscountValue = dto.DiscountValue.Value;

                if (dto.MinOrderValue.HasValue)
                    voucher.MinOrderValue = dto.MinOrderValue.Value;

                if (dto.ExpiryDate.HasValue)
                    voucher.ExpiryDate = DateOnly.FromDateTime(dto.ExpiryDate.Value.Date);

                if (dto.UsageLimit.HasValue)
                    voucher.UsageLimit = dto.UsageLimit.Value;

                await _voucherRepo.UpdateAsync(voucher);

                // Get usage statistics
                var usedCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == voucherId && uv.IsUsed);
                var availableCount = voucher.UsageLimit.HasValue ? voucher.UsageLimit.Value - usedCount : int.MaxValue;
                var isExpired = voucher.ExpiryDate.HasValue && voucher.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
                var isFullyUsed = voucher.UsageLimit.HasValue && usedCount >= voucher.UsageLimit.Value;

                response.Result = new VoucherDto
                {
                    VoucherId = voucher.VoucherId,
                    Code = voucher.Code,
                    Description = voucher.Description,
                    DiscountType = voucher.DiscountType,
                    DiscountValue = voucher.DiscountValue,
                    MinOrderValue = voucher.MinOrderValue,
                    ExpiryDate = voucher.ExpiryDate,
                    UsageLimit = voucher.UsageLimit,
                    CreatedBy = voucher.CreatedBy,
                    CreatedAt = voucher.CreatedAt,
                    CreatedByName = voucher.CreatedByNavigation?.FullName ?? "Unknown",
                    UsedCount = usedCount,
                    AvailableCount = availableCount,
                    IsExpired = isExpired,
                    IsFullyUsed = isFullyUsed
                };
                response.Message = "Voucher updated successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<VoucherDto>> GetVoucherByIdAsync(int voucherId)
        {
            var response = new ResponseDto<VoucherDto>();
            try
            {
                var voucher = await _context.Vouchers
                    .Include(v => v.CreatedByNavigation)
                    .FirstOrDefaultAsync(v => v.VoucherId == voucherId);

                if (voucher == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Voucher not found";
                    return response;
                }

                // Get usage statistics
                var usedCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == voucherId && uv.IsUsed);
                var availableCount = voucher.UsageLimit.HasValue ? voucher.UsageLimit.Value - usedCount : int.MaxValue;
                var isExpired = voucher.ExpiryDate.HasValue && voucher.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
                var isFullyUsed = voucher.UsageLimit.HasValue && usedCount >= voucher.UsageLimit.Value;

                response.Result = new VoucherDto
                {
                    VoucherId = voucher.VoucherId,
                    Code = voucher.Code,
                    Description = voucher.Description,
                    DiscountType = voucher.DiscountType,
                    DiscountValue = voucher.DiscountValue,
                    MinOrderValue = voucher.MinOrderValue,
                    ExpiryDate = voucher.ExpiryDate,
                    UsageLimit = voucher.UsageLimit,
                    CreatedBy = voucher.CreatedBy,
                    CreatedAt = voucher.CreatedAt,
                    CreatedByName = voucher.CreatedByNavigation?.FullName ?? "Unknown",
                    UsedCount = usedCount,
                    AvailableCount = availableCount,
                    IsExpired = isExpired,
                    IsFullyUsed = isFullyUsed
                };
                response.Message = "Voucher found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<VoucherDto>>> GetAllVouchersAsync()
        {
            var response = new ResponseDto<List<VoucherDto>>();
            try
            {
                var vouchers = await _context.Vouchers
                    .Include(v => v.CreatedByNavigation)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                var voucherDtos = new List<VoucherDto>();
                foreach (var voucher in vouchers)
                {
                    var usedCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == voucher.VoucherId && uv.IsUsed);
                    var availableCount = voucher.UsageLimit.HasValue ? voucher.UsageLimit.Value - usedCount : int.MaxValue;
                    var isExpired = voucher.ExpiryDate.HasValue && voucher.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
                    var isFullyUsed = voucher.UsageLimit.HasValue && usedCount >= voucher.UsageLimit.Value;

                    voucherDtos.Add(new VoucherDto
                    {
                        VoucherId = voucher.VoucherId,
                        Code = voucher.Code,
                        Description = voucher.Description,
                        DiscountType = voucher.DiscountType,
                        DiscountValue = voucher.DiscountValue,
                        MinOrderValue = voucher.MinOrderValue,
                        ExpiryDate = voucher.ExpiryDate,
                        UsageLimit = voucher.UsageLimit,
                        CreatedBy = voucher.CreatedBy,
                        CreatedAt = voucher.CreatedAt,
                        CreatedByName = voucher.CreatedByNavigation?.FullName ?? "Unknown",
                        UsedCount = usedCount,
                        AvailableCount = availableCount,
                        IsExpired = isExpired,
                        IsFullyUsed = isFullyUsed
                    });
                }

                response.Result = voucherDtos;
                response.Message = $"Found {voucherDtos.Count} vouchers";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<VoucherDto>>> GetActiveVouchersAsync()
        {
            var response = new ResponseDto<List<VoucherDto>>();
            try
            {
                var now = DateOnly.FromDateTime(DateTime.UtcNow);
                var vouchers = await _context.Vouchers
                    .Include(v => v.CreatedByNavigation)
                    .Where(v => (!v.ExpiryDate.HasValue || v.ExpiryDate.Value >= now))
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync();

                var voucherDtos = new List<VoucherDto>();
                foreach (var voucher in vouchers)
                {
                    var usedCount = await _context.UserVouchers.CountAsync(uv => uv.VoucherId == voucher.VoucherId && uv.IsUsed);
                    var isFullyUsed = voucher.UsageLimit.HasValue && usedCount >= voucher.UsageLimit.Value;

                    // Skip fully used vouchers
                    if (isFullyUsed) continue;

                    var availableCount = voucher.UsageLimit.HasValue ? voucher.UsageLimit.Value - usedCount : int.MaxValue;

                    voucherDtos.Add(new VoucherDto
                    {
                        VoucherId = voucher.VoucherId,
                        Code = voucher.Code,
                        Description = voucher.Description,
                        DiscountType = voucher.DiscountType,
                        DiscountValue = voucher.DiscountValue,
                        MinOrderValue = voucher.MinOrderValue,
                        ExpiryDate = voucher.ExpiryDate,
                        UsageLimit = voucher.UsageLimit,
                        CreatedBy = voucher.CreatedBy,
                        CreatedAt = voucher.CreatedAt,
                        CreatedByName = voucher.CreatedByNavigation?.FullName ?? "Unknown",
                        UsedCount = usedCount,
                        AvailableCount = availableCount,
                        IsExpired = false,
                        IsFullyUsed = false
                    });
                }

                response.Result = voucherDtos;
                response.Message = $"Found {voucherDtos.Count} active vouchers";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> DeleteVoucherAsync(int voucherId, int adminId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.VoucherId == voucherId);
                if (voucher == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Voucher not found";
                    return response;
                }

                // Check if voucher has been used
                var hasUsedVouchers = await _context.UserVouchers.AnyAsync(uv => uv.VoucherId == voucherId && uv.IsUsed);
                if (hasUsedVouchers)
                {
                    response.IsSuccess = false;
                    response.Message = "Cannot delete voucher that has been used";
                    return response;
                }

                // Delete user vouchers first
                var userVouchers = await _context.UserVouchers.Where(uv => uv.VoucherId == voucherId).ToListAsync();
                _context.UserVouchers.RemoveRange(userVouchers);

                // Delete voucher
                await _voucherRepo.RemoveAsync(voucher);

                response.Result = true;
                response.Message = "Voucher deleted successfully";
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



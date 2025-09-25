using System;

namespace PalService.DTOs
{
    // Voucher and booking preview
    public class VoucherPreviewDto
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty; // Percent | Fixed
        public decimal DiscountValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsApplicable { get; set; }
    }

    // Voucher management
    public class CreateVoucherDto
    {
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "Percent"; // Percent | Fixed
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? UsageLimit { get; set; }
        public bool ToPassengers { get; set; } = true;
        public bool ToDrivers { get; set; } = true;
        public bool OnlyVip { get; set; } = false; // if true, send only to VIP
    }

    public class UserVoucherItemDto
    {
        public int UserVoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsUsed { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}









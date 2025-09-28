using System.ComponentModel.DataAnnotations;

namespace PalService.DTOs
{
    public class CreateVoucherDto
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = string.Empty; // "Percentage" or "Fixed"

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }

        public bool OnlyVip { get; set; } = false;
        public bool ToPassengers { get; set; } = true;
        public bool ToDrivers { get; set; } = true;

        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
    }

    public class UpdateVoucherDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? DiscountType { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Discount value must be greater than 0")]
        public decimal? DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderValue { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }
    }

    public class VoucherDto
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public int UsedCount { get; set; }
        public int AvailableCount { get; set; }
        public bool IsExpired { get; set; }
        public bool IsFullyUsed { get; set; }
    }

    public class VoucherListDto
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public int? UsageLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public int UsedCount { get; set; }
        public bool IsExpired { get; set; }
        public bool IsFullyUsed { get; set; }
    }

    public class VoucherPreviewDto
    {
        public int VoucherId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPrice { get; set; }
        public bool IsApplicable { get; set; }
        public string? Reason { get; set; }
    }
}
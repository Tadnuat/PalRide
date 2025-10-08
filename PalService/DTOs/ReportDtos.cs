using System.ComponentModel.DataAnnotations;

namespace PalService.DTOs
{
    public class CreateReportDto
    {
        [Required]
        public int ReportedUserId { get; set; }

        public int? TripId { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Details { get; set; }
    }

    public class UpdateReportDto
    {
        [StringLength(50)]
        public string? Status { get; set; } // "Pending", "Investigating", "Resolved", "Dismissed"

        [StringLength(1000)]
        public string? AdminNotes { get; set; }
    }

    public class ReportDto
    {
        public int ReportId { get; set; }
        public int ReporterId { get; set; }
        public int ReportedUserId { get; set; }
        public int? TripId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? AdminId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReportedUserName { get; set; } = string.Empty;
        public string? AdminName { get; set; }
        public string? TripInfo { get; set; }
        public string? AdminNotes { get; set; }
    }

    public class ReportListDto
    {
        public int ReportId { get; set; }
        public int ReporterId { get; set; }
        public int ReportedUserId { get; set; }
        public int? TripId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ReporterName { get; set; } = string.Empty;
        public string ReportedUserName { get; set; } = string.Empty;
        public string? TripInfo { get; set; }
    }

    public class ReportFilterDto
    {
        public string? Status { get; set; }
        public string? Reason { get; set; }
        public int? ReporterId { get; set; }
        public int? ReportedUserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ReportStatsDto
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int InvestigatingReports { get; set; }
        public int ResolvedReports { get; set; }
        public int DismissedReports { get; set; }
        public Dictionary<string, int> ReasonCounts { get; set; } = new Dictionary<string, int>();
        public List<ReportListDto> RecentReports { get; set; } = new List<ReportListDto>();
    }
}




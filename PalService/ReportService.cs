using Microsoft.EntityFrameworkCore;
using PalRepository.DBContexts;
using PalRepository.Models;
using PalRepository.PalRepository;
using PalRepository.UnitOfWork;
using PalService.DTOs;
using PalService.Interface;

namespace PalService
{
    public class ReportService : IReportService
    {
        private readonly PalRideContext _context;
        private readonly GenericRepository<Report> _reportRepo;

        public ReportService(PalRideContext context, GenericRepository<Report> reportRepo)
        {
            _context = context;
            _reportRepo = reportRepo;
        }

        public async Task<ResponseDto<ReportDto>> CreateReportAsync(int reporterId, CreateReportDto dto)
        {
            var response = new ResponseDto<ReportDto>();
            try
            {
                // Validate reported user exists
                var reportedUser = await _context.Users.FindAsync(dto.ReportedUserId);
                if (reportedUser == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Reported user not found";
                    return response;
                }

                // Validate trip if provided
                if (dto.TripId.HasValue)
                {
                    var tripExists = await _context.Trips.FindAsync(dto.TripId.Value);
                    if (tripExists == null)
                    {
                        response.IsSuccess = false;
                        response.Message = "Trip not found";
                        return response;
                    }
                }

                // Check if user already reported this person for the same trip
                var existingReport = await _context.Reports
                    .FirstOrDefaultAsync(r => r.ReporterId == reporterId && 
                                            r.ReportedUserId == dto.ReportedUserId && 
                                            r.TripId == dto.TripId &&
                                            r.Status == "Pending");

                if (existingReport != null)
                {
                    response.IsSuccess = false;
                    response.Message = "You have already reported this user for this trip";
                    return response;
                }

                var report = new Report
                {
                    ReporterId = reporterId,
                    ReportedUserId = dto.ReportedUserId,
                    TripId = dto.TripId,
                    Reason = dto.Reason.Trim(),
                    Details = dto.Details?.Trim(),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _reportRepo.CreateAsync(report);

                // Load related information
                var reporter = await _context.Users.FindAsync(reporterId);
                var trip = dto.TripId.HasValue ? await _context.Trips.FindAsync(dto.TripId.Value) : null;

                response.Result = new ReportDto
                {
                    ReportId = report.ReportId,
                    ReporterId = report.ReporterId,
                    ReportedUserId = report.ReportedUserId,
                    TripId = report.TripId,
                    Reason = report.Reason,
                    Details = report.Details,
                    Status = report.Status,
                    AdminId = report.AdminId,
                    CreatedAt = report.CreatedAt,
                    UpdatedAt = report.UpdatedAt,
                    ReporterName = reporter?.FullName ?? "Unknown",
                    ReportedUserName = reportedUser.FullName,
                    AdminName = null,
                    TripInfo = trip != null ? $"{trip.PickupLocation} → {trip.DropoffLocation}" : null,
                    AdminNotes = null
                };
                response.Message = "Report created successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReportDto>> UpdateReportAsync(int reportId, int adminId, UpdateReportDto dto)
        {
            var response = new ResponseDto<ReportDto>();
            try
            {
                var report = await _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Admin)
                    .Include(r => r.Trip)
                    .FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Report not found";
                    return response;
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(dto.Status))
                    report.Status = dto.Status;

                if (dto.AdminNotes != null)
                    report.Details = dto.AdminNotes.Trim();

                report.AdminId = adminId;
                report.UpdatedAt = DateTime.UtcNow;

                await _reportRepo.UpdateAsync(report);

                response.Result = new ReportDto
                {
                    ReportId = report.ReportId,
                    ReporterId = report.ReporterId,
                    ReportedUserId = report.ReportedUserId,
                    TripId = report.TripId,
                    Reason = report.Reason,
                    Details = report.Details,
                    Status = report.Status,
                    AdminId = report.AdminId,
                    CreatedAt = report.CreatedAt,
                    UpdatedAt = report.UpdatedAt,
                    ReporterName = report.Reporter?.FullName ?? "Unknown",
                    ReportedUserName = report.ReportedUser?.FullName ?? "Unknown",
                    AdminName = report.Admin?.FullName ?? "Unknown",
                    TripInfo = report.Trip != null ? $"{report.Trip.PickupLocation} → {report.Trip.DropoffLocation}" : null,
                    AdminNotes = dto.AdminNotes
                };
                response.Message = "Report updated successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<bool>> DeleteReportAsync(int reportId, int adminId)
        {
            var response = new ResponseDto<bool>();
            try
            {
                var report = await _context.Reports.FirstOrDefaultAsync(r => r.ReportId == reportId);
                if (report == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Report not found";
                    return response;
                }

                // Only allow deletion of pending reports
                if (report.Status != "Pending")
                {
                    response.IsSuccess = false;
                    response.Message = "Only pending reports can be deleted";
                    return response;
                }

                await _reportRepo.RemoveAsync(report);
                response.Result = true;
                response.Message = "Report deleted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReportDto>> GetReportByIdAsync(int reportId)
        {
            var response = new ResponseDto<ReportDto>();
            try
            {
                var report = await _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Admin)
                    .Include(r => r.Trip)
                    .FirstOrDefaultAsync(r => r.ReportId == reportId);

                if (report == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Report not found";
                    return response;
                }

                response.Result = new ReportDto
                {
                    ReportId = report.ReportId,
                    ReporterId = report.ReporterId,
                    ReportedUserId = report.ReportedUserId,
                    TripId = report.TripId,
                    Reason = report.Reason,
                    Details = report.Details,
                    Status = report.Status,
                    AdminId = report.AdminId,
                    CreatedAt = report.CreatedAt,
                    UpdatedAt = report.UpdatedAt,
                    ReporterName = report.Reporter?.FullName ?? "Unknown",
                    ReportedUserName = report.ReportedUser?.FullName ?? "Unknown",
                    AdminName = report.Admin?.FullName,
                    TripInfo = report.Trip != null ? $"{report.Trip.PickupLocation} → {report.Trip.DropoffLocation}" : null,
                    AdminNotes = report.Details
                };
                response.Message = "Report found";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<ReportDto>>> GetAllReportsAsync(ReportFilterDto filter)
        {
            var response = new ResponseDto<List<ReportDto>>();
            try
            {
                var query = _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Admin)
                    .Include(r => r.Trip)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    query = query.Where(r => r.Status == filter.Status);
                }

                if (!string.IsNullOrWhiteSpace(filter.Reason))
                {
                    query = query.Where(r => r.Reason.Contains(filter.Reason));
                }

                if (filter.ReporterId.HasValue)
                {
                    query = query.Where(r => r.ReporterId == filter.ReporterId.Value);
                }

                if (filter.ReportedUserId.HasValue)
                {
                    query = query.Where(r => r.ReportedUserId == filter.ReportedUserId.Value);
                }

                if (filter.FromDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);
                }

                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(r => new ReportDto
                    {
                        ReportId = r.ReportId,
                        ReporterId = r.ReporterId,
                        ReportedUserId = r.ReportedUserId,
                        TripId = r.TripId,
                        Reason = r.Reason,
                        Details = r.Details,
                        Status = r.Status,
                        AdminId = r.AdminId,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ReporterName = r.Reporter.FullName,
                        ReportedUserName = r.ReportedUser.FullName,
                        AdminName = r.Admin != null ? r.Admin.FullName : null,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null,
                        AdminNotes = r.Details
                    })
                    .ToListAsync();

                response.Result = reports;
                response.Message = $"Found {reports.Count} reports";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<ReportDto>>> GetUserReportsAsync(int userId)
        {
            var response = new ResponseDto<List<ReportDto>>();
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Admin)
                    .Include(r => r.Trip)
                    .Where(r => r.ReporterId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReportDto
                    {
                        ReportId = r.ReportId,
                        ReporterId = r.ReporterId,
                        ReportedUserId = r.ReportedUserId,
                        TripId = r.TripId,
                        Reason = r.Reason,
                        Details = r.Details,
                        Status = r.Status,
                        AdminId = r.AdminId,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ReporterName = r.Reporter.FullName,
                        ReportedUserName = r.ReportedUser.FullName,
                        AdminName = r.Admin != null ? r.Admin.FullName : null,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null,
                        AdminNotes = r.Details
                    })
                    .ToListAsync();

                response.Result = reports;
                response.Message = $"Found {reports.Count} reports";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<ReportStatsDto>> GetReportStatsAsync()
        {
            var response = new ResponseDto<ReportStatsDto>();
            try
            {
                var reports = await _context.Reports.ToListAsync();

                var totalReports = reports.Count;
                var pendingReports = reports.Count(r => r.Status == "Pending");
                var investigatingReports = reports.Count(r => r.Status == "Investigating");
                var resolvedReports = reports.Count(r => r.Status == "Resolved");
                var dismissedReports = reports.Count(r => r.Status == "Dismissed");

                var reasonCounts = reports.GroupBy(r => r.Reason)
                    .ToDictionary(g => g.Key, g => g.Count());

                var recentReports = await _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Trip)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new ReportListDto
                    {
                        ReportId = r.ReportId,
                        ReporterId = r.ReporterId,
                        ReportedUserId = r.ReportedUserId,
                        TripId = r.TripId,
                        Reason = r.Reason,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt,
                        ReporterName = r.Reporter.FullName,
                        ReportedUserName = r.ReportedUser.FullName,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null
                    })
                    .ToListAsync();

                response.Result = new ReportStatsDto
                {
                    TotalReports = totalReports,
                    PendingReports = pendingReports,
                    InvestigatingReports = investigatingReports,
                    ResolvedReports = resolvedReports,
                    DismissedReports = dismissedReports,
                    ReasonCounts = reasonCounts,
                    RecentReports = recentReports
                };
                response.Message = "Report stats retrieved successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<ResponseDto<List<ReportDto>>> GetReportsByStatusAsync(string status)
        {
            var response = new ResponseDto<List<ReportDto>>();
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.Reporter)
                    .Include(r => r.ReportedUser)
                    .Include(r => r.Admin)
                    .Include(r => r.Trip)
                    .Where(r => r.Status == status)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ReportDto
                    {
                        ReportId = r.ReportId,
                        ReporterId = r.ReporterId,
                        ReportedUserId = r.ReportedUserId,
                        TripId = r.TripId,
                        Reason = r.Reason,
                        Details = r.Details,
                        Status = r.Status,
                        AdminId = r.AdminId,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ReporterName = r.Reporter.FullName,
                        ReportedUserName = r.ReportedUser.FullName,
                        AdminName = r.Admin != null ? r.Admin.FullName : null,
                        TripInfo = r.Trip != null ? $"{r.Trip.PickupLocation} → {r.Trip.DropoffLocation}" : null,
                        AdminNotes = r.Details
                    })
                    .ToListAsync();

                response.Result = reports;
                response.Message = $"Found {reports.Count} {status.ToLower()} reports";
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

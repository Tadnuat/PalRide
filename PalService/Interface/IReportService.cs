using PalService.DTOs;

namespace PalService.Interface
{
    public interface IReportService
    {
        Task<ResponseDto<ReportDto>> CreateReportAsync(int reporterId, CreateReportDto dto);
        Task<ResponseDto<ReportDto>> UpdateReportAsync(int reportId, int adminId, UpdateReportDto dto);
        Task<ResponseDto<bool>> DeleteReportAsync(int reportId, int adminId);
        Task<ResponseDto<ReportDto>> GetReportByIdAsync(int reportId);
        Task<ResponseDto<List<ReportDto>>> GetAllReportsAsync(ReportFilterDto filter);
        Task<ResponseDto<List<ReportDto>>> GetUserReportsAsync(int userId);
        Task<ResponseDto<ReportStatsDto>> GetReportStatsAsync();
        Task<ResponseDto<List<ReportDto>>> GetReportsByStatusAsync(string status);
    }
}




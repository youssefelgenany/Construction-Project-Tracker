using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Reports;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IReportsService
{
    Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(ReportFilterQuery filter);

    Task<ProjectHealthDto> GetProjectHealthAsync(ReportFilterQuery filter);

    Task<IEnumerable<ProjectProgressPointDto>> GetProjectProgressTimelineAsync(ReportFilterQuery filter);

    Task<IEnumerable<EngineerPerformanceReportRowDto>> GetEngineerPerformanceReportAsync(ReportFilterQuery filter);

    Task<IEnumerable<WorkloadBarDto>> GetWorkloadBarsAsync(ReportFilterQuery filter);

    Task<TaskAnalyticsDto> GetTaskAnalyticsAsync(ReportFilterQuery filter);

    Task<IEnumerable<ReportActivityDto>> GetActivityAsync(ReportFilterQuery filter);

    Task<IEnumerable<AttentionProjectDto>> GetAttentionProjectsAsync(ReportFilterQuery filter);

    // Legacy endpoints retained for compatibility during migration
    Task<ReportsSummaryDto> GetSummaryAsync(ReportFilterQuery filter);

    Task<ProjectStatusDistributionDto> GetProjectStatusDistributionAsync(ReportFilterQuery filter);

    Task<TasksByPriorityDto> GetTasksByPriorityAsync(ReportFilterQuery filter);

    Task<TasksByStatusDto> GetTasksByStatusAsync(ReportFilterQuery filter);

    Task<IEnumerable<ProjectProgressChartDto>> GetProjectProgressAsync(ReportFilterQuery filter);

    Task<IEnumerable<MonthlyProjectsDto>> GetMonthlyProjectsAsync(ReportFilterQuery filter);

    Task<IEnumerable<EngineerWorkloadDto>> GetEngineerWorkloadAsync(ReportFilterQuery filter);

    Task<PagedResult<ReportProjectRowDto>> GetProjectsTableAsync(
        ReportFilterQuery filter,
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize);

    Task<FileStreamResult> ExportExcelAsync(ReportFilterQuery filter, string? search);

    Task<FileStreamResult> ExportPdfAsync(ReportFilterQuery filter, string? search);
}

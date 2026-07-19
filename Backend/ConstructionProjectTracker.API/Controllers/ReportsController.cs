using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Predictions;
using ConstructionProjectTracker.API.DTOs.Reports;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;
    private readonly IProjectPredictionService _predictionService;

    public ReportsController(IReportsService reportsService, IProjectPredictionService predictionService)
    {
        _reportsService = reportsService;
        _predictionService = predictionService;
    }

    /// <summary>
    /// Returns delay predictions for every active (In Progress) project.
    /// </summary>
    [HttpGet("project-delay-predictions")]
    [ProducesResponseType(typeof(IEnumerable<ProjectDelayPredictionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDelayPredictionDto>>> GetProjectDelayPredictions()
        => Ok(await _predictionService.GetActiveProjectPredictionsAsync());

    [HttpGet("executive-summary")]
    [ProducesResponseType(typeof(ExecutiveSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExecutiveSummaryDto>> GetExecutiveSummary([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetExecutiveSummaryAsync(filter));

    [HttpGet("project-health")]
    [ProducesResponseType(typeof(ProjectHealthDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectHealthDto>> GetProjectHealth([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetProjectHealthAsync(filter));

    [HttpGet("project-progress")]
    [ProducesResponseType(typeof(IEnumerable<ProjectProgressPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectProgressPointDto>>> GetProjectProgressTimeline(
        [FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetProjectProgressTimelineAsync(filter));

    [HttpGet("engineer-performance")]
    [ProducesResponseType(typeof(IEnumerable<EngineerPerformanceReportRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EngineerPerformanceReportRowDto>>> GetEngineerPerformance(
        [FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetEngineerPerformanceReportAsync(filter));

    [HttpGet("workload")]
    [ProducesResponseType(typeof(IEnumerable<WorkloadBarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<WorkloadBarDto>>> GetWorkload([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetWorkloadBarsAsync(filter));

    [HttpGet("task-analytics")]
    [ProducesResponseType(typeof(TaskAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskAnalyticsDto>> GetTaskAnalytics([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetTaskAnalyticsAsync(filter));

    [HttpGet("activity")]
    [ProducesResponseType(typeof(IEnumerable<ReportActivityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ReportActivityDto>>> GetActivity([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetActivityAsync(filter));

    [HttpGet("attention")]
    [ProducesResponseType(typeof(IEnumerable<AttentionProjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AttentionProjectDto>>> GetAttention([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetAttentionProjectsAsync(filter));

    [HttpGet("export/excel")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportExcel([FromQuery] ReportFilterQuery filter, [FromQuery] string? search)
        => await _reportsService.ExportExcelAsync(filter, search);

    [HttpGet("export/pdf")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportPdf([FromQuery] ReportFilterQuery filter, [FromQuery] string? search)
        => await _reportsService.ExportPdfAsync(filter, search);

    // Legacy endpoints
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ReportsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportsSummaryDto>> GetSummary([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetSummaryAsync(filter));

    [HttpGet("project-status")]
    [ProducesResponseType(typeof(ProjectStatusDistributionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectStatusDistributionDto>> GetProjectStatus([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetProjectStatusDistributionAsync(filter));

    [HttpGet("tasks-by-priority")]
    [ProducesResponseType(typeof(TasksByPriorityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TasksByPriorityDto>> GetTasksByPriority([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetTasksByPriorityAsync(filter));

    [HttpGet("tasks-by-status")]
    [ProducesResponseType(typeof(TasksByStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TasksByStatusDto>> GetTasksByStatus([FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetTasksByStatusAsync(filter));

    [HttpGet("project-progress-bars")]
    [ProducesResponseType(typeof(IEnumerable<ProjectProgressChartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectProgressChartDto>>> GetProjectProgressBars(
        [FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetProjectProgressAsync(filter));

    [HttpGet("monthly-projects")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyProjectsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MonthlyProjectsDto>>> GetMonthlyProjects(
        [FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetMonthlyProjectsAsync(filter));

    [HttpGet("engineer-workload")]
    [ProducesResponseType(typeof(IEnumerable<EngineerWorkloadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EngineerWorkloadDto>>> GetEngineerWorkload(
        [FromQuery] ReportFilterQuery filter)
        => Ok(await _reportsService.GetEngineerWorkloadAsync(filter));

    [HttpGet("projects")]
    [ProducesResponseType(typeof(PagedResult<ReportProjectRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReportProjectRowDto>>> GetProjectsTable(
        [FromQuery] ReportFilterQuery filter,
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
        => Ok(await _reportsService.GetProjectsTableAsync(filter, search, sortBy, descending, pageNumber, pageSize));
}

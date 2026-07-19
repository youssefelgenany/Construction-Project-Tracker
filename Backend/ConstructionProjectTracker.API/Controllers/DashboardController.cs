using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Dashboard;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Risks;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IRiskAnalysisService _riskAnalysisService;
    private readonly ITaskScheduleService _taskScheduleService;

    public DashboardController(
        IDashboardService dashboardService,
        IRiskAnalysisService riskAnalysisService,
        ITaskScheduleService taskScheduleService)
    {
        _dashboardService = dashboardService;
        _riskAnalysisService = riskAnalysisService;
        _taskScheduleService = taskScheduleService;
    }

    /// <summary>
    /// Returns high-level dashboard summary statistics.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var summary = await _dashboardService.GetSummaryAsync(userId, isAdmin);
        return Ok(summary);
    }

    /// <summary>
    /// Returns project progress data for charts.
    /// </summary>
    [HttpGet("project-progress")]
    [ProducesResponseType(typeof(IEnumerable<ProjectProgressChartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectProgressChartDto>>> GetProjectProgress()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var data = await _dashboardService.GetProjectProgressAsync(userId, isAdmin);
        return Ok(data);
    }

    /// <summary>
    /// Returns engineer workload. Admins see all engineers; engineers see only their own.
    /// </summary>
    [HttpGet("engineer-workload")]
    [ProducesResponseType(typeof(IEnumerable<EngineerWorkloadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EngineerWorkloadDto>>> GetEngineerWorkload()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var data = await _dashboardService.GetEngineerWorkloadAsync(userId, isAdmin);
        return Ok(data);
    }

    /// <summary>
    /// Returns the highest-performing engineers for the dashboard widget.
    /// </summary>
    [HttpGet("top-performing-engineers")]
    [ProducesResponseType(typeof(IEnumerable<EngineerPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EngineerPerformanceDto>>> GetTopPerformingEngineers()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var data = await _dashboardService.GetTopPerformingEngineersAsync(userId, isAdmin);
        return Ok(data);
    }

    /// <summary>
    /// Returns risk counts and at-risk projects for the dashboard.
    /// </summary>
    [HttpGet("risk-summary")]
    [ProducesResponseType(typeof(DashboardRiskSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardRiskSummaryDto>> GetRiskSummary()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var data = await _riskAnalysisService.GetDashboardRiskSummaryAsync(userId, isAdmin);
        return Ok(data);
    }

    /// <summary>
    /// Returns blocked tasks, critical tasks, and projects behind schedule counts.
    /// </summary>
    [HttpGet("schedule-summary")]
    [ProducesResponseType(typeof(ScheduleSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduleSummaryDto>> GetScheduleSummary()
    {
        var (userId, isAdmin) = GetCurrentUserContext();
        var data = await _taskScheduleService.GetScheduleSummaryAsync(userId, isAdmin);
        return Ok(data);
    }

    /// <summary>
    /// Returns project status distribution for pie charts. Admin only.
    /// </summary>
    [HttpGet("project-status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProjectStatusDistributionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProjectStatusDistributionDto>> GetProjectStatusDistribution()
    {
        var data = await _dashboardService.GetProjectStatusDistributionAsync();
        return Ok(data);
    }

    /// <summary>
    /// Returns monthly project creation counts for the current year. Admin only.
    /// </summary>
    [HttpGet("monthly-projects")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<MonthlyProjectsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<MonthlyProjectsDto>>> GetMonthlyProjects()
    {
        var data = await _dashboardService.GetMonthlyProjectsAsync();
        return Ok(data);
    }

    /// <summary>
    /// Returns the 10 most recent activities across the system. Admin only.
    /// </summary>
    [HttpGet("recent-activities")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<RecentActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<RecentActivityDto>>> GetRecentActivities()
    {
        var data = await _dashboardService.GetRecentActivitiesAsync();
        return Ok(data);
    }

    private (int UserId, bool IsAdmin) GetCurrentUserContext()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");
        return (userId, isAdmin);
    }
}

using ConstructionProjectTracker.API.DTOs.ProjectAssignments;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/project-assignments")]
[Authorize]
public class ProjectAssignmentsController : ControllerBase
{
    private readonly IProjectAssignmentService _assignmentService;

    public ProjectAssignmentsController(IProjectAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Assigns an engineer to a project. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProjectAssignmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectAssignmentResponseDto>> AssignEngineer([FromBody] AssignEngineerDto dto)
    {
        try
        {
            var assignment = await _assignmentService.AssignEngineerAsync(dto);
            return CreatedAtAction(nameof(GetProjectEngineers), new { projectId = dto.ProjectId }, assignment);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Removes a project assignment. Admin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var removed = await _assignmentService.RemoveAssignmentAsync(id);
        if (!removed)
            return NotFound(new { message = $"Assignment with id {id} was not found." });

        return NoContent();
    }

    /// <summary>
    /// Returns all engineers assigned to a project.
    /// </summary>
    [HttpGet("project/{projectId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectEngineerAssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectEngineerAssignmentDto>>> GetProjectEngineers(int projectId)
    {
        var engineers = await _assignmentService.GetProjectEngineersAsync(projectId);
        return Ok(engineers);
    }

    /// <summary>
    /// Returns all projects assigned to an engineer.
    /// </summary>
    [HttpGet("engineer/{engineerId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetEngineerProjects(int engineerId)
    {
        var projects = await _assignmentService.GetEngineerProjectsAsync(engineerId);
        return Ok(projects);
    }

    /// <summary>
    /// Checks whether an engineer is assigned to a project.
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> CheckAssignment(
        [FromQuery] int projectId,
        [FromQuery] int engineerId)
    {
        var isAssigned = await _assignmentService.IsEngineerAssignedToProjectAsync(projectId, engineerId);
        return Ok(isAssigned);
    }
}

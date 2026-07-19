using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class EngineersController : ControllerBase
{
    private readonly IEngineerService _engineerService;

    public EngineersController(IEngineerService engineerService)
    {
        _engineerService = engineerService;
    }

    /// <summary>
    /// Returns a paginated list of engineers with optional search and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EngineerResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EngineerResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _engineerService.GetAllAsync(search, sortBy, descending, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Returns engineer workload metrics sorted by capacity (lowest workload first).
    /// </summary>
    [HttpGet("workload")]
    [ProducesResponseType(typeof(PagedResult<EngineerWorkloadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EngineerWorkloadDto>>> GetWorkload(
        [FromQuery] string? search,
        [FromQuery] string? workloadLevel,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _engineerService.GetWorkloadAsync(search, workloadLevel, null, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Returns engineer performance metrics derived from historical task data.
    /// </summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PagedResult<EngineerPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EngineerPerformanceDto>>> GetPerformance(
        [FromQuery] string? search,
        [FromQuery] string? sortBy,
        [FromQuery] bool descending = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        var result = await _engineerService.GetPerformanceAsync(search, sortBy, descending, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Returns detailed engineer information including assignment counts.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EngineerDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngineerDetailsDto>> GetById(int id)
    {
        var engineer = await _engineerService.GetByIdAsync(id);
        if (engineer is null)
            return NotFound(new { message = $"Engineer with id {id} was not found." });

        return Ok(engineer);
    }

    /// <summary>
    /// Returns detailed engineer performance history.
    /// </summary>
    [HttpGet("{id:int}/performance")]
    [ProducesResponseType(typeof(EngineerPerformanceDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngineerPerformanceDetailsDto>> GetPerformanceById(int id)
    {
        var performance = await _engineerService.GetPerformanceByIdAsync(id);
        if (performance is null)
            return NotFound(new { message = $"Engineer with id {id} was not found." });

        return Ok(performance);
    }

    /// <summary>
    /// Creates a new engineer with an associated user account.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EngineerResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EngineerResponseDto>> Create([FromBody] CreateEngineerDto dto)
    {
        try
        {
            var engineer = await _engineerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = engineer.Id }, engineer);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing engineer.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EngineerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EngineerResponseDto>> Update(int id, [FromBody] UpdateEngineerDto dto)
    {
        var engineer = await _engineerService.UpdateAsync(id, dto);
        if (engineer is null)
            return NotFound(new { message = $"Engineer with id {id} was not found." });

        return Ok(engineer);
    }

    /// <summary>
    /// Deletes an engineer when they have no project or task assignments.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _engineerService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = $"Engineer with id {id} was not found." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

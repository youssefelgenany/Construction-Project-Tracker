using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.ProjectAssignments;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class ProjectAssignmentService : IProjectAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectAssignmentService> _logger;

    public ProjectAssignmentService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<ProjectAssignmentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProjectAssignmentResponseDto> AssignEngineerAsync(AssignEngineerDto dto)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
        if (!projectExists)
            throw new InvalidOperationException("Project does not exist.");

        var engineerExists = await _context.Engineers.AnyAsync(e => e.Id == dto.EngineerId);
        if (!engineerExists)
            throw new InvalidOperationException("Engineer does not exist.");

        var isDuplicate = await _context.ProjectAssignments.AnyAsync(pa =>
            pa.ProjectId == dto.ProjectId && pa.EngineerId == dto.EngineerId);

        if (isDuplicate)
            throw new InvalidOperationException("This engineer is already assigned to the project.");

        var assignment = new ProjectAssignment
        {
            ProjectId = dto.ProjectId,
            EngineerId = dto.EngineerId,
            AssignedDate = DateTime.UtcNow
        };

        _context.ProjectAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Engineer assigned: ProjectId={ProjectId}, EngineerId={EngineerId}",
            dto.ProjectId,
            dto.EngineerId);

        return await MapToResponseDtoAsync(assignment.Id);
    }

    public async Task<bool> RemoveAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.ProjectAssignments.FindAsync(assignmentId);
        if (assignment is null)
            return false;

        var projectId = assignment.ProjectId;
        var engineerId = assignment.EngineerId;

        _context.ProjectAssignments.Remove(assignment);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Assignment removed: ProjectId={ProjectId}, EngineerId={EngineerId}",
            projectId,
            engineerId);

        return true;
    }

    public async Task<IEnumerable<ProjectEngineerAssignmentDto>> GetProjectEngineersAsync(int projectId)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
            return Array.Empty<ProjectEngineerAssignmentDto>();

        var assignments = await _context.ProjectAssignments
            .AsNoTracking()
            .Include(pa => pa.Engineer)
                .ThenInclude(e => e.User)
            .Where(pa => pa.ProjectId == projectId)
            .OrderByDescending(pa => pa.AssignedDate)
            .ToListAsync();

        return assignments.Select(pa => new ProjectEngineerAssignmentDto
        {
            Id = pa.Id,
            EngineerId = pa.EngineerId,
            FullName = pa.Engineer.User.FullName,
            Email = pa.Engineer.User.Email,
            Position = pa.Engineer.Position,
            AssignedDate = pa.AssignedDate
        });
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetEngineerProjectsAsync(int engineerId)
    {
        var engineerExists = await _context.Engineers.AnyAsync(e => e.Id == engineerId);
        if (!engineerExists)
            return Array.Empty<ProjectResponseDto>();

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectAssignments.Any(pa => pa.EngineerId == engineerId))
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProjectResponseDto>>(projects);
    }

    public async Task<bool> IsEngineerAssignedToProjectAsync(int projectId, int engineerId) =>
        await _context.ProjectAssignments.AnyAsync(pa =>
            pa.ProjectId == projectId && pa.EngineerId == engineerId);

    private async Task<ProjectAssignmentResponseDto> MapToResponseDtoAsync(int assignmentId)
    {
        var assignment = await _context.ProjectAssignments
            .AsNoTracking()
            .Include(pa => pa.Project)
            .Include(pa => pa.Engineer)
                .ThenInclude(e => e.User)
            .FirstAsync(pa => pa.Id == assignmentId);

        return _mapper.Map<ProjectAssignmentResponseDto>(assignment);
    }
}

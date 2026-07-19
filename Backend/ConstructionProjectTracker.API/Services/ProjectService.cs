using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Common;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Enums;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class ProjectService : IProjectService
{
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;

    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<ProjectService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResult<ProjectResponseDto>> GetAllAsync(
        string? search,
        string? sortBy,
        bool descending,
        int pageNumber,
        int pageSize)
    {
        pageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
        pageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        var query = _context.Projects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        query = ApplySorting(query, sortBy, descending);

        var totalCount = await query.CountAsync();

        var projects = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProjectResponseDto>
        {
            Items = _mapper.Map<List<ProjectResponseDto>>(projects),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<ProjectDetailsDto?> GetByIdAsync(int id)
    {
        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
            return null;

        var details = _mapper.Map<ProjectDetailsDto>(project);

        details.AssignedEngineersCount = await _context.ProjectAssignments
            .CountAsync(pa => pa.ProjectId == id);
        details.TasksCount = await _context.Tasks
            .CountAsync(t => t.ProjectId == id);
        details.DocumentsCount = await _context.Documents
            .CountAsync(d => d.ProjectId == id);

        return details;
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectDto dto)
    {
        var project = _mapper.Map<Project>(dto);
        project.Status = ProjectStatus.NotStarted;
        project.ProgressPercentage = 0;

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project created: {ProjectId} - {ProjectName}", project.Id, project.Name);

        return _mapper.Map<ProjectResponseDto>(project);
    }

    public async Task<ProjectResponseDto?> UpdateAsync(int id, UpdateProjectDto dto)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null)
            return null;

        _mapper.Map(dto, project);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project updated: {ProjectId} - {ProjectName}", project.Id, project.Name);

        return _mapper.Map<ProjectResponseDto>(project);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null)
            return false;

        var projectId = project.Id;
        var projectName = project.Name;

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Project deleted: {ProjectId} - {ProjectName}", projectId, projectName);

        return true;
    }

    private static IQueryable<Project> ApplySorting(IQueryable<Project> query, string? sortBy, bool descending)
    {
        return sortBy?.ToLowerInvariant() switch
        {
            "budget" => descending
                ? query.OrderByDescending(p => p.Budget)
                : query.OrderBy(p => p.Budget),
            "startdate" => descending
                ? query.OrderByDescending(p => p.StartDate)
                : query.OrderBy(p => p.StartDate),
            "enddate" => descending
                ? query.OrderByDescending(p => p.EndDate)
                : query.OrderBy(p => p.EndDate),
            _ => descending
                ? query.OrderByDescending(p => p.Name)
                : query.OrderBy(p => p.Name)
        };
    }
}

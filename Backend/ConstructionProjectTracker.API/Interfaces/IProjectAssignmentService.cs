using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.ProjectAssignments;
using ConstructionProjectTracker.API.DTOs.Projects;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IProjectAssignmentService
{
    Task<ProjectAssignmentResponseDto> AssignEngineerAsync(AssignEngineerDto dto);

    Task<bool> RemoveAssignmentAsync(int assignmentId);

    Task<IEnumerable<ProjectEngineerAssignmentDto>> GetProjectEngineersAsync(int projectId);

    Task<IEnumerable<ProjectResponseDto>> GetEngineerProjectsAsync(int engineerId);

    Task<bool> IsEngineerAssignedToProjectAsync(int projectId, int engineerId);
}

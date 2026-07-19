using AutoMapper;
using ConstructionProjectTracker.API.DTOs.Auth;
using ConstructionProjectTracker.API.DTOs.Documents;
using ConstructionProjectTracker.API.DTOs.Engineers;
using ConstructionProjectTracker.API.DTOs.Projects;
using ConstructionProjectTracker.API.DTOs.Tasks;
using ConstructionProjectTracker.API.DTOs.ProjectAssignments;
using ConstructionProjectTracker.API.Entities;

namespace ConstructionProjectTracker.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        CreateMap<Project, ProjectResponseDto>();
        CreateMap<Project, ProjectDetailsDto>()
            .ForMember(dest => dest.AssignedEngineersCount, opt => opt.Ignore())
            .ForMember(dest => dest.TasksCount, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentsCount, opt => opt.Ignore());

        CreateMap<CreateProjectDto, Project>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ProgressPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore());

        CreateMap<UpdateProjectDto, Project>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ProgressPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.Tasks, opt => opt.Ignore())
            .ForMember(dest => dest.Documents, opt => opt.Ignore());

        CreateMap<Engineer, EngineerResponseDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive));

        CreateMap<Engineer, EngineerDetailsDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.User.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.User.IsActive))
            .ForMember(dest => dest.AssignedProjectsCount, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTasksCount, opt => opt.Ignore());

        CreateMap<CreateEngineerDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Engineer, opt => opt.Ignore())
            .ForMember(dest => dest.UploadedDocuments, opt => opt.Ignore());

        CreateMap<CreateEngineerDto, Engineer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTasks, opt => opt.Ignore());

        CreateMap<UpdateEngineerDto, Engineer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectAssignments, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedTasks, opt => opt.Ignore());

        CreateMap<ProjectAssignment, ProjectAssignmentResponseDto>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name))
            .ForMember(dest => dest.EngineerName, opt => opt.MapFrom(src => src.Engineer.User.FullName));

        CreateMap<AssignEngineerDto, ProjectAssignment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedDate, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Engineer, opt => opt.Ignore());

        CreateMap<TaskItem, TaskResponseDto>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name))
            .ForMember(dest => dest.EngineerName, opt => opt.MapFrom(src =>
                src.AssignedEngineer != null ? src.AssignedEngineer.User.FullName : null))
            .ForMember(dest => dest.CompletionReport, opt => opt.Ignore());

        CreateMap<TaskItem, TaskDetailsDto>()
            .ForMember(dest => dest.AssignedEngineer, opt => opt.MapFrom(src => src.AssignedEngineer))
            .ForMember(dest => dest.Project, opt => opt.MapFrom(src => src.Project))
            .ForMember(dest => dest.CompletionReport, opt => opt.Ignore());

        CreateMap<TaskCompletionReport, TaskCompletionReportDto>()
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedByUser.FullName));

        CreateMap<CreateTaskDto, TaskItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CompletionPercentage, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedEngineer, opt => opt.Ignore())
            .ForMember(dest => dest.CompletionReport, opt => opt.Ignore());

        CreateMap<UpdateTaskDto, TaskItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ProjectId, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedEngineerId, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedEngineer, opt => opt.Ignore())
            .ForMember(dest => dest.CompletionReport, opt => opt.Ignore());

        CreateMap<Document, DocumentResponseDto>()
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedByUser.FullName))
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore());

        CreateMap<Document, DocumentDetailsDto>()
            .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project.Name))
            .ForMember(dest => dest.UploadedBy, opt => opt.MapFrom(src => src.UploadedByUser.FullName))
            .ForMember(dest => dest.DownloadUrl, opt => opt.Ignore());
    }
}

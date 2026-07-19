namespace ConstructionProjectTracker.API.DTOs.Engineers;

public class EngineerDetailsDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; }
    public int AssignedProjectsCount { get; set; }
    public int AssignedTasksCount { get; set; }
}

using ConstructionProjectTracker.API.Enums;

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class CriticalPathTaskDto
{
    public int Order { get; set; }
    public int TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DurationDays { get; set; }
    public int EarlyStartDay { get; set; }
    public int EarlyFinishDay { get; set; }
    public int LateStartDay { get; set; }
    public int LateFinishDay { get; set; }
    public int SlackDays { get; set; }
    public bool IsCritical { get; set; }
    public ConstructionProjectTracker.API.Enums.TaskStatus Status { get; set; }
    public int CompletionPercentage { get; set; }
}

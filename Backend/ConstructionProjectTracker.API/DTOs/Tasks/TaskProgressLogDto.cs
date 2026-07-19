namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class TaskProgressLogDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int EngineerId { get; set; }
    public string EngineerName { get; set; } = string.Empty;
    public int PreviousProgress { get; set; }
    public int NewProgress { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

namespace ConstructionProjectTracker.API.DTOs.Tasks;

public class CreateTaskProgressLogDto
{
    public int NewProgress { get; set; }
    public string Description { get; set; } = string.Empty;
}

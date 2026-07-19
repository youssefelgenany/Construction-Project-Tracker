namespace ConstructionProjectTracker.API.DTOs.Dashboard;

public class ScheduleSummaryDto
{
    public int BlockedTasksCount { get; set; }
    public int CriticalTasksCount { get; set; }
    public int ProjectsBehindScheduleCount { get; set; }
}

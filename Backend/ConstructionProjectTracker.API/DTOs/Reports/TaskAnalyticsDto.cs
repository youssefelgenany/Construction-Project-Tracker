namespace ConstructionProjectTracker.API.DTOs.Reports;

public class TaskAnalyticsDto
{
    public TaskPriorityBreakdownDto ByPriority { get; set; } = new();
    public TaskStatusBreakdownDto ByStatus { get; set; } = new();
    public OverdueVsCompletedDto OverdueVsCompleted { get; set; } = new();
    public List<MonthlyCountPointDto> CompletionTrend { get; set; } = [];
}

public class TaskPriorityBreakdownDto
{
    public int Low { get; set; }
    public int Medium { get; set; }
    public int High { get; set; }
    public int Critical { get; set; }
}

public class TaskStatusBreakdownDto
{
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int PendingReview { get; set; }
    public int Completed { get; set; }
    public int Blocked { get; set; }
    public int Ready { get; set; }
}

public class OverdueVsCompletedDto
{
    public int Overdue { get; set; }
    public int Completed { get; set; }
}

public class MonthlyCountPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int Count { get; set; }
}

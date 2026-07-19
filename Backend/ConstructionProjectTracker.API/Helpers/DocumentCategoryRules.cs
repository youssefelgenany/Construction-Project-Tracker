namespace ConstructionProjectTracker.API.Helpers;

public static class DocumentCategoryRules
{
    public static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contract",
        "Drawing",
        "Report",
        "Invoice",
        "Permit",
        "Inspection",
        "Photo",
        "Other"
    };

    public static bool IsAllowedCategory(string category) =>
        AllowedCategories.Contains(category);
}

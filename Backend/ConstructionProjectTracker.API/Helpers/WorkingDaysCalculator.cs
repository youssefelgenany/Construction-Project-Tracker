namespace ConstructionProjectTracker.API.Helpers;

/// <summary>
/// Company calendar: working days are Sunday–Thursday; Friday and Saturday are non-working.
/// All schedule math for predictions and future recovery/risk features must reuse these helpers.
/// </summary>
public static class WorkingDaysCalculator
{
    public static bool IsWorkingDay(DateTime date)
    {
        var day = date.DayOfWeek;
        return day is not DayOfWeek.Friday and not DayOfWeek.Saturday;
    }

    /// <summary>
    /// Working days in the half-open interval [fromDate, toDate).
    /// </summary>
    public static int CountWorkingDays(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        if (to <= from)
        {
            return 0;
        }

        var count = 0;
        for (var day = from; day < to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Working days in the closed interval [fromDate, toDate] (planned project duration).
    /// </summary>
    public static int CountTotalWorkingDays(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;

        if (to < from)
        {
            return 0;
        }

        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Working days remaining after <paramref name="fromDate"/> through <paramref name="toDate"/> inclusive.
    /// Interval is (fromDate, toDate].
    /// </summary>
    public static int CountRemainingWorkingDays(DateTime fromDate, DateTime toDate)
        => CountWorkingDays(fromDate.Date.AddDays(1), toDate.Date.AddDays(1));

    /// <summary>
    /// Working days strictly between two dates up to and including <paramref name="toDate"/>.
    /// Interval is (fromDate, toDate] — used for actual delay after the planned end date.
    /// </summary>
    public static int CountWorkingDaysBetween(DateTime fromDate, DateTime toDate)
        => CountRemainingWorkingDays(fromDate, toDate);

    public static DateTime AddWorkingDays(DateTime fromDate, int workingDaysToAdd)
    {
        if (workingDaysToAdd <= 0)
        {
            return fromDate.Date;
        }

        var date = fromDate.Date;
        var added = 0;

        while (added < workingDaysToAdd)
        {
            date = date.AddDays(1);
            if (IsWorkingDay(date))
            {
                added++;
            }
        }

        return date;
    }

    public static DateTime AddWorkingDays(DateTime fromDate, double workingDaysToAdd)
    {
        if (workingDaysToAdd <= 0)
        {
            return fromDate.Date;
        }

        var wholeDays = (int)Math.Round(workingDaysToAdd, MidpointRounding.AwayFromZero);
        if (wholeDays <= 0)
        {
            return fromDate.Date;
        }

        return AddWorkingDays(fromDate, wholeDays);
    }

    /// <summary>
    /// Working days in (fromDate, toDate] — how many working days to add to <paramref name="fromDate"/>
    /// to land on or past <paramref name="toDate"/>.
    /// </summary>
    public static int GetWorkingDaysShift(DateTime fromDate, DateTime toDate)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        if (to <= from)
        {
            return 0;
        }

        var shift = 0;
        for (var day = from.AddDays(1); day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                shift++;
            }
        }

        return shift;
    }
}

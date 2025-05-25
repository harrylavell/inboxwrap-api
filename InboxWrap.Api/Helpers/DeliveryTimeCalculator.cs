using InboxWrap.Models;

namespace InboxWrap.Helpers;

public static class DeliveryTimeCalculator
{
    public static DateTime? CalculateNextDeliveryUtc(UserPreferences preferences)
    {
        if (preferences.DeliveryTimes == null || preferences.DeliveryTimes.Count == 0)
        {
            return null;
        }

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZoneId);
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        // Find next available delivery time (today or tomorrow).
        TimeOnly? nextTime = preferences.DeliveryTimes
            .Select(t => TimeOnly.Parse(t))
            .OrderBy(t => t)
            .Where(t => t > TimeOnly.FromDateTime(now))
            .Cast<TimeOnly?>()
            .FirstOrDefault();

        DateTime date = now.Date;
        if (nextTime == null) // No time left today, use first time tomorrow.
        {
            date = date.AddDays(1);
            nextTime = TimeOnly.Parse(preferences.DeliveryTimes.OrderBy(t => t).First());
        }

        var localDateTime = date + nextTime.Value.ToTimeSpan();
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, tz);
    }
}

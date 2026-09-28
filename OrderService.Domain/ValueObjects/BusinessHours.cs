namespace OrderService.Domain.ValueObjects;

public sealed record BusinessHours(TimeOnly OpenAt, TimeOnly CloseAt)
{
    public bool IsWithinBusinessHours(DateTime timestampUtc, TimeZoneInfo timeZone)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(timestampUtc, timeZone);
        var timeOfDay = TimeOnly.FromDateTime(localTime);
        return OpenAt <= timeOfDay && timeOfDay < CloseAt;
    }
}

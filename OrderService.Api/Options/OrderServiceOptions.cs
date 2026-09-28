using OrderService.Domain.Services;
using OrderService.Domain.ValueObjects;

namespace OrderService.Api.Options;

public sealed class OrderServiceOptions
{
    public const string SectionName = "OrderService";

    public decimal MinimumOrderTotal { get; set; } = 10m;
    public int MaximumItemQuantity { get; set; } = 100;
    public int MaxSubmissionsPerMinute { get; set; } = 1;
    public int MaxSubmissionsPerDay { get; set; } = 100;
    public int DraftExpirationHours { get; set; } = 24;
    public TimeOnly BusinessHoursOpenAt { get; set; } = new(8, 0);
    public TimeOnly BusinessHoursCloseAt { get; set; } = new(20, 0);
    public string TimeZoneId { get; set; } = "UTC";

    public OrderSubmissionPolicyOptions ToDomainOptions()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return new OrderSubmissionPolicyOptions(
            MinimumOrderTotal,
            MaximumItemQuantity,
            MaxSubmissionsPerMinute,
            MaxSubmissionsPerDay,
            TimeSpan.FromHours(DraftExpirationHours),
            new BusinessHours(BusinessHoursOpenAt, BusinessHoursCloseAt),
            timeZone);
    }
}

using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Services;

public sealed record OrderSubmissionPolicyOptions(
    decimal MinimumOrderTotal,
    int MaximumItemQuantity,
    int MaxSubmissionsPerMinute,
    int MaxSubmissionsPerDay,
    TimeSpan DraftExpiration,
    BusinessHours BusinessHours,
    TimeZoneInfo TimeZone);

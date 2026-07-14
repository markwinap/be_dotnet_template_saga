namespace OrderService.Domain.ValueObjects;

public sealed record Address(string Line1, string City, string State, string PostalCode, string Country);

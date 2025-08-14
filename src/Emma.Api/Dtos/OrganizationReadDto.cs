using System;

namespace Emma.Api.Dtos;

public sealed class OrganizationReadDto
{
    public Guid Id { get; init; }
    public Guid OrgGuid { get; init; }
    public string Name { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? PlanId { get; init; }
    public string? PlanType { get; init; }
    public int? SeatCount { get; init; }
    public bool IsActive { get; init; }
}

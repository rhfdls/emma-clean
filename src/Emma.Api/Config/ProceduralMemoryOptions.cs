namespace Emma.Api.Config;

public sealed record ProceduralMemoryOptions
{
    public bool Enabled { get; init; }
    public string Ring { get; init; } = "pilot"; // pilot|ga
    public string[] OrgAllowlist { get; init; } = System.Array.Empty<string>();
    // SPRINT2: Phase0
    public bool UseIndustry { get; init; } = false;
}

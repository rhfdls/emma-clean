namespace Emma.Infrastructure.Config;

public sealed record CosmosOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public string DatabaseId { get; init; } = string.Empty;
    public ContainersOptions Containers { get; init; } = new();

    public sealed record ContainersOptions
    {
        public string Procedures { get; init; } = "procedures";
        public string ProcedureTraces { get; init; } = "procedure-traces";
        public string ProcedureVersions { get; init; } = "procedure-versions";
        public string ProcedureInsights { get; init; } = "procedure-insights";
    }
}

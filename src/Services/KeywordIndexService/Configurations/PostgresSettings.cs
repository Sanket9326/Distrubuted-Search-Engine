public sealed class PostgresSettings
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; init; } = string.Empty;
}

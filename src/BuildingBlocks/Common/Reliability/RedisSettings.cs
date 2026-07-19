namespace Common.Reliability;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = string.Empty;
}

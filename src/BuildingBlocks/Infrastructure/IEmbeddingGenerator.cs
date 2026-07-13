namespace Infrastructure;

public interface IEmbeddingGenerator
{
    Task<IReadOnlyList<float[]>> GenerateAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
}

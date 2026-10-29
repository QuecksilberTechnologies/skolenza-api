namespace skolenza.application.Interfaces;

/// <summary>Resolves DNS TXT records through the configured infrastructure resolver.</summary>
public interface IDnsTxtResolver
{
    Task<IReadOnlyList<string>> ResolveAsync(string recordName, CancellationToken cancellationToken);
}

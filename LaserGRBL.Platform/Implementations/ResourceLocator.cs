using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Implementations;

public sealed class ResourceLocator
{
    private readonly string resourceRoot;
    public ResourceLocator(string resourceRoot) => this.resourceRoot = resourceRoot;

    public OperationResult<string> Find(params string[] segments)
    {
        var path = Path.Combine([resourceRoot, .. segments]);
        return File.Exists(path)
            ? OperationResult<string>.Success(path)
            : OperationResult<string>.Failure("Bundled resource was not found.", path);
    }
}

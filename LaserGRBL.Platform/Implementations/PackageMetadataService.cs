using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class PackageMetadataService : IPackageMetadataService
{
    public OperationResult<PackageMetadata> Validate(PackageMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.PackageId)) return OperationResult<PackageMetadata>.Failure("Package id is required.");
        if (!metadata.Format.Equals("tar.gz", StringComparison.OrdinalIgnoreCase)) return OperationResult<PackageMetadata>.Failure("Task 15 packaging supports tar.gz metadata.");
        if (metadata.RuntimeIdentifiers.Count == 0) return OperationResult<PackageMetadata>.Failure("At least one runtime identifier is required.");
        if (!metadata.Dependencies.Any(dependency => dependency.Name == "dotnet-runtime-8.0" || dependency.Name == "self-contained")) return OperationResult<PackageMetadata>.Failure("Runtime dependency policy is required.");
        if (!metadata.NoticeFiles.Any(file => file.Equals("LICENSE.md", StringComparison.OrdinalIgnoreCase))) return OperationResult<PackageMetadata>.Failure("LICENSE.md must be included in package notices.");
        if (!metadata.IntegrityAlgorithm.Equals("SHA256", StringComparison.OrdinalIgnoreCase)) return OperationResult<PackageMetadata>.Failure("Package integrity must use SHA256.");
        return OperationResult<PackageMetadata>.Success(metadata);
    }
}

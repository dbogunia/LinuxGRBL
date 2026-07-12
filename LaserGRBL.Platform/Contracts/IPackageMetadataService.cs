using LaserGRBL.Core.Abstractions;

namespace LaserGRBL.Platform.Contracts;

public sealed record PackageDependency(string Name, string Purpose, bool Required);

public sealed record PackageMetadata(
    string PackageId,
    Version Version,
    string Format,
    IReadOnlyList<string> RuntimeIdentifiers,
    IReadOnlyList<PackageDependency> Dependencies,
    IReadOnlyList<string> NoticeFiles,
    string IntegrityAlgorithm);

public interface IPackageMetadataService
{
    OperationResult<PackageMetadata> Validate(PackageMetadata metadata);
}

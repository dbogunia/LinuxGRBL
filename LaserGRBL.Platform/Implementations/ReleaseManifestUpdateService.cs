using System.Text.Json;
using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class ReleaseManifestUpdateService(
    IUpdateManifestClient manifests,
    Uri manifestUri,
    Version currentVersion,
    bool enabled = true) : IUpdateService
{
    public async Task<OperationResult<UpdateInfo?>> CheckAsync(CancellationToken cancellationToken = default)
    {
        if (!enabled) return OperationResult<UpdateInfo?>.Success(null);
        if (manifestUri.Scheme != Uri.UriSchemeHttps) return OperationResult<UpdateInfo?>.Failure("Update manifest URL must use HTTPS.", manifestUri.ToString());

        var result = await manifests.GetManifestAsync(manifestUri, cancellationToken);
        if (!result.Succeeded || result.Value is null)
            return OperationResult<UpdateInfo?>.Failure(result.Error?.Message ?? "Update manifest unavailable.", result.Error?.Detail, result.Error?.Exception);

        try
        {
            var manifest = JsonSerializer.Deserialize<ReleaseManifest>(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (manifest is null || !Version.TryParse(manifest.Version, out var version))
                return OperationResult<UpdateInfo?>.Failure("Update manifest does not contain a valid version.");
            if (string.IsNullOrWhiteSpace(manifest.ReleaseUrl) || !Uri.TryCreate(manifest.ReleaseUrl, UriKind.Absolute, out var releaseUri))
                return OperationResult<UpdateInfo?>.Failure("Update manifest does not contain a valid release URL.");
            if (releaseUri.Scheme != Uri.UriSchemeHttps) return OperationResult<UpdateInfo?>.Failure("Update release URL must use HTTPS.");
            if (version <= currentVersion) return OperationResult<UpdateInfo?>.Success(null);
            if (!Version.TryParse(manifest.ArtifactVersion, out var artifactVersion) || artifactVersion != version)
                return OperationResult<UpdateInfo?>.Failure("Update artifact version does not match manifest version.");
            if (string.IsNullOrWhiteSpace(manifest.Sha256) || manifest.Sha256.Length != 64 || manifest.Sha256.Any(value => !Uri.IsHexDigit(value)))
                return OperationResult<UpdateInfo?>.Failure("Update manifest does not contain a valid SHA256 artifact integrity value.");

            return OperationResult<UpdateInfo?>.Success(new UpdateInfo(version, releaseUri, manifest.Notes, artifactVersion, manifest.Sha256));
        }
        catch (JsonException exception)
        {
            return OperationResult<UpdateInfo?>.Failure("Update manifest JSON is invalid.", exception.Message, exception);
        }
    }

    private sealed record ReleaseManifest(string Version, string ReleaseUrl, string? Notes, string? ArtifactVersion, string? Sha256);
}

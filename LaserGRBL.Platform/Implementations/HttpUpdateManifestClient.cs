using LaserGRBL.Core.Abstractions;
using LaserGRBL.Platform.Contracts;

namespace LaserGRBL.Platform.Implementations;

public sealed class HttpUpdateManifestClient(HttpClient? client = null) : IUpdateManifestClient
{
    private readonly HttpClient client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

    public async Task<OperationResult<string>> GetManifestAsync(Uri manifestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.GetAsync(manifestUri, cancellationToken);
            if (!response.IsSuccessStatusCode) return OperationResult<string>.Failure("Update manifest request failed.", response.StatusCode.ToString());
            return OperationResult<string>.Success(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return OperationResult<string>.Failure("Update manifest request was cancelled.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return OperationResult<string>.Failure("Unable to fetch update manifest.", manifestUri.ToString(), exception);
        }
    }
}

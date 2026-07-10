namespace LaserGRBL.Core.Abstractions;

public sealed record FileTypeFilter(string Name, IReadOnlyList<string> Extensions);

public sealed record FileDialogRequest(string Title, IReadOnlyList<FileTypeFilter> Filters, bool AllowMultiple = false);

public interface IFileDialogService
{
    Task<OperationResult<IReadOnlyList<string>>> OpenAsync(FileDialogRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<string>> SaveAsync(FileDialogRequest request, CancellationToken cancellationToken = default);
}

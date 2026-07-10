namespace LaserGRBL.Core.Abstractions;

public interface IUiDispatcher
{
    bool CheckAccess();

    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);
}

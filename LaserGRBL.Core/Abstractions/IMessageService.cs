namespace LaserGRBL.Core.Abstractions;

public enum MessageSeverity { Information, Warning, Error, Confirmation }

public sealed record MessageRequest(string Title, string Message, MessageSeverity Severity);

public interface IMessageService
{
    Task<bool> ShowAsync(MessageRequest request, CancellationToken cancellationToken = default);
}

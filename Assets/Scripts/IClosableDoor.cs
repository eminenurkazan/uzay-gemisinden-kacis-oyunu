public interface IClosableDoor : IOpenableDoor
{
    bool IsClosed { get; }
    bool IsClosing { get; }
    bool CanCloseSafely { get; }
    bool TryClose(out string failureReason);
}

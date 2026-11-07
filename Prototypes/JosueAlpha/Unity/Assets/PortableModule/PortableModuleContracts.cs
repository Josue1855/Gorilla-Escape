namespace JungleParty.Portable
{
    public interface IPortableInputSource
    {
        string RoomCode { get; }
        bool PhoneConnected { get; }
        bool CameraConnected { get; }
        bool TryConsumeMotion(out string json);
        bool TryConsumePose(out string json);
    }

    public interface IPortableModuleStatus
    {
        bool IsRunning { get; }
        string Status { get; }
        string PublicControllerUrl { get; }
    }
}

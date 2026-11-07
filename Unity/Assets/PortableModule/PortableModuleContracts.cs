using GorillaEscape.Gameplay.Input;
namespace JungleParty.Portable
{
    public interface IPortableInputSource
    {
        bool IsInputReady { get; }
        JungleInputStatus InputStatus { get; }
        bool TryConsumeFrame(out JungleFusedFrame frame);
        bool TryAuthorize(JungleAction action,long revision);
    }
    public interface IPortableModuleStatus
    {
        bool IsRunning { get; }
        string Status { get; }
    }
}

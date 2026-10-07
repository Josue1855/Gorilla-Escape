using System;

namespace GorillaEscape.Network
{
    public enum IpcState { STOPPED, STARTING, CONNECTING, RUNNING, STOPPING, FAILED }

    /// <summary>Launch allowance belongs to the Unity execution, not a scene or attempt.</summary>
    public sealed class IpcLaunchBudget
    {
        public static readonly IpcLaunchBudget Execution = new IpcLaunchBudget();
        private int launches;
        public int Launches { get { lock (this) return launches; } }
        public void Reserve()
        {
            lock (this) { if (launches >= 3) throw new InvalidOperationException("LAUNCH_LIMIT"); launches++; }
        }
    }

    /// <summary>Serialized transitions; an older generation cannot affect a new attempt.</summary>
    public sealed class IpcLifecycle
    {
        private readonly object gate = new object();
        private long generation;
        private IpcState state = IpcState.STOPPED;
        private bool cleanupComplete = true;
        public IpcState State { get { lock (gate) return state; } }
        public bool CleanupComplete { get { lock (gate) return cleanupComplete; } }
        public long Begin()
        {
            lock (gate)
            {
                if (!cleanupComplete || (state != IpcState.STOPPED && state != IpcState.FAILED))
                    throw new InvalidOperationException("BUSY");
                generation++; state = IpcState.STARTING; cleanupComplete = false; return generation;
            }
        }
        public bool Move(long attempt, IpcState next, bool cleaned = false)
        {
            lock (gate)
            {
                if (attempt != generation) return false;
                bool valid = (state == IpcState.STARTING && next == IpcState.CONNECTING)
                    || (state == IpcState.CONNECTING && next == IpcState.RUNNING)
                    || ((state == IpcState.STARTING || state == IpcState.CONNECTING || state == IpcState.RUNNING) && next == IpcState.STOPPING)
                    || (state == IpcState.STOPPING && (next == IpcState.STOPPED || next == IpcState.FAILED));
                if (!valid || (next == IpcState.STOPPED && !cleaned)) throw new InvalidOperationException("INVALID_TRANSITION");
                state = next; cleanupComplete = cleaned; return true;
            }
        }
    }
}

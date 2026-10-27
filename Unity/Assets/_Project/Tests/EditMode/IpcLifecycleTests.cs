using System;
using GorillaEscape.Network;
using NUnit.Framework;

public class IpcLifecycleTests
{
    [Test] public void SuccessfulLifecycleRequiresHandshakeAndCleanup()
    {
        var state = new IpcLifecycle(); long generation = state.Begin();
        Assert.That(state.State, Is.EqualTo(IpcState.STARTING));
        state.Move(generation,IpcState.CONNECTING); state.Move(generation,IpcState.RUNNING);
        state.Move(generation,IpcState.STOPPING); state.Move(generation,IpcState.STOPPED,true);
        Assert.That(state.CleanupComplete, Is.True);
    }
    [TestCase(IpcState.STARTING)] [TestCase(IpcState.CONNECTING)] [TestCase(IpcState.RUNNING)]
    public void FailureMustPassThroughStopping(IpcState at)
    {
        var state = new IpcLifecycle(); long generation = state.Begin();
        if (at != IpcState.STARTING) state.Move(generation,IpcState.CONNECTING);
        if (at == IpcState.RUNNING) state.Move(generation,IpcState.RUNNING);
        Assert.Throws<InvalidOperationException>(()=>state.Move(generation,IpcState.FAILED));
        state.Move(generation,IpcState.STOPPING); state.Move(generation,IpcState.FAILED,true);
        Assert.That(state.State, Is.EqualTo(IpcState.FAILED));
    }
    [Test] public void IncompleteCleanupBlocksRecovery()
    {
        var state = new IpcLifecycle(); long generation = state.Begin();
        state.Move(generation,IpcState.STOPPING); state.Move(generation,IpcState.FAILED,false);
        Assert.Throws<InvalidOperationException>(()=>state.Begin());
    }
    [Test] public void OldGenerationCannotChangeNewAttempt()
    {
        var state = new IpcLifecycle(); long old = state.Begin();
        state.Move(old,IpcState.STOPPING); state.Move(old,IpcState.FAILED,true);
        long next = state.Begin();
        foreach (IpcState late in Enum.GetValues(typeof(IpcState))) Assert.That(state.Move(old,late), Is.False);
        Assert.That(state.State,Is.EqualTo(IpcState.STARTING));
        state.Move(next,IpcState.CONNECTING);
    }
    [Test] public void ActiveAttemptRejectsAnotherStart()
    {
        var state = new IpcLifecycle(); state.Begin(); Assert.Throws<InvalidOperationException>(()=>state.Begin());
    }
    [Test] public void LaunchBudgetAllowsExactlyThreeAttempts()
    {
        var budget = new IpcLaunchBudget(); budget.Reserve(); budget.Reserve(); budget.Reserve();
        Assert.Throws<InvalidOperationException>(()=>budget.Reserve()); Assert.That(budget.Launches,Is.EqualTo(3));
    }
}

using System.Collections.Generic;
using BHR.Modules;

namespace BHR.Tests.Modules;

public class KillTimerManagerTests
{
    public KillTimerManagerTests()
    {
        KillTimerManager.Initializate();
    }

    [Fact]
    public void Initializate_ClearsAllKillTimers()
    {
        KillTimerManager.AllKillTimers[1] = 5f;
        KillTimerManager.AllKillTimers[2] = 10f;

        KillTimerManager.Initializate();

        Assert.Empty(KillTimerManager.AllKillTimers);
    }

    [Fact]
    public void GetKillTimer_ForTrackedPlayer_ReturnsStoredTimer()
    {
        KillTimerManager.AllKillTimers[42] = 7.5f;

        var timer = ((byte)42).GetKillTimer();

        Assert.Equal(7.5f, timer);
    }

    [Fact]
    public void GetKillTimer_ForUntrackedPlayer_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => ((byte)42).GetKillTimer());
    }
}
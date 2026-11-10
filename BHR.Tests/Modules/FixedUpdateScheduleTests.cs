using BHR;

namespace BHR.Tests.Modules;

public class FixedUpdateScheduleTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Advance_RunsFullUpdateAtLeastOncePerThirtyTicks(bool lowLoadEnabled)
    {
        int ticksRemaining = 30;
        int fullUpdates = 0;

        for (int tick = 0; tick < 60; tick++)
        {
            if (!FixedUpdateSchedule.Advance(ref ticksRemaining, lowLoadEnabled))
                fullUpdates++;
        }

        Assert.Equal(lowLoadEnabled ? 2 : 60, fullUpdates);
        Assert.Equal(30, ticksRemaining);
    }
}

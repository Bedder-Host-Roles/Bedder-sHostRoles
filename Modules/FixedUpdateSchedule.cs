namespace BHR;

public static class FixedUpdateSchedule
{
    public static bool Advance(ref int ticksRemaining, bool lowLoadEnabled)
    {
        ticksRemaining--;
        if (ticksRemaining <= 0)
        {
            ticksRemaining = 30;
            return false;
        }

        return lowLoadEnabled;
    }
}

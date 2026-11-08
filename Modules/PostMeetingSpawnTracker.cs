using System;

namespace BHR;

public sealed class PostMeetingSpawnTracker
{
    private readonly HashSet<object> pending = [];

    public bool HasPendingSpawns => pending.Count > 0;

    public Action Track(Action spawn)
    {
        var token = new object();
        pending.Add(token);
        return () =>
        {
            try
            {
                spawn();
            }
            finally
            {
                pending.Remove(token);
            }
        };
    }
}

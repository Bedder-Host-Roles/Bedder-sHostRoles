namespace BHR.Tests.Modules;

public class PostMeetingSpawnTrackerTests
{
    [Fact]
    public void PendingSpawns_RemainPendingUntilEverySpawnCompletes()
    {
        var tracker = new PostMeetingSpawnTracker();
        var completed = new List<int>();
        Assert.False(tracker.HasPendingSpawns);
        var first = tracker.Track(() =>
        {
            Assert.True(tracker.HasPendingSpawns);
            completed.Add(1);
        });
        var second = tracker.Track(() => completed.Add(2));

        Assert.True(tracker.HasPendingSpawns);
        second();
        Assert.True(tracker.HasPendingSpawns);
        first();
        Assert.False(tracker.HasPendingSpawns);
        Assert.Equal(new[] { 2, 1 }, completed);
    }

    [Fact]
    public void PendingSpawns_AreReleasedWhenSpawnFails()
    {
        var tracker = new PostMeetingSpawnTracker();
        var spawn = tracker.Track(() => throw new InvalidOperationException("spawn failed"));
        var otherSpawn = tracker.Track(() => { });

        Assert.Throws<InvalidOperationException>(spawn);
        Assert.True(tracker.HasPendingSpawns);
        otherSpawn();
        Assert.False(tracker.HasPendingSpawns);
    }
}

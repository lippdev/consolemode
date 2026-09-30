using ConsoleMode.Services;

namespace ConsoleMode.Tests;

public sealed class RestoreWaitPolicyTests
{
    [Fact]
    public void KeepsWaitingWhileAnotherRestoreIsActiveAndWithinTheBound()
    {
        Assert.True(RestoreWaitPolicy.ShouldWait(true, RestoreWaitPolicy.Timeout - TimeSpan.FromMilliseconds(1)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopsWaitingWhenRestoreFinishedOrBoundWasReached(bool restoreInProgress)
    {
        Assert.False(RestoreWaitPolicy.ShouldWait(restoreInProgress, RestoreWaitPolicy.Timeout));
    }
}

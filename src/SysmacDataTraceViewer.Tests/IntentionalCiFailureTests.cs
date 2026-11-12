using Xunit;

namespace SysmacDataTraceViewer.Tests;

public sealed class IntentionalCiFailureTests
{
    [Fact]
    public void IntentionalFailureStopsCi() => Assert.Fail("Intentional CI failure validation. Do not merge this probe.");
}

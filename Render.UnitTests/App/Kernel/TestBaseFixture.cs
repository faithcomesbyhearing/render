using Moq;
using Render.TempFromVessel.Kernel;
using Splat;

namespace Render.UnitTests.App.Kernel;

// Contains logic that must be executed before initializing properties or fields in the test class.
public class TestBaseFixture
{
    public TestBaseFixture()
    {
        Locator.CurrentMutable.RegisterConstant(new Mock<IAppSettings>().Object, typeof(IAppSettings));
    }
}
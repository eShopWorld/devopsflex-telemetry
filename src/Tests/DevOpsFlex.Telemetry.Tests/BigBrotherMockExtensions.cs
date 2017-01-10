namespace DevOpsFlex.Telemetry.Tests
{
    using Moq;

    public static class BigBrotherMockExtensions
    {
        public static Mock<BigBrother> WithoutSetup(this Mock<BigBrother> bbMock)
        {
            bbMock.Setup(x => x.SetupSubscriptions());
            bbMock.Setup(x => x.SetupTelemetryClient(It.IsAny<string>(), It.IsAny<string>()));

            return bbMock;
        }
    }
}

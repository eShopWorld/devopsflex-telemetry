using System;
using Eshopworld.Core;
using Eshopworld.Tests.Core;
using Xunit;

namespace Eshopworld.Telemetry.Tests
{
    public class ExceptionTests
    {
        private static readonly string DevKey = Environment.GetEnvironmentVariable("devai", EnvironmentVariableTarget.User);

        [Fact]
        [IsLayer0]
        public void CustomExceptionTest()
        {
            var customException = new CustomException
            {
                MyCustomDateTime = DateTime.UtcNow,
                MyCustomProperty = "Hello World"
            };

            var bigBrother = new BigBrother(DevKey, DevKey).DeveloperMode();

            bigBrother.Publish(customException.ToExceptionEvent());
            bigBrother.Flush();
        }

        public class CustomException : Exception
        {
            public string MyCustomProperty { get; set; }

            public DateTime MyCustomDateTime { get; set; }
        }
    }
}
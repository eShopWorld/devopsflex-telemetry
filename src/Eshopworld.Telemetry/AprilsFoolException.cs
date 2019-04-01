using System;

namespace Eshopworld.Telemetry
{
    public class AprilsFoolException : Exception
    {
        public AprilsFoolException()
            : base("You have been pranked by Tooling! Happy April's fool day!")
        {
        }

        public override string HelpLink => "https://en.wikipedia.org/wiki/April_Fools%27_Day";
    }
}

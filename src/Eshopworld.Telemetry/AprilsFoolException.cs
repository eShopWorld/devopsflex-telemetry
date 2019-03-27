using System;

namespace Eshopworld.Telemetry
{
    public class AprilsFoolException : Exception
    {
        public AprilsFoolException()
            : base("You have been pranked by Tooling! Happy April's fool day!")
        {
        }
    }
}

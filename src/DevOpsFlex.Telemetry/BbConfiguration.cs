namespace DevOpsFlex.Telemetry
{
    public static class BbConfiguration
    {
        public static IConfigureSources UseEventSourceSink()
        {
            return new EventSourceSink();
        }

        public static void ForExceptions(this IConfigureSources source)
        {
            switch (source)
            {
                case EventSourceSink _:
                    // SETUP
                    break;
                case null:
                    break;
            }
        }
    }

    public class EventSourceSink : IConfigureSources
    {
    }

    public interface IConfigureSources
    {
    }
}

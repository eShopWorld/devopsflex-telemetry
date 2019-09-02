namespace Eshopworld.Telemetry.Kusto
{
    public class QueuedClientOptions
    {
        public QueuedClientOptions(int ingestionInterval = 1000, int bufferSizeItems = 50, bool flushImmediately = true)
        {
            IngestionInterval = ingestionInterval;
            BufferSizeItems = bufferSizeItems;
            FlushImmediately = flushImmediately;
        }

        public int IngestionInterval { get; set; } 
        public int BufferSizeItems { get; set; }
        public bool FlushImmediately { get; set; }
    }
}
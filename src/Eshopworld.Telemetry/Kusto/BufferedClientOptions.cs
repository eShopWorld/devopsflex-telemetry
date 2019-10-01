namespace Eshopworld.Telemetry.Kusto
{
    using System;

    public class BufferedClientOptions
    {
        /// <param name="ingestionInterval">Local app buffer ingestion time buffer. Defaults to 1 sec interval</param>
        /// <param name="bufferSizeItems">Max buffer size before flush</param>
        /// <param name="flushImmediately">Flush immediately from Kusto aggregator</param>
        public BufferedClientOptions(TimeSpan? ingestionInterval, int bufferSizeItems = 100, bool flushImmediately = true)
        {
            IngestionInterval = ingestionInterval ?? TimeSpan.FromMilliseconds(1000);
            BufferSizeItems = bufferSizeItems;
            FlushImmediately = flushImmediately;
        }


        public TimeSpan IngestionInterval { get; set; } 
        public int BufferSizeItems { get; set; }
        public bool FlushImmediately { get; set; }
    }
}
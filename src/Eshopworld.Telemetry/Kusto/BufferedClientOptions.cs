using System;

namespace Eshopworld.Telemetry.Kusto
{
    public class BufferedClientOptions
    {
        /// <param name="ingestionInterval">Defaults to 1sec interval</param>
        /// <param name="bufferSizeItems">Max buffer size before flush</param>
        /// <param name="flushImmediately">Flush immediately from Kusto aggregator</param>
        public BufferedClientOptions(TimeSpan? ingestionInterval, int bufferSizeItems = 100, bool flushImmediately = true)
        {
            IngestionInterval = ingestionInterval ?? TimeSpan.FromMilliseconds(1000);
            BufferSizeItems = bufferSizeItems;
            FlushImmediately = flushImmediately;
        }

        /// <param name="intervalMiliseconds">Defaults to 1sec interval</param>
        /// <param name="bufferSizeItems">Max buffer size before flush</param>
        /// <param name="flushImmediately">Flush immediately from Kusto aggregator</param>
        public BufferedClientOptions(int intervalMiliseconds = 1000, int bufferSizeItems = 100, bool flushImmediately = true)
        {
            IngestionInterval = TimeSpan.FromMilliseconds(intervalMiliseconds);
            BufferSizeItems = bufferSizeItems;
            FlushImmediately = flushImmediately;
        }

        public TimeSpan IngestionInterval { get; set; } 
        public int BufferSizeItems { get; set; }
        public bool FlushImmediately { get; set; }
    }
}
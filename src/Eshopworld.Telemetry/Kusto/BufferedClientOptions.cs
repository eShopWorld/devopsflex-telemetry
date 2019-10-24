using System;

namespace Eshopworld.Telemetry.Kusto
{
    public class BufferedClientOptions
    {
        /// <summary>
        /// Local app buffer ingestion time buffer. Defaults to 1 sec interval
        /// </summary>
        public TimeSpan IngestionInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Max buffer size before flush
        /// </summary>
        public int BufferSizeItems { get; set; } = 100;

        /// <summary>
        /// Flush immediately from Kusto aggregator
        /// </summary>
        public bool FlushImmediately { get; set; } = true;
    }
}
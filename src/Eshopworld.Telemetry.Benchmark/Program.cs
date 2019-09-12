using System.Linq;
using System.Threading;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using Eshopworld.Telemetry.Kusto;

namespace Eshopworld.Telemetry.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Running;
    using Eshopworld.Core;

    public class Program
    {
        [CoreJob]
        [Config(typeof(Config))]
        public class KustoBenchmark
        {
            private class Config : ManualConfig
            {
                public Config() 
                {
                    Options |= ConfigOptions.DisableOptimizationsValidator;
                }
            }

            private BigBrother bb;

            public KustoBenchmark()
            {
                var kustoName = Environment.GetEnvironmentVariable("kusto_name", EnvironmentVariableTarget.Machine);
                var kustoLocation = Environment.GetEnvironmentVariable("kusto_location", EnvironmentVariableTarget.Machine);
                var kustoDatabase = Environment.GetEnvironmentVariable("kusto_database", EnvironmentVariableTarget.Machine);
                var kustoTenantId = Environment.GetEnvironmentVariable("kusto_tenant_id", EnvironmentVariableTarget.Machine);

                bb = new BigBrother("", "");
                bb.UseKusto(kustoName, kustoLocation, kustoDatabase, kustoTenantId);
            }

            [Benchmark]
            public void One_NoCheck_Direct()
            {
                bb.HandleKustoEvent(new KustoBenchmarkEvent()).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            [Benchmark]
            public void Fifty_NoCheck_Direct()
            {
                var tasks = new List<Task>();
                for (int i = 0; i < 50; i++)
                {
                    tasks.Add(bb.HandleKustoEvent(new KustoBenchmarkEvent()));
                }

                Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();
            }

            [Benchmark]
            public void TwoHundred_NoCheck_Direct()
            {
                var tasks = new List<Task>();
                for (int i = 0; i < 200; i++)
                {
                    tasks.Add(bb.HandleKustoEvent(new KustoBenchmarkEvent()));
                }

                Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public class KustoBenchmarksManual
        {
            public void Queued_buffer_1s_500msg()
            {
                Console.WriteLine("Queued_buffer_1s_500msg");

                var kustoName = "";
                var kustoLocation = "";
                var kustoDatabase = "";
                var kustoTenantId = "";

                var semaphore = new SemaphoreSlim(0);

                var brother = new BigBrother();
                brother.UseKusto(b =>
                {
                    b.UseCluster(kustoName, kustoLocation, kustoDatabase, kustoTenantId);
                    b.UseQueuedIngestion<KustoBenchmarkEvent>(CancellationToken.None, 1000, 500);
                    b.OnMessageSent(x =>
                    {
                        if (x >= 2)
                            semaphore.Release();
                    });
                });

                for (int i = 0; i < 2; i++)
                {
                    brother.Publish(new KustoBenchmarkEvent());
                }

                Console.WriteLine("Waiting ...");
                semaphore.Wait();
            }
        }

        public class KustoBenchmarkEvent : DomainEvent
        {
            public KustoBenchmarkEvent()
            {
                Id = Guid.NewGuid();
                SomeInt = new Random().Next(100);
                SomeStringOne = Guid.NewGuid().ToString();
                SomeStringTwo = Guid.NewGuid().ToString();
                SomeDateTime = DateTime.Now;
                SomeTimeSpan = TimeSpan.FromMinutes(new Random().Next(60));
            }

            public Guid Id { get; set; }

            public int SomeInt { get; set; }

            public string SomeStringOne { get; set; }

            public string SomeStringTwo { get; set; }

            public string BlaBla { set; get; }

            public DateTime SomeDateTime { get; set; }

            public TimeSpan SomeTimeSpan { get; set; }
        }

        public static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<KustoBenchmark>();

            var benchmark = new KustoBenchmarksManual();
            benchmark.Queued_buffer_1s_500msg();
            Console.WriteLine("done.");
        }
    }
}

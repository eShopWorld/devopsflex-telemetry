using System.Diagnostics;
using System.Threading;
using Eshopworld.Telemetry.Kusto;

namespace Eshopworld.Telemetry.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
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
            [Arguments(200)]
            public void TwoHundred_NoCheck_Direct(int count)
            {
                var tasks = new List<Task>();
                for (int i = 0; i < count; i++)
                {
                    tasks.Add(bb.HandleKustoEvent(new KustoBenchmarkEvent()));
                }

                Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public class KustoBenchmarksManual
        {
            public Task Queued_buffer_1s_500msg(int count)
            {
                Console.WriteLine("Queued_buffer_1s_500msg");

                var kustoName = "eswtest";
                var kustoLocation = "westeurope";
                var kustoDatabase = "tele-poc";
                var kustoTenantId = "";

                var source = new TaskCompletionSource<bool>();

                var brother = new BigBrother();
                brother.UseKusto(b =>
                {
                    b.UseCluster(kustoName, kustoLocation, kustoDatabase, kustoTenantId);
                    b.UseQueuedIngestion<KustoBenchmarkEvent>(CancellationToken.None, 1000, 500);

                    b.OnMessageSent(x =>
                    {
                        Console.WriteLine($"Ingested {x} messages");
                        if (x >= count)
                            source.SetResult(true);
                    });
                });

                for (int i = 0; i < count; i++)
                {
                    brother.Publish(new KustoBenchmarkEvent());
                }

                Console.WriteLine("Waiting ...");

                return source.Task;
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

        public static async Task Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<KustoBenchmark>();

            var count = 2;

            var stopwatch = Stopwatch.StartNew();

            var benchmark = new KustoBenchmarksManual();
            await benchmark.Queued_buffer_1s_500msg(count);

            Console.WriteLine($"done queued in {stopwatch.ElapsedMilliseconds}ms");

            Console.WriteLine("waiting 30 sec to cool down...");
            await Task.Delay(TimeSpan.FromSeconds(30));

            stopwatch.Restart();

            var benchmark2 = new KustoBenchmark();
            benchmark2.TwoHundred_NoCheck_Direct(count);

            Console.WriteLine($"done direct in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}

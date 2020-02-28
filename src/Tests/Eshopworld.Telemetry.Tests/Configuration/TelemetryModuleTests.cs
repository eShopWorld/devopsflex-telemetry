using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Eshopworld.Core;
using Eshopworld.Telemetry.Configuration;
using Eshopworld.Tests.Core;
using FluentAssertions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Xunit;

namespace Eshopworld.Telemetry.Tests.Configuration
{
    public class TelemetryModuleTests
    {
        [Fact, IsLayer0]
        public void BigBrotherIsRegistered()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TelemetryModule>();

            var container = containerBuilder.Build();

            container.IsRegistered<IBigBrother>().Should().BeTrue();
        }

        [Fact, IsLayer0]
        public void BigBrotherCanBeBuild()
        {
            var events = new ConcurrentBag<ITelemetry>();
            var instrumentationKey = Guid.NewGuid().ToString();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register(c => new TelemetryClientBuilder().Build(t => events.Add(t)));
            containerBuilder.Register(c => new TelemetrySettings { InstrumentationKey = instrumentationKey, InternalKey = Guid.Empty.ToString() });
            containerBuilder.RegisterModule<TelemetryModule>();
            var container = containerBuilder.Build();

            var bb = container.Resolve<IBigBrother>();
            bb.Publish(new TestEvent());

            events.Should().OnlyContain(t => IsTestEvent(t));
        }

        private static bool IsTestEvent(ITelemetry telemetry)
            => telemetry is EventTelemetry ev && ev.Name == nameof(TestEvent);

        [Fact, IsLayer0]
        public void BigBrotherCanBeConfigured()
        {
            var events = new ConcurrentBag<ITelemetry>();
            var baseEvents = new ConcurrentBag<BaseEvent>();
            var instrumentationKey = Guid.NewGuid().ToString();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TelemetryModule>();
            containerBuilder.Register(c => new TelemetryClientBuilder().Build(t => events.Add(t)));
            containerBuilder.Register(c => new TelemetrySettings { InstrumentationKey = instrumentationKey, InternalKey = Guid.Empty.ToString() });
            containerBuilder.Register(c => new TestBigBrotherInitializer(e => baseEvents.Add(e))).As<IBigBrotherInitializer>();
            var container = containerBuilder.Build();
            var testEvent = new TestEvent();

            var bb = container.Resolve<IBigBrother>();
            bb.Publish(testEvent);

            events.Should().OnlyContain(t => IsTestEvent(t));
            baseEvents.Should().OnlyContain(e => e == testEvent);
        }

        [Fact, IsLayer0]
        public void LogicalCallTelemetryInitializerAddsProperties()
        {
            var events = new ConcurrentBag<ITelemetry>();
            var instrumentationKey = Guid.NewGuid().ToString();
            var containerBuilder = new ContainerBuilder();
            containerBuilder.Register(c => new TelemetryClientBuilder().AddInitializers(c.Resolve<IEnumerable<ITelemetryInitializer>>()).Build(t => events.Add(t)));
            containerBuilder.Register(c => new TelemetrySettings { InstrumentationKey = instrumentationKey, InternalKey = Guid.Empty.ToString() });
            containerBuilder.RegisterModule<TelemetryModule>();
            var container = containerBuilder.Build();

            var bb = container.Resolve<IBigBrother>();
            LogicalCallTelemetryInitializer.Instance.SetProperty("TestProp", "TestValue");
            bb.Publish(new TestEvent());
            bb.Flush();

            events.Should().HaveCount(1);
            events.First().Should().BeOfType<EventTelemetry>()
                .Which.Properties.Should().ContainKey("TestProp")
                .WhichValue.Should().Be("TestValue");
        }

        [Fact, IsLayer0]
        public void LogicalCallTelemetryInitializerIsRegistered()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TelemetryModule>();
            var container = containerBuilder.Build();

            var telemetryInitializers = container.Resolve<IEnumerable<ITelemetryInitializer>>();

            telemetryInitializers.Should().Contain(LogicalCallTelemetryInitializer.Instance);
        }

        [Fact, IsLayer0]
        public void BigBrotherEventsPublisherInitializerIsRegistered()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule<TelemetryModule>();
            var container = containerBuilder.Build();

            var telemetryInitializers = container.Resolve<IEnumerable<IBigBrotherInitializer>>();

            telemetryInitializers.Should().Contain(x => x is BigBrotherEventsPublisherInitializer);
        }

        private class TestEvent : TelemetryEvent
        {
        }

        private class TestBigBrotherInitializer : IBigBrotherInitializer
        {
            private readonly Action<BaseEvent> _baseEventAction;

            public TestBigBrotherInitializer(Action<BaseEvent> baseEventAction)
            {
                _baseEventAction = baseEventAction;
            }

            public void Initialize(BigBrother bigBrother, IComponentContext componentContext)
            {
                var (telemetryObservable, telemetryObserver, internalObservable) = bigBrother;
                telemetryObservable.Subscribe(e => _baseEventAction(e));
            }
        }
    }
}

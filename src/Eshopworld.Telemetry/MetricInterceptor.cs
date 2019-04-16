using System;
using Castle.DynamicProxy;
using Eshopworld.Core;
using Eshopworld.Telemetry.InternalEvents;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry
{
    public class MetricInterceptor : IInterceptor
    {
        private readonly Metric _metric;
        private readonly Func<Metric, ITrackedMetric, bool> _func;
        private readonly IObserver<TelemetryEvent> _internalObserver;

        public MetricInterceptor(Metric metric, Func<Metric, ITrackedMetric, bool> func, IObserver<TelemetryEvent> internalObserver)
        {
            _metric = metric;
            _func = func;
            _internalObserver = internalObserver;
        }

        public void Intercept(IInvocation invocation)
        {
            try
            {
                var target = (ITrackedMetric)invocation.InvocationTarget;

                if (!_func(_metric, target))
                {
                    _internalObserver.OnNext(new MetricCapEvent(_metric.Identifier.MetricId));
                }

                invocation.Proceed();
            }
            catch (Exception ex)
            {
                _internalObserver.OnNext(ex.ToExceptionEvent());
            }
        }
    }
}
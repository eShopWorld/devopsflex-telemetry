using System;
using Castle.DynamicProxy;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry
{
    public class MetricInterceptor : IInterceptor
    {
        private readonly Metric _metric;
        private readonly Func<Metric, ITrackedMetric, bool> _func;

        public MetricInterceptor(Metric metric, Func<Metric, ITrackedMetric, bool> func)
        {
            _metric = metric;
            _func = func;
        }

        public void Intercept(IInvocation invocation)
        {
            var target = (ITrackedMetric) invocation.InvocationTarget;

            var result = _func(_metric, target);
            invocation.Proceed();
        }
    }
}
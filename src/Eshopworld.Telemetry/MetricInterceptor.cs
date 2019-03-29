using Castle.DynamicProxy;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry
{
    public class MetricInterceptor : IInterceptor
    {
        private readonly Metric _metric;

        public MetricInterceptor(Metric metric)
        {
            _metric = metric;
        }

        public void Intercept(IInvocation invocation)
        {
            //_metric.TrackValue(invocation.Arguments[0], target.Queue); // requires a lot more work here
            invocation.Proceed();
        }
    }
}
using System;
using System.Reflection;
using Castle.DynamicProxy;
using Eshopworld.Core;

namespace Eshopworld.Telemetry
{
    public class MetricProxyGenerationHook : IProxyGenerationHook
    {
        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            return methodInfo.Name.Equals($"set_{nameof(ITrackedMetric.Metric)}");
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
            if (memberInfo.Name.Equals($"set_{nameof(ITrackedMetric.Metric)}"))
            {
                throw new InvalidOperationException($"The Metric property setter needs to be marked as virtual on type {memberInfo.DeclaringType?.FullName}");
            }
        }

        public void MethodsInspected() { /* Not required by the implementation */ }
    }
}
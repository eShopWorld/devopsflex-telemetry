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
            if (!methodInfo.Name.Equals($"set_{nameof(ITrackedMetric.Metric)}")) return false;
            if (methodInfo.IsVirtual) return true;

            throw new InvalidOperationException($"The Metric property setter needs to be marked as virtual on type {methodInfo.DeclaringType?.FullName}");
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo) { /* Not required by the implementation */ }

        public void MethodsInspected() { /* Not required by the implementation */ }
    }
}
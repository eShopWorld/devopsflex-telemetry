namespace DevOpsFlex.Telemetry.Processors
{
    using System;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Sets the 'cloud_RoleName' role name attribute on a AI telemetry item
    /// </summary>
    /// <remarks>See https://docs.microsoft.com/en-us/azure/application-insights/app-insights-monitor-multi-role-apps </remarks>
    /// <example>
    /// &lt;TelemetryProcessors&gt;
    /// &lt;!-- Insert this in ApplicationInsights.config --&gt;
    ///     &lt;Add Type="DevOpsFlex.Telemetry.RoleNameSetter, DevOpsFlex.Telemetry"&gt;
    ///         &lt;RoleName>MyApplication&lt;/RoleName&gt;
    ///     &lt;/Add&gt;
    /// &lt;/TelemetryProcessors&gt;
    /// 
    /// // OR 
    /// // alternatively, you can initialize the filter in code. In an initialization class (Global.asax.cs) - insert the processor into the chain:
    /// 
    /// var builder = TelemetryConfiguration.Active.TelemetryProcessorChainBuilder;
    /// builder.Use((next) => new RoleNameSetter(next) { RoleName = "MyApplication" });
    /// builder.Build();
    /// </example>
    public class RoleNameSetter : ITelemetryProcessor
    {
        public static string RoleName { get; set; }

        private ITelemetryProcessor Next { get; }

        public RoleNameSetter([NotNull] ITelemetryProcessor next)
        {
#if DEBUG
            Next = next ?? throw new ArgumentNullException(nameof(next));
#endif
            RoleName = System.Reflection.Assembly.GetEntryAssembly()?.FullName; // this will not resolve for an ASP.NET application.
        }

        public void Process(ITelemetry item)
        {
            if (item?.Context != null)
            {
                if (string.IsNullOrWhiteSpace(item.Context.Cloud.RoleName)) item.Context.Cloud.RoleName = RoleName;
            }

            Next.Process(item);
        }
    }
}
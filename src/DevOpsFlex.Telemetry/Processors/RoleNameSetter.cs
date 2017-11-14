namespace DevOpsFlex.Telemetry.Processors
{
    using System;
    using JetBrains.Annotations;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Sets the 'cloud_RoleName' role name attribute on a AI telemetry item. 
    /// The RoleName will default to the <see cref="System.Reflection.Assembly.GetEntryAssembly()">EntryAssembly</see> name if possible. 
    /// If this cant be determined, you must specify it manually
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
    /// // optional, will default to entry assembly name if not specified
    /// RoleNameSetter.RoleName = "myCustomAppName";
    /// builder.Use((next) => new RoleNameSetter(next));
    /// builder.Build();
    /// </example>
    public class RoleNameSetter : ITelemetryProcessor
    {
        /// <summary>
        /// Override the default value of RoleName, which is determined from the entry assembly name
        /// </summary>
        public static string RoleName { get; set; }

        private ITelemetryProcessor Next { get; }

        static RoleNameSetter()
        {
            RoleName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name; // this will not resolve for an ASP.NET application
        }

        public RoleNameSetter([NotNull] ITelemetryProcessor next)
        {
            Next = next ?? throw new ArgumentNullException(nameof(next));
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
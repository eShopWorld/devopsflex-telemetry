using System;
using System.Linq;
using System.Linq.Expressions;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry
{
    /// <summary>
    /// Contains extensions to other constructs that deal with the generation and compilation of expression trees.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Validate, generate and compile an expression tree to call the right target for (up to) <see cref="TelemetryClient.GetMetric(string, string, string, string, string)"/>.
        /// </summary>
        /// <param name="type">The <see cref="ITrackedMetric"/> type that we are issuing the metric for.</param>
        /// <returns>The compiled expression tree.</returns>
        public static Func<TelemetryClient, Type, Metric> GenerateExpressionGetMetric(this Type type)
        {
            var dimensions = type.GetProperties()
                                 .Where(
                                     p =>
                                         p.Name != nameof(ITrackedMetric.Metric)
                                         && p.GetMethod.IsPublic
                                         && p.GetMethod.ReturnType == typeof(string)
                                 ).ToList();

            if (dimensions.Count > 4)
                throw new InvalidOperationException($"Application Insights only supports 4 metric dimensions and the type {type.FullName} has {dimensions.Count}");

            var getMetricMethod = typeof(TelemetryClient).GetMethod(nameof(TelemetryClient.GetMetric), dimensions.Select(p => p.PropertyType).Prepend(typeof(string)).ToArray());
            if (getMetricMethod == null)
                throw new InvalidOperationException($"Couldn't find a {nameof(TelemetryClient.GetMetric)} that matches the required signature with {dimensions.Count} dimensions");

            var metricType = Expression.Parameter(typeof(Type), "metricType");
            var telemetryClientParam = Expression.Parameter(typeof(TelemetryClient), "telemetryClient");

            var getMetricCall = Expression.Call(
                telemetryClientParam,
                getMetricMethod,
                dimensions.Select(p => Expression.Constant(p.Name))
                          .Cast<Expression>()
                          .Prepend(Expression.Constant(type.Name))
                          .ToArray());

            return Expression.Lambda<Func<TelemetryClient, Type, Metric>>(getMetricCall, telemetryClientParam, metricType).Compile();
        }
    }
}

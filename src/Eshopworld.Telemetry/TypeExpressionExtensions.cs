using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Eshopworld.Core;
using Microsoft.ApplicationInsights;

namespace Eshopworld.Telemetry
{
    /// <summary>
    /// Contains extensions to other constructs that deal with the generation and compilation of expression trees.
    /// </summary>
    public static class TypeExpressionExtensions
    {
        /// <summary>
        /// Gets all the valid properties for <see cref="Metric"/> dimensions that the <see cref="Type"/> contains
        /// </summary>
        /// <param name="type">The <see cref="Type"/> that we are getting the metric dimensions for.</param>
        /// <returns>A list of all the properties that represent <see cref="Metric"/> dimensions.</returns>
        public static List<PropertyInfo> GetMetricDimensions(this Type type)
        {
            return type.GetProperties()
                       .Where(
                           p =>
                               p.Name != nameof(ITrackedMetric.Metric)
                               && p.GetMethod.IsPublic
                               && p.GetMethod.ReturnType == typeof(string)
                       ).ToList();
        }

        /// <summary>
        /// Validate, generate and compile an expression tree to call the right target for (up to) <see cref="TelemetryClient.GetMetric(string, string, string, string, string)"/>.
        /// </summary>
        /// <returns>The compiled expression tree.</returns>
        /// <typeparam name="T">The <see cref="ITrackedMetric"/> type that we are issuing the metric for.</typeparam>
        /// <param name="client">The <see cref="TelemetryClient"/> we're using to create the tracked Metric.</param>
        /// <returns>The <see cref="Metric"/> that's the result of invoking <see cref="TelemetryClient.GetMetric(string)"/></returns>
        public static Metric InvokeGetMetric<T>(this TelemetryClient client) where T : ITrackedMetric
        {
            var type = typeof(T);
            var dimensions = type.GetMetricDimensions();

            if (dimensions.Count > 4)
                throw new InvalidOperationException($"Application Insights only supports 4 metric dimensions and the type {type.FullName} has {dimensions.Count}");

            var getMetricMethod = typeof(TelemetryClient).GetMethod(nameof(TelemetryClient.GetMetric), dimensions.Select(p => p.PropertyType).Prepend(typeof(string)).ToArray());
            if (getMetricMethod == null)
                throw new InvalidOperationException($"Couldn't find a {nameof(TelemetryClient.GetMetric)} that matches the required signature with {dimensions.Count} dimensions");

            return (Metric) getMetricMethod.Invoke(
                client,
                dimensions.Select(p => p.Name)
                          .Prepend(type.Name)
                          .Cast<object>()
                          .ToArray());
        }

        /// <summary>
        /// Validate, generate and compile an expression tree to get all dimension properties and call the right target for (up to)
        ///     <see cref="Metric.TrackValue(double, string, string, string, string)"/>
        /// </summary>
        /// <param name="type">The real type of the TelemetryMetric object we want to track value for.</param>
        /// <returns>The compiled expression tree.</returns>
        public static Func<Metric, ITrackedMetric, bool> GenerateExpressionTrackValue(this Type type)
        {
            var dimensions = type.GetMetricDimensions();

            // if we have 0 dimensions, there's no need to build and expression tree to get dimension values before calling TrackValue
            if (dimensions.Count == 0)
            {
                return (m, tm) =>
                {
                    m.TrackValue(tm.Metric);
                    return true;
                };
            }

            var valueProperty = type.GetProperty(nameof(ITrackedMetric.Metric));
            if (valueProperty == null)
                throw new InvalidOperationException($"Couldn't find the {nameof(ITrackedMetric.Metric)} property that matches the required signature");

            var trackValueMethod = typeof(Metric).GetMethod(nameof(Metric.TrackValue), dimensions.Select(p => p.PropertyType).Prepend(typeof(double)).ToArray());
            if (trackValueMethod == null)
                throw new InvalidOperationException($"Couldn't find a {nameof(TelemetryClient.GetMetric)} that matches the required signature with {dimensions.Count} dimensions");

            var metric = Expression.Parameter(typeof(Metric), "metric");
            var trackedMetric = Expression.Parameter(typeof(ITrackedMetric), "trackedMetric");

            var trackedMetricCast = Expression.Convert(trackedMetric, type);

            var getMetricCall = Expression.Call(
                metric,
                trackValueMethod,
                dimensions.Select(p => Expression.Property(trackedMetricCast, p.GetMethod))
                          .Prepend(Expression.Property(trackedMetricCast, valueProperty.GetMethod))
                          .Cast<Expression>()
                          .ToArray());

            return Expression.Lambda<Func<Metric, ITrackedMetric, bool>>(getMetricCall, metric, trackedMetric).Compile();
        }
    }
}

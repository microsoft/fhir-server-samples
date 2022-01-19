namespace FhirIngestion.Tools.Common.Observability
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using FhirIngestion.Tools.Common.Helpers;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Helper methods to write messages to the console.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class ApplicationInfoTelemetry
    {
        private static ILogger _logger;
        private static TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes ApplicationInfoTelemetry used throughout Applicaiton.
        /// </summary>
        /// <param name="instrumentationKey">Instrumentation key for AppInsights instance.</param>
        public static void Initialize(string instrumentationKey)
        {
            if (_logger == null && instrumentationKey != null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddApplicationInsightsTelemetryWorkerService(instrumentationKey);

                IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

                _telemetryClient = serviceProvider.GetRequiredService<TelemetryClient>();
                _logger = serviceProvider.GetRequiredService<ILogger<ILogger>>();
            }
            else
            {
                MessageHelper.Verbose($"Ignoring ApplicationInsights, because no key is given.");
            }
        }

        /// <summary>
        /// Logs message.
        /// </summary>
        /// <param name="message">Message to log in AppInsights.</param>
        /// <param name="type">Type of message to show in AppInsights.</param>
        public static void LogMessage(string message, LogLevel type = LogLevel.Information)
        {
            if (message != null && _logger != null)
            {
                _logger.Log(type, message);
            }
        }

        /// <summary>
        /// Flushes logs and sends to Application Insights.
        /// </summary>
        public static void Flush()
        {
            if (_logger != null)
            {
                _telemetryClient.Flush();
                Task.Delay(5000).Wait();
            }
        }

        /// <summary>
        /// Gets logger.
        /// </summary>
        public static ILogger GetLogger() => _logger;

        /// <summary>
        /// Tracks the custom event in AppInsights.
        /// </summary>
        /// <param name="telemetry">The event to track.</param>
        public static void TrackEvent(EventTelemetry telemetry)
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.TrackEvent(telemetry);
            }
        }

        /// <summary>
        /// Tracks to metric in AppInsights.
        /// </summary>
        /// <param name="metricId">The identifier of the metric to track.</param>
        /// <param name="value">The value of the metric to track.</param>
        public static void TrackMetric(string metricId, long value)
        {
            if (_telemetryClient != null)
            {
                var metric = _telemetryClient.GetMetric(metricId);
                metric.TrackValue(value);
            }
        }

        /// <summary>
        /// Tracks an Exception in AppInsights.
        /// </summary>
        /// <param name="telemetry">The Exception to track.</param>
        public static void TrackException(ExceptionTelemetry telemetry)
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.TrackException(telemetry);
            }
        }

        /// <summary>
        /// Starts an operation in AppInsights. As a result all subsequent AppInsight messages
        /// created with the ApplicationInfoTelemetry class will automatically belong to this operation.
        /// The operation can be stopped by calling the StopOperation function.
        /// </summary>
        /// <param name="operationName">The name of the Operation to start.</param>
        /// <returns>Operation item object.</returns>
        public static IOperationHolder<RequestTelemetry> StartOperation(string operationName)
        {
            if (_telemetryClient != null)
            {
                var tc = _telemetryClient.StartOperation<RequestTelemetry>(operationName);
                return tc;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Starts a new Dependency Operation, this can be done after starting a normal Operation.
        /// Dependency Operations will show up as children of the Operation above. It can be stopped
        /// by calling the StopOperation function.
        /// </summary>
        /// <param name="operationName">The name of the Dependency Operation to start.</param>
        /// <returns>Operation item object.</returns>
        public static IOperationHolder<DependencyTelemetry> StartDependencyOperation(string operationName)
        {
            if (_telemetryClient != null)
            {
                var tc = _telemetryClient.StartOperation<DependencyTelemetry>(operationName);
                return tc;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Stops an Operation.
        /// </summary>
        /// <param name="operation">The name of the Operation to stop.</param>
        public static void StopOperation(IOperationHolder<RequestTelemetry> operation)
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.StopOperation(operation);
            }
        }

        /// <summary>
        /// Stops an Operation.
        /// </summary>
        /// <param name="operation">The name of the Operation to stop.</param>
        public static void StopOperation(IOperationHolder<DependencyTelemetry> operation)
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.StopOperation(operation);
            }
        }
    }
}

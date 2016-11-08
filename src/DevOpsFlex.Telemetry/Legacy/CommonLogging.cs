namespace DevOpsFlex.Telemetry.Legacy
{
    using System;

    internal class LegacyTrace : BbTelemetryEvent
    {
        public string Message { get; set; }
    }

    internal class LegacyDebug : BbTelemetryEvent
    {
        public string Logger { get; set; }

        public string Message { get; set; }
    }

    internal class LegacyInfo : BbTelemetryEvent
    {
        public string Logger { get; set; }

        public string Message { get; set; }
    }

    internal class LegacyWarning : BbTelemetryEvent
    {
        public string Logger { get; set; }

        public string Message { get; set; }
    }

    internal class LegacyError : BbTelemetryEvent
    {
        public string Logger { get; set; }

        public string Message { get; set; }
    }

    internal class LegacyException : BbExceptionEvent
    {
        public string Message { get; set; }
    }

    public static class Log
    {
        private static BigBrother _bb;

        public static void Init(string aiKey, string internalKey)
        {
            _bb = new BigBrother(aiKey, internalKey);
        }

        public static void Trace(object message)
        {
            if(_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if(!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyTrace
            {
                Message = (string) message
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void TraceFormat(string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyTrace
            {
                Message = string.Format(format, args)
            });
        }

        public static void Debug(object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyDebug
            {
                Message = (string)message
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void DebugFormat(string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyDebug
            {
                Message = string.Format(format, args)
            });
        }

        public static void Info(string logger, object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyInfo
            {
                Logger = logger,
                Message = (string)message
            });
        }

        public static void Error(string logger, object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyError
            {
                Logger = logger,
                Message = (string)message
            });
        }

        public static void Debug(string logger, object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyDebug
            {
                Logger = logger,
                Message = (string)message
            });
        }


        public static void Warn(string logger, object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyWarning
            {
                Logger = logger,
                Message = (string)message
            });
        }


        public static void Info(object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyInfo
            {
                Message = (string)message
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void InfoFormat(string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyInfo
            {
                Message = string.Format(format, args)
            });
        }

        public static void Warn(object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyWarning
            {
                Message = (string)message
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void WarnFormat(string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyWarning
            {
                Message = string.Format(format, args)
            });
        }

        public static void Error(object message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");
            if (!(message is string)) throw new InvalidOperationException("Nope, telemetry doesn't like stuff passed in as either object or dynamic, you need to refactor your stuff before using this.");

            _bb.Publish(new LegacyError
            {
                Message = (string)message
            });
        }

        public static void Error(Exception exception, string message)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyException
            {
                Exception = exception,
                Message = message
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void ErrorFormat(string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyError
            {
                Message = string.Format(format, args)
            });
        }

        [Obsolete("Stop being lazy by using string format wrappers, swap to the non string format signature instead.")]
        public static void ErrorFormat(Exception exception, string format, params object[] args)
        {
            if (_bb == null) throw new InvalidOperationException($"Dude, you need to call {nameof(Init)}(aiKey, internalKey) before you start using this guy to actually log stuff.");

            _bb.Publish(new LegacyException
            {
                Exception = exception,
                Message = string.Format(format, args)
            });
        }
    }
}
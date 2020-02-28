namespace Eshopworld.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Security;

    internal static class StackTraceHelper
    {
        /// <summary>
        /// The attribute which marks methods and classes which can be omitted from the stock trace of a logged exception.
        /// </summary>
        /// <remarks>
        /// It's not available in all .Net runtimes.
        /// </remarks>
        private static readonly Type? StackTraceHiddenAttributeType = typeof(Attribute).Assembly.GetType("System.Diagnostics.StackTraceHiddenAttribute");

        public static bool IsStackSimplificationAvailable => StackTraceHiddenAttributeType != null;

        public static IEnumerable<StackFrame> SimplifyStackTrace(Exception ex)
        {
            var stackTrace = new StackTrace(ex, true);
            var transformedStackTrace = new List<FilteredStackFrame>();
            bool displayFilenames = true;
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var mb = frame.GetMethod();

                // Check whether the frame should be displayed (the first and the last are always displayed)
                if (i > 0 && i < stackTrace.FrameCount - 1 && !ShowInStackTrace(mb))
                    continue;

                var fileName = default(string);
                var lineNumber = 0;

                // source location printing (based on the code CLR Core)
                if (displayFilenames && (frame.GetILOffset() != -1))
                {
                    try
                    {
                        fileName = frame.GetFileName();
                    }
                    catch (SecurityException)
                    {
                        displayFilenames = false;
                    }

                    if (fileName != null)
                    {
                        lineNumber = frame.GetFileLineNumber();
                    }
                }


                var sf = new FilteredStackFrame(fileName, lineNumber, mb);
                transformedStackTrace.Add(sf);
            }
            return transformedStackTrace;
        }

        private static bool ShowInStackTrace(MethodBase mb)
        {
            return StackTraceHiddenAttributeType == null
                || !(mb.IsDefined(StackTraceHiddenAttributeType)
                || (mb.DeclaringType?.IsDefined(StackTraceHiddenAttributeType) ?? false));
        }

        private class FilteredStackFrame : StackFrame
        {
            private readonly int lineNo;
            private readonly string? fileName;
            private readonly MethodBase method;

            public FilteredStackFrame(string? fileName, int lineNo, MethodBase method)
            {
                this.lineNo = lineNo;
                this.fileName = fileName;
                this.method = method;
            }

            public override int GetFileLineNumber()
            {
                return lineNo;
            }

            public override string? GetFileName()
            {
                return fileName;
            }

            public override string ToString()
            {
                return $"{method} {lineNo}";
            }

            public override MethodBase GetMethod()
            {
                return method;
            }
        }
    }
}

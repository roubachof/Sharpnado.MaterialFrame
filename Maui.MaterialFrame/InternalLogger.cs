﻿using System;
using System.Runtime.CompilerServices;
using System.Threading;

[assembly:InternalsVisibleTo("Sharpnado.MaterialFrame.Android")]
[assembly:InternalsVisibleTo("Sharpnado.MaterialFrame.iOS")]
[assembly:InternalsVisibleTo("Sharpnado.MaterialFrame.macOS")]
[assembly:InternalsVisibleTo("Sharpnado.MaterialFrame.UWP")]
[assembly:InternalsVisibleTo("Sharpnado.Presentation.Forms")]

namespace Sharpnado.MaterialFrame
{
    internal static class InternalLogger
    {
        public static bool EnableLogging { get; set; } = false;

        public static bool EnableDebug { get; set; } = false;

        public static void DebugIf(bool condition, string tag, Func<string> message)
        {
            if (!EnableDebug || !condition)
            {
                return;
            }

            Debug(tag, message());
        }

        public static void Debug(string tag, Func<string> message)
        {
            if (!EnableDebug)
            {
                return;
            }

            Debug(tag, message());
        }

        public static void Debug(string tag, string format, params object[] parameters)
        {
            if (!EnableDebug)
            {
                return;
            }

            DiagnosticLog(tag + " | DBUG | " + format, parameters);
        }

        public static void Debug(string format, params object[] parameters)
        {
            if (!EnableDebug)
            {
                return;
            }

            DiagnosticLog("DBUG | " + format, parameters);
        }

        public static void Info(string tag, string format, params object[] parameters)
        {
            DiagnosticLog(tag + " | INFO | " + format, parameters);
        }

        public static void Info(string format, params object[] parameters)
        {
            DiagnosticLog("INFO | " + format, parameters);
        }

        public static void Warn(string format, params object[] parameters)
        {
            DiagnosticLog("WARN | " + format, parameters);
        }

        public static void Error(string format, params object[] parameters)
        {
            DiagnosticLog("ERRO | " + format, parameters);
        }

        public static void Error(string tag, Exception exception)
        {
            DiagnosticLog(tag + " | " + "ERRO | " + $"{exception.Message}{Environment.NewLine}{exception}");
        }

        public static void Error(string tag, string message)
        {
            DiagnosticLog(tag + " | " + "ERRO | " + " | " + message);
        }

        public static void Error(Exception exception)
        {
            Error($"{exception.Message}{Environment.NewLine}{exception}");
        }

        private static void DiagnosticLog(string format, params object[] parameters)
        {
            if (!EnableLogging)
            {
                return;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("MM-dd H:mm:ss.fff") + " | Sharpnado | " + format, parameters);
#else
            Console.WriteLine(DateTime.Now.ToString("MM-dd H:mm:ss.fff") + " | Sharpnado | " + format, parameters);
#endif
        }
    }
}

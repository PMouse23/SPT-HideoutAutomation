using BepInEx.Logging;
using EFT.UI;
using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace HideoutAutomation.Helpers
{
    internal static class LogHelper
    {
        internal static void LogDebug(string info)
        {
            Logger?.LogDebug(info);
        }

        internal static void LogError(string error)
        {
            Logger?.LogError(error);
        }

        internal static void LogErrorToConsole(string error)
        {
            LogError(error);
            ToConsole(error, LogType.Error);
        }

        internal static void LogErrorWithNotification(string error)
        {
            LogError(error);
            NotificationManagerClass.DisplayMessageNotification(error, EFT.Communications.ENotificationDurationType.Default, EFT.Communications.ENotificationIconType.Alert, Color.red);
        }

        internal static void LogException(Exception exception)
        {
            Logger?.LogError($"{exception.Message}{Environment.NewLine}{exception.StackTrace}");
        }

        internal static void LogExceptionToConsole(Exception exception)
        {
            LogException(exception);

            ConsoleScreen.LogException(exception);
        }

        internal static void LogInfo(string info)
        {
            Logger?.LogInfo(info);
        }

        internal static void LogInfoToConsole(string info)
        {
            LogInfo(info);
            ToConsole(info, LogType.Log);
        }

        internal static void LogInfoWithNotification(string info)
        {
            LogInfo(info);
            NotificationManagerClass.DisplayMessageNotification(info);
        }

        internal static void LogStackTraceToConsole(StackTrace stackTrace)
        {
            LogInfoToConsole(GetStackTrace(stackTrace));
        }

        private static string GetStackTrace(StackTrace stackTrace)
        {
            string str = string.Empty;
            foreach (StackFrame stackFrame in stackTrace.GetFrames())
            {
                MethodBase method = stackFrame.GetMethod();
                str += $"{stackFrame.GetFileName()} {method?.DeclaringType?.FullName}.{method?.Name} {stackFrame.GetFileLineNumber()} {Environment.NewLine}";
            }
            return str;
        }

        private static void ToConsole(string info, LogType logType)
        {
            ConsoleScreen consoleScreen = MonoBehaviourSingleton<PreloaderUI>.Instance.Console;
            foreach (string line in info.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (logType == LogType.Error)
                    ConsoleScreen.LogError(line);
                else if (logType == LogType.Warning)
                    ConsoleScreen.LogWarning(line);
                else if (logType == LogType.Log)
                    ConsoleScreen.Log(line);
            }
        }

        internal static ManualLogSource Logger { get; set; }
    }
}
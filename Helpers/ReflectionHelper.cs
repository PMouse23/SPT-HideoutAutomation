using System;
using System.Reflection;

#nullable enable

namespace HideoutAutomation.Helpers
{
    internal class ReflectionHelper
    {
        internal static Type? FindType(string typeName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = asm.GetType(typeName);
                if (type != null)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"found type: {typeName}");
                    return type;
                }
            }
            LogHelper.LogInfo($"Did not find type: {typeName}");
            return null;
        }
    }
}
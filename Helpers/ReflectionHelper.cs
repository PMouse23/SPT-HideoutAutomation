using System;
using System.Collections.Generic;
using System.Linq;
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

        internal static Type? FindType(BindingFlags bindingFlags, Func<MethodInfo, bool> isMethodAction)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                IEnumerable<Type> result = asm.GetTypes().Where(isNotInterface).Where(t => t.GetMethods(bindingFlags).Any(isMethodAction));
                if (result.Any())
                    return result.FirstOrDefault();
            }
            return null;
        }

        private static bool isNotInterface(Type type)
        {
            return type != null && type.IsInterface == false;
        }
    }
}
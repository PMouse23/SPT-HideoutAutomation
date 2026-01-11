using Comfort.Common;
using EFT.UI;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.MonoBehaviours;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace HideoutAutomation.Patches.View
{
    internal class SimpleStashPanel_Close : ModulePatch
    {
        [PatchPostfix]
        public static void PatchPostfix(SimpleStashPanel __instance)
        {
            try
            {
                SimpleStashPanel simpleStashPanel = __instance;
                if (simpleStashPanel == null)
                    return;
                Singleton<UpdateMonoBehaviour>.Instance.RemoveSimpleStashPanel(simpleStashPanel);
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(SimpleStashPanel), this.isTargetMethod);
        }

        private bool isTargetMethod(MethodInfo method)
        {
            return method.Name == nameof(SimpleStashPanel.Close);
        }
    }
}
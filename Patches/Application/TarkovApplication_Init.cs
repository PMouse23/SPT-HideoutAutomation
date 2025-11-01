using Comfort.Common;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.MonoBehaviours;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

#nullable enable

namespace HideoutAutomation.Patches.Application
{
    internal class TarkovApplication_Init : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TarkovApplication), this.IsTargetMethod);
        }

        private static void checkForHideoutInProgress()
        {
            if (ModChecker.IsModLoaded("Tyfon.HideoutInProgress"))
            {
                if (Globals.Debug)
                    LogHelper.LogInfo("Tyfon.HideoutInProgress detected");
                Globals.IsHideoutInProgress = true;
                Type? transferButtonTargetType = ReflectionHelper.FindType("HideoutInProgress.TransferButton");
                if (transferButtonTargetType == null)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo("transferButtonTargetType not found.");
                    return;
                }
                Globals.HIPTransferButtonType = transferButtonTargetType;
                MethodInfo? contributeMethodInfo = transferButtonTargetType.GetMethod("Contribute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (contributeMethodInfo == null)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo("contributeMethodInfo not found.");
                    return;
                }
                Globals.HIPContributeMethodInfo = contributeMethodInfo;
                FieldInfo? itemRequirementsFieldInfo = transferButtonTargetType.GetField("itemRequirements", BindingFlags.NonPublic | BindingFlags.Instance);
                if (itemRequirementsFieldInfo == null)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo("itemRequirementsFieldInfo not found.");
                    return;
                }
                Globals.HIPItemRequirementsFieldInfo = itemRequirementsFieldInfo;
                FieldInfo? areaDataFieldInfo = transferButtonTargetType.GetField("areaData", BindingFlags.NonPublic | BindingFlags.Instance);
                if (areaDataFieldInfo == null)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo("areaDataFieldInfo not found.");
                    return;
                }
                Globals.HIPAreaDataFieldInfo = areaDataFieldInfo;
            }
        }

        [PatchPostfix]
        private static void PatchPostfix(ref TarkovApplication __instance, InputTree inputTree)
        {
            checkForHideoutInProgress();

            UpdateMonoBehaviour sptControllerMonoBehaviour = __instance.GetOrAddComponent<UpdateMonoBehaviour>();
            Singleton<UpdateMonoBehaviour>.Create(sptControllerMonoBehaviour);
            if (Globals.Debug)
                LogHelper.LogInfo($"Created UpdateMonoBehaviour");
        }

        private bool IsTargetMethod(MethodInfo method)
        {
            return method.Name == nameof(TarkovApplication.Init);
        }
    }
}
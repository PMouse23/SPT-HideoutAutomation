using Comfort.Common;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.MonoBehaviours;
using SPT.Reflection.Patching;
using System.Reflection;

namespace HideoutAutomation.Patches.Application
{
    internal class TarkovApplication_Init : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(TarkovApplication), this.IsTargetMethod);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref TarkovApplication __instance, InputTree inputTree)
        {
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
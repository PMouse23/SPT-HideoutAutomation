using Comfort.Common;
using EFT.Hideout;
using EFT.UI;
using HarmonyLib;
using HideoutAutomation.Production;
using SPT.Reflection.Patching;
using System.Reflection;

namespace HideoutAutomation.Patches.View
{
    internal class ProduceView_Update : ModulePatch
    {
        [PatchPostfix]
        public static void PatchPostfix(ProduceView __instance, DefaultUIButton ____startButton)
        {
            ProduceView produceView = __instance;
            if (produceView == null)
                return;
            var scheme = produceView.Scheme;
            if (scheme == null)
                return;
            if (scheme.continuous)
                return;
            string schemeId = scheme._id;
            DefaultUIButton startButton = ____startButton;
            if (startButton != null)
                patchStartButton(startButton, schemeId);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ProduceView), this.isTargetMethod);
        }

        private static void patchStartButton(DefaultUIButton startButton, string schemeId)
        {
            int stacked = Singleton<ProductionService>.Instance.GetStackCount(schemeId);
            if (Globals.SpecialShortcut.IsPressed())
                startButton.SetHeaderText($"Unstack ({stacked})");
            else
                startButton.SetHeaderText($"Stack ({stacked})");
        }

        private bool isTargetMethod(MethodInfo method)
        {
            return method.Name == nameof(ProduceView.Update);
        }
    }
}
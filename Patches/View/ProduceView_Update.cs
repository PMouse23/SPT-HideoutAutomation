using EFT.Hideout;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace HideoutAutomation.Patches.View
{
    internal class ProduceView_Update : ModulePatch
    {
        [PatchPostfix]
        public static void PatchPostfix(ProduceView __instance, DefaultUIButton ____startButton)
        {
            DefaultUIButton startButton = ____startButton;
            if (startButton != null)
                patchStartButton(startButton);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(ProduceView), this.isTargetMethod);
        }

        private static void patchStartButton(DefaultUIButton startButton)
        {
            int stacked = 0; //TODO get stacked.
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
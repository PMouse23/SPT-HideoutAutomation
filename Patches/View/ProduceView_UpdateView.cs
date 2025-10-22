using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.UI;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Reflection.Patching;
using System;
using System.Linq;
using System.Reflection;

namespace HideoutAutomation.Patches.View
{
    internal class ProduceView_UpdateView : ModulePatch
    {
        private static MethodInfo unlockCanvasGroupMethod;
        private static Type unlockCanvasGroupType;

        protected override MethodBase GetTargetMethod()
        {
            unlockCanvasGroupType = AccessTools.GetTypesFromAssembly(typeof(AbstractGame).Assembly)
                                      .SingleOrDefault(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(this.isTargetUnlockCanvasGroupMethod));
            if (unlockCanvasGroupType == null)
                LogHelper.LogInfo($"unlockCanvasGroupType not found");
            unlockCanvasGroupMethod = unlockCanvasGroupType.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(this.isTargetUnlockCanvasGroupMethod);
            if (unlockCanvasGroupMethod == null)
                LogHelper.LogInfo($"unlockCanvasGroupMethod not found");
            return AccessTools.FirstMethod(typeof(ProduceView), this.IsTargetMethod);
        }

        private static void OnClick(ProduceView produceView)
        {
            string schemeId = produceView.Scheme._id;
            EAreaType areaType = (EAreaType)produceView.Scheme.areaType;
            Requirement[] requirements = produceView.Scheme.requirements;
            ProductionBuildStoreModel productionBuildStoreModel = new ProductionBuildStoreModel()
            {
                Id = schemeId,
                Requirements = requirements
            };
            string productionId = Singleton<ProductionService>.Instance.AddToStack(productionBuildStoreModel, areaType);
            produceView.OnStartProducing?.Invoke(productionId);
        }

        [PatchPostfix]
        private static void PatchPostfix(ProduceView __instance,
            DefaultUIButton ____startButton,
            HideoutItemViewFactory ____resultItemIconViewFactory,
            object ____viewCanvas)
        {
            try
            {
                DefaultUIButton startButton = ____startButton;
                if (startButton != null)
                {
                    startButton.SetHeaderText("Stack");
                    startButton.OnClick.RemoveAllListeners();
                    startButton.OnClick.AddListener(() => { OnClick(__instance); });
                    startButton.Interactable = true;
                    startButton.gameObject.SetActive(true);
                }

                //TODO set ____resultItemIconViewFactory.SetCounterText(); with amount in production. if production produces more than one then multiply.
                unlockCanvasGroupMethod?.Invoke(null, new object[] { ____viewCanvas, true, false });
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        private bool IsTargetMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.Name == nameof(ProduceView.UpdateView)
                && parameters.Length != 1;
        }

        private bool isTargetUnlockCanvasGroupMethod(MethodInfo methodInfo)
        {
            return methodInfo.Name == "SetUnlockStatus"
                && methodInfo.GetParameters().Length == 3;
        }
    }
}
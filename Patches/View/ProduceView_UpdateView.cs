using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.UI;
using HarmonyLib;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Common.Utils;
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
            Requirement[] requirements = produceView.Scheme.requirements;
            ProductionBuildStoreModel productionBuildStoreModel = new ProductionBuildStoreModel()
            {
                Id = schemeId,
                Requirements = requirements
            };

            #region POC Store

            string json = Json.Serialize(productionBuildStoreModel);
            ProductionBuildStoreModel deserialized = Json.Deserialize<ProductionBuildStoreModel>(json);
            ProductionBuild productionbuild = new ProductionBuild()
            {
                Id = deserialized.Id,
                Requirements = deserialized.Requirements
            };
            //TODO stack in memory.
            //TODO stack in backend.
            Singleton<HideoutClass>.Instance.StartSingleProduction(productionbuild, delegate
            {
                produceView.OnStartProducing?.Invoke(produceView.Scheme._id);
            });

            #endregion POC Store
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
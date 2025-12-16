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
using UnityEngine;

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

        private static async void OnClick(ProduceView produceView)
        {
            try
            {
                var scheme = produceView.Scheme;
                string schemeId = scheme._id;
                scheme.productionTime = (float)produceView.Producer.CalculateProductionTime(scheme);
                if (Globals.Debug)
                    LogHelper.LogInfoWithNotification($"productionTime: {scheme.productionTime}");
                EAreaType areaType = (EAreaType)scheme.areaType;
                int inProduction = await Singleton<ProductionService>.Instance.GetStackCount(schemeId, areaType);
                TasksExtensions.HandleExceptions(Singleton<HideoutClass>.Instance.StartSingleProduction(scheme, delegate
                {
                    if (inProduction == 0)
                        produceView.OnStartProducing?.Invoke(schemeId);
                }));
                produceView.UpdateView();
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        [PatchPostfix]
        private static async void PatchPostfix(ProduceView __instance,
            DefaultUIButton ____startButton,
            HideoutItemViewFactory ____resultItemIconViewFactory,
            GameObject ____expectedTimePanel,
            object ____viewCanvas)
        {
            try
            {
                ProduceView produceView = __instance;
                if (produceView == null)
                    return;
                var scheme = produceView.Scheme;

                bool canStart = produceView.Boolean_0
                    && (produceView.Producer.CanStart
                    || !produceView.Producer.CanStartScheme(scheme));

                string schemeId = scheme._id;
                EAreaType areaType = (EAreaType)scheme.areaType;
                int stacked = 0; // Singleton<ProductionService>.Instance.GetCountStack(schemeId, areaType);
                DefaultUIButton startButton = ____startButton;
                if (startButton != null)
                {
                    startButton.SetHeaderText($"Stack ({stacked})");
                    startButton.OnClick.RemoveAllListeners();
                    startButton.OnClick.AddListener(() => { OnClick(produceView); });
                    startButton.Interactable = canStart;
                    startButton.gameObject.SetActive(true);
                }
                HideoutItemViewFactory resultItemIconViewFactory = ____resultItemIconViewFactory;
                if (resultItemIconViewFactory != null)
                {
                    int inProduction = await Singleton<ProductionService>.Instance.GetStackCount(schemeId, areaType);
                    if (inProduction > 0)
                        resultItemIconViewFactory.SetCounterText(inProduction.ToString());
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
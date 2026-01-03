using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using HideoutAutomation.Extensions;
using HideoutAutomation.Helpers;
using HideoutAutomation.Production;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
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

        private static bool checkRequirementsWithoutTools(Requirement[] requirements)
        {
            IEnumerable<Item> allStashItems = Singleton<HideoutClass>.Instance.AllStashItems;
            foreach (Requirement requirement in requirements)
            {
                if (requirement is ToolRequirement)
                    continue;
                else if (requirement is ItemRequirement itemRequirement)
                {
                    itemRequirement.Test(allStashItems);
                    if (itemRequirement.Fulfilled == false)
                        return false;
                }
            }
            return true;
        }

        private static bool IsProducingThisScheme(ProduceView produceView, string schemeId)
        {
            bool isProducingThisScheme = produceView.Producer.ProducingItems.ContainsKey(schemeId);
            return isProducingThisScheme;
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
                bool includeCurrentProduction = true;
                int inProductionArea = await Singleton<ProductionService>.Instance.GetAreaCount(areaType, includeCurrentProduction);
                int inStackRecipe = await Singleton<ProductionService>.Instance.GetStackCount(schemeId, areaType);
                bool isProducingThisScheme = IsProducingThisScheme(produceView, schemeId);
                if (isProducingThisScheme)
                    scheme.requirements = scheme.requirements.Where(req => req is not ToolRequirement).ToArray();
                TasksExtensions.HandleExceptions(Singleton<HideoutClass>.Instance.StartSingleProduction(scheme, delegate
                {
                    if (Globals.Debug)
                        LogHelper.LogInfoWithNotification($"inProductionArea: {inProductionArea}, inStackRecipe: {inStackRecipe}");
                    if (inProductionArea == 0)
                        produceView.OnStartProducing?.Invoke(schemeId);
                    if (inProductionArea > 0)
                        produceView.UpdateStackButton(inStackRecipe + 1);
                }));
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
                if (scheme.continuous)
                    return;

                string schemeId = scheme._id;
                EAreaType areaType = (EAreaType)scheme.areaType;

                bool canStart = produceView.Boolean_0
                    && (produceView.Producer.CanStart
                    || !produceView.Producer.CanStartScheme(scheme));

                bool isProducingThisScheme = IsProducingThisScheme(produceView, schemeId);
                if (canStart == false && isProducingThisScheme)
                    canStart = checkRequirementsWithoutTools(scheme.requirements);

                int stacked = await Singleton<ProductionService>.Instance.GetStackCount(schemeId, areaType);
                DefaultUIButton startButton = ____startButton;
                if (startButton != null)
                    patchStartButton(produceView, canStart, stacked, startButton);
                HideoutItemViewFactory resultItemIconViewFactory = ____resultItemIconViewFactory;
                if (resultItemIconViewFactory != null)
                    await patchResultItemIconViewFactory(scheme, schemeId, areaType, resultItemIconViewFactory);
                object viewCanvas = ____viewCanvas;
                if (viewCanvas != null)
                    unlockCanvasGroupMethod?.Invoke(null, new object[] { viewCanvas, true, false });
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        private static async System.Threading.Tasks.Task patchResultItemIconViewFactory(GClass2440 scheme, string schemeId, EAreaType areaType, HideoutItemViewFactory resultItemIconViewFactory)
        {
            int inProduction = await Singleton<ProductionService>.Instance.GetStackCount(schemeId, areaType);
            if (inProduction > 0)
                resultItemIconViewFactory.SetCounterText((scheme.count * inProduction).ToString());
        }

        private static void patchStartButton(ProduceView produceView, bool canStart, int stacked, DefaultUIButton startButton)
        {
            startButton.SetHeaderText($"Stack ({stacked})");
            startButton.OnClick.RemoveAllListeners();
            startButton.OnClick.AddListener(() => { OnClick(produceView); });
            startButton.Interactable = canStart;
            startButton.gameObject.SetActive(true);
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
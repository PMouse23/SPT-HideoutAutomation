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
using System.Threading.Tasks;
using UnityEngine;

namespace HideoutAutomation.Patches.View
{
    internal class ProduceView_UpdateView : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
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
                if (Globals.SpecialShortcut.IsPressed())
                {
                    unstackProduction(produceView, schemeId);
                    return;
                }
                scheme.productionTime = (float)produceView.Producer.CalculateProductionTime(scheme);
                if (Globals.Debug)
                    LogHelper.LogInfoWithNotification($"productionTime: {scheme.productionTime}");
                EAreaType areaType = (EAreaType)scheme.areaType;
                bool includeCurrentProduction = true;
                int inProductionArea = await Singleton<ProductionService>.Instance.GetAreaCount(areaType, includeCurrentProduction);
                bool isProducingThisScheme = IsProducingThisScheme(produceView, schemeId);
                if (isProducingThisScheme)
                    scheme.requirements = scheme.requirements.Where(req => req is not ToolRequirement).ToArray();
                TasksExtensions.HandleExceptions(Singleton<HideoutClass>.Instance.StartSingleProduction(scheme, async delegate
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"inProductionArea: {inProductionArea}");
                    if (inProductionArea == 0)
                        produceView.OnStartProducing?.Invoke(schemeId);
                    await Singleton<ProductionService>.Instance.GetState();
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
            CanvasGroup ____viewCanvas)
        {
            try
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
                CanvasGroup viewCanvas = ____viewCanvas;
                if (viewCanvas != null)
                {
                    viewCanvas.alpha = 1f;
                    viewCanvas.interactable = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogExceptionToConsole(ex);
            }
        }

        private static async Task patchResultItemIconViewFactory(GClass2440 scheme, string schemeId, EAreaType areaType, HideoutItemViewFactory resultItemIconViewFactory)
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

        private static void unstackProduction(ProduceView produceView, string schemeId)
        {
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"unstack: {schemeId}");
            produceView.UpdateStackButton(0); //TODO get stacked
        }

        private bool IsTargetMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.Name == nameof(ProduceView.UpdateView)
                && parameters.Length != 1;
        }
    }
}
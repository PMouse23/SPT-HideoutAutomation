using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace HideoutAutomation.Patches.View
{
    internal class HideoutProductionRequirementView_Show : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.FirstMethod(typeof(HideoutProductionRequirementView), this.IsTargetMethod);
        }

        [PatchPostfix]
        private static void PatchPostfix(HideoutProductionRequirementView __instance, ItemRequirement requirement, HideoutItemViewFactory ____itemViewFactory)
        {
            HideoutItemViewFactory hideoutItemViewFactory = ____itemViewFactory;
            if (hideoutItemViewFactory != null)
            {
                int num = Mathf.Min(requirement.UserItemsCount, requirement.IntCount);
                bool isFulfilled = num >= requirement.IntCount && requirement.Error == null;
                __instance.IsFulfilled = isFulfilled;
                hideoutItemViewFactory.FulfilledStatus = isFulfilled;
                hideoutItemViewFactory.ShowInfo(showCounter: true, showFulfilledStatus: true);
                hideoutItemViewFactory.SetCounterText(string.Format("<color=#{2}><b>{0}</b></color>/{1}", num.ToString("0.##"), requirement.IntCount.ToString("0.##"), "C5C3B2"));
            }
        }

        private bool IsTargetMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.Name == nameof(HideoutProductionRequirementView.Show)
                && parameters.Length != 1;
        }
    }
}
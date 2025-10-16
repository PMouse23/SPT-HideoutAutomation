using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using HideoutAutomation.Helpers;
using SPT.SinglePlayer.Utils.InRaid;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

#nullable enable

namespace HideoutAutomation.MonoBehaviours
{
    internal class UpdateMonoBehaviour : MonoBehaviour
    {
        private CancellationToken? cancellationToken;
        private CancellationTokenSource? cancellationTokenSource;

        public void Start()
        {
            this.StartCoroutine(this.coroutine());
            if (Globals.Debug)
                LogHelper.LogInfo($"StartedCoroutine");
        }

        private IEnumerator coroutine()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = cancellationTokenSource.Token;
            while (true)
            {
                if (Globals.Debug)
                    LogHelper.LogInfo($"Started new run.");
                yield return new WaitForSeconds(0.5f);
                if (RaidTimeUtil.HasRaidLoaded() == false)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Not in a raid.");
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Handle started quests.");

                    HideoutClass hideout = Singleton<HideoutClass>.Instance;
                    if (hideout == null)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfo($"hideout == null");
                        yield return new WaitForSeconds(0.5f);
                    }
                    if (hideout != null && hideout.AreaDatas.Count > 0)
                    {
                        foreach (AreaData data in hideout.AreaDatas)
                        {
                            if (cancellationTokenSource.IsCancellationRequested)
                                break;
                            EAreaStatus status = data.Status;
                            EAreaType areaType = data.Template.Type;
                            string name = data.Template.Name;
                            if (Globals.Debug)
                            {
                                LogHelper.LogInfo($"EAreaType: {areaType}, Status={status}");
                                //LogHelper.LogInfo($"CurrentStage={Json.Serialize(data.CurrentStage)}");
                                //LogHelper.LogInfo($"NextStage={Json.Serialize(data.NextStage)}");
                            }
                            switch (status)
                            {
                                case EAreaStatus.ReadyToConstruct:
                                case EAreaStatus.ReadyToUpgrade:
                                    if (this.isAllowedToConstuctOrUpdate(data, status))
                                    {
                                        data.UpgradeAction();
                                        LogHelper.LogInfoWithNotification($"{name} upgrade started.");
                                        yield return new WaitForSeconds(0.5f);
                                    }
                                    break;
                                case EAreaStatus.ReadyToInstallConstruct:
                                case EAreaStatus.ReadyToInstallUpgrade:
                                    if (Globals.AutoInstall)
                                    {
                                        data.UpgradeAction();
                                        LogHelper.LogInfoWithNotification($"{name} upgrade installed.");
                                        yield return new WaitForSeconds(0.5f);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private int getItemCount(string templateId)
        {
            HideoutClass hideout = Singleton<HideoutClass>.Instance;
            if (hideout == null || hideout.InventoryController_0 == null)
                return 0;
            int count = 0;
            IEnumerable<Item> items = hideout.InventoryController_0.Inventory.GetAllItemByTemplate(templateId);
            foreach (Item item in items)
                count += item.StackObjectsCount;
            return count;
        }

        private bool isAllowedToConstuctOrUpdate(AreaData data, EAreaStatus status)
        {
            if (status == EAreaStatus.ReadyToConstruct && Globals.AutoConstruct == false)
                return false;
            if (status == EAreaStatus.ReadyToUpgrade && Globals.AutoUpgrade == false)
                return false;
            RelatedRequirements requirements = data.NextStage.Requirements;
            foreach (Requirement requirement in requirements)
                if (this.isAllowToHandover(requirement) == false)
                    return false;
            return true;
        }

        private bool isAllowToHandover(Requirement requirement)
        {
            if (Globals.Debug)
                LogHelper.LogInfo($"requirement: ClassType={requirement.GetType()} Fulfilled={requirement.Fulfilled} Type={requirement.Type}");
            if (requirement is ItemRequirement itemRequirement
                && this.isAllowToHandover(itemRequirement.Item, itemRequirement.BaseCount) == false)
                return false;
            return true;
        }

        private bool isAllowToHandover(Item item, double handoverValue)
        {
            return this.isBlockedCurrency(item, (int)handoverValue) == false;
        }

        private bool isBlockedCurrency(Item item, int handoverValue)
        {
            if (item is not MoneyItemClass moneyItemClass)
                return false;
            int itemCount = this.getItemCount(item.TemplateId);
            handoverValue = (int)(handoverValue * Globals.ThresholdCurrencyHandover);
            if (Globals.Debug)
                LogHelper.LogInfo($"count: {itemCount}, expected: {handoverValue}");
            return itemCount < handoverValue;
        }
    }
}
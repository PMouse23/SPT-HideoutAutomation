using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HideoutAutomation.Helpers;
using SPT.SinglePlayer.Utils.InRaid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

#nullable enable

namespace HideoutAutomation.MonoBehaviours
{
    internal class UpdateMonoBehaviour : MonoBehaviour
    {
        private readonly List<EAreaType> declinedAreaUpdates = new List<EAreaType>();
        private CancellationToken? cancellationToken;
        private CancellationTokenSource? cancellationTokenSource;
        private bool didInvestigate = false;
        private bool inDialog = false;
        private bool? lastRemoveCurrencyRequirements = null;
        private DateTime? lastRun = null;

        public void Start()
        {
            this.StartCoroutine(this.coroutine());
            if (Globals.Debug)
                LogHelper.LogInfo($"Started coroutine");
        }

        public void Update()
        {
            bool shouldInvestigate = Globals.Debug && Globals.InvestigateKeys.IsPressed();
            if (this.didInvestigate != shouldInvestigate)
            {
                this.didInvestigate = shouldInvestigate;
                if (shouldInvestigate)
                    this.investigate();
            }
            if (Globals.ResetDeclinedAreaUpdates.IsPressed() == false)
                return;
            if (hasRaidLoaded())
                return;
            if (this.lastRun == null)
                return;
            if (Globals.Debug)
                this.investigate();
            this.StopAllCoroutines();
            this.lastRun = null;
            this.cancellationTokenSource?.Cancel();
            this.StopAllCoroutines();
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification("Stopped coroutine");
            this.declinedAreaUpdates.Clear();
            LogHelper.LogInfoWithNotification($"Declined Area(s) reset.");
            this.StartCoroutine(this.coroutine());
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification("Started coroutine");
        }

        private static string addSpaces(string input, int length)
        {
            if (input == null)
                input = string.Empty;
            if (input.Length > length)
                return input.Substring(0, length);
            int div = length - input.Length;
            length = input.Length + div * 2;
            return input.PadRight(length);
        }

        private static string addSpacesAndFixCount(int count, int length)
        {
            string str = string.Empty;
            if (count > 1000000)
                str = $"{count / 1000000}m";
            else if (count > 1000)
                str = $"{count / 1000}k";
            else
                str = count.ToString();
            return addSpaces(str, length);
        }

        private static bool hasRaidLoaded()
        {
            return RaidTimeUtil.HasRaidLoaded()
                && Singleton<AbstractGame>.Instance?.GameType != EGameType.Hideout;
        }

        private IEnumerator coroutine()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;
            while (true)
            {
                if (Globals.Debug)
                    LogHelper.LogInfo($"Started new run.");
                yield return new WaitForSeconds(0.5f);
                this.lastRun = DateTime.Now;
                if (this.cancellationToken?.IsCancellationRequested == true)
                    yield break;
                if (hasRaidLoaded() == false)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Not in a raid.");
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Handle hideout upgrades.");
                    HideoutClass hideout = Singleton<HideoutClass>.Instance;
                    if (hideout == null)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfo($"hideout == null");
                        yield return new WaitForSeconds(0.5f);
                    }
                    if (this.lastRemoveCurrencyRequirements == null)
                        this.lastRemoveCurrencyRequirements = Globals.RemoveCurrencyRequirements;
                    else if (this.lastRemoveCurrencyRequirements != Globals.RemoveCurrencyRequirements)
                    {
                        this.lastRemoveCurrencyRequirements = Globals.RemoveCurrencyRequirements;
                        this.showDialogWindow("Remove currency requirements has changed. Your game needs to be recreated from the backend. Do this now?", () =>
                        {
                            this.reloadHideoutRequirements(hideout);
                        }, () => { });
                        yield return new WaitForSeconds(1f);
                    }
                    if (hideout != null && hideout.AreaDatas.Count > 0)
                    {
                        foreach (AreaData data in hideout.AreaDatas)
                        {
                            if (this.cancellationTokenSource.IsCancellationRequested)
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
                            this.removeRequirements(data);
                            switch (status)
                            {
                                case EAreaStatus.ReadyToConstruct:
                                case EAreaStatus.ReadyToUpgrade:
                                    if (this.isAllowedToConstuctOrUpdate(data, areaType, status))
                                    {
                                        Action upgradeAction = new Action(() =>
                                        {
                                            data.UpgradeAction();
                                            LogHelper.LogInfoWithNotification($"{name} upgrade started.");
                                        });
                                        if (Globals.UseDialogWindow)
                                        {
                                            string description = this.getUpgradeDescription(data);
                                            this.showDialogWindow(description, () =>
                                            {
                                                upgradeAction.Invoke();
                                            }, () =>
                                            {
                                                this.declinedAreaUpdates.Add(areaType);
                                                if (Globals.Debug)
                                                    LogHelper.LogInfo("upgrade declined");
                                            });
                                        }
                                        else
                                            upgradeAction.Invoke();
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

        private string getUpgradeDescription(AreaData data)
        {
            string name = data.Template.Name;
            int nextLevel = -1;
            Stage? nextStage = data.NextStage;
            string requirements = Environment.NewLine;
            bool hasItemRequirements = false;
            string itemRequirements = string.Empty;
            string areaRequirements = string.Empty;
            string skillRequirements = string.Empty;
            if (nextStage != null)
            {
                nextLevel = nextStage.Level;
                itemRequirements += $"{Environment.NewLine}Expected Inventory Name";
                foreach (Requirement requirement in nextStage.Requirements)
                {
                    if (requirement is ItemRequirement itemRequirement)
                    {
                        string itemName = itemRequirement.ItemName;
                        int count = itemRequirement.BaseCount;
                        int inventory = this.getItemCount(itemRequirement.TemplateId);
                        itemRequirements += $"{Environment.NewLine}{addSpacesAndFixCount(count, 7)} {addSpacesAndFixCount(inventory, 8)} {itemName}";
                        hasItemRequirements = true;
                    }
                    if (requirement is AreaRequirement areaRequirement)
                    {
                        EAreaType areaType = areaRequirement.AreaType;
                        int requiredLevel = areaRequirement.RequiredLevel;
                        areaRequirements += $"{Environment.NewLine} - {areaType} level {requiredLevel}.";
                    }
                    if (requirement is SkillRequirement skillRequirement)
                    {
                        string skillName = skillRequirement.SkillName;
                        int skillLevel = skillRequirement.SkillLevel;
                        skillRequirements += $"{Environment.NewLine} - {skillName} level {skillLevel}.";
                    }
                }
                if (hasItemRequirements)
                {
                    requirements += $"{Environment.NewLine}Items:";
                    requirements += itemRequirements;
                }
                if (string.IsNullOrWhiteSpace(areaRequirements) == false)
                {
                    requirements += $"{Environment.NewLine}{Environment.NewLine}Area(s):";
                    requirements += areaRequirements;
                }
                if (string.IsNullOrWhiteSpace(skillRequirements) == false)
                {
                    requirements += $"{Environment.NewLine}{Environment.NewLine}Skill(s):";
                    requirements += skillRequirements;
                }
            }
            return $"Upgrade {name} to level {nextLevel}.{requirements}";
        }

        private void investigate()
        {
            TimeSpan? dtm = DateTime.Now - this.lastRun;
            LogHelper.LogInfoWithNotification($"HA: Last run: {dtm?.Seconds ?? -1} seconds ago.");
            if (hasRaidLoaded())
                LogHelper.LogErrorWithNotification("HA: Appears to have raid loaded");
            if (this.cancellationToken?.IsCancellationRequested == true)
                LogHelper.LogErrorWithNotification("HA: CancellationRequested");
            HideoutClass hideout = Singleton<HideoutClass>.Instance;
            if (hideout == null)
            {
                if (Globals.Debug)
                    LogHelper.LogErrorWithNotification($"HA: hideout == null");
            }
            LogHelper.LogInfoWithNotification($"HA: hideout: {hideout?.AreaDatas.Count ?? 0} areas.");
        }

        private bool isAllowedToConstuctOrUpdate(AreaData data, EAreaType areaType, EAreaStatus status)
        {
            if (status != EAreaStatus.ReadyToConstruct
             && status != EAreaStatus.ReadyToUpgrade)
                return false;
            if (status == EAreaStatus.ReadyToConstruct
             && Globals.AutoConstruct == false)
                return false;
            if (status == EAreaStatus.ReadyToUpgrade
             && Globals.AutoUpgrade == false)
                return false;
            if (this.declinedAreaUpdates.Contains(areaType))
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

        private void reloadHideoutRequirements(HideoutClass? hideout)
        {
            if (hideout == null)
                return;
            foreach (AreaData areaData in hideout.AreaDatas)
                this.reloadRequirements(areaData);

            if (Globals.Debug)
                LogHelper.LogInfo("Reloaded hideout requirements.");
        }

        private void reloadRequirements(AreaData data)
        {
            if (TarkovApplication.Exist(out TarkovApplication? tarkovApplication))
            {
                tarkovApplication.HideoutControllerAccess.UnloadHideout();
                tarkovApplication.RecreateCurrentBackend();
            }
        }

        private void removeRequirements(AreaData data)
        {
            if (Globals.RemoveAreaRequirements == false
             && Globals.RemoveCurrencyRequirements == false
             && Globals.RemoveItemRequirements == false
             && Globals.RemoveSkillRequirements == false
             && Globals.RemoveTraderRequirements == false)
                return;
            List<Requirement> requirementsToRemove = [];
            RelatedRequirements requirements = data.NextStage.Requirements;
            foreach (Requirement requirement in requirements)
            {
                if (Globals.RemoveAreaRequirements && requirement is AreaRequirement areaRequirement)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveCurrencyRequirements && requirement is ItemRequirement itemRequirement && itemRequirement.Item is MoneyItemClass)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveItemRequirements && requirement is ItemRequirement itemRequirement2 && itemRequirement2.Item is not MoneyItemClass)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveSkillRequirements && requirement is SkillRequirement skillRequirement)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveTraderRequirements && requirement is TraderUnlockRequirement traderUnlockRequirement
                                                          || requirement is TraderLoyaltyRequirement traderLoyaltyRequirement)
                    requirementsToRemove.Add(requirement);
            }
            if (requirementsToRemove.Count > 0)
            {
                foreach (Requirement requirement in requirementsToRemove)
                    requirements.Data.Remove(requirement);
                data.DecideStatus(data.CurrentLevel);
            }
        }

        private void showDialogWindow(string description, Action acceptAction, Action cancelAction)
        {
            if (this.inDialog)
                return;
            acceptAction += () =>
            {
                this.inDialog = false;
            };
            cancelAction += () =>
            {
                this.inDialog = false;
            };
            string? caption = null;
            int time = 0;
            bool forceShow = false;
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft;
            var handle = ItemUiContext.Instance.ShowMessageWindow(description, acceptAction, cancelAction, caption, time, forceShow, alignment);
            this.inDialog = true;
        }
    }
}
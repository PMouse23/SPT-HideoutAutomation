using Comfort.Common;
using EFT;
using EFT.Hideout;
using EFT.InventoryLogic;
using EFT.UI;
using HideoutAutomation.Construction.Requests;
using HideoutAutomation.Helpers;
using HideoutAutomation.HIP;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#nullable enable

namespace HideoutAutomation.MonoBehaviours
{
    internal class UpdateMonoBehaviour : MonoBehaviour
    {
        private static readonly EAreaStatus[] interactableStatuses =
        [
            EAreaStatus.LockedToConstruct,
            EAreaStatus.ReadyToConstruct,
            EAreaStatus.LockedToUpgrade,
            EAreaStatus.ReadyToUpgrade
        ];

        private readonly List<EAreaType> declinedAreaUpdates = new List<EAreaType>();
        private readonly List<HIPContributionCandidate> hipPendingCandidates = [];
        private CancellationToken? cancellationToken;
        private CancellationTokenSource? cancellationTokenSource;
        private bool didInvestigate = false;
        private bool hipCheckContributionCycle = false;
        private bool hipContributionDeniedThisCycle = false;
        private bool hipContributionScanCompleted = false;
        private InventoryController? hipWatchedInventory;
        private float hipNextInventoryCheck = 0f;
        private float hipLastStashChange = -1f;

        // Item id -> how many of that stack an earlier area of the same scan already claimed
        private readonly Dictionary<string, int> hipReservedItems = new Dictionary<string, int>();
        private bool inDialog = false;
        private bool hipContributionInProgress;
        private bool upgradeInProgress;
        private readonly RequirementReloadPrompt requirementReloadPrompt = new RequirementReloadPrompt();
        private bool? lastRemoveAreaRequirements = null;
        private bool? lastRemoveCurrencyRequirements = null;
        private bool? lastRemoveItemRequirements = null;
        private bool? lastRemoveSkillRequirements = null;
        private bool? lastRemoveTraderRequirements = null;
        private DateTime? lastRun = null;
        private List<SimpleStashPanel> simpleStashPanels = [];

        public void Start()
        {
            this.startCoroutine();
        }

        public void Update()
        {
            bool hasRaidLoaded = RaidHelper.HasRaidLoaded();
            if (hasRaidLoaded)
            {
                this.hipCheckContributionCycle = true;
                this.stopCoroutines();
                return;
            }

            if (this.hipCheckContributionCycle)
            {
                this.hipCheckContributionCycle = false;
                this.hideoutInProgressResetContributionConfirmationCycle();
            }

            this.hideoutInProgressWatchStash();

            if (this.cancellationToken?.IsCancellationRequested == true)
                this.startCoroutine();

            bool shouldInvestigate = Globals.Debug && Globals.InvestigateKeys.IsPressed();
            if (this.didInvestigate != shouldInvestigate)
            {
                this.didInvestigate = shouldInvestigate;
                if (shouldInvestigate)
                    this.investigate();
            }
            if (Globals.ResetDeclinedAreaUpdates.IsPressed() == false)
                return;
            if (RaidHelper.HasRaidLoaded())
                return;
            if (this.lastRun == null)
                return;
            if (Globals.Debug)
                this.investigate();
            this.stopCoroutines();

            this.declinedAreaUpdates.Clear();
            this.requirementReloadPrompt.Reset();
            this.hideoutInProgressResetContributionConfirmationCycle();
            LogHelper.LogInfoWithNotification($"Declined Area(s) reset.");

            this.startCoroutine();
        }

        internal void AddSimpleStashPanel(SimpleStashPanel simpleStashPanel)
        {
            if (!this.simpleStashPanels.Contains(simpleStashPanel))
                this.simpleStashPanels.Add(simpleStashPanel);
        }

        internal void RemoveSimpleStashPanel(SimpleStashPanel simpleStashPanel)
        {
            this.simpleStashPanels.Remove(simpleStashPanel);
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

        private static void forceScrollRefresh(ScrollRect? scroll, float verticalNormalizedPosition)
        {
            if (scroll == null)
                return;

            scroll.verticalNormalizedPosition = verticalNormalizedPosition;
        }

        private static ScrollRect? getStashScroll(SimpleStashPanel panel)
        {
            FieldInfo field = typeof(SimpleStashPanel)
                .GetField("_stashScroll", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(panel) as ScrollRect;
        }

        private bool CanFindAllItems(IEnumerable<string> itemIds)
        {
            FindItemsRequest findItemsRequest = new FindItemsRequest()
            {
                itemIds = itemIds.ToArray()
            };
            try
            {
                string response = RequestHandler.PutJson("/hideoutautomation/CanFindAllItems", JsonConvert.SerializeObject(findItemsRequest));
                return JsonConvert.DeserializeObject<bool>(response);
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception);
                return false;
            }
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
                if (this.hipContributionInProgress || this.upgradeInProgress || this.inDialog)
                    continue;
                if (RaidHelper.HasRaidLoaded() == false)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Not in a raid.");
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Handle hideout upgrades.");
                    HideoutRepresentation hideout = Singleton<HideoutRepresentation>.Instance;
                    if (hideout == null)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfo($"hideout == null");
                        yield return new WaitForSeconds(0.5f);
                    }
                    int requirementSettings = getRequirementSettings();
                    if (this.hasRequirementSettingChanged() && this.requirementReloadPrompt.ShouldPrompt(requirementSettings))
                    {
                        this.showDialogWindow("One of the requirement settings has been changed. Your game needs to be recreated from the backend. Do this now?", () =>
                        {
                            this.reloadHideoutRequirements(hideout);
                        }, () =>
                        {
                            this.requirementReloadPrompt.Defer(requirementSettings);
                            LogHelper.LogInfoWithNotification("Requirement reload deferred. Restart the game or press the reset shortcut to ask again.");
                        });
                        yield return new WaitUntil(() => !this.inDialog && !this.upgradeInProgress);
                        continue; // Re-read the singleton after a backend recreation.
                    }
                    if (hideout != null && hideout.AreaDatas.Count > 0)
                    {
                        bool isHIPEnabled = Globals.IsHideoutInProgress
                            && Globals.IsHideoutInProgressSupportEnabled;
                        bool shouldScanHIPContributions = isHIPEnabled
                            && Globals.EnableHideoutInProgressConfirmation
                            && this.hipContributionScanCompleted == false
                            && this.hipContributionDeniedThisCycle == false;

                        // Each scan starts fresh, so an area is never listed twice and a stack is never offered to two areas
                        this.hipReservedItems.Clear();
                        if (shouldScanHIPContributions)
                            this.hipPendingCandidates.Clear();

                        foreach (AreaData data in hideout.AreaDatas)
                        {
                            if (this.cancellationTokenSource.IsCancellationRequested)
                                break;
                            EAreaStatus status = data.Status;
                            EAreaType areaType = data.Template.Type;
                            string name = data.Template.Name;
                            if (Globals.Debug)
                                LogHelper.LogInfo($"EAreaType: {areaType}, Status={status}");
                            List<Requirement> requirements = this.removeRequirements(data);
                            bool canProcessHIPContribution = isHIPEnabled
                                && interactableStatuses.Contains(status)
                                && this.hideoutInProgressRequirementsAreMet(requirements);
                            if (canProcessHIPContribution)
                            {
                                if (Globals.EnableHideoutInProgressConfirmation)
                                {
                                    if (shouldScanHIPContributions)
                                    {
                                        HIPContributionCandidate? candidate = this.hideoutInProgressCreateContributionCandidate(hideout, data, areaType, requirements);
                                        if (candidate != null)
                                            this.hipPendingCandidates.Add(candidate);
                                    }
                                }
                                else
                                {
                                    HIPContributionCandidate? candidate = this.hideoutInProgressCreateContributionCandidate(hideout, data, areaType, requirements);
                                    if (candidate != null)
                                    {
                                        Task contribution = this.hideoutInProgressExecuteContributionCandidates(new[] { candidate });
                                        yield return new WaitUntil(() => contribution.IsCompleted);
                                    }
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                            switch (status)
                            {
                                case EAreaStatus.ReadyToConstruct:
                                case EAreaStatus.ReadyToUpgrade:
                                    if (this.isAllowedToConstuctOrUpdate(data, areaType, status))
                                    {
                                        Action upgradeAction = () => { _ = this.upgradeAreaAsync(data, status); };
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
                                        yield return new WaitUntil(() => !this.inDialog && !this.upgradeInProgress);
                                    }
                                    break;
                                case EAreaStatus.ReadyToInstallConstruct:
                                case EAreaStatus.ReadyToInstallUpgrade:
                                    if (Globals.AutoInstall)
                                    {
                                        Task upgrade = this.upgradeAreaAsync(data, status);
                                        yield return new WaitUntil(() => upgrade.IsCompleted);
                                    }
                                    break;
                            }
                        }

                        if (shouldScanHIPContributions)
                        {
                            this.hipContributionScanCompleted = true;
                            if (Globals.Debug)
                                LogHelper.LogInfo($"(HIP): scan complete. Candidates={this.hipPendingCandidates.Count}");
                        }

                        this.tryShowHIPContributionConfirmation();
                    }
                }
            }
        }

        private int getItemCount(string templateId, bool isSpawnedInSession)
        {
            HideoutRepresentation hideout = Singleton<HideoutRepresentation>.Instance;
            if (hideout == null || hideout._inventoryController == null)
                return 0;
            int count = 0;
            IEnumerable<Item> items = hideout._inventoryController.Inventory.GetAllItemByTemplate(templateId);
            foreach (Item item in items)
            {
                if (isSpawnedInSession == false || item.SpawnedInSession == true)
                    count += item.StackObjectsCount;
            }
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
                        int inventory = this.getItemCount(itemRequirement.TemplateId, itemRequirement.IsSpawnedInSession);
                        if (count > 0)
                        {
                            itemRequirements += $"{Environment.NewLine}{addSpacesAndFixCount(count, 7)} {addSpacesAndFixCount(inventory, 8)} {itemName}";
                            hasItemRequirements = true;
                        }
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

        private bool hasRequirementSettingChanged()
        {
            if (this.lastRemoveCurrencyRequirements == null)
            {
                this.lastRemoveAreaRequirements = Globals.RemoveAreaRequirements;
                this.lastRemoveCurrencyRequirements = Globals.RemoveCurrencyRequirements;
                this.lastRemoveItemRequirements = Globals.RemoveItemRequirements;
                this.lastRemoveSkillRequirements = Globals.RemoveSkillRequirements;
                this.lastRemoveTraderRequirements = Globals.RemoveTraderRequirements;
                return false;
            }
            return this.lastRemoveAreaRequirements != Globals.RemoveAreaRequirements
                || this.lastRemoveCurrencyRequirements != Globals.RemoveCurrencyRequirements
                || this.lastRemoveItemRequirements != Globals.RemoveItemRequirements
                || this.lastRemoveSkillRequirements != Globals.RemoveSkillRequirements
                || this.lastRemoveTraderRequirements != Globals.RemoveTraderRequirements;
        }

        private static int getRequirementSettings()
        {
            return (Globals.RemoveAreaRequirements ? 1 : 0)
                | (Globals.RemoveCurrencyRequirements ? 2 : 0)
                | (Globals.RemoveItemRequirements ? 4 : 0)
                | (Globals.RemoveSkillRequirements ? 8 : 0)
                | (Globals.RemoveTraderRequirements ? 16 : 0);
        }

        private void investigate()
        {
            TimeSpan? dtm = DateTime.Now - this.lastRun;
            LogHelper.LogInfoWithNotification($"HA: Last run: {dtm?.Seconds ?? -1} seconds ago.");
            if (RaidHelper.HasRaidLoaded())
                LogHelper.LogErrorWithNotification("HA: Appears to have raid loaded");
            if (this.cancellationToken?.IsCancellationRequested == true)
                LogHelper.LogErrorWithNotification("HA: CancellationRequested");
            HideoutRepresentation hideout = Singleton<HideoutRepresentation>.Instance;
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
            {
                if (Globals.IsHideoutInProgress && requirement is ItemRequirement itemRequirement && itemRequirement.Fulfilled == false)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"blocked: IsHideoutInProgress, ClassType={requirement.GetType()} Fulfilled={requirement.Fulfilled} Type={requirement.Type}, BaseCount={itemRequirement.BaseCount}");
                    return false;
                }
                else if (this.isAllowToHandover(requirement) == false)
                    return false;
            }
            return true;
        }

        private async Task upgradeAreaAsync(AreaData data, EAreaStatus expectedStatus)
        {
            if (this.upgradeInProgress || this.hipContributionInProgress || RaidHelper.HasRaidLoaded() || data.Status != expectedStatus)
                return;
            if ((expectedStatus == EAreaStatus.ReadyToConstruct || expectedStatus == EAreaStatus.ReadyToUpgrade)
                && !this.isAllowedToConstuctOrUpdate(data, data.Template.Type, expectedStatus))
                return;
            if ((expectedStatus == EAreaStatus.ReadyToInstallConstruct || expectedStatus == EAreaStatus.ReadyToInstallUpgrade)
                && !Globals.AutoInstall)
                return;
            this.upgradeInProgress = true;
            try
            {
                await data.UpgradeAction();
                bool installing = expectedStatus == EAreaStatus.ReadyToInstallConstruct || expectedStatus == EAreaStatus.ReadyToInstallUpgrade;
                LogHelper.LogInfoWithNotification($"{data.Template.Name} upgrade {(installing ? "installed" : "started")}.");
            }
            catch (Exception exception)
            {
                LogHelper.LogExceptionToConsole(exception);
            }
            finally
            {
                this.upgradeInProgress = false;
            }
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
            if (item is not Money moneyItemClass)
                return false;
            int itemCount = this.getItemCount(item.TemplateId, item.SpawnedInSession);
            handoverValue = (int)(handoverValue * Globals.ThresholdCurrencyHandover);
            if (Globals.Debug)
                LogHelper.LogInfo($"count: {itemCount}, expected: {handoverValue}");
            return itemCount < handoverValue;
        }

        private IEnumerator refreshSimpleStashPanels()
        {
            //HACK finally found a way to hard refresh the draw of SimpleStashPanel.
            foreach (SimpleStashPanel simpleStashPanel in this.simpleStashPanels)
            {
                ScrollRect? scroll = getStashScroll(simpleStashPanel);
                float originalPosition = scroll?.verticalNormalizedPosition ?? 1.0f;
                yield return null; //EndOfFrame
                forceScrollRefresh(scroll, Mathf.Clamp01(originalPosition - 0.1f));
                yield return null; //EndOfFrame
                forceScrollRefresh(scroll, Mathf.Clamp01(originalPosition + 0.2f));
                yield return null; //EndOfFrame
                forceScrollRefresh(scroll, originalPosition);
            }
        }

        private void reloadHideoutRequirements(HideoutRepresentation? hideout)
        {
            this.requirementReloadPrompt.Reset();
            this.lastRemoveAreaRequirements = null;
            this.lastRemoveCurrencyRequirements = null;
            this.lastRemoveItemRequirements = null;
            this.lastRemoveSkillRequirements = null;
            this.lastRemoveTraderRequirements = null;

            if (hideout == null)
                return;
            _ = this.reloadRequirements();
        }

        private async Task reloadRequirements()
        {
            if (this.upgradeInProgress || this.hipContributionInProgress || RaidHelper.HasRaidLoaded())
                return;
            this.upgradeInProgress = true;
            try
            {
                if (TarkovApplication.Exist(out TarkovApplication? tarkovApplication))
                {
                    await tarkovApplication.HideoutControllerAccess.UnloadHideout();
                    await tarkovApplication.RecreateCurrentBackend();
                    LogHelper.LogInfo("Reloaded hideout requirements.");
                }
            }
            catch (Exception exception)
            {
                LogHelper.LogExceptionToConsole(exception);
            }
            finally
            {
                this.upgradeInProgress = false;
            }
        }

        private List<Requirement> removeRequirements(AreaData data)
        {
            List<Requirement> requirementsToRemove = [];
            RelatedRequirements? requirements = data.NextStage?.Requirements;
            if (requirements == null)
                return requirementsToRemove;

            if (Globals.RemoveAreaRequirements == false
             && Globals.RemoveCurrencyRequirements == false
             && Globals.RemoveItemRequirements == false
             && Globals.RemoveSkillRequirements == false
             && Globals.RemoveTraderRequirements == false)
                return requirements.Data;

            foreach (Requirement requirement in requirements)
            {
                if (Globals.RemoveAreaRequirements && requirement is AreaRequirement areaRequirement)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveCurrencyRequirements && requirement is ItemRequirement itemRequirement && itemRequirement.Item is Money)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveItemRequirements && requirement is ItemRequirement itemRequirement2 && itemRequirement2.Item is not Money)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveSkillRequirements && requirement is SkillRequirement skillRequirement)
                    requirementsToRemove.Add(requirement);
                else if (Globals.RemoveTraderRequirements && (requirement is TraderUnlockRequirement
                                                          || requirement is TraderLoyaltyRequirement))
                    requirementsToRemove.Add(requirement);
            }
            if (requirementsToRemove.Count > 0)
            {
                foreach (Requirement requirement in requirementsToRemove)
                    requirements.Data.Remove(requirement);
                data.DecideStatus(data.CurrentLevel);
            }
            return requirements.Data;
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

        private void startCoroutine()
        {
            this.StartCoroutine(this.coroutine());
            if (Globals.Debug)
                LogHelper.LogInfo($"Started coroutine");
        }

        private void stopCoroutines()
        {
            if (this.lastRun == null)
                return;

            this.StopAllCoroutines();
            this.lastRun = null;
            this.cancellationTokenSource?.Cancel();
            this.StopAllCoroutines();
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification("Stopped coroutine");
        }

        #region HideoutInProgress

        private string hideoutInProgressBuildContributionDescription(IEnumerable<HIPContributionCandidate> candidates)
            => HIPContributionText.BuildPrompt(candidates);

        /// <summary>
        /// Rescans for contributions after the stash changes, instead of only once per raid cycle.
        /// Event driven: nothing runs until an item is added, removed or restacked, and a burst of changes
        /// leads to one rescan once the stash has been quiet. A declined prompt stays declined for the cycle.
        /// </summary>
        private void hideoutInProgressWatchStash()
        {
            float now = Time.unscaledTime;
            if (now >= this.hipNextInventoryCheck)
            {
                this.hipNextInventoryCheck = now + 1f;
                InventoryController? inventory = Singleton<HideoutRepresentation>.Instance?._inventoryController;
                if (!ReferenceEquals(inventory, this.hipWatchedInventory))
                {
                    if (this.hipWatchedInventory != null)
                    {
                        this.hipWatchedInventory.AddItemEvent -= this.hideoutInProgressOnItemAdded;
                        this.hipWatchedInventory.RemoveItemEvent -= this.hideoutInProgressOnItemRemoved;
                        this.hipWatchedInventory.RefreshItemEvent -= this.hideoutInProgressOnItemRefreshed;
                    }
                    this.hipWatchedInventory = inventory;
                    if (inventory != null)
                    {
                        inventory.AddItemEvent += this.hideoutInProgressOnItemAdded;
                        inventory.RemoveItemEvent += this.hideoutInProgressOnItemRemoved;
                        inventory.RefreshItemEvent += this.hideoutInProgressOnItemRefreshed;
                    }
                }
            }

            if (this.hipLastStashChange < 0f
                || now - this.hipLastStashChange < Globals.HideoutInProgressRescanDelay
                || this.hipContributionInProgress
                || this.inDialog)
                return;

            this.hipLastStashChange = -1f;
            if (this.hipContributionScanCompleted && this.hipContributionDeniedThisCycle == false)
            {
                this.hipContributionScanCompleted = false;
                if (Globals.Debug)
                    LogHelper.LogInfo("(HIP): stash changed, rescanning.");
            }
        }

        private void hideoutInProgressOnItemAdded(AddItemEventArgs args) => this.hideoutInProgressMarkStashChanged(args.Status);

        private void hideoutInProgressOnItemRemoved(RemoveItemEventArgs args) => this.hideoutInProgressMarkStashChanged(args.Status);

        private void hideoutInProgressOnItemRefreshed(RefreshItemEventArgs args) => this.hipLastStashChange = Time.unscaledTime;

        private void hideoutInProgressMarkStashChanged(CommandStatus status)
        {
            if (status == CommandStatus.Succeed)
                this.hipLastStashChange = Time.unscaledTime;
        }

        /// <summary>
        /// Calls hip Transferbutton.Contribute() function.
        /// https://github.com/tyfon7/hip/blob/main/src/TransferButton.cs
        /// </summary>
        /// <returns></returns>
        private async Task<bool> hideoutInProgressContribute(AreaData areaData, ItemRequirement[] itemRequirements)
        {
            Type? transferButtonType = Globals.HIPTransferButtonType;
            if (Globals.Debug)
                LogHelper.LogInfo($"(HIP): transferButtonType={transferButtonType}");
            if (transferButtonType == null || Globals.HIPItemRequirementsFieldInfo == null ||
                Globals.HIPAreaDataFieldInfo == null || Globals.HIPContributeMethodInfo == null)
                return false;

            Component? transferButton = null;
            try
            {
                // TransferButton is a Unity component. Creating it with Activator bypasses Unity's lifecycle.
                transferButton = this.gameObject.AddComponent(transferButtonType);
                Globals.HIPItemRequirementsFieldInfo.SetValue(transferButton, itemRequirements);
                Globals.HIPAreaDataFieldInfo.SetValue(transferButton, areaData);
                int countBefore = itemRequirements.Sum(requirement => requirement.BaseCount);
                if (Globals.HIPContributeMethodInfo.Invoke(transferButton, null) is not Task contribution)
                    return false;
                await contribution;
                return itemRequirements.Sum(requirement => requirement.BaseCount) < countBefore;
            }
            catch (Exception exception)
            {
                LogHelper.LogExceptionToConsole(exception);
                return false;
            }
            finally
            {
                if (transferButton != null)
                    UnityEngine.Object.Destroy(transferButton);
            }
        }

        private HIPContributionCandidate? hideoutInProgressCreateContributionCandidate(HideoutRepresentation hideout, AreaData data, EAreaType areaType, List<Requirement> requirements)
        {
            if (Globals.Debug)
                LogHelper.LogInfo($"(HIP): CheckItemRequirements.");

            List<string> itemIds = [];
            List<string> contributionMessages = [];
            List<HIPContributionLine> lines = [];
            ItemRequirement[] itemRequirements = requirements.OfType<ItemRequirement>().Where(r => r.Item is not Money).ToArray();
            foreach (ItemRequirement itemRequirement in itemRequirements)
            {
                var contributions = hideout.GetItemReferences([itemRequirement]);
                if (contributions.Count > 0)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"(HIP): contributions.Count={contributions.Count}");
                    int count = 0;
                    foreach (var contribution in contributions)
                    {
                        string itemId = contribution.Item.Id;
                        this.hipReservedItems.TryGetValue(itemId, out int reserved);
                        int available = Math.Min(contribution.Count, this.hideoutInProgressStackSize(contribution.Item) - reserved);
                        if (available <= 0)
                            continue;

                        this.hipReservedItems[itemId] = reserved + available;
                        count += available;
                        string contributionMessage = $"Contribute {available} of {contribution.Item.LocalizedName()}.";
                        itemIds.Add(itemId);
                        contributionMessages.Add(contributionMessage);
                    }
                    if (count > 0)
                        lines.Add(new HIPContributionLine(itemRequirement.Item.LocalizedName(), count, itemRequirement.BaseCount));
                }
            }

            if (itemIds.Count == 0 || this.CanFindAllItems(itemIds) == false)
                return null;

            return new HIPContributionCandidate(data, areaType, itemRequirements, itemIds, contributionMessages, lines);
        }

        private int hideoutInProgressStackSize(Item item) => Math.Max(1, item.StackObjectsCount);

        private async Task hideoutInProgressExecuteContributionCandidates(IEnumerable<HIPContributionCandidate> candidates)
        {
            if (this.hipContributionInProgress || this.upgradeInProgress || RaidHelper.HasRaidLoaded())
                return;
            this.hipContributionInProgress = true;
            bool contributed = false;
            List<HIPContributionCandidate> donated = [];
            try
            {
                foreach (HIPContributionCandidate candidate in candidates)
                {
                    if (RaidHelper.HasRaidLoaded())
                        break;
                    var hideout = Singleton<HideoutRepresentation>.Instance;
                    if (hideout == null || !hideout.AreaDatas.Contains(candidate.AreaData) ||
                        !interactableStatuses.Contains(candidate.AreaData.Status))
                        continue;
                    if (this.CanFindAllItems(candidate.ItemIds) == false)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfo($"(HIP): stale candidate skipped for area {candidate.AreaType}.");
                        continue;
                    }

                    if (await this.hideoutInProgressContribute(candidate.AreaData, candidate.ItemRequirements))
                    {
                        contributed = true;
                        donated.Add(candidate);
                        foreach (string contributionMessage in candidate.ContributionMessages)
                            LogHelper.LogInfo(contributionMessage);
                    }
                }
                if (donated.Count > 0)
                    LogHelper.LogInfoWithNotification(HIPContributionText.BuildDonated(donated));
                if (contributed && !RaidHelper.HasRaidLoaded())
                    this.StartCoroutine(this.refreshSimpleStashPanels());
            }
            catch (Exception exception)
            {
                LogHelper.LogExceptionToConsole(exception);
            }
            finally
            {
                this.hipContributionInProgress = false;
            }
        }

        private bool hideoutInProgressRequirementsAreMet(List<Requirement> requirements)
        {
            if (Globals.OnlyContributeWhenAreaRequirementsAreMet == false)
                return true;
            return requirements.OfType<AreaRequirement>().Any(r => r.Fulfilled == false) == false;
        }

        private void hideoutInProgressResetContributionConfirmationCycle()
        {
            this.hipContributionDeniedThisCycle = false;
            this.hipContributionScanCompleted = false;
            this.hipPendingCandidates.Clear();
            if (Globals.Debug)
                LogHelper.LogInfo("(HIP): confirmation cycle reset.");
        }

        private void tryShowHIPContributionConfirmation()
        {
            if (Globals.EnableHideoutInProgressConfirmation == false
                || this.hipContributionDeniedThisCycle
                || this.inDialog
                || this.hipPendingCandidates.Count == 0)
                return;

            string description = this.hideoutInProgressBuildContributionDescription(this.hipPendingCandidates);
            if (string.IsNullOrWhiteSpace(description))
                return;

            HIPContributionCandidate[] candidates = this.hipPendingCandidates.ToArray();
            this.hipPendingCandidates.Clear();

            this.showDialogWindow(description, () =>
            {
                if (RaidHelper.HasRaidLoaded())
                    return;

                if (Globals.Debug)
                    LogHelper.LogInfo("(HIP): contribution prompt accepted.");
                _ = this.hideoutInProgressExecuteContributionCandidates(candidates);
            }, () =>
            {
                this.hipContributionDeniedThisCycle = true;
                if (Globals.Debug)
                    LogHelper.LogInfo("(HIP): contribution prompt denied.");
            });
        }

        #endregion HideoutInProgress
    }
}

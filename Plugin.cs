using BepInEx;
using BepInEx.Configuration;
using HideoutAutomation.Helpers;
using HideoutAutomation.Patches.Application;
using System;
using UnityEngine;

namespace HideoutAutomation
{
    [BepInPlugin("com.KnotScripts.HideoutAutomation", "HideoutAutomation", VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.0.0";

        private ConfigEntry<bool> autoConstruct;
        private ConfigEntry<bool> autoInstall;
        private ConfigEntry<bool> autoUpgrade;
        private ConfigEntry<bool> debug;
        private ConfigEntry<bool> enableHideoutInProgressSupport;
        private ConfigEntry<bool> onlyContributeWhenAreaRequirementsAreMet;
        private ConfigEntry<bool> removeAreaRequirements;
        private ConfigEntry<bool> removeCurrencyRequirements;
        private ConfigEntry<bool> removeItemRequirements;
        private ConfigEntry<bool> removeSkillRequirements;
        private ConfigEntry<bool> removeTraderRequirements;
        private ConfigEntry<KeyboardShortcut> resetDeclinedAreaUpdates;
        private ConfigEntry<double> thresholdCurrencyHandover;
        private ConfigEntry<bool> useDialogWindow;

        private void Awake()
        {
            try
            {
                LogHelper.Logger = this.Logger;

                this.enablePatches();
                this.setConfigurables();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception);
            }
        }

        private void enablePatches()
        {
            new TarkovApplication_Init().Enable();
        }

        private void global_SettingChanged(object sender, EventArgs e)
        {
            this.setGlobalSettings();
        }

        private void setConfigurables()
        {
            this.debug = this.Config.Bind("General", "Debug", false, "Debug");
            this.debug.SettingChanged += this.global_SettingChanged;

            this.autoConstruct = this.Config.Bind("Automation", "AutoConstruct", true, "Auto construct areas when they're available.");
            this.autoConstruct.SettingChanged += this.global_SettingChanged;

            this.autoInstall = this.Config.Bind("Automation", "AutoInstall", true, "Auto install areas when construction of update is complete.");
            this.autoInstall.SettingChanged += this.global_SettingChanged;

            this.autoUpgrade = this.Config.Bind("Automation", "AutoUpgrade", true, "Auto update areas when they're available.");
            this.autoUpgrade.SettingChanged += this.global_SettingChanged;

            this.resetDeclinedAreaUpdates = this.Config.Bind("Upgrading", "RestartUpgradingAreas", new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl), "Keys to press to reset the (in memory) declined area upgrades and restarts the couritine. So the DialogWindow will pop up again for these areas.");
            this.resetDeclinedAreaUpdates.SettingChanged += this.global_SettingChanged;

            this.thresholdCurrencyHandover = this.Config.Bind("Upgrading", "ThresholdCurrencyHandover", 1.5, "Threshold for the number of times you want to have the currency amount before spending it.");
            this.thresholdCurrencyHandover.SettingChanged += this.global_SettingChanged;

            this.useDialogWindow = this.Config.Bind("Upgrading", "UseDialogWindow", true, "Always show a dialog window to accept the hideout upgrade.");
            this.useDialogWindow.SettingChanged += this.global_SettingChanged;

            this.removeAreaRequirements = this.Config.Bind("Payment", "RemoveAreaRequirements", false, "Remove the area requirements for construction and upgrades.");
            this.removeAreaRequirements.SettingChanged += this.global_SettingChanged;

            this.removeCurrencyRequirements = this.Config.Bind("Payment", "RemoveCurrencyRequirements", false, "Remove the currency requirements for construction and upgrades.");
            this.removeCurrencyRequirements.SettingChanged += this.global_SettingChanged;

            this.removeItemRequirements = this.Config.Bind("Payment", "RemoveItemRequirements", false, "Remove the item requirements for construction and upgrades.");
            this.removeItemRequirements.SettingChanged += this.global_SettingChanged;

            this.removeSkillRequirements = this.Config.Bind("Payment", "RemoveSkillRequirements", false, "Remove the skill requirements for construction and upgrades.");
            this.removeSkillRequirements.SettingChanged += this.global_SettingChanged;

            this.removeTraderRequirements = this.Config.Bind("Payment", "RemoveTraderRequirements", false, "Remove the trader requirements for construction and upgrades.");
            this.removeTraderRequirements.SettingChanged += this.global_SettingChanged;

            this.enableHideoutInProgressSupport = this.Config.Bind("HideoutInProgress", "EnableHideoutInProgressSupport", true, "Experimental (HIP mod support). Disable when problems occur.");
            this.enableHideoutInProgressSupport.SettingChanged += this.global_SettingChanged;

            this.onlyContributeWhenAreaRequirementsAreMet = this.Config.Bind("HideoutInProgress", "OnlyContributeWhenAreaRequirementsAreMet", true, "(HIP mod support) Only contribute when area requirements are met.");
            this.onlyContributeWhenAreaRequirementsAreMet.SettingChanged += this.global_SettingChanged;

            this.setGlobalSettings();
        }

        private void setGlobalSettings()
        {
            Globals.Debug = this.debug.Value;
            Globals.AutoConstruct = this.autoConstruct.Value;
            Globals.AutoInstall = this.autoInstall.Value;
            Globals.AutoUpgrade = this.autoUpgrade.Value;
            Globals.RemoveAreaRequirements = this.removeAreaRequirements.Value;
            Globals.RemoveCurrencyRequirements = this.removeCurrencyRequirements.Value;
            Globals.RemoveItemRequirements = this.removeItemRequirements.Value;
            Globals.RemoveSkillRequirements = this.removeSkillRequirements.Value;
            Globals.RemoveTraderRequirements = this.removeTraderRequirements.Value;
            Globals.ResetDeclinedAreaUpdates = this.resetDeclinedAreaUpdates.Value;
            Globals.ThresholdCurrencyHandover = this.thresholdCurrencyHandover.Value;
            Globals.UseDialogWindow = this.useDialogWindow.Value;
            Globals.OnlyContributeWhenAreaRequirementsAreMet = this.onlyContributeWhenAreaRequirementsAreMet.Value;
            Globals.IsHideoutInProgressSupportEnabled = this.enableHideoutInProgressSupport.Value;
        }
    }
}
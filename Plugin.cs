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
        public const string VERSION = "0.0.1";

        private ConfigEntry<bool> autoConstruct;
        private ConfigEntry<bool> autoInstall;
        private ConfigEntry<bool> autoUpgrade;
        private ConfigEntry<bool> debug;
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

            this.setGlobalSettings();
        }

        private void setGlobalSettings()
        {
            Globals.Debug = this.debug.Value;
            Globals.AutoConstruct = this.autoConstruct.Value;
            Globals.AutoInstall = this.autoInstall.Value;
            Globals.AutoUpgrade = this.autoUpgrade.Value;
            Globals.ResetDeclinedAreaUpdates = this.resetDeclinedAreaUpdates.Value;
            Globals.ThresholdCurrencyHandover = this.thresholdCurrencyHandover.Value;
            Globals.UseDialogWindow = this.useDialogWindow.Value;
        }
    }
}
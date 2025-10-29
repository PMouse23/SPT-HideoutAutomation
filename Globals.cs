#nullable enable

using BepInEx.Configuration;
using System;
using System.Reflection;
using UnityEngine;

internal static class Globals
{
    public static bool AutoConstruct = true;
    public static bool AutoInstall = true;
    public static bool AutoUpgrade = true;
    public static bool Debug = false;
    public static KeyboardShortcut InvestigateKeys = new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl);
    public static bool OnlyContributeWhenAreaRequirementsAreMet = true;
    public static bool RemoveAreaRequirements = false;
    public static bool RemoveCurrencyRequirements = false;
    public static bool RemoveItemRequirements = false;
    public static bool RemoveSkillRequirements = false;
    public static bool RemoveTraderRequirements = false;
    public static KeyboardShortcut ResetDeclinedAreaUpdates = new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl);
    public static double ThresholdCurrencyHandover = 1.5;
    public static bool UseDialogWindow = true;
    #region HideoutInProgress
    public static FieldInfo? HIPAreaDataFieldInfo;
    public static MethodInfo? HIPContributeMethodInfo;
    public static FieldInfo? HIPItemRequirementsFieldInfo;
    public static Type? HIPTransferButtonType;
    public static bool IsHideoutInProgress = false;
    #endregion HideoutInProgress
}
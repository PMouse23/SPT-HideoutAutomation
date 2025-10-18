#nullable enable

using BepInEx.Configuration;
using UnityEngine;

internal static class Globals
{
    public static bool AutoConstruct = true;
    public static bool AutoInstall = true;
    public static bool AutoUpgrade = true;
    public static bool Debug = false;
    public static KeyboardShortcut InvestigateKeys = new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl);
    public static KeyboardShortcut ResetDeclinedAreaUpdates = new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl);
    public static double ThresholdCurrencyHandover = 1.5;
    public static bool UseDialogWindow = true;
}
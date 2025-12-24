using EFT.Hideout;
using EFT.UI;
using HideoutAutomation.Helpers;
using System.Reflection;

namespace HideoutAutomation.Extensions
{
    internal static class ProduceViewExtensions
    {
        public static void UpdateStackButton(this ProduceView produceView, int stacked)
        {
            FieldInfo startButtonField = produceView.GetType().GetField("_startButton", BindingFlags.NonPublic | BindingFlags.Instance);
            if (startButtonField == null)
                LogHelper.LogErrorWithNotification("_startButton field not found");
            object startButtonObj = startButtonField.GetValue(produceView);
            if (startButtonObj is DefaultUIButton startButton)
                startButton.SetHeaderText($"Stack ({stacked})");
        }
    }
}
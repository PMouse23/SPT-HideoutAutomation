namespace HideoutAutomation.Helpers
{
    internal sealed class RequirementReloadPrompt
    {
        private int? deferred;
        public bool ShouldPrompt(int settings) => deferred != settings;
        public void Defer(int settings) => deferred = settings;
        public void Reset() => deferred = null;
    }
}

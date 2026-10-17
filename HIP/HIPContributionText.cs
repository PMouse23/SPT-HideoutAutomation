using System.Collections.Generic;
using System.Linq;
using System.Text;

#nullable enable

namespace HideoutAutomation.HIP
{
    /// <summary>
    /// Texts for the contribution prompt and the notification after contributing.
    /// Items are grouped per area, stacks of the same item are summed, and the prompt shows what is still missing.
    /// </summary>
    internal static class HIPContributionText
    {
        private const string Muted = "#9a9a8c";
        private const string Done = "#8fbf6a";
        private const string Missing = "#d9a441";

        internal static string BuildPrompt(IEnumerable<HIPContributionCandidate> candidates)
        {
            List<HIPContributionCandidate> list = candidates.Where(c => c.Lines.Count > 0).ToList();
            if (list.Count == 0)
                return string.Empty;

            int total = list.Sum(c => c.Lines.Sum(l => l.Count));
            StringBuilder text = new StringBuilder();
            text.Append("<b>Donate to the hideout?</b>\n");
            text.Append($"<size=85%><color={Muted}>{Plural(total, "item")} for {Plural(list.Count, "area")}</color></size>\n");

            foreach (HIPContributionCandidate candidate in list)
            {
                text.Append('\n').Append("<b>").Append(candidate.AreaData.Template.Name).Append("</b>");
                int level = candidate.AreaData.CurrentLevel + 1;
                if (level > 0)
                    text.Append($" <color={Muted}>level {level}</color>");
                text.Append('\n');

                foreach (HIPContributionLine line in candidate.Lines)
                {
                    text.Append("   ").Append(line.Name).Append("  <b>x").Append(line.Count).Append("</b>");
                    if (line.Needed > 0)
                    {
                        int left = line.Needed - line.Count;
                        text.Append(left <= 0
                            ? $"   <color={Done}>done</color>"
                            : $"   <color={Missing}>{left} still needed</color>");
                    }
                    text.Append('\n');
                }
            }

            return text.ToString().TrimEnd('\n');
        }

        internal static string BuildDonated(IEnumerable<HIPContributionCandidate> contributed)
        {
            Dictionary<string, int> items = new Dictionary<string, int>();
            foreach (HIPContributionLine line in contributed.SelectMany(c => c.Lines))
            {
                items.TryGetValue(line.Name, out int count);
                items[line.Name] = count + line.Count;
            }

            if (items.Count == 0)
                return string.Empty;

            string list = string.Join(", ", items.OrderByDescending(p => p.Value).Select(p => $"{p.Key} x{p.Value}"));
            return $"Donated {Plural(items.Values.Sum(), "item")} to the hideout: {list}";
        }

        private static string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";
    }
}

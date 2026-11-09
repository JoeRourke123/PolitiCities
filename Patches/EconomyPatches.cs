using HarmonyLib;
using PolitiCities.Politics;

namespace PolitiCities.Patches
{
    [HarmonyPatch(typeof(EconomyManager), nameof(EconomyManager.AddPrivateIncome), new[] { typeof(int), typeof(Service), typeof(SubService), typeof(Level), typeof(int) })]
    public static class StimulusIncomePatch
    {
        public const string PolicyId = "stimulusPackage";
        public const float IncomeMultiplier = 1.10f;

        public static void Prefix(ref int amount)
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null || amount <= 0)
            {
                return;
            }
            float progress;
            if (s.PolicyProgress.TryGetValue(PolicyId, out progress) && progress >= 1f)
            {
                amount = (int)(amount * IncomeMultiplier);
            }
        }
    }
}

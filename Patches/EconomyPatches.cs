using HarmonyLib;
using PolitiCities.Politics;

namespace PolitiCities.Patches
{
    [HarmonyPatch(typeof(EconomyManager), nameof(EconomyManager.AddPrivateIncome), new[] { typeof(int), typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(ItemClass.Level), typeof(int) })]
    public static class StimulusIncomePatch
    {
        public const string PolicyId = "stimulusPackage";
        public const float IncomeMultiplier = 1.10f;

        public static void Prefix(ref int amount)
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null || amount <= 0 || !Settings.EnablePolitics)
            {
                return;
            }
            float multiplier = PoliticalEngine.CrisisIncomeMultiplier(s)
                * PoliticalEngine.ForeignTradeMultiplier(s)
                * PoliticalEngine.PolicyIncomeMultiplier(s);
            amount = (int)(amount * multiplier);
        }
    }

    [HarmonyPatch(typeof(EconomyManager), nameof(EconomyManager.GetBudget), new[] { typeof(ItemClass.Service), typeof(ItemClass.SubService), typeof(bool) })]
    public static class MinistryBudgetPatch
    {
        public static void Postfix(ItemClass.Service service, ref int __result)
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null || !Settings.EnablePolitics || __result <= 0)
            {
                return;
            }
            __result = (int)(__result * PoliticalEngine.MinistryMultiplier(s, service.ToString()));
        }
    }
}

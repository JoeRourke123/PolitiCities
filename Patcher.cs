using HarmonyLib;

namespace PolitiCities
{
    public static class Patcher
    {
        private const string HarmonyId = "com.github.joerourke123.politicities";

        private static bool patched;

        public static void PatchAll()
        {
            if (patched)
            {
                return;
            }

            patched = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Patcher).Assembly);
        }

        public static void UnpatchAll()
        {
            if (!patched)
            {
                return;
            }

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
        }
    }
}

using CitiesHarmony.API;
using ICities;

namespace PolitiCities
{
    public class PolitiCitiesMod : IUserMod
    {
        public string Name => "PolitiCities";

        public string Description => "Adds more politics to your Cities Skylines city.";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(Patcher.PatchAll);
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            Settings.BuildOptionsPanel(helper);
        }
    }
}

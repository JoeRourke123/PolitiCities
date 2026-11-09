using ColossalFramework;
using ICities;

namespace PolitiCities
{
    public static class Settings
    {
        private const string SettingsFile = "PolitiCities";

        private static readonly SavedBool s_enablePolitics = new SavedBool("EnablePolitics", SettingsFile, true);
        private static readonly SavedBool s_enforceCouncil = new SavedBool("EnforceCouncilVotes", SettingsFile, false);
        private static readonly SavedBool s_districtElections = new SavedBool("DistrictElections", SettingsFile, true);

        public static bool EnablePolitics
        {
            get { return s_enablePolitics.value; }
        }

        public static bool EnforceCouncilVotes
        {
            get { return s_enforceCouncil.value; }
        }

        public static bool DistrictElectionsEnabled
        {
            get { return s_districtElections.value; }
        }

        public static void BuildOptionsPanel(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("PolitiCities");
            group.AddCheckbox("Enable politics simulation", s_enablePolitics.value, v => s_enablePolitics.value = v);
            group.AddCheckbox("Council must approve city policies", s_enforceCouncil.value, v => s_enforceCouncil.value = v);
            group.AddCheckbox("District elections (locks opposition districts)", s_districtElections.value, v => s_districtElections.value = v);
        }
    }
}

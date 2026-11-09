using ICities;

namespace PolitiCities
{
    public static class Settings
    {
        public static bool EnablePolitics = true;
        public static bool EnforceCouncilVotes;
        public static bool DistrictElectionsEnabled = true;

        public static void BuildOptionsPanel(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("PolitiCities");
            group.AddCheckbox("Enable politics simulation", EnablePolitics, value => EnablePolitics = value);
            group.AddCheckbox("Council must approve city policies", EnforceCouncilVotes, value => EnforceCouncilVotes = value);
            group.AddCheckbox("District elections", DistrictElectionsEnabled, value => DistrictElectionsEnabled = value);
        }
    }
}

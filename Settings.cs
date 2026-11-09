using ICities;

namespace PolitiCities
{
    public static class Settings
    {
        public static bool EnablePolitics = true;

        public static void BuildOptionsPanel(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("PolitiCities");
            group.AddCheckbox("Enable politics simulation", EnablePolitics, value => EnablePolitics = value);
        }
    }
}

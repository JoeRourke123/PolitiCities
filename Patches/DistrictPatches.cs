using HarmonyLib;
using PolitiCities.Politics;
using UnityEngine;

namespace PolitiCities.Patches
{
    [HarmonyPatch(typeof(DistrictManager), nameof(DistrictManager.SetDistrictPolicy))]
    public static class DistrictLockPatch
    {
        public static bool Allow;

        public static bool Prefix(byte district)
        {
            if (Allow)
            {
                return true;
            }
            PoliticalState s = PoliticalData.Current;
            if (s == null || !Settings.DistrictElectionsEnabled)
            {
                return true;
            }
            if (!PoliticalEngine.CanPlayerEditDistrict(s, district))
            {
                Debug.Log("[PolitiCities] District " + district + " is controlled by the opposition; policy change blocked");
                return false;
            }
            return true;
        }
    }
}

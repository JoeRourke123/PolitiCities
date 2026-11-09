using HarmonyLib;
using PolitiCities.Politics;
using UnityEngine;

namespace PolitiCities.Patches
{
    [HarmonyPatch(typeof(DistrictManager), nameof(DistrictManager.SetCityPolicy))]
    public static class CouncilVotePatch
    {
        public static bool Prefix(DistrictPolicies.Policies policy)
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null || !Settings.EnforceCouncilVotes)
            {
                return true;
            }
            bool approved = PoliticalEngine.CouncilVote(s, policy.ToString());
            if (!approved)
            {
                Debug.Log("[PolitiCities] Council rejected city policy " + policy + " (coalition " + s.Council.CoalitionSeats() + "/" + Council.SeatCount + ")");
            }
            return approved;
        }
    }
}

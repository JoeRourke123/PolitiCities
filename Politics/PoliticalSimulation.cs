using System;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace PolitiCities.Politics
{
    public class PoliticalSimulation : ThreadingExtensionBase
    {
        private System.Random random;

        public override void OnAfterSimulationTick()
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null)
            {
                return;
            }
            if (random == null)
            {
                random = new System.Random(s.RandomSeed);
            }

            PoliticalEngine.Tick(s, random, ReadRealApproval());

            if (Settings.DistrictElectionsEnabled && s.TickCounter == 0)
            {
                RunDistrictElections(s);
            }
        }

        private float ReadRealApproval()
        {
            try
            {
                StatisticBase happiness = Singleton<StatisticsManager>.instance.Get(StatisticType.CityHappiness);
                if (happiness != null)
                {
                    return happiness.GetLatestInt32() / 100f;
                }
            }
            catch (Exception)
            {
            }
            return -1f;
        }

        private void RunDistrictElections(PoliticalState s)
        {
            DistrictManager dm = Singleton<DistrictManager>.instance;
            int count = dm.m_districtCount;
            for (int i = 0; i < count; i++)
            {
                int months;
                s.DistrictElectionMonths.TryGetValue(i, out months);
                if (months < PoliticalEngine.MonthsPerDistrictElection)
                {
                    s.DistrictElectionMonths[i] = months + 1;
                    continue;
                }

                District d = dm.m_districts.m_buffer[i];
                var info = new DistrictInfo { Id = i, Happiness = d.m_finalHappiness, Crime = d.m_finalCrimeRate };
                int winner = PoliticalEngine.ResolveDistrictElection(s, info, random);
                s.RulingParty[i] = winner;
                s.DistrictElectionMonths[i] = 0;
                ApplyPlatform(dm, i, Party.Roster[winner]);
                string msg = "District " + dm.GetDistrictName(i) + " elected " + Party.Roster[winner].Name;
                PoliticalEngine.LogEvent(s, msg);
                Debug.Log("[PolitiCities] " + msg);
            }
        }

        public static bool TryFundCampaign(PoliticalState s, long amount)
        {
            if (s == null || amount <= 0)
            {
                return false;
            }
            EconomyManager eco = Singleton<EconomyManager>.instance;
            if (eco == null || eco.InternalCashAmount < amount)
            {
                return false;
            }
            eco.AddResource(EconomyManager.Resource.PolicyCost, -(int)amount, ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Level.None);
            PoliticalEngine.FundCampaign(s, (int)amount);
            PoliticalEngine.LogEvent(s, "Campaign funded with " + amount + " from city funds");
            return true;
        }

        private void ApplyPlatform(DistrictManager dm, int district, Party winner)
        {
            foreach (string policyName in winner.Platform)
            {
                try
                {
                    DistrictPolicies.Policies policy = (DistrictPolicies.Policies)Enum.Parse(typeof(DistrictPolicies.Policies), policyName);
                    dm.SetDistrictPolicy(policy, (byte)district);
                }
                catch (Exception e)
                {
                    Debug.Log("[PolitiCities] Could not apply district policy " + policyName + ": " + e.Message);
                }
            }
        }
    }
}

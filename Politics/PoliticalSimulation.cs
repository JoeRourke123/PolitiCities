using System;
using ColossalFramework;
using ICities;
using UnityEngine;

namespace PolitiCities.Politics
{
    public class PoliticalSimulation : ThreadingExtensionBase
    {
        private Random random;

        public override void OnAfterSimulationTick()
        {
            PoliticalState s = PoliticalData.Current;
            if (s == null)
            {
                return;
            }
            if (random == null)
            {
                random = new Random(s.RandomSeed);
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
                Debug.Log("[PolitiCities] District " + i + " (" + dm.GetDistrictName(i) + ") elected " + Party.Roster[winner].Name);
            }
        }

        private void ApplyPlatform(DistrictManager dm, int district, Party winner)
        {
            foreach (string policyName in winner.Platform)
            {
                try
                {
                    Policies policy = (Policies)Enum.Parse(typeof(Policies), policyName);
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

using System;
using ICities;

namespace PolitiCities.Politics
{
    public class PoliticalSimulation : ThreadingExtensionBase
    {
        public const int TicksPerMonth = 100000;
        public const int MonthsPerElection = 48;
        public const float ImplementationMonths = 3f;
        public const float CampaignDecayPerMonth = 0.9f;

        private Random random;

        public override void OnCreated(IThreading threading)
        {
            base.OnCreated(threading);
        }

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

            s.TickCounter++;
            AdvanceImplementation(s);

            if (s.TickCounter >= TicksPerMonth)
            {
                s.TickCounter = 0;
                MonthlyUpdate(s);
            }
        }

        private void MonthlyUpdate(PoliticalState s)
        {
            DriftVoteShares(s);
            DecayCampaigns(s);

            s.MonthsSinceElection++;
            if (s.MonthsSinceElection >= MonthsPerElection)
            {
                s.MonthsSinceElection = 0;
                s.Council.RecomputeFromShares(s.VoteShare);
            }
        }

        private void DriftVoteShares(PoliticalState s)
        {
            float drift = (s.LeaderApproval - 0.5f) * 0.02f;
            float total = 0f;
            for (int i = 0; i < s.VoteShare.Length; i++)
            {
                float noise = ((float)random.NextDouble() - 0.5f) * 0.04f;
                s.VoteShare[i] = Math.Max(0.01f, s.VoteShare[i] + drift + noise);
                total += s.VoteShare[i];
            }
            for (int i = 0; i < s.VoteShare.Length; i++)
            {
                s.VoteShare[i] /= total;
            }
        }

        private void AdvanceImplementation(PoliticalState s)
        {
            float rate = 1f / (TicksPerMonth * ImplementationMonths) / Math.Max(0.1f, s.AdminCapacity);
            var keys = new System.Collections.Generic.List<string>(s.PolicyProgress.Keys);
            foreach (string policy in keys)
            {
                float p = s.PolicyProgress[policy] + rate;
                s.PolicyProgress[policy] = p >= 1f ? 1f : p;
            }
        }

        private void DecayCampaigns(PoliticalState s)
        {
            var keys = new System.Collections.Generic.List<int>(s.CampaignBoost.Keys);
            foreach (int district in keys)
            {
                float b = s.CampaignBoost[district] * CampaignDecayPerMonth;
                if (b < 0.001f)
                {
                    s.CampaignBoost.Remove(district);
                }
                else
                {
                    s.CampaignBoost[district] = b;
                }
            }
        }

        public static void SpendOnDistrict(PoliticalState s, int districtId, int amount)
        {
            if (s.CampaignBudget < amount || amount <= 0)
            {
                return;
            }
            s.CampaignBudget -= amount;
            float current;
            s.CampaignBoost.TryGetValue(districtId, out current);
            s.CampaignBoost[districtId] = current + amount * 0.001f;
        }

        public static void EnactPolicy(PoliticalState s, string policyId, int pilotDistrict)
        {
            if (!s.PolicyProgress.ContainsKey(policyId))
            {
                s.PolicyProgress[policyId] = 0f;
            }
            if (pilotDistrict >= 0)
            {
                s.PilotDistricts[policyId] = pilotDistrict;
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace PolitiCities.Politics
{
    public struct DistrictInfo
    {
        public int Id;
        public float Happiness;
        public float Crime;
    }

    public static class PoliticalEngine
    {
        public const int TicksPerMonth = 100000;
        public const int MonthsPerElection = 48;
        public const int MonthsPerDistrictElection = 12;
        public const float ImplementationMonths = 3f;
        public const float CampaignDecayPerMonth = 0.9f;
        public const float SliderStep = 0.05f;

        public static void Tick(PoliticalState s, Random rng, float realApproval)
        {
            s.TickCounter++;
            AdvanceImplementation(s);
            if (s.TickCounter >= TicksPerMonth)
            {
                s.TickCounter = 0;
                MonthlyUpdate(s, rng, realApproval);
            }
        }

        public static void MonthlyUpdate(PoliticalState s, Random rng, float realApproval)
        {
            if (realApproval >= 0f)
            {
                s.LeaderApproval = Clamp01(realApproval);
            }
            ApplySliderEffects(s);
            DriftVoteShares(s, rng);
            DecayCampaigns(s);

            s.MonthsSinceElection++;
            if (s.MonthsSinceElection >= MonthsPerElection)
            {
                s.MonthsSinceElection = 0;
                s.Council.RecomputeFromShares(s.VoteShare);
            }
        }

        public static void ApplySliderEffects(PoliticalState s)
        {
            float delta = SliderDelta(s, "welfare") * 0.04f
                + SliderDelta(s, "greenInvestment") * 0.02f
                - SliderDelta(s, "policeFunding") * 0.02f;
            s.LeaderApproval = Clamp01(s.LeaderApproval + delta);
        }

        public static void DriftVoteShares(PoliticalState s, Random rng)
        {
            float drift = (s.LeaderApproval - 0.5f) * 0.02f;
            float total = 0f;
            for (int i = 0; i < s.VoteShare.Length; i++)
            {
                float noise = ((float)rng.NextDouble() - 0.5f) * 0.04f;
                s.VoteShare[i] = Math.Max(0.01f, s.VoteShare[i] + drift + noise);
                total += s.VoteShare[i];
            }
            for (int i = 0; i < s.VoteShare.Length; i++)
            {
                s.VoteShare[i] /= total;
            }
        }

        public static void AdvanceImplementation(PoliticalState s)
        {
            float rate = Math.Max(0.1f, s.AdminCapacity) / (TicksPerMonth * ImplementationMonths);
            var keys = new List<string>(s.PolicyProgress.Keys);
            foreach (string policy in keys)
            {
                float p = s.PolicyProgress[policy] + rate;
                s.PolicyProgress[policy] = p >= 1f ? 1f : p;
            }
        }

        public static void DecayCampaigns(PoliticalState s)
        {
            var keys = new List<int>(s.CampaignBoost.Keys);
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

        public static bool SpendOnDistrict(PoliticalState s, int districtId, int amount)
        {
            if (s.CampaignBudget < amount || amount <= 0)
            {
                return false;
            }
            s.CampaignBudget -= amount;
            float current;
            s.CampaignBoost.TryGetValue(districtId, out current);
            s.CampaignBoost[districtId] = current + amount * 0.001f;
            return true;
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

        public static bool CouncilVote(PoliticalState s, string policyId, Random rng)
        {
            if (s.Council.GoverningParty >= 0)
            {
                Party p = Party.Roster[s.Council.GoverningParty];
                if (Array.IndexOf(p.Platform, policyId) >= 0)
                {
                    return true;
                }
            }
            if (s.Council.CoalitionSeats() <= Council.SeatCount / 2)
            {
                return false;
            }
            return rng.NextDouble() < 0.5;
        }

        public static int ResolveDistrictElection(PoliticalState s, DistrictInfo d, Random rng)
        {
            float anger = (100f - Clamp(d.Happiness, 0f, 100f)) / 100f;
            float crime = Clamp(d.Crime, 0f, 100f) / 100f;
            int best = 0;
            float bestScore = float.MinValue;
            for (int i = 0; i < Party.Roster.Length; i++)
            {
                Party p = Party.Roster[i];
                float noise = (float)(rng.NextDouble() - 0.5) * 0.1f;
                float score = s.VoteShare[i]
                    + 0.3f * p.Order * crime
                    + 0.15f * (p.Green - p.Economy) * anger
                    + noise;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        public static void AdjustSlider(PoliticalState s, string key, float delta)
        {
            float v;
            if (!s.PolicySliders.TryGetValue(key, out v))
            {
                v = 0.5f;
            }
            s.PolicySliders[key] = Clamp01(v + delta);
        }

        public static void PromotePilot(PoliticalState s, string policyId)
        {
            s.PilotDistricts.Remove(policyId);
        }

        private static float SliderDelta(PoliticalState s, string key)
        {
            float v;
            return s.PolicySliders.TryGetValue(key, out v) ? v - 0.5f : 0f;
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}

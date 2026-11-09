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

    public struct CrisisDef
    {
        public string Id;
        public float ApprovalHit;
        public float IncomeHit;
        public int DurationMonths;

        public CrisisDef(string id, float approvalHit, float incomeHit, int durationMonths)
        {
            Id = id;
            ApprovalHit = approvalHit;
            IncomeHit = incomeHit;
            DurationMonths = durationMonths;
        }
    }

    public static class PoliticalEngine
    {
        public const int TicksPerMonth = 100000;
        public const int MonthsPerElection = 48;
        public const int MonthsPerDistrictElection = 12;
        public const float ImplementationMonths = 3f;
        public const float CampaignDecayPerMonth = 0.9f;
        public const float SliderStep = 0.05f;
        public const float CrisisChancePerMonth = 0.05f;
        public const float ForeignEventChancePerMonth = 0.1f;
        public const int EventLogCapacity = 50;

        public static readonly CrisisDef[] CrisisTable =
        {
            new CrisisDef("factoryClosures", 0.06f, 0.10f, 6),
            new CrisisDef("tradeShock", 0.04f, 0.15f, 4),
            new CrisisDef("financialPanic", 0.10f, 0.20f, 8),
            new CrisisDef("scandal", 0.08f, 0f, 3),
        };

        public static readonly string[] ForeignPowers = { "Aldermark", "Port Vela", "Kessig Republic" };

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
            s.LeaderApproval = Clamp01(s.LeaderApproval - CrisisApprovalPenalty(s) * 0.1f);
            DriftVoteShares(s, rng);
            DecayCampaigns(s);
            TickCrises(s, rng);
            MaybeTriggerCrisis(s, rng);
            TickForeign(s, rng);

            s.MonthsSinceElection++;
            if (s.MonthsSinceElection >= MonthsPerElection)
            {
                s.MonthsSinceElection = 0;
                s.Council.RecomputeFromShares(s.VoteShare, rng);
                LogEvent(s, "City election held; new council seated.");
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

        public static void FundCampaign(PoliticalState s, int amount)
        {
            if (amount > 0)
            {
                s.CampaignBudget += amount;
            }
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

        public static void MaybeTriggerCrisis(PoliticalState s, Random rng)
        {
            if (s.ActiveCrises.Count > 0)
            {
                return;
            }
            if (rng.NextDouble() < CrisisChancePerMonth)
            {
                CrisisDef c = CrisisTable[rng.Next(CrisisTable.Length)];
                s.ActiveCrises[c.Id] = c.DurationMonths;
                LogEvent(s, "Crisis begins: " + c.Id + " (" + c.DurationMonths + " months)");
            }
        }

        public static void TickCrises(PoliticalState s, Random rng)
        {
            var keys = new List<string>(s.ActiveCrises.Keys);
            foreach (string id in keys)
            {
                int left = s.ActiveCrises[id] - 1;
                if (left <= 0)
                {
                    s.ActiveCrises.Remove(id);
                    LogEvent(s, "Crisis resolved: " + id);
                }
                else
                {
                    s.ActiveCrises[id] = left;
                }
            }
        }

        public static float CrisisApprovalPenalty(PoliticalState s)
        {
            float sum = 0f;
            foreach (var kv in s.ActiveCrises)
            {
                foreach (CrisisDef c in CrisisTable)
                {
                    if (c.Id == kv.Key)
                    {
                        sum += c.ApprovalHit;
                    }
                }
            }
            return sum;
        }

        public static float CrisisIncomeMultiplier(PoliticalState s)
        {
            float mult = 1f;
            foreach (var kv in s.ActiveCrises)
            {
                foreach (CrisisDef c in CrisisTable)
                {
                    if (c.Id == kv.Key)
                    {
                        mult *= 1f - c.IncomeHit;
                    }
                }
            }
            return mult;
        }

        public static void TickForeign(PoliticalState s, Random rng)
        {
            foreach (string power in ForeignPowers)
            {
                float v;
                if (!s.ForeignRelations.TryGetValue(power, out v))
                {
                    v = 50f;
                }
                v += (50f - v) * 0.05f + ((float)rng.NextDouble() - 0.5f) * 4f;
                s.ForeignRelations[power] = Clamp(v, 0f, 100f);
            }
            if (rng.NextDouble() < ForeignEventChancePerMonth)
            {
                string power = ForeignPowers[rng.Next(ForeignPowers.Length)];
                float delta = ((float)rng.NextDouble() - 0.5f) * 30f;
                float v;
                s.ForeignRelations.TryGetValue(power, out v);
                s.ForeignRelations[power] = Clamp(v + delta, 0f, 100f);
                LogEvent(s, "Foreign event with " + power + " (" + (delta >= 0 ? "+" : "") + delta.ToString("0") + " relations)");
            }
        }

        public static string MinistryForService(string service)
        {
            switch (service)
            {
                case "Road":
                case "PublicTransport":
                case "Electricity":
                case "Water":
                    return "Infrastructure";
                case "Beautification":
                case "Garbage":
                    return "Environment";
                case "PoliceDepartment":
                case "FireDepartment":
                case "Disaster":
                    return "Security";
                case "HealthCare":
                case "Education":
                    return "Social Affairs";
                default:
                    return "Finance";
            }
        }

        public static float MinistryMultiplier(PoliticalState s, string service)
        {
            int mIndex = Array.IndexOf(Council.MinistryNames, MinistryForService(service));
            float comp = CompetenceAt(s, mIndex);
            float ministryFactor = 0.8f + 0.4f * comp;
            float financeFactor = 0.95f + 0.1f * CompetenceAt(s, Array.IndexOf(Council.MinistryNames, "Finance"));
            return ministryFactor * financeFactor;
        }

        private static float CompetenceAt(PoliticalState s, int ministryIndex)
        {
            if (ministryIndex < 0 || ministryIndex >= s.Council.MinisterCompetence.Length)
            {
                return 0.5f;
            }
            return s.Council.MinisterCompetence[ministryIndex];
        }

        public static void LogEvent(PoliticalState s, string message)
        {
            s.EventLog.Add(message);
            while (s.EventLog.Count > EventLogCapacity)
            {
                s.EventLog.RemoveAt(0);
            }
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

using System;
using System.IO;
using System.Linq;
using PolitiCities.Politics;
using Xunit;

namespace PolitiCities.Tests
{
    public class CouncilTests
    {
        [Fact]
        public void SeatsSumToCouncilSize()
        {
            var council = new Council();
            council.RecomputeFromShares(new float[] { 0.5f, 0.2f, 0.1f, 0.1f, 0.05f, 0.05f });
            Assert.Equal(Council.SeatCount, council.Seats.Sum());
        }

        [Fact]
        public void LargestPartyGetsMostSeats()
        {
            var council = new Council();
            council.RecomputeFromShares(new float[] { 0.6f, 0.1f, 0.1f, 0.1f, 0.05f, 0.05f });
            int max = council.Seats.Max();
            Assert.Equal(max, council.Seats[0]);
        }

        [Fact]
        public void CoalitionReachesMajorityWithLargestPartyAsLeader()
        {
            var council = new Council();
            council.RecomputeFromShares(new float[] { 0.3f, 0.25f, 0.2f, 0.15f, 0.05f, 0.05f });
            Assert.True(council.CoalitionSeats() > Council.SeatCount / 2);
            Assert.Equal(0, council.GoverningParty);
            Assert.All(council.MinistryParty.Where(m => m != null), m => Assert.Contains(m, Party.Roster.Where((p, i) => council.InCoalition[i]).Select(p => p.Id)));
        }

        [Fact]
        public void SinglePartyMajorityFormsSoloCoalition()
        {
            var council = new Council();
            council.RecomputeFromShares(new float[] { 0.7f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f });
            Assert.Equal(1, council.InCoalition.Count(b => b));
            Assert.Equal(0, council.GoverningParty);
        }
    }

    public class EngineTests
    {
        private static PoliticalState NewState()
        {
            return new PoliticalState();
        }

        [Fact]
        public void VoteSharesStayNormalizedOverManyMonths()
        {
            PoliticalState s = NewState();
            var rng = new Random(42);
            for (int m = 0; m < 120; m++)
            {
                PoliticalEngine.MonthlyUpdate(s, rng, 0.7f);
            }
            float sum = s.VoteShare.Sum();
            Assert.True(Math.Abs(sum - 1f) < 0.001f, "sum was " + sum);
            Assert.All(s.VoteShare, v => Assert.True(v >= 0.01f));
        }

        [Fact]
        public void ImplementationProgressAdvancesAndClampsAtOne()
        {
            PoliticalState s = NewState();
            PoliticalEngine.EnactPolicy(s, "stimulusPackage", -1);
            Assert.Equal(0f, s.PolicyProgress["stimulusPackage"]);
            for (int i = 0; i < PoliticalEngine.TicksPerMonth * 10; i++)
            {
                PoliticalEngine.AdvanceImplementation(s);
            }
            Assert.Equal(1f, s.PolicyProgress["stimulusPackage"]);
        }

        [Fact]
        public void HigherAdminCapacityImplementsFaster()
        {
            PoliticalState slow = NewState();
            PoliticalState fast = NewState();
            fast.AdminCapacity = 2f;
            PoliticalEngine.EnactPolicy(slow, "p", -1);
            PoliticalEngine.EnactPolicy(fast, "p", -1);
            int ticksSlow = 0;
            while (slow.PolicyProgress["p"] < 1f && ticksSlow < 10000000) { PoliticalEngine.AdvanceImplementation(slow); ticksSlow++; }
            int ticksFast = 0;
            while (fast.PolicyProgress["p"] < 1f && ticksFast < 10000000) { PoliticalEngine.AdvanceImplementation(fast); ticksFast++; }
            Assert.True(ticksFast < ticksSlow, $"fast {ticksFast} !< slow {ticksSlow}");
        }

        [Fact]
        public void CampaignSpendRequiresBudgetAndDecays()
        {
            PoliticalState s = NewState();
            s.CampaignBudget = 1000;
            Assert.True(PoliticalEngine.SpendOnDistrict(s, 3, 500));
            Assert.Equal(500, s.CampaignBudget);
            Assert.True(s.CampaignBoost[3] > 0f);
            Assert.False(PoliticalEngine.SpendOnDistrict(s, 3, 99999));
            float before = s.CampaignBoost[3];
            PoliticalEngine.DecayCampaigns(s);
            Assert.True(s.CampaignBoost[3] < before);
            for (int i = 0; i < 1000 && s.CampaignBoost.ContainsKey(3); i++)
            {
                PoliticalEngine.DecayCampaigns(s);
            }
            Assert.False(s.CampaignBoost.ContainsKey(3));
        }

        [Fact]
        public void EnactPolicyRecordsPilotDistrict()
        {
            PoliticalState s = NewState();
            PoliticalEngine.EnactPolicy(s, "p", 7);
            Assert.Equal(0f, s.PolicyProgress["p"]);
            Assert.Equal(7, s.PilotDistricts["p"]);
            PoliticalEngine.PromotePilot(s, "p");
            Assert.False(s.PilotDistricts.ContainsKey("p"));
        }

        [Fact]
        public void CouncilVoteRequiresMajorityUnlessPolicyInGoverningPlatform()
        {
            PoliticalState s = NewState();
            var council = new Council();
            council.RecomputeFromShares(new float[] { 0.3f, 0.25f, 0.2f, 0.15f, 0.05f, 0.05f });
            s.Council = council;
            Assert.True(council.CoalitionSeats() > Council.SeatCount / 2);
            Assert.True(PoliticalEngine.CouncilVote(s, "anything"));

            PoliticalState noMajority = NewState();
            var c2 = new Council();
            c2.RecomputeFromShares(new float[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.1f, 0.1f });
            noMajority.Council = c2;
            Assert.False(PoliticalEngine.CouncilVote(noMajority, "notInPlatform"));
            string platformPolicy = Party.Roster[c2.GoverningParty].Platform[0];
            Assert.True(PoliticalEngine.CouncilVote(noMajority, platformPolicy));
        }

        [Fact]
        public void DistrictElectionIsDeterministicPerSeed()
        {
            PoliticalState s = NewState();
            var info = new DistrictInfo { Id = 1, Happiness = 40f, Crime = 60f };
            int a = PoliticalEngine.ResolveDistrictElection(s, info, new Random(7));
            int b = PoliticalEngine.ResolveDistrictElection(s, info, new Random(7));
            Assert.Equal(a, b);
        }

        [Fact]
        public void HighCrimeDistrictFavorsOrderParty()
        {
            PoliticalState s = NewState();
            var info = new DistrictInfo { Id = 1, Happiness = 50f, Crime = 100f };
            int winner = PoliticalEngine.ResolveDistrictElection(s, info, new Random(1));
            int orderIndex = Array.FindIndex(Party.Roster, p => p.Id == "order");
            Assert.Equal(orderIndex, winner);
        }

        [Fact]
        public void SliderAdjustClampsToUnitRange()
        {
            PoliticalState s = NewState();
            for (int i = 0; i < 100; i++)
            {
                PoliticalEngine.AdjustSlider(s, "welfare", PoliticalEngine.SliderStep);
            }
            Assert.Equal(1f, s.PolicySliders["welfare"]);
            for (int i = 0; i < 200; i++)
            {
                PoliticalEngine.AdjustSlider(s, "welfare", -PoliticalEngine.SliderStep);
            }
            Assert.Equal(0f, s.PolicySliders["welfare"]);
        }

        [Fact]
        public void TickTriggersMonthlyUpdate()
        {
            PoliticalState s = NewState();
            var rng = new Random(3);
            for (int i = 0; i < PoliticalEngine.TicksPerMonth; i++)
            {
                PoliticalEngine.Tick(s, rng, 0.6f);
            }
            Assert.Equal(1, s.MonthsSinceElection);
            Assert.Equal(0, s.TickCounter);
            Assert.Equal(0.6f, s.LeaderApproval);
        }

        [Fact]
        public void SaveLoadRoundtripPreservesState()
        {
            PoliticalState s = NewState();
            s.CampaignBudget = 777;
            s.LeaderApproval = 0.42f;
            PoliticalEngine.EnactPolicy(s, "stimulusPackage", 4);
            PoliticalEngine.SpendOnDistrict(s, 2, 100);
            s.RulingParty[2] = 1;
            s.DistrictElectionMonths[2] = 5;
            PoliticalEngine.AdjustSlider(s, "welfare", 0.2f);
            s.Council.RecomputeFromShares(new float[] { 0.4f, 0.3f, 0.1f, 0.1f, 0.05f, 0.05f });

            PoliticalState loaded = new PoliticalState();
            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    s.Write(w);
                }
                ms.Position = 0;
                using (var r = new BinaryReader(ms, System.Text.Encoding.UTF8, true))
                {
                    loaded.Read(r);
                }
            }

            Assert.Equal(777, loaded.CampaignBudget);
            Assert.Equal(0.42f, loaded.LeaderApproval);
            Assert.Equal(0f, loaded.PolicyProgress["stimulusPackage"]);
            Assert.Equal(4, loaded.PilotDistricts["stimulusPackage"]);
            Assert.True(loaded.CampaignBoost.ContainsKey(2));
            Assert.Equal(1, loaded.RulingParty[2]);
            Assert.Equal(5, loaded.DistrictElectionMonths[2]);
            Assert.Equal(s.PolicySliders["welfare"], loaded.PolicySliders["welfare"]);
            Assert.Equal(s.Council.Seats, loaded.Council.Seats);
            Assert.Equal(s.Council.GoverningParty, loaded.Council.GoverningParty);
            for (int i = 0; i < s.VoteShare.Length; i++)
            {
                Assert.Equal(s.VoteShare[i], loaded.VoteShare[i]);
            }
        }
    }
}

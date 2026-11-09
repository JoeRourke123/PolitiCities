using System;
using System.IO;
using System.Linq;
using PolitiCities.Politics;
using Xunit;

namespace PolitiCities.Tests
{
    public class ReferendumTests
    {
        [Fact]
        public void ReferendumPassBoostsApproval()
        {
            PoliticalState s = new PoliticalState();
            s.LeaderApproval = 0.9f;
            bool pass = PoliticalEngine.HoldReferendum(s, new Random(0), "test");
            Assert.True(pass);
            Assert.True(s.LeaderApproval > 0.9f);
            Assert.Contains(s.EventLog, e => e.Contains("passed"));
        }

        [Fact]
        public void ReferendumFailLowersApproval()
        {
            PoliticalState s = new PoliticalState();
            s.LeaderApproval = 0.1f;
            bool pass = PoliticalEngine.HoldReferendum(s, new Random(0), "test");
            Assert.False(pass);
            Assert.True(s.LeaderApproval < 0.1f);
            Assert.Contains(s.EventLog, e => e.Contains("failed"));
        }
    }

    public class PollHistoryTests
    {
        [Fact]
        public void HistoryIsCappedAndTrendDetected()
        {
            PoliticalState s = new PoliticalState();
            var rng = new Random(2);
            for (int i = 0; i < 60; i++)
            {
                PoliticalEngine.MonthlyUpdate(s, rng, 0.9f);
            }
            Assert.Equal(PoliticalEngine.PollHistoryMonths, s.PollHistory.Count);
            int trend = PoliticalEngine.PollTrend(s, 0);
            Assert.True(trend >= -1 && trend <= 1);
            Assert.Equal(s.VoteShare.Length, s.PollHistory[s.PollHistory.Count - 1].Length);
        }
    }

    public class DistrictLockTests
    {
        [Fact]
        public void UnruledDistrictIsEditable()
        {
            PoliticalState s = new PoliticalState();
            Assert.True(PoliticalEngine.CanPlayerEditDistrict(s, 0));
        }

        [Fact]
        public void OppositionDistrictIsLocked()
        {
            PoliticalState s = new PoliticalState();
            s.Council.RecomputeFromShares(new float[] { 0.7f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f });
            s.RulingParty[3] = 3;
            Assert.False(s.Council.InCoalition[3]);
            Assert.False(PoliticalEngine.CanPlayerEditDistrict(s, 3));
        }

        [Fact]
        public void CoalitionDistrictIsEditable()
        {
            PoliticalState s = new PoliticalState();
            s.Council.RecomputeFromShares(new float[] { 0.7f, 0.05f, 0.05f, 0.05f, 0.05f, 0.1f });
            s.RulingParty[0] = 0;
            Assert.True(s.Council.InCoalition[0]);
            Assert.True(PoliticalEngine.CanPlayerEditDistrict(s, 0));
        }
    }

    public class PolicyEffectTests
    {
        [Fact]
        public void GreenNewDealBoostsApprovalWhenImplemented()
        {
            PoliticalState s = new PoliticalState();
            s.LeaderApproval = 0.5f;
            PoliticalEngine.EnactPolicy(s, "greenNewDeal", -1);
            s.PolicyProgress["greenNewDeal"] = 1f;
            PoliticalEngine.ApplyPolicyEffects(s);
            Assert.True(s.LeaderApproval > 0.5f);
        }

        [Fact]
        public void GreenNewDealDoesNothingWhileImplementing()
        {
            PoliticalState s = new PoliticalState();
            s.LeaderApproval = 0.5f;
            PoliticalEngine.EnactPolicy(s, "greenNewDeal", -1);
            PoliticalEngine.ApplyPolicyEffects(s);
            Assert.Equal(0.5f, s.LeaderApproval);
        }
    }

    public class PersistenceV4Tests
    {
        [Fact]
        public void RoundtripPreservesPollHistory()
        {
            PoliticalState s = new PoliticalState();
            var rng = new Random(11);
            for (int i = 0; i < 5; i++)
            {
                PoliticalEngine.MonthlyUpdate(s, rng, 0.6f);
            }
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
            Assert.Equal(s.PollHistory.Count, loaded.PollHistory.Count);
            for (int i = 0; i < s.PollHistory.Count; i++)
            {
                Assert.Equal(s.PollHistory[i], loaded.PollHistory[i]);
            }
        }
    }
}

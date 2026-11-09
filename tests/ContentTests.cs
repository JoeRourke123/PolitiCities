using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PolitiCities.Politics;
using Xunit;

namespace PolitiCities.Tests
{
    public class ContentSanityTests
    {
        [Fact]
        public void PolicyIdsAreUniqueAndWellFormed()
        {
            var ids = Content.Policies.Select(p => p.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.All(Content.Policies, p =>
            {
                Assert.False(string.IsNullOrEmpty(p.Name));
                Assert.False(string.IsNullOrEmpty(p.Description));
                Assert.InRange(p.UnlockApproval, 0f, 1f);
                Assert.InRange(p.IncomeMultiplier, 0.5f, 1.5f);
            });
        }

        [Fact]
        public void EveryEventHasAtLeastTwoChoices()
        {
            var ids = Content.Events.Select(e => e.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            Assert.All(Content.Events, e => Assert.True(e.Choices.Length >= 2, e.Id));
        }

        [Fact]
        public void EveryPartyHasAPlatform()
        {
            Assert.All(Party.Roster, p => Assert.True(p.Platform.Length >= 2, p.Id));
            var partyIds = Party.Roster.Select(p => p.Id).ToList();
            Assert.Equal(partyIds.Count, partyIds.Distinct().Count());
        }

        [Fact]
        public void ReferendumSubjectsAreRealPolicies()
        {
            Assert.All(Content.ReferendumSubjects, s => Assert.NotNull(Content.PolicyById(s)));
        }
    }

    public class PolicyAggregateTests
    {
        [Fact]
        public void ImplementedPoliciesStackMultipliers()
        {
            PoliticalState s = new PoliticalState();
            Assert.Equal(1f, PoliticalEngine.PolicyIncomeMultiplier(s));
            PoliticalEngine.EnactPolicy(s, "stimulusPackage", -1);
            s.PolicyProgress["stimulusPackage"] = 1f;
            Assert.Equal(1.10f, PoliticalEngine.PolicyIncomeMultiplier(s), 3);
            PoliticalEngine.EnactPolicy(s, "taxReform", -1);
            s.PolicyProgress["taxReform"] = 1f;
            Assert.Equal(1.10f * 1.06f, PoliticalEngine.PolicyIncomeMultiplier(s), 3);
        }

        [Fact]
        public void ApprovalDeltaSumsOnlyImplemented()
        {
            PoliticalState s = new PoliticalState();
            PoliticalEngine.EnactPolicy(s, "austerityPackage", -1);
            Assert.Equal(0f, PoliticalEngine.PolicyApprovalDelta(s), 5);
            s.PolicyProgress["austerityPackage"] = 1f;
            Assert.Equal(-0.03f, PoliticalEngine.PolicyApprovalDelta(s), 5);
        }

        [Fact]
        public void UnlockGatesOnApproval()
        {
            PoliticalState s = new PoliticalState();
            s.LeaderApproval = 0.35f;
            Assert.True(PoliticalEngine.IsPolicyUnlocked(s, "stimulusPackage"));
            Assert.False(PoliticalEngine.IsPolicyUnlocked(s, "surveillanceState"));
            s.LeaderApproval = 0.65f;
            Assert.True(PoliticalEngine.IsPolicyUnlocked(s, "surveillanceState"));
        }
    }

    public class EventAndCorruptionTests
    {
        [Fact]
        public void EventRollSetsPendingAndChoiceAppliesEffects()
        {
            PoliticalState s = new PoliticalState();
            s.ForeignRelations["Aldermark"] = 50f;
            bool rolled = false;
            for (int seed = 0; seed < 500 && !rolled; seed++)
            {
                PoliticalEngine.RollEvent(s, new Random(seed));
                rolled = s.PendingEvent != null;
            }
            Assert.True(rolled);
            EventDef e = Content.EventById(s.PendingEvent);
            float approvalBefore = s.LeaderApproval;
            int budgetBefore = s.CampaignBudget;
            PoliticalEngine.ChooseEventOption(s, new Random(0), 0);
            Assert.Null(s.PendingEvent);
            EventChoice c = e.Choices[0];
            Assert.Equal(approvalBefore + c.ApprovalDelta, s.LeaderApproval, 5);
            Assert.Equal(Math.Max(0, budgetBefore + c.BudgetDelta), s.CampaignBudget);
        }

        [Fact]
        public void NoNewEventWhileOnePending()
        {
            PoliticalState s = new PoliticalState();
            s.PendingEvent = "protests";
            for (int seed = 0; seed < 200; seed++)
            {
                PoliticalEngine.RollEvent(s, new Random(seed));
            }
            Assert.Equal("protests", s.PendingEvent);
        }

        [Fact]
        public void CorruptionRisesWithPropagandaAndCanScandal()
        {
            PoliticalState s = new PoliticalState();
            PoliticalEngine.EnactPolicy(s, "propagandaMachine", -1);
            s.PolicyProgress["propagandaMachine"] = 1f;
            s.Corruption = 0.65f;
            bool scandal = false;
            for (int seed = 0; seed < 500 && !scandal; seed++)
            {
                float before = s.LeaderApproval;
                PoliticalEngine.TickCorruption(s, new Random(seed));
                if (before - s.LeaderApproval > 0.05f)
                {
                    scandal = true;
                }
            }
            Assert.True(scandal);
            Assert.Contains(s.EventLog, e => e.Contains("SCANDAL"));
        }

        [Fact]
        public void TradeMultiplierFollowsRelations()
        {
            PoliticalState s = new PoliticalState();
            foreach (string p in PoliticalEngine.ForeignPowers)
            {
                s.ForeignRelations[p] = 100f;
            }
            Assert.Equal(1.1f, PoliticalEngine.ForeignTradeMultiplier(s), 3);
            foreach (string p in PoliticalEngine.ForeignPowers)
            {
                s.ForeignRelations[p] = 0f;
            }
            Assert.Equal(0.9f, PoliticalEngine.ForeignTradeMultiplier(s), 3);
        }
    }

    public class PersistenceV5Tests
    {
        [Fact]
        public void RoundtripPreservesCorruptionPendingEventAndIntegrity()
        {
            PoliticalState s = new PoliticalState();
            s.Corruption = 0.42f;
            s.PendingEvent = "oilShock";
            s.Council.RecomputeFromShares(new float[] { 0.4f, 0.3f, 0.1f, 0.1f, 0.05f, 0.05f, 0f, 0f }, new Random(4));

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

            Assert.Equal(0.42f, loaded.Corruption);
            Assert.Equal("oilShock", loaded.PendingEvent);
            Assert.Equal(s.Council.MinisterIntegrity, loaded.Council.MinisterIntegrity);
        }
    }
}

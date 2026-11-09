using System;
using System.IO;
using System.Linq;
using PolitiCities.Politics;
using Xunit;

namespace PolitiCities.Tests
{
    public class CrisisTests
    {
        [Fact]
        public void NoCrisisByDefault()
        {
            PoliticalState s = new PoliticalState();
            Assert.Equal(1f, PoliticalEngine.CrisisIncomeMultiplier(s));
            Assert.Equal(0f, PoliticalEngine.CrisisApprovalPenalty(s));
        }

        [Fact]
        public void ActiveCrisisReducesIncomeAndApproval()
        {
            PoliticalState s = new PoliticalState();
            s.ActiveCrises["financialPanic"] = 8;
            float mult = PoliticalEngine.CrisisIncomeMultiplier(s);
            Assert.Equal(0.8f, mult, 3);
            Assert.Equal(0.10f, PoliticalEngine.CrisisApprovalPenalty(s), 3);
        }

        [Fact]
        public void CrisesTickDownAndResolve()
        {
            PoliticalState s = new PoliticalState();
            s.ActiveCrises["scandal"] = 2;
            PoliticalEngine.TickCrises(s, new Random(1));
            Assert.True(s.ActiveCrises.ContainsKey("scandal"));
            PoliticalEngine.TickCrises(s, new Random(1));
            Assert.False(s.ActiveCrises.ContainsKey("scandal"));
            Assert.Contains(s.EventLog, e => e.Contains("resolved"));
        }

        [Fact]
        public void MaybeTriggerCrisisEventuallyFires()
        {
            PoliticalState s = new PoliticalState();
            bool fired = false;
            for (int seed = 0; seed < 500 && !fired; seed++)
            {
                PoliticalEngine.MaybeTriggerCrisis(s, new Random(seed));
                fired = s.ActiveCrises.Count > 0;
            }
            Assert.True(fired);
        }

        [Fact]
        public void NoNewCrisisWhileOneActive()
        {
            PoliticalState s = new PoliticalState();
            s.ActiveCrises["scandal"] = 5;
            for (int seed = 0; seed < 500; seed++)
            {
                PoliticalEngine.MaybeTriggerCrisis(s, new Random(seed));
            }
            Assert.Equal(1, s.ActiveCrises.Count);
            Assert.True(s.ActiveCrises.ContainsKey("scandal"));
        }
    }

    public class MinistryTests
    {
        [Fact]
        public void ServiceMapsToExpectedMinistry()
        {
            Assert.Equal("Infrastructure", PoliticalEngine.MinistryForService("Road"));
            Assert.Equal("Environment", PoliticalEngine.MinistryForService("Garbage"));
            Assert.Equal("Security", PoliticalEngine.MinistryForService("PoliceDepartment"));
            Assert.Equal("Social Affairs", PoliticalEngine.MinistryForService("Education"));
            Assert.Equal("Finance", PoliticalEngine.MinistryForService("Residential"));
        }

        [Fact]
        public void MinistryMultiplierScalesWithCompetence()
        {
            PoliticalState s = new PoliticalState();
            s.Council.RecomputeFromShares(new float[] { 0.6f, 0.1f, 0.1f, 0.1f, 0.05f, 0.05f });
            int idx = Array.IndexOf(Council.MinistryNames, "Infrastructure");
            s.Council.MinisterCompetence[idx] = 1f;
            float high = PoliticalEngine.MinistryMultiplier(s, "Road");
            s.Council.MinisterCompetence[idx] = 0f;
            float low = PoliticalEngine.MinistryMultiplier(s, "Road");
            Assert.True(high > low);
            Assert.InRange(high, 0.5f, 2f);
            Assert.InRange(low, 0.5f, 2f);
        }
    }

    public class ForeignAndEventTests
    {
        [Fact]
        public void ForeignRelationsStayInRangeAndDriftToMiddle()
        {
            PoliticalState s = new PoliticalState();
            s.ForeignRelations["Aldermark"] = 100f;
            var rng = new Random(5);
            for (int i = 0; i < 50; i++)
            {
                PoliticalEngine.TickForeign(s, rng);
            }
            foreach (var kv in s.ForeignRelations)
            {
                Assert.InRange(kv.Value, 0f, 100f);
            }
            Assert.True(s.ForeignRelations["Aldermark"] < 100f);
            Assert.Equal(PoliticalEngine.ForeignPowers.Length, s.ForeignRelations.Count);
        }

        [Fact]
        public void EventLogIsCapped()
        {
            PoliticalState s = new PoliticalState();
            for (int i = 0; i < 200; i++)
            {
                PoliticalEngine.LogEvent(s, "event " + i);
            }
            Assert.Equal(PoliticalEngine.EventLogCapacity, s.EventLog.Count);
            Assert.Equal("event 199", s.EventLog[s.EventLog.Count - 1]);
        }

        [Fact]
        public void FundCampaignAddsBudget()
        {
            PoliticalState s = new PoliticalState();
            PoliticalEngine.FundCampaign(s, 500);
            PoliticalEngine.FundCampaign(s, -100);
            Assert.Equal(500, s.CampaignBudget);
        }
    }

    public class PersistenceV3Tests
    {
        [Fact]
        public void RoundtripPreservesCrisesForeignEventsAndMinisters()
        {
            PoliticalState s = new PoliticalState();
            s.ActiveCrises["tradeShock"] = 3;
            s.ForeignRelations["Port Vela"] = 77f;
            PoliticalEngine.LogEvent(s, "hello");
            s.Council.RecomputeFromShares(new float[] { 0.4f, 0.3f, 0.1f, 0.1f, 0.05f, 0.05f }, new Random(9));

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

            Assert.Equal(3, loaded.ActiveCrises["tradeShock"]);
            Assert.Equal(77f, loaded.ForeignRelations["Port Vela"]);
            Assert.Contains("hello", loaded.EventLog);
            Assert.Equal(s.Council.MinisterCompetence, loaded.Council.MinisterCompetence);
        }
    }
}

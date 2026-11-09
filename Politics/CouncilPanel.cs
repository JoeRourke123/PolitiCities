using System.Text;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace PolitiCities.Politics
{
    public sealed class CouncilPanel
    {
        private static readonly string[] SliderKeys = { "policeFunding", "welfare", "greenInvestment" };

        private UIPanel panel;
        private UILabel body;
        private UILabel[] sliderLabels = new UILabel[SliderKeys.Length];

        public bool IsOpen
        {
            get { return panel != null; }
        }

        public void Show()
        {
            if (panel != null)
            {
                return;
            }
            UIView view = UIView.GetAView();
            panel = view.AddUIComponent(typeof(UIPanel)) as UIPanel;
            panel.name = "PolitiCitiesCouncilPanel";
            panel.size = new Vector2(480f, 660f);
            panel.relativePosition = new Vector3(100f, 40f);
            panel.backgroundSprite = "MenuPanelInfo";

            UILabel title = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            title.text = "PolitiCities — Council";
            title.textScale = 1.2f;
            title.relativePosition = new Vector3(12f, 8f);

            body = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            body.textScale = 0.85f;
            body.relativePosition = new Vector3(12f, 36f);
            body.size = new Vector2(456f, 400f);
            body.wordWrap = true;

            for (int i = 0; i < SliderKeys.Length; i++)
            {
                int index = i;
                float y = 446f + i * 30f;
                UILabel label = panel.AddUIComponent(typeof(UILabel)) as UILabel;
                label.textScale = 0.85f;
                label.relativePosition = new Vector3(12f, y);
                label.size = new Vector2(200f, 20f);
                sliderLabels[i] = label;

                UIButton minus = panel.AddUIComponent(typeof(UIButton)) as UIButton;
                minus.text = "-";
                minus.size = new Vector2(26f, 24f);
                minus.relativePosition = new Vector3(220f, y);
                minus.eventClicked += (c, p) => AdjustSlider(index, -PoliticalEngine.SliderStep);

                UIButton plus = panel.AddUIComponent(typeof(UIButton)) as UIButton;
                plus.text = "+";
                plus.size = new Vector2(26f, 24f);
                plus.relativePosition = new Vector3(252f, y);
                plus.eventClicked += (c, p) => AdjustSlider(index, PoliticalEngine.SliderStep);
            }

            UIButton enact = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            enact.text = "Enact stimulus package";
            enact.size = new Vector2(180f, 28f);
            enact.relativePosition = new Vector3(12f, 540f);
            enact.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null)
                {
                    PoliticalEngine.EnactPolicy(s, Patches.StimulusIncomePatch.PolicyId, -1);
                }
                Refresh();
            };

            UIButton referendum = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            referendum.text = "Referendum: greenNewDeal";
            referendum.size = new Vector2(190f, 28f);
            referendum.relativePosition = new Vector3(200f, 574f);
            referendum.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null)
                {
                    bool pass = PoliticalEngine.HoldReferendum(s, new System.Random(s.RandomSeed + s.MonthsSinceElection), "greenNewDeal");
                    if (pass)
                    {
                        PoliticalEngine.EnactPolicy(s, "greenNewDeal", -1);
                        s.PolicyProgress["greenNewDeal"] = 1f;
                    }
                }
                Refresh();
            };

            UIButton fund = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            fund.text = "Fund campaign 5000";
            fund.size = new Vector2(160f, 28f);
            fund.relativePosition = new Vector3(12f, 574f);
            fund.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null && !PoliticalSimulation.TryFundCampaign(s, 5000))
                {
                    PoliticalEngine.LogEvent(s, "Campaign funding failed: insufficient city funds");
                }
                Refresh();
            };

            UIButton promote = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            promote.text = "Promote pilots city-wide";
            promote.size = new Vector2(180f, 28f);
            promote.relativePosition = new Vector3(200f, 540f);
            promote.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null)
                {
                    foreach (var kv in new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, int>>(s.PilotDistricts))
                    {
                        PoliticalEngine.PromotePilot(s, kv.Key);
                    }
                }
                Refresh();
            };

            UIButton close = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            close.text = "Close";
            close.size = new Vector2(100f, 28f);
            close.relativePosition = new Vector3(368f, 620f);
            close.eventClicked += (c, p) => Hide();

            Refresh();
        }

        private void AdjustSlider(int index, float delta)
        {
            PoliticalState s = PoliticalData.Current;
            if (s != null)
            {
                PoliticalEngine.AdjustSlider(s, SliderKeys[index], delta);
            }
            Refresh();
        }

        public void Hide()
        {
            if (panel != null)
            {
                Object.Destroy(panel.gameObject);
                panel = null;
                body = null;
            }
        }

        public void Refresh()
        {
            if (body == null)
            {
                return;
            }
            PoliticalState s = PoliticalData.Current;
            if (s == null)
            {
                body.text = "No game loaded.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Vote intention:");
            for (int i = 0; i < Party.Roster.Length; i++)
            {
                Party p = Party.Roster[i];
                int seats = i < s.Council.Seats.Length ? s.Council.Seats[i] : 0;
                string coalition = i < s.Council.InCoalition.Length && s.Council.InCoalition[i] ? " [gov]" : "";
                int trend = PoliticalEngine.PollTrend(s, i);
                string arrow = trend > 0 ? " ^" : (trend < 0 ? " v" : " -");
                sb.AppendLine("  " + p.Name + ": " + (s.VoteShare[i] * 100f).ToString("0.0") + "%" + arrow + "  seats " + seats + coalition);
            }
            sb.AppendLine();
            sb.AppendLine("Governing: " + (s.Council.GoverningParty >= 0 ? Party.Roster[s.Council.GoverningParty].Name : "none") + " (" + s.Council.CoalitionSeats() + "/" + Council.SeatCount + ")");
            sb.AppendLine("Ministries:");
            for (int m = 0; m < Council.MinistryNames.Length; m++)
            {
                string pid = m < s.Council.MinistryParty.Length ? s.Council.MinistryParty[m] : null;
                Party owner = pid != null ? Party.ById(pid) : null;
                sb.AppendLine("  " + Council.MinistryNames[m] + ": " + (owner != null ? owner.Name : "vacant"));
            }
            sb.AppendLine();
            sb.AppendLine("Implementing: " + PolicyProgressText(s));
            sb.AppendLine("Pilots: " + (s.PilotDistricts.Count == 0 ? "none" : string.Join(", ", Keys(s.PilotDistricts))));
            sb.AppendLine();
            sb.AppendLine("Districts (ruling party):");
            int shown = 0;
            DistrictManager dm = Singleton<DistrictManager>.instance;
            int count = dm != null ? dm.m_districtCount : 0;
            for (int i = 0; i < count && shown < 6; i++)
            {
                int ruler;
                if (s.RulingParty.TryGetValue(i, out ruler))
                {
                    sb.AppendLine("  " + i + " " + dm.GetDistrictName(i) + ": " + Party.Roster[ruler].Name);
                    shown++;
                }
            }
            if (shown == 0)
            {
                sb.AppendLine("  no elections held yet");
            }
            sb.AppendLine();
            sb.AppendLine("Campaign budget: " + s.CampaignBudget + "  boosts: " + s.CampaignBoost.Count);
            sb.AppendLine("Approval: " + (s.LeaderApproval * 100f).ToString("0") + "%  next city election in " + (PoliticalEngine.MonthsPerElection - s.MonthsSinceElection) + " months");
            sb.AppendLine();
            sb.AppendLine("Active crises: " + (s.ActiveCrises.Count == 0 ? "none" : string.Join(", ", CrisisNames(s))));
            sb.AppendLine("Foreign relations:");
            foreach (var kv in s.ForeignRelations)
            {
                sb.AppendLine("  " + kv.Key + ": " + kv.Value.ToString("0"));
            }
            sb.AppendLine();
            sb.AppendLine("Events:");
            int from = System.Math.Max(0, s.EventLog.Count - 5);
            for (int i = from; i < s.EventLog.Count; i++)
            {
                sb.AppendLine("  " + s.EventLog[i]);
            }
            body.text = sb.ToString();

            for (int i = 0; i < SliderKeys.Length; i++)
            {
                float v;
                s.PolicySliders.TryGetValue(SliderKeys[i], out v);
                sliderLabels[i].text = SliderKeys[i] + ": " + v.ToString("0.00");
            }
        }

        private static string[] CrisisNames(PoliticalState s)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (var kv in s.ActiveCrises)
            {
                names.Add(kv.Key + " (" + kv.Value + "m left)");
            }
            return names.ToArray();
        }

        private static string PolicyProgressText(PoliticalState s)
        {
            if (s.PolicyProgress.Count == 0)
            {
                return "none";
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in s.PolicyProgress)
            {
                parts.Add(kv.Key + " " + (kv.Value * 100f).ToString("0") + "%");
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string[] Keys(System.Collections.Generic.Dictionary<string, int> d)
        {
            var keys = new System.Collections.Generic.List<string>();
            foreach (var kv in d)
            {
                keys.Add(kv.Key + "->district " + kv.Value);
            }
            return keys.ToArray();
        }
    }
}

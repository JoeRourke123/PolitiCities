using System;
using System.Collections.Generic;
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
        private readonly List<UIComponent> dynamicControls = new List<UIComponent>();
        private readonly System.Random uiRandom = new System.Random();

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
            panel.size = new Vector2(500f, 880f);
            panel.relativePosition = new Vector3(100f, 20f);
            panel.backgroundSprite = "MenuPanelInfo";

            UILabel title = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            title.text = "PolitiCities — Council";
            title.textScale = 1.2f;
            title.relativePosition = new Vector3(12f, 8f);

            body = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            body.textScale = 0.85f;
            body.relativePosition = new Vector3(12f, 34f);
            body.size = new Vector2(476f, 330f);
            body.wordWrap = true;

            UILabel policiesHeader = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            policiesHeader.text = "Policy programme (enact to start implementing):";
            policiesHeader.textScale = 0.9f;
            policiesHeader.relativePosition = new Vector3(12f, 372f);

            for (int i = 0; i < SliderKeys.Length; i++)
            {
                int index = i;
                float y = 742f + i * 30f;
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

            UIButton fund = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            fund.text = "Fund campaign 5000";
            fund.size = new Vector2(160f, 28f);
            fund.relativePosition = new Vector3(12f, 838f);
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
            promote.size = new Vector2(170f, 28f);
            promote.relativePosition = new Vector3(180f, 838f);
            promote.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null)
                {
                    foreach (var kv in new List<KeyValuePair<string, int>>(s.PilotDistricts))
                    {
                        PoliticalEngine.PromotePilot(s, kv.Key);
                    }
                }
                Refresh();
            };

            UIButton close = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            close.text = "Close";
            close.size = new Vector2(100f, 28f);
            close.relativePosition = new Vector3(388f, 838f);
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
                dynamicControls.Clear();
            }
        }

        public void Refresh()
        {
            if (body == null)
            {
                return;
            }
            foreach (UIComponent c in dynamicControls)
            {
                if (c != null)
                {
                    Object.Destroy(c.gameObject);
                }
            }
            dynamicControls.Clear();

            PoliticalState s = PoliticalData.Current;
            if (s == null)
            {
                body.text = "No game loaded.";
                return;
            }

            body.text = BuildBodyText(s);
            BuildPolicyRows(s);
            BuildEventControls(s);

            for (int i = 0; i < SliderKeys.Length; i++)
            {
                float v;
                s.PolicySliders.TryGetValue(SliderKeys[i], out v);
                sliderLabels[i].text = SliderKeys[i] + ": " + v.ToString("0.00");
            }
        }

        private string BuildBodyText(PoliticalState s)
        {
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
                float comp = m < s.Council.MinisterCompetence.Length ? s.Council.MinisterCompetence[m] : 0f;
                sb.AppendLine("  " + Council.MinistryNames[m] + ": " + (owner != null ? owner.Name : "vacant") + " (comp " + comp.ToString("0.00") + ")");
            }
            sb.AppendLine();
            sb.AppendLine("Implementing: " + PolicyProgressText(s));
            sb.AppendLine("Pilots: " + (s.PilotDistricts.Count == 0 ? "none" : string.Join(", ", PilotText(s))));
            sb.AppendLine();
            sb.AppendLine("Districts (ruling party):");
            int shown = 0;
            DistrictManager dm = Singleton<DistrictManager>.instance;
            int count = dm != null ? dm.m_districtCount : 0;
            for (int i = 0; i < count && shown < 5; i++)
            {
                int ruler;
                if (s.RulingParty.TryGetValue(i, out ruler))
                {
                    string lockMark = PoliticalEngine.CanPlayerEditDistrict(s, i) ? "" : " [locked]";
                    sb.AppendLine("  " + dm.GetDistrictName(i) + ": " + Party.Roster[ruler].Name + lockMark);
                    shown++;
                }
            }
            if (shown == 0)
            {
                sb.AppendLine("  no elections held yet");
            }
            sb.AppendLine();
            sb.AppendLine("Campaign budget: " + s.CampaignBudget + "  Corruption: " + (s.Corruption * 100f).ToString("0") + "%");
            sb.AppendLine("Approval: " + (s.LeaderApproval * 100f).ToString("0") + "%  next city election in " + (PoliticalEngine.MonthsPerElection - s.MonthsSinceElection) + " months");
            sb.AppendLine("Crises: " + (s.ActiveCrises.Count == 0 ? "none" : string.Join(", ", CrisisNames(s))));
            sb.AppendLine("Foreign avg: " + PoliticalEngine.AverageForeignRelations(s).ToString("0"));
            sb.AppendLine();
            sb.AppendLine("Events:");
            int from = Math.Max(0, s.EventLog.Count - 4);
            for (int i = from; i < s.EventLog.Count; i++)
            {
                sb.AppendLine("  " + s.EventLog[i]);
            }
            return sb.ToString();
        }

        private void BuildPolicyRows(PoliticalState s)
        {
            for (int pi = 0; pi < Content.Policies.Length; pi++)
            {
                PolicyDef d = Content.Policies[pi];
                float y = 392f + pi * 24f;
                bool implemented = PoliticalEngine.IsPolicyImplemented(s, d.Id);
                bool inProgress = s.PolicyProgress.ContainsKey(d.Id) && !implemented;
                bool unlocked = PoliticalEngine.IsPolicyUnlocked(s, d.Id);
                float progress;
                s.PolicyProgress.TryGetValue(d.Id, out progress);

                UILabel label = panel.AddUIComponent(typeof(UILabel)) as UILabel;
                label.textScale = 0.8f;
                label.relativePosition = new Vector3(12f, y);
                label.size = new Vector2(300f, 20f);
                string state = implemented ? "ACTIVE" : (inProgress ? (progress * 100f).ToString("0") + "%" : (unlocked ? "ready" : "locked (approval " + d.UnlockApproval.ToString("0%") + ")"));
                label.text = d.Name + " — " + state;
                label.tooltip = d.Description;
                dynamicControls.Add(label);

                UIButton enact = panel.AddUIComponent(typeof(UIButton)) as UIButton;
                enact.text = "Enact";
                enact.size = new Vector2(70f, 22f);
                enact.relativePosition = new Vector3(330f, y);
                enact.isEnabled = unlocked && !implemented && !inProgress;
                enact.eventClicked += (c, p) =>
                {
                    PoliticalEngine.EnactPolicy(s, d.Id, -1);
                    PoliticalEngine.LogEvent(s, "Policy enacted: " + d.Name);
                    Refresh();
                };
                dynamicControls.Add(enact);

                UIButton referendum = panel.AddUIComponent(typeof(UIButton)) as UIButton;
                referendum.text = "Referendum";
                referendum.size = new Vector2(90f, 22f);
                referendum.relativePosition = new Vector3(406f, y);
                bool isSubject = Array.IndexOf(Content.ReferendumSubjects, d.Id) >= 0;
                referendum.isEnabled = isSubject && unlocked && !implemented && !inProgress;
                referendum.eventClicked += (c, p) =>
                {
                    if (PoliticalEngine.HoldReferendum(s, uiRandom, d.Name))
                    {
                        PoliticalEngine.EnactPolicy(s, d.Id, -1);
                        s.PolicyProgress[d.Id] = 1f;
                        PoliticalEngine.LogEvent(s, d.Name + " implemented instantly by referendum");
                    }
                    Refresh();
                };
                dynamicControls.Add(referendum);
            }
        }

        private void BuildEventControls(PoliticalState s)
        {
            EventDef e = Content.EventById(s.PendingEvent);
            UILabel header = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            header.textScale = 0.9f;
            header.relativePosition = new Vector3(12f, 592f);
            header.text = e != null ? "CURRENT EVENT:" : "No pending event.";
            dynamicControls.Add(header);
            if (e == null)
            {
                return;
            }

            UILabel text = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            text.textScale = 0.85f;
            text.relativePosition = new Vector3(12f, 612f);
            text.size = new Vector2(476f, 40f);
            text.wordWrap = true;
            text.text = e.Text;
            dynamicControls.Add(text);

            for (int i = 0; i < e.Choices.Length; i++)
            {
                int choice = i;
                EventChoice c = e.Choices[i];
                UIButton button = panel.AddUIComponent(typeof(UIButton)) as UIButton;
                button.text = c.Text;
                button.size = new Vector2(476f, 24f);
                button.relativePosition = new Vector3(12f, 656f + i * 26f);
                button.eventClicked += (comp, p) =>
                {
                    PoliticalEngine.ChooseEventOption(s, uiRandom, choice);
                    Refresh();
                };
                dynamicControls.Add(button);
            }
        }

        private static string PolicyProgressText(PoliticalState s)
        {
            if (s.PolicyProgress.Count == 0)
            {
                return "none";
            }
            var parts = new List<string>();
            foreach (var kv in s.PolicyProgress)
            {
                parts.Add(kv.Key + " " + (kv.Value * 100f).ToString("0") + "%");
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string[] CrisisNames(PoliticalState s)
        {
            var names = new List<string>();
            foreach (var kv in s.ActiveCrises)
            {
                names.Add(kv.Key + " (" + kv.Value + "m left)");
            }
            return names.ToArray();
        }

        private static string[] PilotText(PoliticalState s)
        {
            var keys = new List<string>();
            foreach (var kv in s.PilotDistricts)
            {
                keys.Add(kv.Key + "->district " + kv.Value);
            }
            return keys.ToArray();
        }
    }
}

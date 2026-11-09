using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace PolitiCities.Politics
{
    public sealed class CouncilPanel
    {
        private static readonly string[] SliderKeys = { "policeFunding", "welfare", "greenInvestment" };
        private static readonly string[] TabNames = { "Council", "Policies", "Events", "Districts" };

        private UIPanel panel;
        private UITabContainer tabPages;
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
            panel.size = new Vector2(600f, 660f);
            panel.relativePosition = new Vector3(80f, 40f);
            panel.backgroundSprite = "MenuPanelInfo";

            UILabel title = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            title.text = "PolitiCities";
            title.textScale = 1.25f;
            title.relativePosition = new Vector3(12f, 4f);
            title.size = new Vector2(300f, 24f);

            UITabstrip strip = panel.AddUIComponent(typeof(UITabstrip)) as UITabstrip;
            strip.relativePosition = new Vector3(0f, 32f);
            strip.size = new Vector2(600f, 28f);

            tabPages = panel.AddUIComponent(typeof(UITabContainer)) as UITabContainer;
            tabPages.relativePosition = new Vector3(0f, 64f);
            tabPages.size = new Vector2(600f, 560f);
            strip.tabPages = tabPages;

            for (int i = 0; i < TabNames.Length; i++)
            {
                strip.AddTab(TabNames[i]);
            }
            strip.selectedIndex = 0;

            UIButton fund = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            fund.text = "Fund campaign 5000";
            fund.size = new Vector2(170f, 26f);
            fund.relativePosition = new Vector3(8f, 628f);
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
            promote.size = new Vector2(180f, 26f);
            promote.relativePosition = new Vector3(186f, 628f);
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
            close.size = new Vector2(90f, 26f);
            close.relativePosition = new Vector3(500f, 628f);
            close.eventClicked += (c, p) => Hide();

            Refresh();
        }

        public void Hide()
        {
            if (panel != null)
            {
                UnityEngine.Object.Destroy(panel.gameObject);
                panel = null;
                tabPages = null;
            }
        }

        public void Refresh()
        {
            if (panel == null)
            {
                return;
            }
            PoliticalState s = PoliticalData.Current;
            if (s == null)
            {
                return;
            }
            BuildCouncilPage(s);
            BuildPoliciesPage(s);
            BuildEventsPage(s);
            BuildDistrictsPage(s);
        }

        private UIPanel Page(int index)
        {
            while (tabPages.components.Count <= index)
            {
                tabPages.AddUIComponent(typeof(UIPanel));
            }
            UIPanel page = tabPages.components[index] as UIPanel;
            page.autoLayout = true;
            page.autoLayoutDirection = LayoutDirection.Vertical;
            page.autoLayoutPadding = new RectOffset(8, 8, 6, 6);
            page.size = tabPages.size;
            ClearPage(page);
            return page;
        }

        private static void ClearPage(UIPanel page)
        {
            var children = new List<UIComponent>(page.components);
            foreach (UIComponent c in children)
            {
                if (c != null)
                {
                    UnityEngine.Object.Destroy(c.gameObject);
                }
            }
        }

        private static UILabel AddLabel(UIComponent parent, string text, float scale, float width, float height)
        {
            UILabel label = parent.AddUIComponent(typeof(UILabel)) as UILabel;
            label.text = text;
            label.textScale = scale;
            label.size = new Vector2(width, height);
            return label;
        }

        private static UIButton AddButton(UIComponent parent, string text, float width, float height, MouseEventHandler onClick)
        {
            UIButton button = parent.AddUIComponent(typeof(UIButton)) as UIButton;
            button.text = text;
            button.size = new Vector2(width, height);
            if (onClick != null)
            {
                button.eventClicked += onClick;
            }
            return button;
        }

        private static UIPanel AddRow(UIComponent parent, float width, float height)
        {
            UIPanel row = parent.AddUIComponent(typeof(UIPanel)) as UIPanel;
            row.autoLayout = true;
            row.autoLayoutDirection = LayoutDirection.Horizontal;
            row.autoLayoutPadding = new RectOffset(2, 2, 2, 2);
            row.size = new Vector2(width, height);
            return row;
        }

        private void BuildCouncilPage(PoliticalState s)
        {
            UIPanel page = Page(0);
            AddLabel(page, "Vote intention (poll trend, council seats):", 1.0f, 560f, 22f);
            for (int i = 0; i < Party.Roster.Length; i++)
            {
                Party p = Party.Roster[i];
                int seats = i < s.Council.Seats.Length ? s.Council.Seats[i] : 0;
                string coalition = i < s.Council.InCoalition.Length && s.Council.InCoalition[i] ? " [in government]" : "";
                int trend = PoliticalEngine.PollTrend(s, i);
                string arrow = trend > 0 ? "up" : (trend < 0 ? "down" : "flat");
                AddLabel(page, p.Name + ":  " + (s.VoteShare[i] * 100f).ToString("0.0") + "% (" + arrow + ")  -  seats " + seats + coalition, 0.9f, 560f, 20f);
            }

            AddLabel(page, "", 0.9f, 560f, 8f);
            AddLabel(page, "Governing: " + (s.Council.GoverningParty >= 0 ? Party.Roster[s.Council.GoverningParty].Name : "none") + "  -  coalition " + s.Council.CoalitionSeats() + "/" + Council.SeatCount + " seats", 0.9f, 560f, 20f);
            AddLabel(page, "Ministries:", 1.0f, 560f, 22f);
            for (int m = 0; m < Council.MinistryNames.Length; m++)
            {
                string pid = m < s.Council.MinistryParty.Length ? s.Council.MinistryParty[m] : null;
                Party owner = pid != null ? Party.ById(pid) : null;
                float comp = m < s.Council.MinisterCompetence.Length ? s.Council.MinisterCompetence[m] : 0f;
                float integ = m < s.Council.MinisterIntegrity.Length ? s.Council.MinisterIntegrity[m] : 0f;
                AddLabel(page, "  " + Council.MinistryNames[m] + ": " + (owner != null ? owner.Name : "vacant") + "  (competence " + comp.ToString("0.00") + ", integrity " + integ.ToString("0.00") + ")", 0.9f, 560f, 20f);
            }

            AddLabel(page, "", 0.9f, 560f, 8f);
            AddLabel(page, "Approval: " + (s.LeaderApproval * 100f).ToString("0") + "%   Corruption: " + (s.Corruption * 100f).ToString("0") + "%   Campaign budget: " + s.CampaignBudget, 0.9f, 560f, 20f);
            AddLabel(page, "Next city election in " + (PoliticalEngine.MonthsPerElection - s.MonthsSinceElection) + " months   Active crises: " + (s.ActiveCrises.Count == 0 ? "none" : JoinCrises(s)), 0.9f, 560f, 20f);
            AddLabel(page, "Foreign relations (avg " + PoliticalEngine.AverageForeignRelations(s).ToString("0") + "): " + JoinForeign(s), 0.9f, 560f, 20f);

            AddLabel(page, "", 0.9f, 560f, 8f);
            AddLabel(page, "Policy sliders:", 1.0f, 560f, 22f);
            for (int i = 0; i < SliderKeys.Length; i++)
            {
                string key = SliderKeys[i];
                UIPanel row = AddRow(page, 560f, 26f);
                float v;
                s.PolicySliders.TryGetValue(key, out v);
                AddLabel(row, key, 0.9f, 220f, 22f);
                AddLabel(row, v.ToString("0.00"), 0.9f, 60f, 22f);
                AddButton(row, "-", 30f, 22f, (c, p) => { PoliticalEngine.AdjustSlider(s, key, -PoliticalEngine.SliderStep); Refresh(); });
                AddButton(row, "+", 30f, 22f, (c, p) => { PoliticalEngine.AdjustSlider(s, key, PoliticalEngine.SliderStep); Refresh(); });
            }
        }

        private void BuildPoliciesPage(PoliticalState s)
        {
            UIPanel page = Page(1);
            AddLabel(page, "Policy programme - Enact starts implementation; Referendum implements instantly if it passes:", 0.9f, 560f, 20f);
            foreach (PolicyDef def in Content.Policies)
            {
                PolicyDef d = def;
                bool implemented = PoliticalEngine.IsPolicyImplemented(s, d.Id);
                bool inProgress = s.PolicyProgress.ContainsKey(d.Id) && !implemented;
                bool unlocked = PoliticalEngine.IsPolicyUnlocked(s, d.Id);
                float progress;
                s.PolicyProgress.TryGetValue(d.Id, out progress);
                string state = implemented ? "ACTIVE" : (inProgress ? "implementing " + (progress * 100f).ToString("0") + "%" : (unlocked ? "ready" : "locked (needs " + d.UnlockApproval.ToString("0%") + " approval)"));

                UIPanel row = AddRow(page, 560f, 26f);
                UILabel name = AddLabel(row, d.Name, 0.9f, 170f, 22f);
                name.tooltip = d.Description;
                AddLabel(row, state, 0.85f, 180f, 22f);
                UIButton enact = AddButton(row, "Enact", 80f, 22f, (c, p) =>
                {
                    PoliticalEngine.EnactPolicy(s, d.Id, -1);
                    PoliticalEngine.LogEvent(s, "Policy enacted: " + d.Name);
                    Refresh();
                });
                enact.isEnabled = unlocked && !implemented && !inProgress;
                bool isSubject = Array.IndexOf(Content.ReferendumSubjects, d.Id) >= 0;
                UIButton referendum = AddButton(row, "Referendum", 100f, 22f, (c, p) =>
                {
                    if (PoliticalEngine.HoldReferendum(s, uiRandom, d.Name))
                    {
                        PoliticalEngine.EnactPolicy(s, d.Id, -1);
                        s.PolicyProgress[d.Id] = 1f;
                        PoliticalEngine.LogEvent(s, d.Name + " implemented instantly by referendum");
                    }
                    Refresh();
                });
                referendum.isEnabled = isSubject && unlocked && !implemented && !inProgress;
                referendum.tooltip = "Put " + d.Name + " to a direct vote";
            }
            AddLabel(page, "", 0.9f, 560f, 8f);
            AddLabel(page, "Pilots: " + (s.PilotDistricts.Count == 0 ? "none" : JoinPilots(s)) + "   Implementing: " + JoinProgress(s), 0.85f, 560f, 20f);
        }

        private void BuildEventsPage(PoliticalState s)
        {
            UIPanel page = Page(2);
            EventDef e = Content.EventById(s.PendingEvent);
            if (e != null)
            {
                AddLabel(page, "CURRENT EVENT:", 1.0f, 560f, 22f);
                UILabel text = AddLabel(page, e.Text, 0.9f, 560f, 44f);
                text.wordWrap = true;
                for (int i = 0; i < e.Choices.Length; i++)
                {
                    int choice = i;
                    AddButton(page, e.Choices[i].Text, 560f, 24f, (c, p) =>
                    {
                        PoliticalEngine.ChooseEventOption(s, uiRandom, choice);
                        Refresh();
                    });
                }
            }
            else
            {
                AddLabel(page, "No pending event right now.", 0.9f, 560f, 22f);
            }
            AddLabel(page, "", 0.9f, 560f, 8f);
            AddLabel(page, "Event log:", 1.0f, 560f, 22f);
            int from = Math.Max(0, s.EventLog.Count - 12);
            for (int i = from; i < s.EventLog.Count; i++)
            {
                AddLabel(page, s.EventLog[i], 0.85f, 560f, 18f);
            }
        }

        private void BuildDistrictsPage(PoliticalState s)
        {
            UIPanel page = Page(3);
            DistrictManager dm = Singleton<DistrictManager>.instance;
            int count = dm != null ? dm.m_districtCount : 0;
            AddLabel(page, "Districts - elections every " + PoliticalEngine.MonthsPerDistrictElection + " months. Opposition districts are locked. Spend campaign funds to boost a district:", 0.85f, 560f, 36f).wordWrap = true;
            int shown = 0;
            for (int i = 0; i < count && shown < 12; i++)
            {
                int district = i;
                int ruler;
                string ruling = s.RulingParty.TryGetValue(i, out ruler) ? Party.Roster[ruler].Name : "no election yet";
                bool editable = PoliticalEngine.CanPlayerEditDistrict(s, district);
                string name = dm.GetDistrictName(i);
                UIPanel row = AddRow(page, 560f, 26f);
                AddLabel(row, name + ": " + ruling + (editable ? "" : " [locked]"), 0.9f, 330f, 22f);
                float boost;
                s.CampaignBoost.TryGetValue(district, out boost);
                AddLabel(row, "boost " + boost.ToString("0.00"), 0.85f, 90f, 22f);
                AddButton(row, "Spend 500", 100f, 22f, (c, p) =>
                {
                    PoliticalEngine.SpendOnDistrict(s, district, 500);
                    Refresh();
                });
                shown++;
            }
            if (count > 12)
            {
                AddLabel(page, "... and " + (count - 12) + " more districts", 0.85f, 560f, 20f);
            }
        }

        private static string JoinCrises(PoliticalState s)
        {
            var parts = new List<string>();
            foreach (var kv in s.ActiveCrises)
            {
                parts.Add(kv.Key + " (" + kv.Value + "m)");
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string JoinForeign(PoliticalState s)
        {
            var parts = new List<string>();
            foreach (var kv in s.ForeignRelations)
            {
                parts.Add(kv.Key + " " + kv.Value.ToString("0"));
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string JoinPilots(PoliticalState s)
        {
            var parts = new List<string>();
            foreach (var kv in s.PilotDistricts)
            {
                parts.Add(kv.Key + " in district " + kv.Value);
            }
            return string.Join(", ", parts.ToArray());
        }

        private static string JoinProgress(PoliticalState s)
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
    }
}

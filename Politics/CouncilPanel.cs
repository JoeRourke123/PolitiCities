using System.Text;
using ColossalFramework.UI;
using UnityEngine;

namespace PolitiCities.Politics
{
    public sealed class CouncilPanel
    {
        private UIPanel panel;
        private UILabel body;

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
            panel.size = new Vector2(460f, 520f);
            panel.relativePosition = new Vector3(100f, 60f);
            panel.backgroundSprite = "MenuPanelInfo";

            UILabel title = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            title.text = "PolitiCities — Council";
            title.textScale = 1.2f;
            title.relativePosition = new Vector3(12f, 8f);

            body = panel.AddUIComponent(typeof(UILabel)) as UILabel;
            body.textScale = 0.9f;
            body.relativePosition = new Vector3(12f, 40f);
            body.size = new Vector2(436f, 420f);
            body.wordWrap = true;

            UIButton enact = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            enact.text = "Enact stimulus package";
            enact.size = new Vector2(180f, 30f);
            enact.relativePosition = new Vector3(12f, 480f);
            enact.eventClicked += (c, p) =>
            {
                PoliticalState s = PoliticalData.Current;
                if (s != null)
                {
                    PoliticalSimulation.EnactPolicy(s, Patches.StimulusIncomePatch.PolicyId, -1);
                }
                Refresh();
            };

            UIButton close = panel.AddUIComponent(typeof(UIButton)) as UIButton;
            close.text = "Close";
            close.size = new Vector2(100f, 30f);
            close.relativePosition = new Vector3(350f, 480f);
            close.eventClicked += (c, p) => Hide();

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
                sb.AppendLine("  " + p.Name + ": " + (s.VoteShare[i] * 100f).ToString("0.0") + "%  seats " + seats + coalition);
            }
            sb.AppendLine();
            sb.AppendLine("Governing party: " + (s.Council.GoverningParty >= 0 ? Party.Roster[s.Council.GoverningParty].Name : "none") + " (" + s.Council.CoalitionSeats() + "/" + Council.SeatCount + ")");
            sb.AppendLine("Ministries:");
            for (int m = 0; m < Council.MinistryNames.Length; m++)
            {
                string pid = m < s.Council.MinistryParty.Length ? s.Council.MinistryParty[m] : null;
                Party owner = pid != null ? Party.ById(pid) : null;
                sb.AppendLine("  " + Council.MinistryNames[m] + ": " + (owner != null ? owner.Name : "vacant"));
            }
            sb.AppendLine();
            sb.AppendLine("Policy sliders:");
            foreach (var kv in s.PolicySliders)
            {
                sb.AppendLine("  " + kv.Key + ": " + kv.Value.ToString("0.00"));
            }
            sb.AppendLine();
            sb.AppendLine("Implementing policies:");
            foreach (var kv in s.PolicyProgress)
            {
                string pilot = s.PilotDistricts.ContainsKey(kv.Key) ? " (pilot district " + s.PilotDistricts[kv.Key] + ")" : "";
                sb.AppendLine("  " + kv.Key + ": " + (kv.Value * 100f).ToString("0") + "%" + pilot);
            }
            sb.AppendLine();
            sb.AppendLine("Campaign budget: " + s.CampaignBudget);
            sb.AppendLine("Campaign boosts: " + s.CampaignBoost.Count + " district(s)");
            sb.AppendLine("Approval: " + (s.LeaderApproval * 100f).ToString("0") + "%   Next election in " + (PoliticalSimulation.MonthsPerElection - s.MonthsSinceElection) + " months");

            body.text = sb.ToString();
        }
    }
}

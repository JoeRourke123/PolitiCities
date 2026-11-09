using System;
using System.Collections.Generic;

namespace PolitiCities.Politics
{
    public sealed class PolicyDef
    {
        public string Id;
        public string Name;
        public string Description;
        public float UnlockApproval;
        public float ApprovalDeltaPerMonth;
        public float IncomeMultiplier = 1f;
        public float ForeignDelta;

        public PolicyDef(string id, string name, string description, float unlockApproval, float approvalDelta, float incomeMultiplier, float foreignDelta)
        {
            Id = id;
            Name = name;
            Description = description;
            UnlockApproval = unlockApproval;
            ApprovalDeltaPerMonth = approvalDelta;
            IncomeMultiplier = incomeMultiplier;
            ForeignDelta = foreignDelta;
        }
    }

    public sealed class EventChoice
    {
        public string Text;
        public float ApprovalDelta;
        public int BudgetDelta;
        public float CorruptionDelta;
        public float ForeignDelta;

        public EventChoice(string text, float approvalDelta, int budgetDelta, float corruptionDelta, float foreignDelta)
        {
            Text = text;
            ApprovalDelta = approvalDelta;
            BudgetDelta = budgetDelta;
            CorruptionDelta = corruptionDelta;
            ForeignDelta = foreignDelta;
        }
    }

    public sealed class EventDef
    {
        public string Id;
        public string Text;
        public EventChoice[] Choices;

        public EventDef(string id, string text, params EventChoice[] choices)
        {
            Id = id;
            Text = text;
            Choices = choices;
        }
    }

    public static class Content
    {
        public static readonly PolicyDef[] Policies =
        {
            new PolicyDef("stimulusPackage", "Stimulus Package", "+10% private income, small approval cost", 0.3f, -0.02f, 1.10f, 0f),
            new PolicyDef("greenNewDeal", "Green New Deal", "+approval, slight income cost, better foreign relations", 0.4f, 0.01f, 0.97f, 2f),
            new PolicyDef("austerityPackage", "Austerity Package", "+income, hurts approval", 0.4f, -0.03f, 1.05f, -1f),
            new PolicyDef("surveillanceState", "Surveillance State", "hurts approval and foreign relations", 0.6f, -0.02f, 1.0f, -5f),
            new PolicyDef("openBorders", "Open Borders", "+approval, +trade income, +foreign relations", 0.5f, 0.01f, 1.03f, 8f),
            new PolicyDef("housingProgram", "Housing Program", "+approval, slight income cost", 0.45f, 0.02f, 0.98f, 0f),
            new PolicyDef("taxReform", "Tax Reform", "+income, small approval cost", 0.55f, -0.01f, 1.06f, 0f),
            new PolicyDef("propagandaMachine", "Propaganda Machine", "short-term approval, raises corruption fast", 0.6f, 0.015f, 1.0f, 0f),
        };

        public static readonly EventDef[] Events =
        {
            new EventDef("factoryClosure", "A major factory in the industrial district is closing down.",
                new EventChoice("Bail it out (-2000)", 0.03f, -2000, 0.02f, 0f),
                new EventChoice("Let it close", -0.04f, 0, 0f, 0f),
                new EventChoice("Say nothing", -0.01f, 0, 0.01f, 0f)),
            new EventDef("foreignDelegation", "A delegation from a foreign power requests a state visit.",
                new EventChoice("Welcome them", 0.01f, -500, 0f, 10f),
                new EventChoice("Snub them", -0.02f, 0, 0f, -10f)),
            new EventDef("protests", "Protests have erupted in the city center.",
                new EventChoice("Negotiate", 0.02f, -1000, 0.01f, 0f),
                new EventChoice("Crack down", -0.05f, 0, 0.03f, -3f)),
            new EventDef("whistleblower", "A whistleblower leaks internal documents.",
                new EventChoice("Investigate (-1000)", 0.02f, -1000, -0.03f, 0f),
                new EventChoice("Deny everything", -0.02f, 0, 0.05f, 0f)),
            new EventDef("techExpo", "A international tech expo wants to be hosted in your city.",
                new EventChoice("Host it (-1500)", 0.03f, -1500, 0.01f, 5f),
                new EventChoice("Decline", -0.01f, 0, 0f, 0f)),
            new EventDef("rentCrisis", "Rents are rising sharply and citizens are angry.",
                new EventChoice("Impose rent control", 0.03f, 0, 0f, 0f),
                new EventChoice("Do nothing", -0.03f, 0, 0f, 0f)),
            new EventDef("oilShock", "Oil prices have spiked after a foreign conflict.",
                new EventChoice("Subsidize fuel (-3000)", 0.02f, -3000, 0.02f, 0f),
                new EventChoice("Free market", -0.02f, 0, 0f, 2f)),
            new EventDef("dataBreach", "A data breach exposes city hall records.",
                new EventChoice("Disclose it", -0.01f, -800, -0.02f, 0f),
                new EventChoice("Cover it up", -0.03f, 0, 0.08f, 0f)),
            new EventDef("grantOffer", "The regional government offers a grant with conditions.",
                new EventChoice("Accept (+5000)", 0.01f, 5000, 0.01f, 3f),
                new EventChoice("Decline", -0.01f, 0, 0f, -3f)),
            new EventDef("concert", "A famous artist offers a cheap concert for your city.",
                new EventChoice("Subsidize it (-1000)", 0.02f, -1000, 0f, 2f),
                new EventChoice("Decline", -0.01f, 0, 0f, 0f)),
        };

        public static readonly string[] ReferendumSubjects = { "greenNewDeal", "austerityPackage", "surveillanceState", "openBorders" };

        public static PolicyDef PolicyById(string id)
        {
            foreach (PolicyDef p in Policies)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        public static EventDef EventById(string id)
        {
            foreach (EventDef e in Events)
            {
                if (e.Id == id)
                {
                    return e;
                }
            }
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;

namespace PolitiCities.Politics
{
    public sealed class PoliticalState
    {
        public const int SaveVersion = 1;

        public float[] VoteShare = new float[Party.Roster.Length];
        public float LeaderApproval = 0.6f;
        public float AdminCapacity = 1f;
        public int CampaignBudget;
        public int MonthsSinceElection;
        public int TickCounter;
        public int RandomSeed = 1;

        public Council Council = new Council();
        public Dictionary<string, float> PolicySliders = new Dictionary<string, float>();
        public Dictionary<string, float> PolicyProgress = new Dictionary<string, float>();
        public Dictionary<string, int> PilotDistricts = new Dictionary<string, int>();
        public Dictionary<int, float> CampaignBoost = new Dictionary<int, float>();

        public PoliticalState()
        {
            for (int i = 0; i < VoteShare.Length; i++)
            {
                VoteShare[i] = 1f / VoteShare.Length;
            }
            PolicySliders["policeFunding"] = 0.5f;
            PolicySliders["welfare"] = 0.5f;
            PolicySliders["greenInvestment"] = 0.5f;
            Council.RecomputeFromShares(VoteShare);
        }

        public void Write(BinaryWriter w)
        {
            w.Write(SaveVersion);
            w.Write(RandomSeed);
            w.Write(LeaderApproval);
            w.Write(AdminCapacity);
            w.Write(CampaignBudget);
            w.Write(MonthsSinceElection);
            w.Write(TickCounter);
            w.Write(VoteShare.Length);
            foreach (float s in VoteShare)
            {
                w.Write(s);
            }
            WriteDict(w, PolicySliders);
            WriteDict(w, PolicyProgress);
            WriteDict(w, PilotDistricts);
            WriteDict(w, CampaignBoost);
            w.Write(Council.Seats.Length);
            foreach (int s in Council.Seats)
            {
                w.Write(s);
            }
            w.Write(Council.InCoalition.Length);
            foreach (bool b in Council.InCoalition)
            {
                w.Write(b);
            }
            w.Write(Council.MinistryParty.Length);
            foreach (string m in Council.MinistryParty)
            {
                w.Write(m ?? string.Empty);
            }
            w.Write(Council.GoverningParty);
        }

        public void Read(BinaryReader r)
        {
            int version = r.ReadInt32();
            if (version != SaveVersion)
            {
                return;
            }
            RandomSeed = r.ReadInt32();
            LeaderApproval = r.ReadSingle();
            AdminCapacity = r.ReadSingle();
            CampaignBudget = r.ReadInt32();
            MonthsSinceElection = r.ReadInt32();
            TickCounter = r.ReadInt32();
            int n = r.ReadInt32();
            VoteShare = new float[n];
            for (int i = 0; i < n; i++)
            {
                VoteShare[i] = r.ReadSingle();
            }
            PolicySliders = ReadDictFloat(r);
            PolicyProgress = ReadDictFloat(r);
            PilotDistricts = ReadDictInt(r);
            CampaignBoost = ReadDictIntFloat(r);
            int seats = r.ReadInt32();
            Council.Seats = new int[seats];
            for (int i = 0; i < seats; i++)
            {
                Council.Seats[i] = r.ReadInt32();
            }
            int flags = r.ReadInt32();
            Council.InCoalition = new bool[flags];
            for (int i = 0; i < flags; i++)
            {
                Council.InCoalition[i] = r.ReadBoolean();
            }
            int ministries = r.ReadInt32();
            Council.MinistryParty = new string[ministries];
            for (int i = 0; i < ministries; i++)
            {
                string m = r.ReadString();
                Council.MinistryParty[i] = m.Length == 0 ? null : m;
            }
            Council.GoverningParty = r.ReadInt32();
        }

        private static void WriteDict(BinaryWriter w, Dictionary<string, float> d)
        {
            w.Write(d.Count);
            foreach (KeyValuePair<string, float> kv in d)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
        }

        private static void WriteDict(BinaryWriter w, Dictionary<string, int> d)
        {
            w.Write(d.Count);
            foreach (KeyValuePair<string, int> kv in d)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
        }

        private static void WriteDict(BinaryWriter w, Dictionary<int, float> d)
        {
            w.Write(d.Count);
            foreach (KeyValuePair<int, float> kv in d)
            {
                w.Write(kv.Key);
                w.Write(kv.Value);
            }
        }

        private static Dictionary<string, float> ReadDictFloat(BinaryReader r)
        {
            var d = new Dictionary<string, float>();
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                d[r.ReadString()] = r.ReadSingle();
            }
            return d;
        }

        private static Dictionary<string, int> ReadDictInt(BinaryReader r)
        {
            var d = new Dictionary<string, int>();
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                d[r.ReadString()] = r.ReadInt32();
            }
            return d;
        }

        private static Dictionary<int, float> ReadDictIntFloat(BinaryReader r)
        {
            var d = new Dictionary<int, float>();
            int n = r.ReadInt32();
            for (int i = 0; i < n; i++)
            {
                d[r.ReadInt32()] = r.ReadSingle();
            }
            return d;
        }
    }
}

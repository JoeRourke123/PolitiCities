using System;
using System.Collections.Generic;

namespace PolitiCities.Politics
{
    public sealed class Council
    {
        public const int SeatCount = 81;

        public static readonly string[] MinistryNames =
        {
            "Finance", "Infrastructure", "Environment", "Security", "Social Affairs"
        };

        public int[] Seats = new int[Party.Roster.Length];
        public bool[] InCoalition = new bool[Party.Roster.Length];
        public string[] MinistryParty = new string[MinistryNames.Length];
        public int GoverningParty = -1;

        public void RecomputeFromShares(float[] shares)
        {
            Array.Clear(Seats, 0, Seats.Length);
            float total = 0f;
            foreach (float s in shares)
            {
                total += s;
            }
            if (total <= 0f)
            {
                return;
            }

            float[] exact = new float[shares.Length];
            int assigned = 0;
            for (int i = 0; i < shares.Length; i++)
            {
                exact[i] = shares[i] / total * SeatCount;
                Seats[i] = (int)Math.Floor(exact[i]);
                assigned += Seats[i];
            }

            while (assigned < SeatCount)
            {
                int best = 0;
                float bestRemainder = -1f;
                for (int i = 0; i < shares.Length; i++)
                {
                    float remainder = exact[i] - Seats[i];
                    if (remainder > bestRemainder)
                    {
                        bestRemainder = remainder;
                        best = i;
                    }
                }
                Seats[best]++;
                assigned++;
            }

            FormCoalition();
        }

        public void FormCoalition()
        {
            Array.Clear(InCoalition, 0, InCoalition.Length);
            Array.Clear(MinistryParty, 0, MinistryParty.Length);
            GoverningParty = -1;

            int[] bySeats = new int[Party.Roster.Length];
            for (int i = 0; i < bySeats.Length; i++)
            {
                bySeats[i] = i;
            }
            Array.Sort(bySeats, (a, b) => Seats[b].CompareTo(Seats[a]));

            int coalitionSeats = 0;
            int members = 0;
            foreach (int i in bySeats)
            {
                if (Seats[i] == 0)
                {
                    continue;
                }
                InCoalition[i] = true;
                coalitionSeats += Seats[i];
                members++;
                if (GoverningParty < 0)
                {
                    GoverningParty = i;
                }
                if (coalitionSeats > SeatCount / 2)
                {
                    break;
                }
            }

            for (int m = 0; m < MinistryNames.Length; m++)
            {
                int best = -1;
                int bestSeats = -1;
                for (int i = 0; i < Party.Roster.Length; i++)
                {
                    if (InCoalition[i] && Seats[i] > bestSeats)
                    {
                        bool taken = false;
                        for (int k = 0; k < m; k++)
                        {
                            if (MinistryParty[k] == Party.Roster[i].Id)
                            {
                                taken = true;
                                break;
                            }
                        }
                        if (!taken)
                        {
                            bestSeats = Seats[i];
                            best = i;
                        }
                    }
                }
                MinistryParty[m] = best >= 0 ? Party.Roster[best].Id : null;
            }
        }

        public int CoalitionSeats()
        {
            int sum = 0;
            for (int i = 0; i < InCoalition.Length; i++)
            {
                if (InCoalition[i])
                {
                    sum += Seats[i];
                }
            }
            return sum;
        }
    }
}

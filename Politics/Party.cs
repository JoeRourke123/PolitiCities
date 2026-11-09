namespace PolitiCities.Politics
{
    public sealed class Party
    {
        public string Id;
        public string Name;
        public string ColorHex;
        public float Economy;
        public float Green;
        public float Order;

        public static readonly Party[] Roster =
        {
            new Party { Id = "workers",  Name = "Workers' Alliance",  ColorHex = "#C0392B", Economy = -0.9f, Green = 0.1f,  Order = 0.2f },
            new Party { Id = "green",    Name = "Green Future",       ColorHex = "#27AE60", Economy = -0.2f, Green = 0.9f,  Order = 0.1f },
            new Party { Id = "market",   Name = "Free Market Forum",  ColorHex = "#2980B9", Economy = 0.9f,  Green = -0.3f, Order = -0.2f },
            new Party { Id = "order",    Name = "Order & Tradition",  ColorHex = "#8E44AD", Economy = 0.2f,  Green = -0.2f, Order = 0.9f },
            new Party { Id = "centrist", Name = "Centrist Union",     ColorHex = "#7F8C8D", Economy = 0f,    Green = 0f,    Order = 0f },
            new Party { Id = "liberty",  Name = "Libertarian Voice",  ColorHex = "#F1C40F", Economy = 0.6f,  Green = -0.1f, Order = -0.9f },
        };

        public static Party ById(string id)
        {
            foreach (Party p in Roster)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            return null;
        }
    }
}

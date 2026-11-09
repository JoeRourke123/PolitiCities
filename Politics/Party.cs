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
        public string[] Platform;

        public static readonly Party[] Roster =
        {
            new Party { Id = "workers",  Name = "Workers' Alliance",  ColorHex = "#C0392B", Economy = -0.9f, Green = 0.1f,  Order = 0.2f,  Platform = new[] { "FreeTransport", "SchoolsOut" } },
            new Party { Id = "green",    Name = "Green Future",       ColorHex = "#27AE60", Economy = -0.2f, Green = 0.9f,  Order = 0.1f,  Platform = new[] { "Recycling", "PreferParks" } },
            new Party { Id = "market",   Name = "Free Market Forum",  ColorHex = "#2980B9", Economy = 0.9f,  Green = -0.3f, Order = -0.2f, Platform = new[] { "HighTicketPrices", "FreeWifi" } },
            new Party { Id = "order",    Name = "Order & Tradition",  ColorHex = "#8E44AD", Economy = 0.2f,  Green = -0.2f, Order = 0.9f,  Platform = new[] { "SmokingBan", "PetBan" } },
            new Party { Id = "centrist", Name = "Centrist Union",     ColorHex = "#7F8C8D", Economy = 0f,    Green = 0f,    Order = 0f,    Platform = new[] { "RecreationBoost", "EducationBoost" } },
            new Party { Id = "liberty",  Name = "Libertarian Voice",  ColorHex = "#F1C40F", Economy = 0.6f,  Green = -0.1f, Order = -0.9f, Platform = new[] { "RecreationalUse", "HighTicketPrices" } },
            new Party { Id = "tech",     Name = "Tech Progress",      ColorHex = "#16A085", Economy = 0.3f,  Green = 0.3f,  Order = -0.3f, Platform = new[] { "FreeWifi", "Hightech" } },
            new Party { Id = "populist", Name = "Populist Bloc",      ColorHex = "#D35400", Economy = -0.4f, Green = -0.4f, Order = 0.6f,  Platform = new[] { "SmokingBan", "PetBan", "PowerSaving" } },
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

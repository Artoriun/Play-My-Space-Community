namespace PlayMySpace.PMSC.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class WorldData
    {
        public Dictionary<string, SpawnLocation> locations;
        public string currentServerTime;

        public WorldData()
        {
            locations = new Dictionary<string, SpawnLocation>();
            currentServerTime = DateTime.UtcNow.ToString();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Locations: \n");

            foreach (SpawnLocation location in locations.Values)
            {
                stringBuilder.Append(location + " \t");
            }

            return stringBuilder.ToString();
        }
    }
}

namespace PlayMySpace.PMSC.Models
{
    using System.Text;

    public class SpawnLocation
    {
        public string locationId;
        public bool active;
        public PlayableLocationLatLng snappedPoint;
        public string spawnableType;

        public SpawnLocation()
        {

        }

        public SpawnLocation(string locationId, bool active, PlayableLocationLatLng snappedPoint, string spawnableType)
        {
            this.locationId = locationId;
            this.active = active;
            this.snappedPoint = snappedPoint;
            this.spawnableType = spawnableType;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Id: " + locationId + "  spawnableType: " + spawnableType);

            return sb.ToString();
        }
    }
}

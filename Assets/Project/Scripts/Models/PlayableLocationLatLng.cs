namespace PlayMySpace.PMSC.Models
{
    public class PlayableLocationLatLng
    {
        public double latitude;
        public double longitude;

        public override string ToString()
        {
            return "latitude: " + latitude + ", longitude: " + longitude;
        }
    }
}

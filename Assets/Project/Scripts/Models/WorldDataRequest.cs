namespace PlayMySpace.PMSC.Models
{
    public class WorldDataRequest
    {
        public PlayableLocationLatLng northEast;
        public PlayableLocationLatLng southWest;

        public override string ToString()
        {
            return "northeast: " + northEast + ", southeast: " + southWest;
        }
    }
}

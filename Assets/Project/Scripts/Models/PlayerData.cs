namespace PlayMySpace.PMSC.Models
{
    public class PlayerData
    {
        public string userId;
        public string name;

        public PlayerData()
        {
        }

        public PlayerData(string userId, string name)
        {
            this.userId = userId;
            this.name = name;
        }
    }
}

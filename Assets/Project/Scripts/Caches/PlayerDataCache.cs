namespace PlayMySpace.PMSC.Caches
{
    using UnityEngine;
    using PlayMySpace.PMSC.Models;
    using Framework.Patterns;

    /// <summary>
    /// PlayerDataCache.cs
    /// 
    /// A cache containing PlayerData.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PlayerDataCache : PersistentSingleton<PlayerDataCache>
    {
        #region Class Members
        private PlayerData playerData;
        #endregion

        #region Class Accessors
        public PlayerData PlayerData
        {
            get
            {
                return playerData;
            }
            set
            {
                playerData = value;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();
            playerData = new PlayerData();
        }
        #endregion
    }
}

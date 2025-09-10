namespace Framework.Firestore.Wrappers
{
    using System;
    using UnityEngine;
    using PlayMySpace.PMSC.Services;
    using PlayMySpace.PMSC.Models;

    public class PlayerDataWrapper : DataWrapper<PlayerDataWrapper, PlayerData>
    {
        #region Class Members
        [SerializeField] private PlayerService playerService;
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();
        }
        #endregion

        #region Class Implementation - Private
        #endregion

        #region Class Implementation - Public
        public override void GetData(Action<PlayerData> successCallback = null, Action<string> errorCallback = null)
        {
            StartCoroutine(playerService.GetPlayerData(successCallback, errorCallback));
        }

        public override void PostData(PlayerData data, Action<PlayerData> successCallback = null, Action<string> errorCallback = null)
        {
            StartCoroutine(playerService.PostPlayerData(data, successCallback, errorCallback));
        }
        #endregion
    }
}

namespace PlayMySpace.PMSC.Network
{
    using UnityEngine;
    using Mirror;

    /// <summary>
    /// StartMirrorServer.cs
    /// 
    /// Handles the launching of the Mirror server that all the Mirror clients connect to.
    /// 
    /// Copyright © 2021 PlayMySpace
    /// </summary>
    public class StartMirrorServer : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private MirrorConfiguration mirrorConfiguration;
        [SerializeField] private NetworkManager networkManager;
        #endregion

        #region MonoBehaviour Stuff
        private void Start()
        {
            if (mirrorConfiguration.buildType == BuildType.REMOTE_SERVER)
            {
                StartRemoteServer();
            }
        }
        #endregion

        #region Class Implementation - Private
        private void StartRemoteServer()
        {

        }
        #endregion

        #region Class Implementation - Public
        public void OnStartLocalServer()
        {
            if (mirrorConfiguration.buildType == BuildType.LOCAL_SERVER)
            {
                networkManager.StartServer();
            }
        }
        #endregion
    }
}

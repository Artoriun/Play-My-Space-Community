namespace PlayMySpace.PMSC.Network
{
    using UnityEngine;
    using Mirror;

    /// <summary>
    /// StartMirrorClient.cs
    /// 
    /// Handles the launching of a Mirror client that will connect to a Mirror server.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class StartMirrorClient : MonoBehaviour
    {
        #region Class Members
        [SerializeField] MirrorConfiguration mirrorConfiguration;
        [SerializeField] StartMirrorServer startMirrorServer;
        [SerializeField] NetworkManager networkManager;
        #endregion

        #region Class Implementation - Private
        private void RemoteUserLogin()
        {
            Debug.Log("[StartMirrorClient].RemoteUserLogin");


        }
        #endregion

        #region Class Implementation - Public
        public void OnUserLogin()
        {
            if (mirrorConfiguration.buildType == BuildType.REMOTE_CLIENT)
            {
                if (mirrorConfiguration.buildId == "")
                {
                    throw new System.Exception("A remote client must have a PlayFab buildId.");
                }
                else
                {
                    RemoteUserLogin();
                }
            }
            else if (mirrorConfiguration.buildType == BuildType.LOCAL_CLIENT)
            {
                networkManager.StartClient();
            }
        }
        #endregion
    }
}

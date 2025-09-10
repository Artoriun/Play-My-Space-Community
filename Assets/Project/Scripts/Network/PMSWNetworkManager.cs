namespace PlayMySpace.PMSC.Network
{
    using UnityEngine;
    using Mirror;
    using PlayMySpace.PMSC.Managers;

    /// <summary>
    /// PMSWNetworkManager.cs
    /// 
    /// Handles all network logic for the game. Sends messages from server to clients and vice versa.
    /// 
    /// Copyright © Play My Space 2021
    /// </summary>
    public class PMSWNetworkManager : NetworkManager
    {
        #region Class Members
        #endregion

        #region MonoBehaviour Stuff

        #endregion

        #region Class Implementation
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("Client connected!");
        }

        public void ReplacePlayerObject(NetworkConnection conn, GameObject playerObject)
        {
            NetworkServer.ReplacePlayerForConnection(conn, Instantiate(playerObject), true);
            Debug.Log("Replaced Player Object");
        }

        public void AssignAuthority(NetworkConnection conn, NetworkIdentity netId)
        {
            netId.AssignClientAuthority(conn);
        }

        public void RegisterPrefab(GameObject o)
        {
            NetworkClient.RegisterPrefab(o);
        }

        public GameObject SpawnGameObject(GameObject o, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject go = Instantiate(o, position, rotation, parent);
            NetworkServer.Spawn(go);
            return go;
        }
        #endregion
    }
}
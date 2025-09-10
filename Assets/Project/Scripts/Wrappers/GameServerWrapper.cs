namespace PlayMySpace.PMSC.Wrappers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.Networking;
    using LitJson;
    using Framework.Patterns;
    using PlayMySpace.PMSC.Wrappers;
    using PlayMySpace.PMSC.Models;
    using PlayMySpace.PMSC.Managers;
    using Google.Maps.Coord;

    /// <summary>
    /// GameServerWrapper.cs
    /// 
    /// Wrapper containing functions to communicate with the game server.
    /// 
    /// By Peter de Keijzer
    /// </summary>
    public class GameServerWrapper : Singleton<GameServerWrapper>
    {
        #region Class Members
        [SerializeField] private string serverURL;
        [SerializeField] private Transform playableLocationsTransform;

        [SerializeField] private GameObject[] materialCrystals;

        private MapManager mapManager;
        #endregion

        #region Class Accessors
        public string ServerURL
        {
            get
            {
                return serverURL;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            mapManager = FindObjectOfType<MapManager>();
        }
        #endregion

        #region Class Implementation - Private
        private string GetUserId()
        {
            return AuthWrapper.Instance.UserId;
        }
        #endregion

        #region Class Implementation - Public
        public IEnumerator GetPlayerData(Action<PlayerData> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(serverURL + "/users/" + GetUserId()))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError.Invoke(webRequest.error);
                }
                else
                {
                    // If it exists
                    if (webRequest.responseCode != 204)
                    {
                        PlayerData data = JsonMapper.ToObject<PlayerData>(webRequest.downloadHandler.text);
                        onSuccess.Invoke(data);
                        Debug.Log("Device ID: " + data.userId + ", Name: " + data.name);
                    }
                    else
                    {
                        onError.Invoke("Doesn't exist lol");
                        Debug.Log("Doesn't exist lol");
                    }
                }
            }
        }

        public IEnumerator TestPlayableLocations(WorldDataRequest worldDataRequest)
        {
            string json = "";

            if (worldDataRequest != null)
            {
                json = JsonMapper.ToJson(worldDataRequest);
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(serverURL + "/lol/" + GetUserId(), "POST"))
            {
                byte[] raw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(raw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                }
                else
                {
                    WorldData data = JsonMapper.ToObject<WorldData>(webRequest.downloadHandler.text);

                    Debug.Log(webRequest.downloadHandler.text);
                }
            }
        }

        public IEnumerator PostWorldData(WorldDataRequest worldDataRequest, Action<WorldData> onSuccess, Action<string> onError)
        {
            string json = "";

            if (worldDataRequest != null)
            {
                json = JsonMapper.ToJson(worldDataRequest);
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(serverURL + "/worlds/" + GetUserId(), "POST"))
            {
                byte[] raw = Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(raw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(webRequest.error);
                }
                else
                {
                    WorldData data = JsonMapper.ToObject<WorldData>(webRequest.downloadHandler.text);

                    foreach (KeyValuePair<String, SpawnLocation> kvp in data.locations)
                    {
                        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
                        int crystalNumber = UnityEngine.Random.Range(0, materialCrystals.Length);

                        bool alreadyExists = false;

                        foreach (Transform child in playableLocationsTransform)
                        {
                            if (child.name == kvp.Key)
                            {
                                alreadyExists = true;
                                break;
                            }
                        }

                        if (!alreadyExists)
                        {
                            GameObject materialCrystal = Instantiate(materialCrystals[crystalNumber], mapManager.MapsService.Projection.FromLatLngToVector3(new LatLng(kvp.Value.snappedPoint.latitude, kvp.Value.snappedPoint.longitude)) + Vector3.up * 25, materialCrystals[crystalNumber].transform.rotation, playableLocationsTransform);
                            materialCrystal.name = kvp.Key;
                        }
                    }
                }
            }
        }
        #endregion
    }

}
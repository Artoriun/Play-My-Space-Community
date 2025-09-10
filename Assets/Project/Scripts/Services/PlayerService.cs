namespace PlayMySpace.PMSC.Services
{
    using System;
    using System.Text;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Networking;
    using PlayMySpace.PMSC.Models;
    using PlayMySpace.PMSC.Wrappers;
    using PlayMySpace.PMSC.Caches;
    using LitJson;

    public class PlayerService : MonoBehaviour
    {
        #region Class Members

        #endregion

        #region Class Implementation - Public
        public IEnumerator GetPlayerData(Action<PlayerData> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(GameServerWrapper.Instance.ServerURL + "/users/" + AuthWrapper.Instance.UserId))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError  || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError(webRequest.error);
                }
                else
                {
                    // If it exists
                    if (webRequest.responseCode != 204)
                    {
                        PlayerData data = JsonMapper.ToObject<PlayerData>(webRequest.downloadHandler.text);
                        onSuccess?.Invoke(data);
                        PlayerDataCache.Instance.PlayerData = data;
                    }
                    else
                    {
                        onError?.Invoke("PlayerData doesn't exist lol");
                    }
                }
            }
        }

        public IEnumerator PostPlayerData(PlayerData data, Action<PlayerData> onSuccess, Action<string> onError)
        {
            PlayerDataCache.Instance.PlayerData = data;
            string json = "";

            try
            {
                json = JsonMapper.ToJson(data);
            }
            catch (NullReferenceException e)
            {
                throw e;
            }

            using (UnityWebRequest webRequest = new UnityWebRequest(GameServerWrapper.Instance.ServerURL + "/users/" + AuthWrapper.Instance.UserId, "POST"))
            {
                try
                {
                    byte[] raw = Encoding.UTF8.GetBytes(json);
                    webRequest.uploadHandler = new UploadHandlerRaw(raw);
                }
                catch (Exception e)
                {
                    throw e;
                }

                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    onError(webRequest.error);
                }
                else
                {
                    onSuccess?.Invoke(data);
                }
            }
        }
        #endregion
    }
}

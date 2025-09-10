namespace PlayMySpace.PMSC.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using PlayMySpace.PMSC.Models;
    using PlayMySpace.PMSC.Wrappers;
    using Framework.Firestore.Wrappers;
    using Framework.Patterns;
    using Mirror;

    /// <summary>
    /// LoadingManager.cs
    /// 
    /// Handles the loading of different components of the game.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class LoadingManager : Singleton<LoadingManager>
    {
        #region Class Members
        [SerializeField] private GameObject signInPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private MapManager mapManager;

        private Slider loadingBar;
        private float mapValue = 0;
        private float playerDataValue = 0;
        private float finalValue = 0;
        public enum Type { Map, PlayerData };
        private Action onFinishedLoading;
        #endregion

        #region MonoBehaviour Stuff
        private void Start()
        {
            GameManager.Instance.AddInitializeGameCallback(InitializeLoading);

            if (GameManager.Instance.ServerBuild)
            {
                NetworkManager.singleton.StartServer();
            }
            else
            {
                NetworkManager.singleton.StartClient();
            }
        }
        #endregion

        #region Class Implementation - Private
        private void CompletePlayerDataLoading(PlayerData data)
        {
            Debug.Log(data.name);
            UpdateProgress(Type.PlayerData, 1);
        }

        private void CompletePlayerDataLoading(string error)
        {
            Debug.Log("User doesn't exist yet");
            UpdateProgress(Type.PlayerData, 1);
            GameManager.Instance.StartIntroductionCutscene = true;
        }

        private void InitializeLoading()
        {
            loadingBar = loadingPanel.GetComponentInChildren<Slider>();
            //signInPanel.SetActive(true);

            if (Application.isMobilePlatform)
            {
                AuthWrapper.Instance.AddCallback(StartLoading);
            }
            else
            {
                StartLoading();
            }
        }

        private void StartLoading()
        {
            SoundWrapper.Instance.PlayOneShot(SoundWrapper.SoundEffect.StartGameJingle);
            PlayerDataWrapper.Instance.GetData(CompletePlayerDataLoading, CompletePlayerDataLoading);
            //signInPanel.SetActive(false);
            loadingPanel.SetActive(true);
        }
        #endregion

        #region Class Implementation - Public
        public void UpdateProgress(Type loadingType, float value)
        {
            switch(loadingType)
            {
                case Type.Map:
                    mapValue = value;
                    break;
                case Type.PlayerData:
                    playerDataValue = value;
                    break;
            }

            finalValue = mapManager.gameObject.activeInHierarchy ? mapValue * 0.9f + playerDataValue * 0.1f : playerDataValue;
            loadingBar.value = finalValue;

            if (finalValue >= 1)
            {
                loadingPanel.SetActive(false);
                onFinishedLoading?.Invoke();
            }
        }

        public void AddCallback(Action callback)
        {
            onFinishedLoading += callback;
        }
        #endregion
    }
}
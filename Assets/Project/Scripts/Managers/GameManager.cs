namespace PlayMySpace.PMSC.Managers
{
    using System;
    using UnityEngine;
    using PlayMySpace.PMSC.Network;
    using PlayMySpace.PMSC.Input;
    using PlayMySpace.PMSC.UI;
    using PlayMySpace.PMSC.Wrappers;
    using Framework.Patterns;

/// <summary>
/// GameManager.cs
/// 
/// Ties all the different managers, controllers and other scripts together.
/// 
/// Copyright © 2021 Play My Space
/// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        #region Class Members
        [SerializeField] private bool serverBuild = false;

        [Header("Managers")]
        [SerializeField] private MapManager mapManager;
        [SerializeField] private PlayerLogicManager playerLogicManager;
        [SerializeField] private EventManager eventManager;
        [SerializeField] private PMSWNetworkManager networkManager;

        [Header("Controllers")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private MinimapController minimapController;

        [Header("UI Stuff")]
        [SerializeField] private GameObject introductionPanel;
        [SerializeField] private GameObject guiPanel;
        [SerializeField] private GameObject collectibleTipPanel;
        [SerializeField] private GameObject eventPanel;

        private bool startIntroductionCutscene = false;

        // Callbacks
        private Action onGameStarted, onInitializeGame;
        #endregion

        #region Class Accessors
        public GameObject EventPanel
        {
            get { return eventPanel; }
        }

        public MinimapController MinimapController
        {
            get { return minimapController; }
        }

        public PMSWNetworkManager NetworkManager
        {
            get { return networkManager; }
        }

        public bool ServerBuild
        {
            get
            {
                return serverBuild;
            }
        }

        public PlayerLogicManager PlayerLogicManager
        {
            get { return playerLogicManager; }
        }
        public CameraController CameraController
        {
            get { return cameraController; }
        }

        public EventManager EventManager
        {
            get { return eventManager; }
        }

        public MapManager MapManager
        {
            get
            {
                return mapManager;
            }
        }

        public bool StartIntroductionCutscene
        {
            get
            {
                return startIntroductionCutscene;
            }
            set
            {
                startIntroductionCutscene = value;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();

            if (!serverBuild)
            {
                InitializeCallbacks();
            }
            else
            {
                GameObject.Find("UI").SetActive(false);
            }
        }

        private void Start()
        {
            if (!serverBuild)
            {
                InitializeGame();
            }
        }
        #endregion

        #region Class Implementation - Private
        private void InitializeCallbacks()
        {
            LoadingManager.Instance.AddCallback(FinishLoading);
        }

        private void InitializeGame()
        {
            onInitializeGame?.Invoke();

            //if (guiPanel.activeInHierarchy)
            //{
            //    guiPanel.SetActive(false);
            //}


            //if (introductionPanel.activeInHierarchy)
            //{
            //    introductionPanel.SetActive(false);
            //}

            SignInUser();
        }

        private void SignInUser()
        {
            if (!Application.isMobilePlatform)
            {
                AuthWrapper.Instance.InvokeAuthenticatedCallbacks();
            }
        }
        #endregion

        #region Class Implementation - Public
        public void IntroductionFinished()
        {
            //collectibleTipPanel.SetActive(true);
            //guiPanel.SetActive(true);
            onGameStarted?.Invoke();

            //else
            //{
            //    throw new NullReferenceException(GetType().Name + ".IntroductionFinished: callbacksWhenStarted is null!");
            //}
        }

        public void FinishLoading()
        {
            if (startIntroductionCutscene)
            {
                //introductionPanel.SetActive(true);
            }
            else
            {
                //collectibleTipPanel.SetActive(true);
                //guiPanel.GetComponent<GUIPanelScript>().ChangeAvatarName();
                //guiPanel.SetActive(true);
                onGameStarted?.Invoke();
            }
        }

        public void AddInitializeGameCallback(Action callback)
        {
            onInitializeGame += callback;
        }

        public void AddGameStartedCallback(Action callback)
        {
            onGameStarted += callback;
        }
        #endregion
    }
}

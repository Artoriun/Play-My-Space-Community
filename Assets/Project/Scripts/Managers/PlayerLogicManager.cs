namespace PlayMySpace.PMSC.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;
    using PlayMySpace.PMSC.Input;
    using TMPro;

    /// <summary>
    /// PlayerLogicManager.cs
    /// 
    /// Handles all user input related to the world map.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PlayerLogicManager : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject monsterPrefab;
        [SerializeField] private GameObject worldLight;
        [SerializeField] private CameraController cameraController;

        [Header("UI Stuff")]
        [SerializeField] private Button switchPetModeButton;
        [SerializeField] private Sprite petModePetImage;
        [SerializeField] private Sprite petModeMonsterImage;

        public GameObject whale;
        private GameObject pet;
        private GameObject monster;

        public Action onSwitchPetMode;

        private bool playerControlEnabled = true;

        public enum PetMode { Pet, Monster }
        [HideInInspector] public PetMode petMode = PetMode.Pet;
        #endregion

        #region Class Accessors
        public bool IsLocalPlayer
        {
            get { return petMode == PetMode.Pet ? pet.GetComponent<PetController>().isLocalPlayer : monster.GetComponent<PetMonsterController>().isLocalPlayer; }
        }
        public GameObject Player
        {
            get { return petMode == PetMode.Pet ? pet : monster; }
        }

        public GameObject WorldLight
        {
            get { return worldLight; }
            set { worldLight = value; }
        }

        public bool PlayerControlEnabled
        {
            get { return playerControlEnabled; }
            set { playerControlEnabled = value; }
        }
        public GameObject Pet
        {
            get { return pet; }
            set { pet = value; }
        }

        public GameObject Monster
        {
            get { return monster; }
            set { monster = value; }
        }

        public GameObject MonsterPrefab
        {
            get { return monsterPrefab; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            GameManager.Instance.AddGameStartedCallback(delegate
            {
                switchPetModeButton.onClick.AddListener(SwitchPetMode);
            });
        }

        private void Update()
        {
        }
        #endregion

        #region Class Implementation - Private
        #endregion

        #region Class Implementation - Public
        public void SwitchPetMode()
        {
            if (petMode == PetMode.Pet)
            {
                switchPetModeButton.GetComponent<Image>().sprite = petModePetImage;
                switchPetModeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Pet Mode";
                pet.GetComponent<NetworkedPetController>().PetToMonsterTransformation();
            }
            else
            {
                switchPetModeButton.GetComponent<Image>().sprite = petModeMonsterImage;
                switchPetModeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Monster Mode";
                monster.GetComponent<PetMonsterController>().MonsterToPetTransformation();
            }
        }

        public void PetToMonsterTransformation()
        {
            pet.GetComponent<NetworkedPetController>().PetToMonsterTransformation();
        }

        public void MonsterToPetTransformation()
        {
            if (monster != null)
            {
                monster.GetComponent<PetMonsterController>().MonsterToPetTransformation();
            }
        }
        #endregion
    }
}

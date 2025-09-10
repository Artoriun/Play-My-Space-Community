namespace PlayMySpace.PMSC.Landmarks
{
    using System;
    using UnityEngine;
    using PlayMySpace.PMSC.Events;
    using PlayMySpace.PMSC.Managers;
    using PlayMySpace.PMSC.Input;
    using Mirror;

    /// <summary>
    /// TokyoTowerScript.cs
    /// 
    /// Handles all logic related to the Tokyo Tower landmark.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class TokyoTowerScript : NetworkBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject petMonsterEventPrefab;
        #endregion

        #region Class Accessors
        public GameObject PetMonsterEventPrefab
        {
            get { return petMonsterEventPrefab; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void OnEnable()
        {
            StartEvent();
        }
        #endregion

        #region Class Implementation - Public
        public void StartEvent()
        {
            Instantiate(petMonsterEventPrefab, transform.position, transform.rotation);
        }
        #endregion
    }
}

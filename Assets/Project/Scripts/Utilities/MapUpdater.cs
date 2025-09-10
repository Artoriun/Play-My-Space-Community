namespace PlayMySpace.PMSC.Utilities
{
    using System.Collections;
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;

    /// <summary>
    /// MapUpdater.cs
    /// 
    /// Dynamically updates the world map depending on the player's current location/orientation.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class MapUpdater : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private MapManager mapManager;
        [SerializeField] private GameObject groundPlane;

        [Header("Parameters for loading/unload map regions")]
        [SerializeField] private float loadDistance = 1500;
        [SerializeField] private float unloadDelay = 5;

        private Vector3 previousAvatarPosition;
        private bool loadMapRegion, unloadMapRegion;
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
        }

        private void Start()
        {
            previousAvatarPosition = Vector3.zero;
            loadMapRegion = false;
            unloadMapRegion = false;
            StartCoroutine(UnloadMapRegion());
        }

        private void Update()
        {
            if (GameManager.Instance.PlayerLogicManager.Pet != null)
            {
                float squaredDistance = Vector3.SqrMagnitude(GameManager.Instance.PlayerLogicManager.Player.transform.position - previousAvatarPosition);

                if (squaredDistance > loadDistance * loadDistance)
                {
                    loadMapRegion = true;
                }

                if (loadMapRegion)
                {
                    groundPlane.transform.position = new Vector3(GameManager.Instance.PlayerLogicManager.Player.transform.position.x, groundPlane.transform.position.y, GameManager.Instance.PlayerLogicManager.Player.transform.position.z);
                    mapManager.LoadMapRegion();
                    previousAvatarPosition = GameManager.Instance.PlayerLogicManager.Player.transform.position;
                    loadMapRegion = false;
                    unloadMapRegion = true;
                }
            }
        }
        #endregion

        #region Class Implementation - Private
        private IEnumerator UnloadMapRegion()
        {
            while (true)
            {
                if (unloadMapRegion)
                {
                    mapManager.MapsService.MakeMapLoadRegion().AddCircle(GameManager.Instance.PlayerLogicManager.Player.transform.position, 1000).UnloadOutside();
                    unloadMapRegion = false;
                }

                yield return new WaitForSeconds(unloadDelay);
            }
        }
        #endregion
    }
}

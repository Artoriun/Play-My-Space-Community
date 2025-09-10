namespace PlayMySpace.PMSC.UI
{
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;

    /// <summary>
    /// MinimapController.cs
    /// 
    /// Contains code to manage the minimap zoom level and rotation based on player input and location.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private Camera minimapCamera;
        [SerializeField] private int[] zoomDistances = { 50, 100, 150 };
        [SerializeField] private int zoomLevel = 1;

        private bool startUpdating = false;
        #endregion

        #region Class Accessors
        public int ZoomLevel
        {
            get { return zoomLevel; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            LoadingManager.Instance.AddCallback(delegate { startUpdating = true; });
        }

        private void Update()
        {
            if (startUpdating)
            {
                UpdateMinimapCamera();
            }
        }
        #endregion

        #region Class Implementation - Private
        /// <summary>
        /// Updates the MinimapCamera's position and rotation based on the Player and CameraController transforms.
        /// </summary>
        private void UpdateMinimapCamera()
        {
            // Update the MinimapCamera's position based on the Player's position
            GameObject player = GameManager.Instance.PlayerLogicManager.petMode == PlayerLogicManager.PetMode.Pet ? GameManager.Instance.PlayerLogicManager.Pet : GameManager.Instance.PlayerLogicManager.Monster;
            minimapCamera.transform.position = new Vector3(player.transform.position.x,
                                                           minimapCamera.transform.position.y,
                                                           player.transform.position.z);

            // Update the MinimapCamera's rotation based on CameraController's rotation
            minimapCamera.transform.rotation = Quaternion.Euler(90, 0, -GameManager.Instance.CameraController.transform.rotation.eulerAngles.y);
        }
        #endregion

        #region Class Implementation - Public
        /// <summary>
        /// Updates the Minimap's zoom distance based on the ZoomIn and ZoomOut buttons.
        /// </summary>
        /// <param name="increment">The amount to increment zoomLevel by (negative for zooming in, positive for zooming out).</param>
        public void Zoom(int increment)
        {
            if ((zoomLevel > 0 && increment < 0) || (zoomLevel < zoomDistances.Length - 1 && increment > 0))
            {
                zoomLevel += increment;
                minimapCamera.orthographicSize = zoomDistances[zoomLevel];
            }
        }
        #endregion
    }
}

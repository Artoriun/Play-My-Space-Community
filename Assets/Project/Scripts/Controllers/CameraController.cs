namespace PlayMySpace.PMSC.Input
{
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;

    /// <summary>
    /// CameraController.cs
    /// 
    /// Handles camera movement and rotation by the player.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private float zoomLevel = 0.5f;
        [SerializeField] private float verticalRotationSpeed = 4;
        [SerializeField] private float horizontalRotationSpeed = 6;
        [SerializeField] private JoystickController joystickController;

        private Vector3 minZoomPosition, halfwayZoomPosition, maxZoomPosition, minZoomRotation, halfwayZoomRotation, maxZoomRotation;
        private Vector3 offset;
        private Vector2 dragStartPosition;
        private float previousZoomLevel;
        private GameObject target;
        private bool stoppedZooming = true;
        private bool enablePlayerControl = false;
        private bool zooming = false;
        private bool zoomControl = true;
        #endregion

        #region Class Accessors
        public bool ZoomControl
        {
            get { return zoomControl; }
            set { zoomControl = value; }
        }
        public bool Zooming
        {
            get { return zooming; }
        }

        public float ZoomLevel
        {
            get
            {
                return zoomLevel;
            }
            
            set
            {
                zoomLevel = value;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Start()
        {
            LoadingManager.Instance.AddCallback(delegate { enablePlayerControl = true;});
            GameManager.Instance.PlayerLogicManager.onSwitchPetMode += SwitchTarget;
            previousZoomLevel = zoomLevel;
            Camera.main.transform.localPosition = Vector3.Lerp(minZoomPosition, maxZoomPosition, zoomLevel);
            Camera.main.transform.localEulerAngles = Vector3.Lerp(minZoomRotation, maxZoomRotation, zoomLevel);
        }

        private void Update()
        {
            if (enablePlayerControl && target != null)
            {
                if (Application.isEditor)
                {
                    UpdateInEditor();
                }
                else
                {
                    UpdateOnDevice();
                }

                transform.position = new Vector3(transform.position.x, Mathf.Max(target.transform.position.y + 0.25f, transform.position.y), transform.position.z);
            }
        }
        #endregion

        #region Class Implementation - Private
        private void UpdateInEditor()
        {
            // Rotating
            RotateInEditor();

            // Zooming
            if (zoomControl)
            {
                zoomLevel = Mathf.Clamp01(zoomLevel + Input.mouseScrollDelta.y * 0.1f);
            }

            transform.position = target.transform.position + offset + zoomLevel * transform.forward * 20 - transform.forward * 20;

            if (zoomLevel >= 0.98f)
            {
                foreach (SkinnedMeshRenderer renderer in target.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.enabled = false;
                }
            }
            else
            {
                foreach (SkinnedMeshRenderer renderer in target.transform.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.enabled = true;
                }
            }
        }

        private void UpdateOnDevice()
        {
            if (Input.touchCount == 1 && !joystickController.Interacting)
            {
                RotateCamera(verticalRotationSpeed, horizontalRotationSpeed);
            }
            else if (Input.touchCount > 1)
            {
                RotateCamera(1, 0.5f);
            }
            if (Input.touchCount >= 2 && Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved && !joystickController.Interacting)
            {
                // Logic for zooming in and out
                float minPinchSpeed = 250;

                float currentDistance = Vector3.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                float previousDistance = Vector3.Distance(Input.GetTouch(0).position - Input.GetTouch(0).deltaPosition, Input.GetTouch(1).position - Input.GetTouch(1).deltaPosition);
                float touchDelta = currentDistance - previousDistance;
                float touch0Speed = Input.GetTouch(0).deltaPosition.magnitude / Input.GetTouch(0).deltaTime;
                float touch1Speed = Input.GetTouch(1).deltaPosition.magnitude / Input.GetTouch(1).deltaTime;

                if (touchDelta != 0 && touch0Speed > minPinchSpeed && touch1Speed > minPinchSpeed)
                {
                    zooming = true;
                    zoomLevel = Mathf.Clamp01(zoomLevel + 0.001f * touchDelta);

                    if (zoomLevel <= 0.5f)
                    {
                        Camera.main.transform.localPosition = Vector3.Lerp(minZoomPosition, halfwayZoomPosition, zoomLevel * 2);
                        Camera.main.transform.localEulerAngles = Vector3.Lerp(minZoomRotation, halfwayZoomRotation, zoomLevel * 2);
                    }
                    else
                    {
                        Camera.main.transform.localPosition = Vector3.Lerp(halfwayZoomPosition, maxZoomPosition, (zoomLevel - 0.5f) * 2);
                        Camera.main.transform.localEulerAngles = Vector3.Lerp(halfwayZoomRotation, maxZoomRotation, (zoomLevel - 0.5f) * 2);
                    }
                }
                else
                {
                    zooming = false;
                }
            }
        }

        private void RotateInEditor()
        {
            if (Input.GetKey(KeyCode.D))
            {
                Vector3 rotation = Quaternion.AngleAxis(10, Vector3.up).eulerAngles;
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + rotation);
            }

            if (Input.GetKey(KeyCode.A))
            {
                Vector3 rotation = Quaternion.AngleAxis(-10, Vector3.up).eulerAngles;
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + rotation);
            }

            if (Input.GetMouseButton(1))
            {
                if (stoppedZooming)
                {
                    stoppedZooming = false;
                    dragStartPosition = Input.mousePosition;
                }

                Vector3 deltaMousePosition = dragStartPosition - new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                Vector3 newAngles = transform.eulerAngles - new Vector3(Mathf.Clamp(deltaMousePosition.y, -verticalRotationSpeed, verticalRotationSpeed), Mathf.Clamp(deltaMousePosition.x, -horizontalRotationSpeed, horizontalRotationSpeed), 0);

                if (newAngles.x >= 180)
                {
                    newAngles.x -= 360;
                }

                float minZoomAngle = zoomLevel >= 0.99f ? -60 : (zoomLevel >= 0.89f ? -45 : -30);
                transform.eulerAngles = new Vector3(Mathf.Clamp(newAngles.x, minZoomAngle, 80), newAngles.y, newAngles.z);

                dragStartPosition = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(1))
            {
                stoppedZooming = true;
            }

            if (previousZoomLevel != zoomLevel)
            {
                Vector3 newAngles = transform.eulerAngles;

                if (newAngles.x >= 180)
                {
                    newAngles.x -= 360;
                }

                float minZoomAngle = zoomLevel >= 0.99f ? -60 : (zoomLevel >= 0.89f ? -45 : -30);
                transform.eulerAngles = new Vector3(Mathf.Clamp(newAngles.x, minZoomAngle, 80), newAngles.y, newAngles.z);
            }

            previousZoomLevel = zoomLevel;
        }

        /// <summary>
        /// Logic for rotating the camera.
        /// </summary>
        /// <param name="rotationSpeed">The maximum speed of rotation</param>
        private void RotateCamera(float verticalRotationSpeed, float horizontalRotationSpeed)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).deltaPosition.magnitude > 0 && Input.GetTouch(i).phase == TouchPhase.Moved)
                {
                    if (joystickController.Interacting)
                    {
                        verticalRotationSpeed = joystickController.CurrentTouch == i ? 0 : 4;
                        horizontalRotationSpeed = joystickController.CurrentTouch == i ? 0 : 6;
                    }

                    Vector3 newAngles = transform.eulerAngles - new Vector3(Mathf.Clamp(Input.GetTouch(i).deltaPosition.y, -verticalRotationSpeed, verticalRotationSpeed), Mathf.Clamp(Input.GetTouch(i).deltaPosition.x, -horizontalRotationSpeed, horizontalRotationSpeed), 0);

                    if (newAngles.x >= 180)
                    {
                        newAngles.x -= 360;
                    }

                    float minZoomAngle = zoomLevel >= 0.99f ? -60 : (zoomLevel >= 0.89f ? -45 : -30);
                    transform.eulerAngles = new Vector3(Mathf.Clamp(newAngles.x, minZoomAngle, 80), newAngles.y, newAngles.z);
                }
            }
        }

        private void SwitchTarget()
        {
            target = GameManager.Instance.PlayerLogicManager.petMode == PlayerLogicManager.PetMode.Pet ? GameManager.Instance.PlayerLogicManager.Pet : GameManager.Instance.PlayerLogicManager.Monster;
        }
        #endregion

        #region Class Implementation - Public
        public void SwitchTarget(GameObject target)
        {
            this.target = target;
        }

        public void SetCameraPosition()
        {
            target = GameManager.Instance.PlayerLogicManager.Pet;
            offset = transform.position - target.transform.position;
        }
        #endregion
    }
}

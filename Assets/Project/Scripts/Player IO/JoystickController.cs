namespace PlayMySpace.PMSC.Input
{
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEngine.EventSystems;
    using PlayMySpace.PMSC.Managers;

    /// <summary>
    /// JoystickController.cs
    /// 
    /// Handles character movement based on player input via a touch screen "joystick".
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class JoystickController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Class Members
        [SerializeField] private CameraController cameraController;

        private Image joystickImage;
        private bool interactable = true; // Used for disabling joystick in specific cases
        private bool interacting = false;
        private bool mouseOverJoystick = false;
        private Vector2 originalPosition;
        private RectTransform rectTransform;
        private int currentTouch = -1;
        #endregion

        #region Class Accessors
        public RectTransform RectTransform
        {
            get { return rectTransform; }
        }
        public bool Interactable
        {
            get { return interactable; }
            set { interactable = value; }
        }

        public int CurrentTouch
        {
            get { return currentTouch; }
        }

        public bool Interacting
        {
            get { return interacting; }
    }
    #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            joystickImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            originalPosition = rectTransform.position;
        }

        private void FixedUpdate()
        {
            if (!interactable)
            {
                return;
            }

            if (Application.isMobilePlatform)
            {
                JoystickTouchInteraction();
            }
            else
            {
                JoystickMouseInteraction();
            }
        }
        #endregion

        #region Class Implementation - Private
        private void JoystickTouchInteraction()
        {
            if (interacting)
            {
                Touch touch = Input.GetTouch(currentTouch);

                if (touch.phase == TouchPhase.Began)
                {
                    joystickImage.color = new Color(joystickImage.color.r, joystickImage.color.g, joystickImage.color.b, 1);
                    rectTransform.position = touch.position - new Vector2(rectTransform.rect.size.x, rectTransform.rect.size.y) * 0.5f;
                }

                if (touch.phase == TouchPhase.Ended)
                {
                    joystickImage.color = new Color(joystickImage.color.r, joystickImage.color.g, joystickImage.color.b, 0);
                    currentTouch = -1;
                    interacting = false;
                    rectTransform.position = originalPosition;
                }
            }
        }

        private void JoystickMouseInteraction()
        {
            if (Input.GetMouseButtonDown(0) && mouseOverJoystick)
            {
                interacting = true;
                joystickImage.color = new Color(joystickImage.color.r, joystickImage.color.g, joystickImage.color.b, 1);
                rectTransform.position = Input.mousePosition - new Vector3(rectTransform.rect.size.x, rectTransform.rect.size.y) * 0.5f;
            }

            if (interacting)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    joystickImage.color = new Color(joystickImage.color.r, joystickImage.color.g, joystickImage.color.b, 0);
                    interacting = false;
                    rectTransform.position = originalPosition;
                }
            }
        }
        #endregion

        #region Class Implementation - Public
        public void OnPointerEnter(PointerEventData args)
        {
            currentTouch = Input.touchCount - 1;

            if (Application.isMobilePlatform && !cameraController.Zooming)
            {
                interacting = true;
            }
            else if (Application.isEditor)
            {
                mouseOverJoystick = true;
            }
        }

        public void OnPointerExit(PointerEventData args)
        {
            if (Application.isEditor)
            {
                mouseOverJoystick = false;
            }
        }
        #endregion
    }
}

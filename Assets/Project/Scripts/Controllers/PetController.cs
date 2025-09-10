namespace PlayMySpace.PMSC.Input
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using PlayMySpace.PMSC.Managers;
    using Mirror;

    /// <summary>
    /// PetController.cs
    /// 
    /// Contains all logic pertaining to controlling the pet avatar on the world map.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PetController : NetworkBehaviour
    {
        #region Class Member
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private new Collider collider;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject petModel;

        [Header("Movement Variables")]
        [SerializeField] private float movementSpeed = 1000;
        [SerializeField] private float climbingForce = 3;
        [SerializeField] private float jumpHeight = 100;

        private float currentMovementSpeed, currentClimbingForce, currentJumpHeight;
        private JoystickController joystickController;
        private Collider groundedCollider, otherCollider;
        private Vector3 climbableNormal;
        private bool resetVelocity = true;
        private bool grounded = true;
        private bool measureTap = false;
        private bool touchMoved = false;
        private bool climbJump = false;
        private float tapTime = 0;

        public enum State { Normal, Climbing }
        public State ActionState = State.Normal;
        #endregion

        #region Class Accessors
        public Animator Animator
        {
            get { return animator; }
        }

        public GameObject PetModel
        {
            get { return petModel; }
        }

        public Collider GroundedCollider
        {
            get { return groundedCollider; }
        }

        public Vector3 ClimbableNormal
        {
            get { return climbableNormal; }
        }

        public bool Grounded
        {
            get { return grounded; }
            set { grounded = value; }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected virtual void Awake()
        {
            if (SceneManager.GetActiveScene().name == "MapScene")
            {
                GameManager.Instance.AddGameStartedCallback(delegate { joystickController = FindObjectOfType<JoystickController>(); });
            }

            currentMovementSpeed = movementSpeed;
            currentJumpHeight = jumpHeight;
            currentClimbingForce = climbingForce;
        }

        protected virtual void Update()
        {
            if (!GameManager.Instance.PlayerLogicManager.PlayerControlEnabled)
            {
                return;
            }

            if (Application.isMobilePlatform)
            {
                JumpOnDevice();
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    JumpWithMouse();
                }

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    JumpWithSpacebar();
                }
            }
        }

        protected virtual void FixedUpdate()
        {
            if (joystickController == null)// && GameManager.Instance.PlayerLogicManager.PlayerControlEnabled)
            {
                return;
            }

            if (Application.isMobilePlatform)
            {
                MoveAvatarWithTouch();
            }
            else
            {
                MoveAvatarWithMouse();
                KeyboardMovement();
            }
        }
        #endregion

        #region Class Implementation - Private
        protected virtual void MoveAvatarWithTouch()
        {
            if (ActionState == State.Normal)
            {
                if (!joystickController.Interacting)
                {
                    Touch touch = Input.GetTouch(joystickController.CurrentTouch);

                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        float moveDistance = Mathf.Clamp01(Vector2.Distance(touch.position, joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f) / 250);
                        moveDistance = moveDistance < 0.2f ? 0 : moveDistance;
                        float moveAngle = -Vector2.SignedAngle(Vector2.up, touch.position - (Vector2)(joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f));
                        moveAngle = moveAngle < 0 ? 360 + moveAngle : moveAngle;

                        Vector3 velocity = Vector3.Normalize(Quaternion.Euler(0, moveAngle, 0) * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)) * moveDistance * currentMovementSpeed * Time.fixedDeltaTime;
                        rigidbody.velocity = new Vector3(velocity.x, rigidbody.velocity.y, velocity.z);
                        animator.SetFloat("movementSpeed", moveDistance);
                        transform.GetChild(0).rotation = Quaternion.LookRotation(Quaternion.Euler(0, moveAngle, 0) * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up), Vector3.up);
                    }

                    if (touch.phase == TouchPhase.Ended)
                    {
                        animator.SetFloat("movementSpeed", 0);
                    }
                }

                if (Input.touchCount == 0)
                {
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x * 0.9f, rigidbody.velocity.y, rigidbody.velocity.z * 0.9f);
                }
            }
            else if (ActionState == State.Climbing)
            {
                if (joystickController.Interacting)
                {
                    Touch touch = Input.GetTouch(joystickController.CurrentTouch);

                    if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    {
                        float moveDistance = Mathf.Clamp01(Vector2.Distance(touch.position, joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f) / 250);
                        moveDistance = moveDistance < 0.2f ? 0 : moveDistance;
                        float moveAngle = -Vector2.SignedAngle(Vector2.up, touch.position - (Vector2)(joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f));
                        moveAngle = moveAngle < 0 ? 360 + moveAngle : moveAngle;

                        Vector3 velocity = Vector3.Normalize(Quaternion.AngleAxis(moveAngle - 90, climbableNormal) * Vector3.Cross(climbableNormal, Vector3.up)) * moveDistance * currentClimbingForce * Time.fixedDeltaTime;
                        rigidbody.velocity = velocity;
                        animator.SetFloat("movementSpeed", moveDistance);
                    }

                    if (touch.phase == TouchPhase.Ended)
                    {
                        animator.SetFloat("movementSpeed", 0);
                    }
                }

                // Friction when climbing
                rigidbody.velocity *= 0.8f;
            }
        }

        protected virtual void MoveAvatarWithMouse()
        {
            if (ActionState == State.Normal)
            {
                if (joystickController.Interacting)
                {
                    if (Input.GetMouseButton(0))
                    {
                        float mouseDistance = Mathf.Clamp01(Vector2.Distance(Input.mousePosition, joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f) / 250);
                        mouseDistance = mouseDistance < 0.2f ? 0 : mouseDistance;
                        float mouseAngle = -Vector2.SignedAngle(Vector2.up, Input.mousePosition - (joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f));
                        mouseAngle = mouseAngle < 0 ? 360 + mouseAngle : mouseAngle;

                        Vector3 velocity = Vector3.Normalize(Quaternion.Euler(0, mouseAngle, 0) * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)) * mouseDistance * currentMovementSpeed * Time.fixedDeltaTime;
                        rigidbody.velocity = new Vector3(velocity.x, rigidbody.velocity.y, velocity.z);
                        animator.SetFloat("movementSpeed", mouseDistance);
                        transform.GetChild(0).rotation = Quaternion.LookRotation(Quaternion.Euler(0, mouseAngle, 0) * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up), Vector3.up);
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    animator.SetFloat("movementSpeed", 0);
                }

                // Friction when moving on a groundable surface
                if (!Input.GetMouseButton(0))
                {
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x * 0.9f, rigidbody.velocity.y, rigidbody.velocity.z * 0.9f);
                }
            }
            else if (ActionState == State.Climbing)
            {
                if (joystickController.Interacting)
                {
                    if (Input.GetMouseButton(0))
                    {
                        float mouseAngle = -Vector2.SignedAngle(Vector2.up, Input.mousePosition - (joystickController.RectTransform.position + new Vector3(joystickController.RectTransform.rect.size.x, joystickController.RectTransform.rect.size.y) * 0.5f));
                        mouseAngle = mouseAngle < 0 ? 360 + mouseAngle : mouseAngle;

                        RaycastHit hit;
                        Vector3 castDirection = -climbableNormal;

                        if (Physics.SphereCast(rigidbody.position, 0.05f, castDirection, out hit, 5))
                        {
                            Debug.DrawRay(transform.position, hit.normal * 20, Color.green);
                            rigidbody.AddForce(-hit.normal * 5, ForceMode.Force);
                            rigidbody.AddForce(Vector3.Normalize(Quaternion.AngleAxis(mouseAngle - 90, hit.normal) * (Vector3.Cross(hit.normal, Vector3.up))) * currentClimbingForce, ForceMode.Impulse);
                        }
                        else if (otherCollider.bounds.extents.y * 2 - transform.position.y <= 0.1f)
                        {
                            ActionState = State.Normal;
                            animator.SetBool("Climbing", false);
                            rigidbody.useGravity = true;
                            rigidbody.AddForce(Vector3.up + transform.forward, ForceMode.Impulse);
                        }

                        transform.GetChild(0).rotation = Quaternion.LookRotation(-climbableNormal);
                        GetComponentInChildren<Animator>().SetFloat("movementSpeed", 1);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        animator.SetFloat("movementSpeed", 0);
                    }
                }

                // Friction when climbing
                rigidbody.velocity *= 0.8f;
            }
        }

        protected virtual void MoveAvatarWithKeyboard(Quaternion rotation)
        {
            if (ActionState == State.Normal)
            {
                Vector3 velocity = Vector3.Normalize(rotation * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)) * currentMovementSpeed * Time.fixedDeltaTime;
                rigidbody.velocity = new Vector3(velocity.x, rigidbody.velocity.y, velocity.z);
                animator.SetFloat("movementSpeed", 1);
                transform.GetChild(0).rotation = Quaternion.LookRotation(rotation * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up), Vector3.up);

                // Friction when moving on a groundable surface
                if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.E))
                {
                    rigidbody.velocity = new Vector3(rigidbody.velocity.x * 0.9f, rigidbody.velocity.y, rigidbody.velocity.z * 0.9f);
                }
            }
            else if (ActionState == State.Climbing)
            {
                RaycastHit hit;
                Vector3 castDirection = -climbableNormal;

                if (Physics.SphereCast(rigidbody.position, 0.5f, castDirection, out hit, 5))
                {
                    Debug.DrawRay(transform.position, -castDirection * 20, Color.green);
                    rigidbody.AddForce(-hit.normal * 5, ForceMode.Force);
                    rigidbody.AddForce(Vector3.Normalize(Quaternion.AngleAxis(rotation.eulerAngles.y - 90, hit.normal) * (Vector3.Cross(hit.normal, Vector3.up))) * currentClimbingForce, ForceMode.Impulse);
                }
                else if (otherCollider.bounds.extents.y * 2 - transform.position.y <= 0.1f)
                {
                    ActionState = State.Normal;
                    animator.SetBool("Climbing", false);
                    rigidbody.useGravity = true;
                    rigidbody.AddForce(Vector3.up + transform.forward, ForceMode.Impulse);
                }

                transform.GetChild(0).rotation = Quaternion.LookRotation(-climbableNormal);
                GetComponentInChildren<Animator>().SetFloat("movementSpeed", 1);
            }
        }

        protected virtual void KeyboardMovement()
        {
            if (Input.GetKey(KeyCode.W))
            {
                MoveAvatarWithKeyboard(Quaternion.Euler(0, 0, 0));
            }

            if (Input.GetKey(KeyCode.Q))
            {
                MoveAvatarWithKeyboard(Quaternion.Euler(0, -90, 0));
            }

            if (Input.GetKey(KeyCode.S))
            {
                MoveAvatarWithKeyboard(Quaternion.Euler(0, 180, 0));
            }

            if (Input.GetKey(KeyCode.E))
            {
                MoveAvatarWithKeyboard(Quaternion.Euler(0, 90, 0));
            }

            if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.Q) || Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.E))
            {
                animator.SetFloat("movementSpeed", 0);
            }
        }

        private IEnumerator StartClimbJumpTimer(float time)
        {
            yield return new WaitForSeconds(time);
            climbJump = true;
        }

        protected virtual void JumpWithSpacebar()
        {
            if (grounded)
            {
                climbJump = false;
                animator.SetTrigger("Jump");
                rigidbody.AddForce(Vector3.up * currentJumpHeight, ForceMode.Impulse);
            }
            else if (ActionState == State.Climbing && climbJump)
            {
                ActionState = State.Normal;
                animator.SetTrigger("Jump");
                transform.GetChild(0).rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(climbableNormal, Vector3.up));
                rigidbody.useGravity = true;
                rigidbody.velocity = Vector3.zero;
                rigidbody.AddForce(climbableNormal * 100 + Vector3.up * currentJumpHeight * 0.5f, ForceMode.Impulse);
            }
        }

        protected virtual void JumpWithMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                measureTap = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (tapTime <= 0.2f && !touchMoved && grounded)
                {
                    animator.SetTrigger("Jump");
                    rigidbody.AddForce(Vector3.up * currentJumpHeight, ForceMode.Impulse);
                }

                measureTap = false;
                touchMoved = false;
                tapTime = 0;
            }

            if (measureTap)
            {
                tapTime += Time.deltaTime;
            }
        }

        protected virtual void JumpOnDevice()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (i == joystickController.CurrentTouch)
                {
                    continue;
                }

                Touch touch = Input.GetTouch(i);

                if (touch.phase == TouchPhase.Began)
                {
                    measureTap = true;
                }
                else if (touch.phase == TouchPhase.Moved)
                {
                    touchMoved = true;
                }
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (tapTime <= 0.2f && !touchMoved && grounded)
                    {
                        animator.SetTrigger("Jump");
                        rigidbody.AddForce(Vector3.up * currentJumpHeight, ForceMode.Impulse);
                    }

                    measureTap = false;
                    touchMoved = false;
                    tapTime = 0;
                }

                if (measureTap)
                {
                    tapTime += Time.deltaTime;
                }
            }
        }

        protected virtual void OnCollisionStay(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (Vector3.Dot(Vector3.up, contact.normal) > 0.9f && rigidbody.velocity.y < 0.01f && (contact.otherCollider.CompareTag("Groundable") || contact.otherCollider.CompareTag("Climbable")))
                {
                    groundedCollider = contact.otherCollider;
                    grounded = true;
                    animator.SetBool("Grounded", grounded);

                    if (ActionState == State.Climbing)
                    {
                        resetVelocity = true;
                        rigidbody.useGravity = true;
                        ActionState = State.Normal;
                        animator.SetBool("Climbing", false);
                    }

                    break;
                }

                float contactDot = Vector3.Dot(contact.normal, Vector3.up);

                bool touched = false;

                if (Application.isMobilePlatform)
                {
                    for (int i = 0; i < Input.touchCount; i++)
                    {
                        if (i != joystickController.CurrentTouch && Input.GetTouch(i).phase == TouchPhase.Ended)
                        {
                            touched = true;
                            break;
                        }
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        touched = true;
                    }
                }

                if (contact.otherCollider.CompareTag("Climbable"))
                {
                    climbableNormal = contact.normal;

                    if (contactDot < 0.25f && contactDot > -0.25f && touched)
                    {
                        otherCollider = contact.otherCollider;
                        ActionState = State.Climbing;
                        animator.SetBool("Climbing", true);
                        rigidbody.useGravity = false;

                        if (resetVelocity)
                        {
                            StartCoroutine(StartClimbJumpTimer(0.1f));
                            rigidbody.velocity = Vector3.zero;
                            resetVelocity = false;
                        }
                    }
                }
            }
        }

        protected virtual void OnCollisionExit(Collision collision)
        {
            if (groundedCollider == collision.collider)
            {
                groundedCollider = null;
                grounded = false;
                animator.SetBool("Grounded", grounded);
            }

            //if (AvatarState == State.Climbing)
            //{
            //    otherCollider = null;
            //    resetVelocity = true;
            //    rigidbody.useGravity = true;
            //    AvatarState = State.Normal;
            //    animator.SetBool("Climbing", false);
            //}
        }
        #endregion
    }
}

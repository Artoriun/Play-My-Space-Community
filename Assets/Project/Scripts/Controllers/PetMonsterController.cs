namespace PlayMySpace.PMSC.Input
{
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;
    using Mirror;

    /// <summary>
    /// PetMonsterController.cs
    /// 
    /// This script handles all logic pertaining to controlling the Pet Monster on the world map.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PetMonsterController : NetworkBehaviour
    {
        #region Class Members
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private GameObject petMonsterModel;

        [Header("Movement Variables")]
        [SerializeField] private float movementSpeed = 1000;
        [Space(10)]

        [Header("PetMonster Transformation")]
        [SerializeField] private PetMonsterTransformation petMonsterTransformation;

        [SerializeField] private Animator animator;

        private float currentMovementSpeed;
        private JoystickController joystickController;
        #endregion

        #region Class Accessors
        public GameObject PetMonsterModel
        {
            get { return petMonsterModel; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            joystickController = FindObjectOfType<JoystickController>();
            currentMovementSpeed = movementSpeed;
        }

        private void Update()
        {

        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer) { return; }

            if (joystickController != null && GameManager.Instance.PlayerLogicManager.PlayerControlEnabled)
            {
                if (Application.isMobilePlatform)
                {
                    //MoveAvatarWithTouch();
                }
                else
                {
                    MoveMonsterWithMouse();

                    if (Input.GetKey(KeyCode.Space))
                    {
                        FlyUp();
                    }
                    else if (transform.position.y > 120)
                    {
                        transform.position = new Vector3(transform.position.x, Mathf.Max(transform.position.y - Time.fixedDeltaTime * 20, 120), transform.position.z);
                    }
                    else if (transform.position.y < 120)
                    {
                        transform.position = new Vector3(transform.position.x, 120, transform.position.z);
                    }
                }
            }
        }
        #endregion

        #region Class Implementation - Private
        private void FlyUp()
        {
            transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y + Time.fixedDeltaTime * 30, 200), transform.position.z);
        }
        private void MoveMonsterWithMouse()
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
                    petMonsterModel.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0, mouseAngle, 0) * Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up), Vector3.up);
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

        [Command]
        private void MonsterToPetTransformationCmd(GameObject g)
        {
            MonsterToPetTransformationClientRpc(g);
        }
        [ClientRpc]
        private void MonsterToPetTransformationClientRpc(GameObject g)
        {
            PetMonsterTransformation transformation = Instantiate(petMonsterTransformation);
            transformation.StartTransformation(g);
        }

        [TargetRpc]
        private void SetPetTargetRpc(NetworkConnection conn, GameObject pet)
        {
            GameManager.Instance.PlayerLogicManager.Pet = pet;
            pet.SetActive(false);
        }
        #endregion

        #region Class Implementation - Public
        public void MonsterToPetTransformation()
        {
            if (isLocalPlayer)
            {
                MonsterToPetTransformationCmd(gameObject);
            }
        }

        [Command]
        public void ReplaceMonsterWithPetCmd(GameObject pet)
        {
            NetworkServer.ReplacePlayerForConnection(connectionToClient, pet, true);
            SetPetTargetRpc(pet.GetComponent<NetworkIdentity>().connectionToClient, pet);
        }
        #endregion
    }
}

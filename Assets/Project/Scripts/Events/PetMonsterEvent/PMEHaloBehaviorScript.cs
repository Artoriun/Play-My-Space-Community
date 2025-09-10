namespace PlayMySpace.PMSC.Events
{
    using System.Collections;
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;
    using Mirror;

    public class PMEHaloBehaviorScript : NetworkBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject haloTouchedEffectPrefab;
        [SerializeField] private GameObject rainbowTrail;

        [SyncVar] private PetMonsterEvent petMonsterEvent;
        [SyncVar] private Vector3 location;
        [SyncVar] private Vector3 position;
        #endregion

        #region Class Accessors
        public Vector3 Location
        {
            get { return location; }
            set { location = value; }
        }

        public PetMonsterEvent PetMonsterEvent
        {
            get { return petMonsterEvent; }
            set { petMonsterEvent = value; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            rainbowTrail.SetActive(true);    
        }

        private void Update()
        {
            transform.rotation = Quaternion.LookRotation(transform.position - GameManager.Instance.CameraController.transform.position);
        }
        #endregion

        #region Class Implementation - Private
        private IEnumerator PopUp()
        {
            float t = 0;
            float lerpSpeed = 2;
            Vector3 startVector = transform.position;

            while (t < 1)
            {
                position = Vector3.Lerp(startVector, location, t);
                position += new Vector3(0, 30 * Mathf.Sin(Mathf.PI * t), 0);
                transform.position = position;
                t += Time.fixedDeltaTime / lerpSpeed;
                yield return new WaitForFixedUpdate();
            }

            Instantiate(haloTouchedEffectPrefab, transform.position, transform.rotation, null);
            rainbowTrail.SetActive(false);
        }
        #endregion

        #region Class Implementation - Public
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject == GameManager.Instance.PlayerLogicManager.Player)
            {
                petMonsterEvent.UpdateHalosCollected();
                Disappear();
            }
        }

        public void MoveToLocation()
        {
            StartCoroutine(PopUp());
        }

        public void Disappear()
        {
            Instantiate(haloTouchedEffectPrefab, transform.position, transform.rotation, null);
            Destroy(gameObject);
        }
        #endregion
    }
}

namespace PlayMySpace.PMSC.Events
{
    using System.Collections;
    using UnityEngine;
    using PlayMySpace.PMSC.Managers;
    using PlayMySpace.PMSC.Input;
    using Mirror;

    /// <summary>
    /// PMECharacterBehaviorScript.cs
    /// 
    /// This script contains logic for the behavior of Devils during the PetMonsterEvent.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PMEDevilBehaviorScript : PMECharacterBehaviorScript
    {
        #region Class Members
        [SerializeField] private GameObject devilHitEffect;
        [SerializeField] private GameObject convertedAngelPrefab;

        [SyncVar] private int direction;

        private Coroutine projectileCoroutine = null;
        #endregion

        #region Class Accessors
        public int Direction
        {
            get { return direction; }
            set { direction = value; }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();
        }
        #endregion

        #region Class Implementation - Protected
        protected override IEnumerator CharacterAppearCoroutine()
        {
            appearEffect.SetActive(true);

            float t = 0;
            float popUpTime = 0.25f;
            Vector3 scale = characterModel.transform.localScale;
            while (characterModel.transform.localScale.x < 1)
            {
                characterModel.transform.localScale = Vector3.Lerp(scale, Vector3.one, t / popUpTime);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            characterModel.transform.localScale = Vector3.one;
        }

        protected IEnumerator ConvertDevilCoroutine(Collision collision)
        {
            // Stop current behavior coroutine and projectile firing coroutine
            StopCoroutine(currentCoroutine);
            StopCoroutine(projectileCoroutine);

            // Make Rigidbody non-kinematic and Collider trigger so we can get add an impulse to the GameObject to push away
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = false;
            rigidbody.AddForce(collision.gameObject.GetComponent<PetMonsterController>().PetMonsterModel.transform.forward * 80, ForceMode.Impulse);
            devilHitEffect.transform.position = collision.contacts[0].point;
            devilHitEffect.SetActive(true);
            animator.SetTrigger("TakeDamage");

            // Wait for GameObject to come to a halt
            float t = 0;
            float rotationSpeed = 0.2f;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.LookRotation(collision.transform.position - transform.position);
            endRotation = Quaternion.Euler(new Vector3(0, endRotation.eulerAngles.y, 0));
            yield return new WaitForSeconds(0.1f);
            while (rigidbody.velocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, t / rotationSpeed); // Rotate the GameObject towards the Player object
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Activate DisappearEffect then destroy the GameObject
            //disappearEffect.SetActive(true);

            t = 0;
            rotationSpeed = 0.2f;
            startRotation = transform.rotation;
            endRotation = Quaternion.LookRotation(collision.transform.position - transform.position);
            endRotation = Quaternion.Euler(new Vector3(0, endRotation.eulerAngles.y, 0));
            while (t < 1)
            {
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, t); // Rotate the GameObject towards the Player object
                t += Time.fixedDeltaTime / rotationSpeed;
                yield return new WaitForFixedUpdate();
            }

            t = 0;
            float lerpTime = 0.1f;
            Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f);

            while (t < lerpTime)
            {
                characterModel.transform.localScale = Vector3.Lerp(Vector3.one, endScale, t / lerpTime);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            PMEConvertedDevilBehaviorScript convertedDevil = Instantiate(convertedAngelPrefab,
                                                                        transform.position,
                                                                        transform.rotation,
                                                                        petMonsterEvent.PetMonsterCharacters)
                                                                        .GetComponent<PMEConvertedDevilBehaviorScript>();
            convertedDevil.TargetCharacter = targetCharacter;
            convertedDevil.PetMonsterEvent = petMonsterEvent;
            convertedDevil.ExecuteBehavior();
            
            Destroy(gameObject);
        }

        protected override IEnumerator DevilSpiralAngelBehaviorCoroutine()
        {
            while (true)
            {
                transform.RotateAround(targetCharacter.transform.position, Vector3.up, direction * 80 * Time.fixedDeltaTime);
                transform.position = new Vector3(transform.position.x, targetCharacter.transform.position.y, transform.position.z);
                yield return new WaitForFixedUpdate();
            }
        }

        protected IEnumerator LaunchDevilProjectile()
        {
            while (true)
            {
                animator.SetTrigger("ProjectileAttack");
                LaunchDevilProjectileClientRpc();
                WaitForSeconds wait = new WaitForSeconds(Random.Range(5, 10));
                yield return wait;
            }
        }

        protected override void ExecuteBehavior()
        {
            if (projectileCoroutine != null)
            {
                StopCoroutine(projectileCoroutine);
            }

            base.ExecuteBehavior();

            if (isServer)
            {
                projectileCoroutine = StartCoroutine(LaunchDevilProjectile());
            }
        }
        #endregion

        #region Class Implementation - Public
        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject == GameManager.Instance.PlayerLogicManager.Player)
            {
                converted = true;
                targetCharacter.GetComponent<PMEAngelBehaviorScript>().Converted = true;
                petMonsterEvent.UpdateSpiritsRescued();

                if (GameManager.Instance.PlayerLogicManager.IsLocalPlayer)
                {
                    Rigidbody playerRigidbody = GameManager.Instance.PlayerLogicManager.Player.GetComponent<Rigidbody>();
                    playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, 0, playerRigidbody.velocity.z);
                }

                StartCoroutine(ConvertDevilCoroutine(collision));
            }
        }
        #endregion

        #region Network Stuff
        [ClientRpc]
        private void LaunchDevilProjectileClientRpc()
        {
            animator.SetTrigger("ProjectileAttack");
        }
        #endregion
    }

}
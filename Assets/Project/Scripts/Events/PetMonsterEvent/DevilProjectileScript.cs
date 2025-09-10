namespace PlayMySpace.PMSC.Events
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// DevilProjectileScript.cs
    /// 
    /// This script contains the logic for the DevilProjectile's behavior.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class DevilProjectileScript : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject projectileExplosionPrefab;

        private PMECharacterBehaviorScript target;
        private float timer = 10;
        #endregion

        #region Class Implementation - Private
        private void Update()
        {
            timer -= Time.fixedDeltaTime;

            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }
        private IEnumerator DevilProjectileBehaviourCoroutine()
        {
            float t = 0;
            float moveSpeed = 1;
            Vector3 currentPosition = transform.position;
            
            while (target != null && Vector3.Distance(transform.position, target.transform.position) > 0.01f)
            {
                transform.position = Vector3.Lerp(currentPosition, target.transform.position, t / moveSpeed);
                Quaternion lookRotation = Quaternion.LookRotation(target.transform.position - transform.position);
                transform.rotation = Quaternion.Euler(lookRotation.eulerAngles.x, lookRotation.eulerAngles.y - 180, lookRotation.eulerAngles.z);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        private void Explode(Vector3 location, Quaternion rotation)
        {
            Instantiate(projectileExplosionPrefab, location, rotation, null);
            Destroy(gameObject);
        }
        #endregion

        #region Class Implementation - Public
        public void ExecuteBehavior(PMECharacterBehaviorScript target)
        {
            this.target = target;
            StartCoroutine(DevilProjectileBehaviourCoroutine());
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.GetComponent<PMEAngelBehaviorScript>() != null)
            {
                collision.gameObject.GetComponent<PMEAngelBehaviorScript>().animator.SetTrigger("TakeDamage");
                Explode(collision.contacts[0].point, Quaternion.LookRotation(-collision.contacts[0].normal));
            }
        }
        #endregion
    }
}

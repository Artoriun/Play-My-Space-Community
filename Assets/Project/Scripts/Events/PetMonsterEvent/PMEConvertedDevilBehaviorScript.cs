namespace PlayMySpace.PMSC.Events
{
    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// PMECharacterBehaviorScript.cs
    /// 
    /// This script contains logic for the behavior of Devils during the PetMonsterEvent.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PMEConvertedDevilBehaviorScript : PMECharacterBehaviorScript
    {
        #region Class Members
        [SerializeField] private GameObject devilConvertEffect;
        [SerializeField] private GameObject convertedDevilHeartEffect;
        [SerializeField] private GameObject towerHitEffectPrefab;
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            converted = true;
        }
        #endregion

        #region Class Implementation - Protected
        protected override IEnumerator CharacterAppearCoroutine()
        {
            devilConvertEffect.SetActive(true);

            // Cartoonish character pop-up effect by rapidly increasing scale to an overly large value and then quickly bringing it down to standard.
            targetCharacter.GetComponent<PMEAngelBehaviorScript>().StopBehavior();
            float t = 0;
            float lerpTime = 0.1f;
            Vector3 startVector = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 endVector = new Vector3(1.5f, 1.5f, 1.5f);
            characterModel.transform.localScale = startVector;

            while (characterModel.transform.localScale.x < endVector.x)
            {
                characterModel.transform.localScale = Vector3.Lerp(startVector, endVector, t);
                t += Time.fixedDeltaTime / lerpTime;
                yield return new WaitForFixedUpdate();
            }

            characterModel.transform.localScale = endVector;

            t = 0;
            lerpTime = 0.075f;
            startVector = endVector;
            endVector = Vector3.one;

            while (characterModel.transform.localScale.x > endVector.x)
            {
                characterModel.transform.localScale = Vector3.Lerp(startVector, endVector, t);
                t += Time.fixedDeltaTime / lerpTime;
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(0.5f);

            // Rotate both ConvertedDevil and Angel to face each other and also have ConvertedDevil move towards Angel
            t = 0;
            lerpTime = 1.5f;
            Quaternion angelStartRotation = targetCharacter.transform.rotation;
            Quaternion startRotation = transform.rotation;
            startVector = transform.position;
            endVector = targetCharacter.transform.position - 8 * (targetCharacter.transform.position - new Vector3(transform.position.x, targetCharacter.transform.position.y, transform.position.z)).normalized;

            while (t < 1)
            {
                targetCharacter.transform.rotation = Quaternion.Lerp(angelStartRotation, Quaternion.LookRotation(transform.position - targetCharacter.transform.position), t);
                transform.rotation = Quaternion.Lerp(startRotation, Quaternion.LookRotation(targetCharacter.transform.position - transform.position), t);
                transform.position = Vector3.Lerp(startVector, endVector, t);
                t += Time.fixedDeltaTime / lerpTime;
                yield return new WaitForFixedUpdate();
            }

            convertedDevilHeartEffect.SetActive(true);
            yield return new WaitForSeconds(1);

            // Rotate ConvertedDevil and Angel to face towards the Tokyo Tower
            t = 0;
            lerpTime = 1;
            angelStartRotation = targetCharacter.transform.rotation;
            Quaternion angelEndRotation = Quaternion.LookRotation(new Vector3(petMonsterEvent.transform.position.x,
                                                                              targetCharacter.transform.position.y,
                                                                              petMonsterEvent.transform.position.z) - targetCharacter.transform.position);
            startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.LookRotation(new Vector3(petMonsterEvent.transform.position.x, transform.position.y, petMonsterEvent.transform.position.z) - transform.position);
            targetCharacter.TrailEffect.SetActive(true);
            trailEffect.SetActive(true);

            while (t < 1)
            {
                targetCharacter.transform.rotation = Quaternion.Lerp(angelStartRotation, angelEndRotation, t);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
                t += Time.fixedDeltaTime / lerpTime;
                yield return new WaitForFixedUpdate();
            }

            // Have ConvertedDevil and Angel fly towards the TokyoTower then disappear inside of it
            t = 0;
            lerpTime = 5;
            Vector3 angelStartVector = targetCharacter.transform.position;
            startVector = transform.position;
            endVector = new Vector3(petMonsterEvent.transform.position.x, transform.position.y, petMonsterEvent.transform.position.z);
            Vector3 endScale = new Vector3(0.5f, 0.5f, 0.5f);

            while (t < 1)
            {
                targetCharacter.transform.position = Vector3.Lerp(angelStartVector, endVector, t);
                transform.position = Vector3.Lerp(startVector, endVector, t);
                targetCharacter.transform.position += new Vector3(0, 30 * Mathf.Sin(Mathf.PI * t), 0);
                transform.position += new Vector3(0, 30 * Mathf.Sin(Mathf.PI * t), 0);
                targetCharacter.CharacterModel.transform.localScale = Vector3.Lerp(Vector3.one, endScale, t);
                characterModel.transform.localScale = Vector3.Lerp(Vector3.one, endScale, t);
                t += Time.fixedDeltaTime / lerpTime;
                yield return new WaitForFixedUpdate();
            }

            Destroy(targetCharacter.gameObject);
            Destroy(gameObject);
        }

        protected IEnumerator CharacterDisappearCoroutine(Transform player)
        {
            float t = 0;
            float rotationSpeed = 0.5f;
            Quaternion startRotation = transform.rotation;

            while (t < 1)
            {
                transform.rotation = Quaternion.Lerp(startRotation, Quaternion.LookRotation(player.position - transform.position), t / rotationSpeed);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            t = 0;
            float scaleSpeed = 0.25f;

            while (t < 1)
            {
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t / scaleSpeed);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            //disappearEffect.SetActive(true);
            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }

        protected override IEnumerator DevilSpiralAngelBehaviorCoroutine()
        {
            yield return null;
        }
        #endregion

        #region Class Implementation - Public
        public void CharacterDisappear(Transform player)
        {
            StartCoroutine(CharacterDisappearCoroutine(player));
        }

        public new void ExecuteBehavior()
        {
            StartCoroutine(CharacterAppearCoroutine());
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name == "TokyoTower")
            {
                Instantiate(towerHitEffectPrefab,
                            collision.contacts[0].point + transform.right * Vector3.Distance(transform.position, targetCharacter.transform.position) * 0.5f,
                            Quaternion.LookRotation(collision.contacts[0].normal),
                            null);
            }
        }
        #endregion
    }

}
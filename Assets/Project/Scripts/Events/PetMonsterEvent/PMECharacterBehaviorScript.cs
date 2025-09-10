namespace PlayMySpace.PMSC.Events
{
    using System.Collections;
    using UnityEngine;
    using Mirror;

    /// <summary>
    /// PMECharacterBehaviorScript.cs
    /// 
    /// This script contains logic for the behavior of Devils during the PetMonsterEvent.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public abstract class PMECharacterBehaviorScript : NetworkBehaviour
    {
        #region Class Members
        [SerializeField] protected GameObject characterModel;
        [SerializeField] protected GameObject appearEffect;
        [SerializeField] protected GameObject disappearEffect;
        [SerializeField] protected GameObject trailEffect;

        public Animator animator;
        public enum PMECharacterBehavior { DevilSpiralAngel }
        public PMECharacterBehavior CharacterBehavior = PMECharacterBehavior.DevilSpiralAngel;

        protected PMECharacterBehaviorScript targetCharacter;
        protected bool executeBehavior = false;
        protected bool converted = false;
        protected Coroutine currentCoroutine = null;
        protected PetMonsterEvent petMonsterEvent;
        #endregion

        #region Class Accessors
        public bool Converted
        {
            get { return converted; }
            set { converted = value; }
        }

        public GameObject TrailEffect
        {
            get { return trailEffect; }
        }

        public GameObject CharacterModel
        {
            get { return characterModel; }
        }

        public PetMonsterEvent PetMonsterEvent
        {
            get { return petMonsterEvent; }
            set { petMonsterEvent = value; }
        }

        public PMECharacterBehaviorScript TargetCharacter
        {
            get { return targetCharacter; }
            set { targetCharacter = value; }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected virtual void Awake()
        {
            StartCoroutine(CharacterAppearCoroutine());
        }
        #endregion

        #region Class Implementation - Protected
        protected abstract IEnumerator CharacterAppearCoroutine();
        protected abstract IEnumerator DevilSpiralAngelBehaviorCoroutine();
        protected virtual IEnumerator CharacterDisappearCoroutine()
        {
            Instantiate(disappearEffect, transform.position, transform.rotation, null);

            float t = 0;
            float lerpSpeed = 0.15f;

            while (t < 1)
            {
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                t += Time.fixedDeltaTime / lerpSpeed;
                yield return new WaitForFixedUpdate();
            }

            Destroy(gameObject);
        }

        protected virtual void ExecuteBehavior()
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }

            switch (CharacterBehavior)
            {
                case PMECharacterBehavior.DevilSpiralAngel:
                    currentCoroutine = StartCoroutine(DevilSpiralAngelBehaviorCoroutine());
                    break;
            }
        }
        #endregion

        #region Class Implementation - Public
        public void SetBehavior(PMECharacterBehavior characterBehavior, PMECharacterBehaviorScript targetCharacter)
        {
            CharacterBehavior = characterBehavior;
            this.targetCharacter = targetCharacter;
            ExecuteBehavior();
        }

        public void SetBehavior(PMECharacterBehavior characterBehavior, PMECharacterBehaviorScript targetCharacter, PetMonsterEvent petMonsterEvent)
        {
            if (this.petMonsterEvent == null)
            {
                this.petMonsterEvent = petMonsterEvent;
            }

            CharacterBehavior = characterBehavior;
            this.targetCharacter = targetCharacter;
            ExecuteBehavior();
        }

        public void StopBehavior()
        {
            StopCoroutine(currentCoroutine);
        }

        public void Disappear()
        {
            StartCoroutine(CharacterDisappearCoroutine());
        }
        #endregion
    }

}
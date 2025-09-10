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
    public class PMEAngelBehaviorScript : PMECharacterBehaviorScript
    {
        #region Class Members
        [SyncVar] private float verticalDistance;
        [SyncVar] private int direction;
        [SyncVar] private Vector3 position;
        #endregion

        #region Class Accessors
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public float VerticalDistance
        {
            get { return verticalDistance; }
            set { verticalDistance = value; }
        }

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

        protected override IEnumerator DevilSpiralAngelBehaviorCoroutine()
        {
            float startingHeight = transform.position.y;
            float t = 0;

            while (true)
            {
                transform.position = new Vector3(transform.position.x, startingHeight + direction * verticalDistance * Mathf.Sin(t * 0.25f), transform.position.z);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        #endregion
    }

}
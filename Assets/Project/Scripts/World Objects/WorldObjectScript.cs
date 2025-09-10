namespace PlayMySpace.PMSC.World
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using PlayMySpace.PMSC.Managers;

    public class WorldObjectScript : MonoBehaviour, IPointerClickHandler
    {
        #region Class Implementation - Public
        public void OnPointerClick(PointerEventData eventData)
        {
            if (Vector3.Distance(GameManager.Instance.PlayerLogicManager.Pet.transform.position, new Vector3(transform.parent.position.x, 0, transform.parent.position.z)) < 100)
            {
                Destroy(transform.parent.gameObject);
                WorldManager.Instance.SpawnedCollectibles--;
            }
        }
        #endregion
    }
}

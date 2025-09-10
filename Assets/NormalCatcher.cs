using UnityEngine;
using PlayMySpace.PMSC.Managers;
using PlayMySpace.PMSC.Input;

public class NormalCatcher : MonoBehaviour
{
    #region Class Members
    private PetController petController;
    #endregion

    #region MonoBehaviour Stuff
    private void Awake()
    {
        petController = GameManager.Instance.PlayerLogicManager.Pet.GetComponent<PetController>();
    }
    #endregion

    #region Class Implementation
    private void OnCollisionStay(Collision other)
    {
        foreach (ContactPoint contact in other.contacts)
        {
            Debug.Log(contact.otherCollider.name);
        }
    }
    #endregion
}

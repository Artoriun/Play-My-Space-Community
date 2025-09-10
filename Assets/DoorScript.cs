using System.Collections;
using UnityEngine;
using PlayMySpace.PMSC.Managers;
using PlayMySpace.PMSC.Input;
using Mirror;

public class DoorScript : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private GameObject entryLocation;
    [SerializeField] private GameObject exitLocation;
    [SerializeField] private Animator fadeToBlackAnimator;

    private CameraController cameraController;
    private bool inside = false;
    private float previousZoomlevel;

    // Start is called before the first frame update
    void Start()
    {
        cameraController = GameManager.Instance.CameraController;
        previousZoomlevel = cameraController.ZoomLevel;
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void FadeToBlack()
    {
        fadeToBlackAnimator.SetTrigger("FadeToBlack");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.GetComponent<NetworkIdentity>().isLocalPlayer) { return; }

        GameObject pet = other.gameObject;
        inside = Vector3.Distance(pet.transform.position, exitLocation.transform.position) > Vector3.Distance(pet.transform.position, entryLocation.transform.position) ? true : false;

        if (!inside)
        {
            pet.transform.position = entryLocation.transform.position;
            previousZoomlevel = cameraController.ZoomLevel;
            cameraController.ZoomLevel = 1;
            cameraController.transform.rotation = transform.rotation;
        }
        else
        {
            pet.transform.position = exitLocation.transform.position;
            //cameraController.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(25, 0, 0));
            //GameManager.Instance.MapManager.InstantiatedAvatar.transform.GetChild(0).rotation = Quaternion.Euler(transform.rotation.eulerAngles - new Vector3(0, 180, 0));
        }
    }
}

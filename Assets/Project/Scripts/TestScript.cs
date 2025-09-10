using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TestScript : NetworkBehaviour
{
    public GameObject monsterPrefab;

    private void Awake()
    {
        GameObject.Find("Transform Button").GetComponent<Button>().onClick.AddListener(PetMonsterTransformation);
    }

    public void PetMonsterTransformation()
    {
        NetworkServer.ReplacePlayerForConnection(connectionToClient, Instantiate(monsterPrefab), true);
    }
}

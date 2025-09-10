using UnityEngine;

public class StartDemo : MonoBehaviour {

	public GameObject[] Prefabs;
	public int PrefabNum;
	public float PosY;
	public float min = 0f;
	public float max = 0f;
	
	void Awake() {
		GenerateLevel();
	}
	
	void GenerateLevel()
	{
		Vector3 prefabPos;

		GameObject[] gos = GameObject.FindObjectsOfType(typeof(GameObject)) as GameObject[];
		foreach(GameObject g in gos)
		{
			if(g.layer == 8)Destroy(g);
		}
		for (var i = 0; i < PrefabNum; i++)
		{

			prefabPos = new Vector3(Random.Range(min, max), PosY, Random.Range(min, max));
			Instantiate(Prefabs[Random.Range(0, Prefabs.Length)], prefabPos, Quaternion.identity);

		}
	}
}

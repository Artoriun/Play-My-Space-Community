using UnityEngine;
using System.Collections;

public class VisualDemoGUI : MonoBehaviour {

	public string nextLevel = "";

	void OnGUI(){
		if (GUI.Button (new Rect (10, 10, 100, 25), "Next Level")) {
			Application.LoadLevel (nextLevel);

		}
	}


}

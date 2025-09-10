using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DynamicLOD))]
public class DynamicLODEditor : Editor {

	private Texture2D logo;

	void OnEnable(){
		logo = (Texture2D) AssetDatabase.LoadAssetAtPath("Assets/DynamicOcclusionSystem/Editor/Images/DynamicLOD.png", typeof(Texture2D));
	}

	public override void OnInspectorGUI(){
		GUILayout.Label(logo);
		DrawDefaultInspector();
	}

}

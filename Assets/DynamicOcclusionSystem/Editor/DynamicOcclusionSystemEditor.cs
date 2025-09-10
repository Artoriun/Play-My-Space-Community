using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DynamicOcclusionSystem))]
public class DynamicOcclusionSystemEditor : Editor {

	private Texture2D logo;
    public int minFPS = 15;

    void OnEnable(){
       
        logo = (Texture2D) AssetDatabase.LoadAssetAtPath("Assets/DynamicOcclusionSystem/Editor/Images/DynamicOcclusion.png", typeof(Texture2D));

    }

    public override void OnInspectorGUI(){

        DynamicOcclusionSystem DOS = target as DynamicOcclusionSystem;
        GUILayout.Label(logo);
        // DrawDefaultInspector();

        
        DOS.cam             = EditorGUILayout.ObjectField(DOS.cam, typeof(Camera), true) as Camera;
        DOS.layerMask       = EditorGUILayout.LayerField(new GUIContent("Layer to cull","Select a Layermask for occlusion and deselect Static checkbox"),DOS.layerMask);
        DOS.simple          = EditorGUILayout.Toggle(new GUIContent("Fast Method","Simple is faster but occluded shadows disapear"), DOS.simple);
        DOS.terrainQuality  = EditorGUILayout.Toggle(new GUIContent("Terrain Quality", "Adapt detail of terrain according Quality Settings"), DOS.terrainQuality);
                
        EditorGUILayout.HelpBox(" If the number of FPS falls below the minimum, then the low level graphic quality.\n If the number of FPS rises above the maximum, the graphic quality level increases \n Maintain a difference of at least 10 frames apart \n[Current Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()] + "] [to: " + DOS.FramesPerSec + " FPS]", MessageType.None, true);
        DOS.autoQuality     = EditorGUILayout.Toggle(new GUIContent("Auto Quality", "Allow automatic Adapt Quality Settings according FPS"), DOS.autoQuality);
        DOS.minimalFPS      = EditorGUILayout.IntSlider("Min FPS to Decrease",DOS.minimalFPS, 5, 20);
        DOS.maximalFPS      = EditorGUILayout.IntSlider("Max FPS to Increment",DOS.maximalFPS, 25, 60);


        DOS.distanceDeactivation    = EditorGUILayout.Toggle(new GUIContent("Enable/Disable GO", "enables or disables GameObjects to the distance"), DOS.distanceDeactivation);
        DOS.renderersOcclusion      = EditorGUILayout.Toggle(new GUIContent("Renderer Occlusion", "Apply Renderers Occlusion"), DOS.renderersOcclusion);
        DOS.lightsOcclusion         = EditorGUILayout.Toggle(new GUIContent("Lights Occusion", "Apply Light Occlusion"), DOS.lightsOcclusion);
        DOS.audioSourceOcclusion    = EditorGUILayout.Toggle(new GUIContent("AudioSources Occlusion", "Apply AudioSource Occlusion"), DOS.audioSourceOcclusion);
        DOS.flaresOcclusion         = EditorGUILayout.Toggle(new GUIContent("LightFlares Occusion", "Apply Flares Occlusion"), DOS.flaresOcclusion);

        DOS.debug = EditorGUILayout.Toggle(new GUIContent("Show Debug", "Show on the screen FPS and Quality level"), DOS.debug);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(DOS);
            DOS.Start();
        }
    }

}

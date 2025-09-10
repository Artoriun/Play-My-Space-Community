/*
Version 1.4.1
  */
  
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[AddComponentMenu("Darkcom/Dynamic Occlusion System")]
public class DynamicOcclusionSystem : MonoBehaviour {

	[Range(0,31)]                   //Limit the range for Layers
    public int 	    layerMask;      // layer where the occlusion begins work

    public Camera   cam;            // Camera where the occlusion begins work
    private float[] distances = new float[32];//Distances for each Layermask. Only use LayerMask var

    private System.Diagnostics.Stopwatch chrono = new System.Diagnostics.Stopwatch();
    private double ms;              // Display chrono

	public bool	debug                   = true; //"Show debug"
    public bool	autoQuality 	        = true; //"Adapt Quality Settings according FPS"				
    public bool	terrainQuality 	        = true; //"Adapt detail of  terrain according Quality Settings"
    public bool	simple			        = true; //"Simple is faster but occluded shadows disapear"		
    public bool renderersOcclusion      = true; //"Apply Renderers Occlusion"                          
    public bool lightsOcclusion         = true; //"Apply Light Occlusion"                              
    public bool audioSourceOcclusion    = true; //"Apply AudioSource Occlusion"                        
    public bool flaresOcclusion         = true; //"Apply Flares Occlusion"                             
    public bool distanceDeactivation    = false;

    [Range (5,20)]	public int minimalFPS = 15; //"if the fps drops below this, the level of low quality also "
    [Range (20,60)]	public int maximalFPS = 30; //"whether fps rises above this, the quality level also rises"

    private GameObject[]    gObjects; // List of All GameObjects raw
    private List <GameObject> gObjectsFiltered = new List<GameObject>(); // List of All GameObjects Filtered
    private List <Renderer> gRenderersFiltered = new List<Renderer>(); // List of Renderers filtered from gRenderers
    private List<AudioSource> aSourcesFiltered = new List<AudioSource>(); // List of AudioSources Filtered from aSources
    private List<Light>     rLightsFiltered = new List<Light>(); // List of Lights filtered from rLights
    private List<LensFlare> rFlaresFiltered = new List<LensFlare>(); // List of LensFlares filtered From rFlares

    private Terrain[] 		terrains; // List of All Terrains actives
    private List<TerrainCollider> terrainCollider = new List<TerrainCollider>();

    private int[] qualityData = new int[4];// [0] = currentQuality [1] = originalQualitySettings [2] = lastFPS [3] = lastQuality;

	public int              FramesPerSec { get; protected set; } // Current FPS

    // Use this for initialization
    public void Start () {
        StopAllCoroutines();
        chrono.Start();         // initialize chrono on start()
        ClearRenderersList();   // clear Lists After disable this script
		UpdateRenderersList (); // update and filtered Lists on Start

        if (cam == null) cam = Camera.main; // if no Camera is assigned, Assign main camera
        else
        {
            distances[layerMask] = cam.farClipPlane / 2;    // assign to layerMask the camera far clip plane
            cam.layerCullDistances = distances;             // activate layerCullDistances
        }
        //---------------------------------------------

        Application.targetFrameRate = 60;        //used for save memory if VSync is not active

        qualityData[1] = QualitySettings.GetQualityLevel();    // Save original quality for use when disabling this script
        StartCoroutine(FPS());              // Start FPS Counter, autoQuality and terrainQuality
        StartCoroutine(IRLateUpdate());     // Start Dinamyc Occlusion

        chrono.Stop(); //Stop chrono to measure performance
        Debug.Log(chrono.Elapsed.Milliseconds.ToString());
        chrono.Reset();
    }

    public void ClearRenderersList(){
        // CLEAR ARRAYS
        gObjects    = new GameObject [0];
		terrains 	= new Terrain[0];
        // CLEAR LISTS
        gObjectsFiltered.Clear();
        gRenderersFiltered.Clear();
        rLightsFiltered.Clear();
        aSourcesFiltered.Clear();
        rFlaresFiltered.Clear();
	}

	public void UpdateRenderersList(){
        // GAMEOBJECTS FILTER
            gObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            if (gObjects.Length > 0) {
                foreach (GameObject gObject in gObjects) {
                    if (/*!gObject.isStatic && */gObject.layer == layerMask) gObjectsFiltered.Add(gObject);
                }
            }
        
        // RENDERERS FILTER
            if (renderersOcclusion)            {
                if (gObjectsFiltered.Count > 0)                {
                    foreach (GameObject gObject in gObjectsFiltered)                    {
                    Renderer gRender = gObject.GetComponent<Renderer>();
                        if (gRender && !gRender.isPartOfStaticBatch) gRenderersFiltered.Add(gRender);
                    }
                }
            }

        // LENS FLARE FILTER
        if (flaresOcclusion)            {
            foreach (GameObject gObject in gObjectsFiltered) {
                LensFlare rFlare = gObject.GetComponent<LensFlare>();
                if(rFlare)rFlaresFiltered.Add(rFlare);
                }            
            }

        
        // AUDIO SOURCES FILTER
        if (audioSourceOcclusion)
        {

            foreach (GameObject gObject in gObjectsFiltered)
            {
                AudioSource aSource = gObject.GetComponent<AudioSource>();
#if (UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
                if(aSource)    aSourcesFiltered.Add(aSource);
#else
                if (aSource && aSource.spatialBlend == 1) aSourcesFiltered.Add(aSource);
#endif
            }
        }

        // LIGHTS FILTER
        if (lightsOcclusion) {

            foreach (GameObject gObject in gObjectsFiltered) {
                Light rLight = gObject.GetComponent<Light>();
                if (rLight && rLight.type != LightType.Directional) rLightsFiltered.Add(rLight);
                }
            }

       
        // ACTIVE TERRAINS
        terrains = Terrain.activeTerrains;

        // TERRAIN COLLIDERS

        foreach (Terrain terrain in terrains) {
           terrainCollider.Add(terrain.GetComponent<TerrainCollider>());
        }
	}


    void SetVisibleRenderer(Renderer render, bool visible){
        //     // IF SIMPLE
        //if ((QualitySettings.shadowCascades == 0 || !SystemInfo.supportsShadows) || simple) {
        //	render.enabled = visible;
        render.gameObject.SetActive(visible);
//			}
//        // IF COMPLEX
//            else if((QualitySettings.shadowCascades > 0 && SystemInfo.supportsShadows) && !simple ){

//#if (UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
//            if (visible)
//            {
//                if (!render.castShadows)  render.castShadows = true;
//                if (!render.receiveShadows)
//                {
//                    render.useLightProbes = true;
//                    render.receiveShadows = true;
//                }
//            }
//            else
//            {
//                if (render.castShadows)  render.castShadows = false;
//                if (render.receiveShadows)
//                {
//                    render.useLightProbes = false;
//                    render.receiveShadows = false;
//                }
//            }
//#else
//            if (visible ) {
//					if(render.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.On)
//						render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
//					if(render.reflectionProbeUsage != UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox)
//						render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbesAndSkybox;
//					if(!render.receiveShadows){
//						render.useLightProbes = true;
//						render.receiveShadows = true;
//			        }
//				}
//            else {
//					if(render.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
//						render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
//					if(render.reflectionProbeUsage != UnityEngine.Rendering.ReflectionProbeUsage.Off)
//						render.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
//					if(render.receiveShadows){
//						render.useLightProbes = false;
//						render.receiveShadows = false;
//						}
//				}
//#endif

//        }
    }

	bool GetVisibleRenderer(Renderer render){
        // if SIMPLE
		if (QualitySettings.shadowCascades == 0 || !SystemInfo.supportsShadows || simple) {
			if(render.enabled)return true;
			else return false;
		}
        // IF NOT
        else  {
#if (UNITY_4_6 || UNITY_4_5 || UNITY_4_4 || UNITY_4_3 || UNITY_4_2 || UNITY_4_1 || UNITY_4_0)
            if (render.receiveShadows && render.castShadows) return true;
#else
            if(render.receiveShadows && render.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.On)return true;
#endif
            else return false;
		}
	}

	private IEnumerator IRLateUpdate(){
        // DYNAMIC OCCLUSION
        while (Application.isPlaying) {
            chrono.Start();

            #region [GameObject Deactivation]
            List<GameObject> gObjectsToRemove = new List<GameObject>();// List of GameObjects that has been elimitated

            if (gObjectsFiltered.Count > 0 && enabled) {
                foreach (GameObject gObj in gObjectsFiltered) {
                    if (gObj)
                    {
                        if (distanceDeactivation)
                        {// DEACTIVE GAME OBJECTS FROM DISTANCES
                            if (ClassExtensions.GetDistanceFast(gObj.transform.position, cam.transform.position) > cam.farClipPlane / 2) gObj.SetActive(false);//SetVisibleRenderer(gRenderer, false);//esto traga 2.4 a 2.5 ms / ahora es 2.3 al cambiar el 3.1446055 por 3
                            else if(ClassExtensions.GetDistanceFast(gObj.transform.position, cam.transform.position) < cam.farClipPlane/2) gObj.gameObject.SetActive(true);
                        }
                    }
                    else gObjectsToRemove.Add(gObj);
                }
            }

            if (gObjectsToRemove.Count > 0) {
                foreach (GameObject gObj in gObjectsToRemove) {
                    gObjectsFiltered.Remove(gObj);
                }
            }
            #endregion

            #region [MeshRenderer] 
            List<Renderer> gRenderersToRemove = new List<Renderer>(); // List of Renderer that has been eliminated

            if (gRenderersFiltered.Count > 0 && enabled)            {
                foreach (Renderer gRenderer in gRenderersFiltered)       {
                    
                    if (gRenderer)                    {
                        if (ClassExtensions.GetDistanceFast(gRenderer.transform.position, cam.transform.position) > ClassExtensions.GetBoundsVolume(gRenderer.bounds))
                        {// VISIBLE FROM CAMERA VISIBLE WITHOUT FRUSTUM PLANES
                            if (!ClassExtensions.BoundsIsVisibleFromViewport(gRenderer.bounds, cam) && GetVisibleRenderer(gRenderer)) SetVisibleRenderer(gRenderer, false);
                            else if (ClassExtensions.BoundsIsVisibleFromViewport(gRenderer.bounds, cam) && !GetVisibleRenderer(gRenderer))SetVisibleRenderer(gRenderer, true);
                        }
                        else
                        {// VISIBLE FROM CAMERA WITH FRUSTUM PLANES 
                            if (!ClassExtensions.BoundsIsVisibleFromPlanes(gRenderer.bounds, cam) && GetVisibleRenderer(gRenderer)) SetVisibleRenderer(gRenderer, false);
                            else if (ClassExtensions.BoundsIsVisibleFromPlanes(gRenderer.bounds, cam) && !GetVisibleRenderer(gRenderer))SetVisibleRenderer(gRenderer, true);
                        }
                                           
                    }

                    

                    // IF RENDERER IS ELIMINATED ADD TO LIST TO REMOVE IN NEXT STEP
                    else gRenderersToRemove.Add(gRenderer);
                }
            }
            // REMOVE RENDERERS FROM FILTERED LIST
            if (gRenderersToRemove.Count > 0) {
                foreach (Renderer gRenderer in gRenderersToRemove) {
                    gRenderersFiltered.Remove(gRenderer);
                }
                gRenderersToRemove.Clear();
            }

#endregion


            #region [Audio Sources]
            List<AudioSource> aSourcesToRemove = new List<AudioSource>(); // List of AudioSources Removed

            if (aSourcesFiltered.Count > 0)
            {
                foreach (AudioSource aSource in aSourcesFiltered)
                {
                    if (aSource)
                    {// AUDIOSOURCES DISTANCE OCCLUSION
                        if (ClassExtensions.GetDistanceFast(aSource.transform.position, cam.transform.position) > aSource.maxDistance * 2)
                            aSource.enabled = false;
                        else aSource.enabled = true;
                    }
                    // ADD TO LIST AUDIOSOURCES ELIMINATED IN NEXT STEP
                    else aSourcesToRemove.Add(aSource);
                }
            }
            // REMOVE RENDERERS FROM FILTERED LIST
            if (aSourcesToRemove.Count > 0) {
                foreach (AudioSource aSource in aSourcesToRemove) {
                    aSourcesFiltered.Remove(aSource);
                }
                aSourcesToRemove.Clear();
            }
#endregion
            //TODO: tratar de fusionar simple con complejo en la vision de las luces
            #region [Lights]
            List<Light> rLightsToRemove = new List<Light>(); // List of Lights Removed

            if (rLightsFiltered.Count > 0)
            {
                foreach (Light rLight in rLightsFiltered)
                {
                    if (rLight)
                    {   // LIGHT VISIBLE FROM CAMERA
                        if (ClassExtensions.Vector3IsVisibleFrom(rLight.transform.position, cam) && rLight.renderMode != LightRenderMode.Auto) rLight.renderMode = LightRenderMode.Auto;
                        else if (!ClassExtensions.Vector3IsVisibleFrom(rLight.transform.position, cam) && rLight.renderMode != LightRenderMode.ForceVertex) rLight.renderMode = LightRenderMode.ForceVertex;
                  
                    }
                    // ADD ELIMINATED lIGHTS TO REMOVE LIST
                    else rLightsToRemove.Add(rLight);
                }
            }
            // REMOVE ELIMINATED LIGHTS FROM FILTERED LIST
            if (rLightsToRemove.Count > 0) {
                foreach (Light rLight in rLightsToRemove) {
                    rLightsFiltered.Remove(rLight);
                }
                rLightsToRemove.Clear();
            }
            #endregion

            #region [Flares]
            List<LensFlare> rFlaresToRemove = new List<LensFlare>(); // List of LensFlares Removed

            if (rFlaresFiltered.Count > 0) {//Los Flares dan conflictos con otras camaras
                foreach (LensFlare rFlare in rFlaresFiltered) {
                    if (rFlare)
                    {// LENS FLARE VISIBLE FROM CAMERA
                        if (ClassExtensions.Vector3IsVisibleFrom(rFlare.transform.position, cam) && !rFlare.enabled) rFlare.enabled = true;
                        else if (!ClassExtensions.Vector3IsVisibleFrom(rFlare.transform.position, cam) && rFlare.enabled) rFlare.enabled = false;
                    }
                    // ADD REMOVE FLARES TO LIST
                    else rFlaresToRemove.Add(rFlare);
                }
            }
            // REMOVE LENSFLARE FROM FILTERED LIST
            if (rFlaresToRemove.Count > 0) {
                foreach (LensFlare rFlare in rFlaresToRemove) {
                    rFlaresFiltered.Remove(rFlare);
                }
                rFlaresToRemove.Clear();
            }
            #endregion

            #region TerrainColliders
            List<TerrainCollider> terrainCollidersToRemove = new List<TerrainCollider>();
            if (terrainCollider.Count > 0)
            {
                foreach (TerrainCollider terCol in terrainCollider)
                {
                    if (terCol != null)
                    {
                        if (terCol.bounds.Contains(cam.transform.position))
                        {
                            terCol.gameObject.GetComponent<Terrain>().heightmapMaximumLOD = 0;
                            terCol.gameObject.GetComponent<Terrain>().enabled = true;
                        }
                        else
                        {
                            if (ClassExtensions.BoundsIsVisibleFromPlanes(terCol.bounds, cam)) terCol.gameObject.GetComponent<Terrain>().enabled = true;
                            else terCol.gameObject.GetComponent<Terrain>().enabled = false;

                            terCol.gameObject.GetComponent<Terrain>().heightmapMaximumLOD = 1;
                        }
                    }
                    else terrainCollidersToRemove.Add(terCol);
                }
            }

            if (terrainCollidersToRemove.Count > 0) {
                foreach (TerrainCollider terCol in terrainCollidersToRemove) {
                    terrainCollider.Remove(terCol);
                }
                terrainCollidersToRemove.Clear();
            }
            #endregion

            chrono.Stop();
            ms = chrono.Elapsed.TotalMilliseconds;
            chrono.Reset();
            yield return new WaitForSeconds(1/(qualityData[0]+1));
        }
    }

    private void OnGUI(){
		if (debug) {
			GUI.Label (new Rect (10, Screen.height - 160, Screen.width /2, 200), "[Renderers: " + gRenderersFiltered.Count + " of " + gObjectsFiltered.Count +"]\n" +
                                                                                   "[GObjects: " + gObjectsFiltered.Count + " of " + gObjects.Length + "]\n" +
                                                                                   "[AudioSources: " + aSourcesFiltered.Count + " of " + gObjectsFiltered.Count + "]\n" +
                                                                                "[Lights: " + rLightsFiltered.Count + " of " + gObjectsFiltered.Count + "]\n"+
                                                                                "[LensFlares: "+ rFlaresFiltered.Count + " of " + gObjectsFiltered.Count + "]\n" +
                                                                                "[Terrains: " + terrains.Length + "]\n" +
                                                                                ms + " ms\n" + FramesPerSec + " FPS " + "Quality " + QualitySettings.names[qualityData[0]]
                                                                                );
		}
	}
    //-------------------------------------------------------------------------------------------
    // AUTOQUALITY
    private IEnumerator FPS() {
		for(;;){
			// Capture frame-per-second
			int lastFrameCount = Time.frameCount;
			float lastTime = Time.realtimeSinceStartup;
			yield return new WaitForSeconds(1);
			float timeSpan = Time.realtimeSinceStartup - lastTime;
			int frameCount = Time.frameCount - lastFrameCount;
			
			// Display it
			FramesPerSec = Mathf.RoundToInt(frameCount / timeSpan);
            if ((FramesPerSec < minimalFPS || FramesPerSec > maximalFPS) && autoQuality) CalculateQuality();
            else {
                if(QualitySettings.GetQualityLevel() == qualityData[1] && !autoQuality)QualitySettings.SetQualityLevel(qualityData[1]);
            }
			qualityData[0] = QualitySettings.GetQualityLevel ();
			
		}
	}


	public void ActivateAutomaticQuality(bool activation){
        // AUTOMATIC QUALITY FOR ACTIVATE FROM OTHER SCRIPT
		autoQuality = activation;
		if(activation == false) QualitySettings.SetQualityLevel(qualityData[1]);
	}

	private void CalculateQuality(){

        // RETURN IF LAST FPS IS EQUAL CURRENT FPS
        if (qualityData[2] == Mathf.FloorToInt (FramesPerSec) || !autoQuality)
			return;
		else
			qualityData[2] = Mathf.FloorToInt (FramesPerSec);
        // INCREASE OR DECREASE QUALITY LEVEL
		if(qualityData[0] > 0 && FramesPerSec < minimalFPS && autoQuality)
			QualitySettings.DecreaseLevel(false);
		else if(qualityData[0] < QualitySettings.names.Length && FramesPerSec > maximalFPS && autoQuality)
			QualitySettings.IncreaseLevel(false);
        // INCREASE OR DECREASE CAMERA FAR CLIP PLANE AND SHADER LOD
		cam.farClipPlane = 3000/QualitySettings.names.Length - qualityData[0];
        distances[layerMask] = cam.farClipPlane / 2;
        Shader.globalMaximumLOD = 100 + (qualityData[0] * 100);
        // INCREASE OR DECREASE TERRAIN DETAILS
        foreach (Terrain terrain in terrains) {
			if(terrainQuality)SetTerrainQuality(terrain,qualityData[0]);
			else SetTerrainQuality(terrain,qualityData[1]);
		}
	}

	private void SetTerrainQuality(Terrain eTerrain,int quality){

		if (qualityData[3] == quality)
			return;
		else
			qualityData[3] = quality;

		eTerrain.heightmapPixelError 	= 5*QualitySettings.names.Length - quality;
		eTerrain.basemapDistance 		= cam.farClipPlane/2;
        eTerrain.detailObjectDensity 	= (1/terrains.Length);// Esta accion es muy cara esto es mejor en Start

        eTerrain.detailObjectDistance 	= Mathf.Clamp(qualityData[2] + 30,30,90);// esta comienza a ser cara despues de 80
    //http://wiki.unity3d.com/index.php?title=Terrain_tutorial
        eTerrain.treeDistance 			= eTerrain.detailObjectDistance * 2; //cam.farClipPlane;//barata
        eTerrain.treeBillboardDistance = eTerrain.detailObjectDistance;//cam.farClipPlane / 2;//barata
		eTerrain.treeMaximumFullLODCount= 50 / QualitySettings.names.Length - quality;//Barata
		
		/*if(qualityData[0]== 0)eTerrain.heightmapMaximumLOD	= 1;
		else eTerrain.heightmapMaximumLOD	= 0;*/
		
	}
	//--------------------------------------------------------------------------------------

	//private void OnDestroy(){}

	private void OnDisable ()	{

		if (Application.isEditor)
			QualitySettings.SetQualityLevel(qualityData[1]);

		foreach (Renderer gRenderer in gRenderersFiltered) {
            if (gRenderer) {
                gRenderer.gameObject.SetActive(true);
                SetVisibleRenderer(gRenderer, true);
            }
		}

        foreach (Light rLight in rLightsFiltered) {
            if (rLight) {
                rLight.gameObject.SetActive(true);
                rLight.enabled = true;
            }
        }

        foreach (AudioSource aSource in aSourcesFiltered) {
            if (aSource) aSource.enabled = true;
        }

        foreach (LensFlare rFlare in rFlaresFiltered) {
            if (rFlare) rFlare.enabled = true;
        }

        StopAllCoroutines();
	}

    private void OnEnable() {
        Start();
    }
	private void OnLevelWasLoaded ()	{
		Start();
	}

}

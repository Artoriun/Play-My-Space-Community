/*
Version 1.4.1
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[System.Serializable]
public class LODData {
    public MeshFilter meshFilter;
    public Mesh lod0;
    public Mesh lod1;
    public Mesh lod2;
    public bool automaticDistance;

    public float lodDistance1;
    public float lodDistance2;
    public MeshRenderer meshRenderer;
}

[AddComponentMenu("Darkcom/Dynamic LOD")]
public class DynamicLOD: MonoBehaviour {

    public List<LODData> lodDatas = new List<LODData>();

	public Camera cam;

    private List<GameObject> childrens = new List<GameObject>();

    private void Start (){
        childrens.Clear();

        if (transform.childCount > 0) {
            for (int i = 0; i < transform.childCount; i++) {
                if (transform.GetChild(i).gameObject.isStatic) childrens.Add(transform.GetChild(i).gameObject);
            }
        }
                
        // pro feature
        if (gameObject.isStatic && childrens.Count > 0) StaticBatchingUtility.Combine(childrens.ToArray(), gameObject);

        
        
        //Experimental

        if (lodDatas.Count > 0) {
            foreach (LODData lodData in lodDatas) {
                if (lodData.lod0 == null) lodData.lod0 = lodData.meshFilter.mesh;

            }
        }

        //-------------------------]

        DynamicOcclusionSystem camObject;
		camObject = (DynamicOcclusionSystem)FindObjectOfType(typeof(DynamicOcclusionSystem));
		if(camObject)cam = camObject.cam;
		if(cam == null)cam = Camera.main;
		StartCoroutine (GestarLOD());
	}


	IEnumerator GestarLOD(){
		while (Application.isPlaying) {

            if (lodDatas.Count > 0) {
                foreach (LODData lodData in lodDatas) {
                    if (lodData.meshRenderer.enabled)
                    {
                        if (QualitySettings.GetQualityLevel() > 1 && lodData.lod0 && lodData.lod1 && lodData.lod2)
                        {
                            if (ClassExtensions.GetDistanceFast(cam.transform.position, lodData.meshRenderer.transform.position) < lodData.lodDistance1 && lodData.lod0)
                                lodData.meshFilter.mesh = lodData.lod0;
                            else if (ClassExtensions.GetDistanceFast(cam.transform.position, lodData.meshRenderer.transform.position) > lodData.lodDistance1 && ClassExtensions.GetDistanceFast(cam.transform.position, lodData.meshRenderer.transform.position) < lodData.lodDistance2 && lodData.lod1)
                                lodData.meshFilter.mesh = lodData.lod1;
                            else if (ClassExtensions.GetDistanceFast(cam.transform.position, lodData.meshRenderer.transform.position) > lodData.lodDistance2 && lodData.lod2)
                                lodData.meshFilter.mesh = lodData.lod2;
                        }

                        else if (QualitySettings.GetQualityLevel() == 1 && lodData.lod1 && lodData.lod2)
                        {
                            if (ClassExtensions.GetDistanceFast(cam.transform.position, lodData.meshRenderer.transform.position) < lodData.lodDistance1 && lodData.lod1)
                                lodData.meshFilter.mesh = lodData.lod1;
                            else lodData.meshFilter.mesh = lodData.lod2;

                        }
                        else if (QualitySettings.GetQualityLevel() == 1 && lodData.lod2) lodData.meshFilter.mesh = lodData.lod2;
                    }
                    else {
                        if (lodData.lod2) lodData.meshFilter.mesh = lodData.lod2;
                        else if (lodData.lod1) lodData.meshFilter.mesh = lodData.lod1;
                    }

                    float temporalVolume = 0;
                    if (lodData.meshRenderer.enabled && temporalVolume != ClassExtensions.GetBoundsVolume(lodData.meshRenderer.bounds))
                    {

                        if (lodData.automaticDistance)
                        {
                            lodData.lodDistance1 = 2 * ClassExtensions.GetBoundsVolume(lodData.meshRenderer.bounds);
                            lodData.lodDistance2 = 3 * ClassExtensions.GetBoundsVolume(lodData.meshRenderer.bounds);
                        }
                        temporalVolume = ClassExtensions.GetBoundsVolume(lodData.meshRenderer.bounds);
                    }
                }
            }

					
            
        yield return new WaitForSeconds(1 / (1+QualitySettings.GetQualityLevel()));
		}
	}
}

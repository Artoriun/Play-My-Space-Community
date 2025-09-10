namespace PlayMySpace.PMSC.Managers
{
    using Google.Maps;
    using Google.Maps.Coord;
    using Google.Maps.Event;
    using Google.Maps.Feature.Style;
    using UnityEngine;
    using UnityEngine.UI;
    using System;
    using System.Collections;
    using PlayMySpace.PMSC.Settings;
    using PlayMySpace.PMSC.Models;
    using PlayMySpace.PMSC.Wrappers;
    using Mirror;

    /// <summary>
    /// MapManager.cs
    /// 
    /// All logic related to the generating of the map and ttttstructures goes here.
    /// 
    /// By Peter de Keijzer
    /// </summary>

    [RequireComponent(typeof(MapsService))]
    public class MapManager : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private GameObject mapLight;
        [SerializeField] private MapsService mapsService;
        [SerializeField] private MapSettings mapSettings;
        [SerializeField] private GameObject pitufo;
        [SerializeField] private GameObject doorPrefab;

        [Header("Custom Materials")]
        [SerializeField] private Material insideWallTextureMaterial;
        [SerializeField] private Material insideFloorTextureMaterial;

        [Header("Custom Structures")]
        [SerializeField] private GameObject tokyoTowerPrefab;

        [Header("UI Stuff")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject introductionPanel;

        [Tooltip("LatLng to load (must be set before hitting play).")]
        public LatLng location = new LatLng(35.6631022228493, 139.73191277201724);

        private GameObject instantiatedPet;
        private bool pitufoInstantiated = false;
        [SerializeField] private bool setPitufoPosition = false;
        [SerializeField] private Vector3 pitufoPosition = Vector3.zero;
        private Vector3 previousPitufoPosition = Vector3.zero;
        private Coroutine moveCharacter;
        private Slider loadingBar;
        private GameObject doorRaycaster;

        // Callbacks
        private Action onAvatarInstantiated;
        private Action onMapLoaded;
        #endregion

        #region Class Accessors
        public MapsService MapsService
        {
            get
            {
                return mapsService;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            Input.location.Start();
            AuthWrapper.Instance.AddCallback(InitializeMap);
            doorRaycaster = new GameObject("doorRaycaster");
        }
        private void Update()
        {
            //if (instantiatedPitufo != null)
            //{
            //    if (!Application.isEditor)
            //    {
            //        Vector3 newPosition = mapsService.Projection.FromLatLngToVector3(new LatLng(Input.location.lastData.latitude, Input.location.lastData.longitude));

            //        if (Vector3.Distance(pitufoPosition, newPosition) > 0.05f)
            //        {
            //            pitufoPosition = newPosition;
            //            moveCharacter = StartCoroutine(MoveCharacter());
            //        }
            //        //instantiatedPitufo.transform.GetChild(0).transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
            //    }
            //    else if (setPitufoPosition)
            //    {
            //        moveCharacter = StartCoroutine(MoveCharacter());
            //    }

            //    //instantiatedPitufo.GetComponentInChildren<Animator>().SetFloat("Velocity", Vector3.Distance(previousPitufoPosition, instantiatedPitufo.transform.position) / Time.deltaTime);
            //    previousPitufoPosition = instantiatedPitufo.transform.position;
            //}
        }
        #endregion

        #region Class Implementation - Private
        private void InitializeMap()
        {
            InitializeLoadingPanel();

            Debug.Log("Loading map");

            if (Application.isEditor)
            {
                // Set real-world location to load.
                mapsService.InitFloatingOrigin(location);

                // Register a listener to be notified when the map is loaded.
                mapsService.Events.MapEvents.Loaded.AddListener(OnLoaded);

                // Load map with default options.
                mapsService.LoadMap(mapSettings.DefaultBounds, mapSettings.MapOptions);

                // Load Playable Locations
                //WorldDataRequest worldDataRequest = new WorldDataRequest();
                //LatLng southWest = mapsService.Projection.FromVector3ToLatLng(InstantiatedAvatar.transform.position + new Vector3(-1000, 0, -1000));
                //LatLng northEast = mapsService.Projection.FromVector3ToLatLng(InstantiatedAvatar.transform.position + new Vector3(1000, 0, 1000));
                //worldDataRequest.southWest = new PlayableLocationLatLng() { latitude = southWest.Lat, longitude = southWest.Lng };
                //worldDataRequest.northEast = new PlayableLocationLatLng() { latitude = northEast.Lat, longitude = northEast.Lng };
                //StartCoroutine(GameServerWrapper.Instance.PostWorldData(worldDataRequest, null, null));
            }
            else
            {
                StartCoroutine(SpawnPitufo(mapsService));
            }
        }

        private void InitializeLoadingPanel()
        {
            if (!loadingPanel.activeInHierarchy)
            {
                loadingPanel.SetActive(true);
            }

            loadingBar = loadingPanel.GetComponentInChildren<Slider>();
        }
        
        private IEnumerator SpawnPitufo(MapsService mapsService)
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
                yield break;

            // Start service before querying location
            Input.location.Start(5, 0.1f);

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                print("Timed out");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                print("Unable to determine device location");
                yield break;
            }
            else
            {
                // Access granted and location value could be retrieved
                print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);

                // Set map origin to current device location
                LatLng pitufoLatLng = new LatLng(Input.location.lastData.latitude, Input.location.lastData.longitude);
                mapsService.InitFloatingOrigin(pitufoLatLng);

                // Register a listener to be notified when the map is loaded.
                mapsService.Events.MapEvents.Loaded.AddListener(OnLoaded);

                // Load map with default options.
                mapsService.LoadMap(mapSettings.DefaultBounds, mapSettings.MapOptions);

                // Spawn Pitufo
                GameManager.Instance.PlayerLogicManager.Pet = Instantiate(pitufo, mapsService.Projection.FromLatLngToVector3(pitufoLatLng), pitufo.transform.rotation, null);
                GameManager.Instance.PlayerLogicManager.Pet.transform.Rotate(Vector3.up, Input.compass.magneticHeading);
                GameManager.Instance.PlayerLogicManager.Pet.name = "Pitufo";
                previousPitufoPosition = instantiatedPet.transform.position;
                mapLight.transform.position = instantiatedPet.transform.position;
                mapLight.transform.parent = instantiatedPet.transform;
                pitufoInstantiated = true;
                onAvatarInstantiated?.Invoke();

                // Load Playable Locations
                WorldDataRequest worldDataRequest = new WorldDataRequest();
                LatLng southWest = mapsService.Projection.FromVector3ToLatLng(instantiatedPet.transform.position + new Vector3(-1000, 0, -1000));
                LatLng northEast = mapsService.Projection.FromVector3ToLatLng(instantiatedPet.transform.position + new Vector3(1000, 0, 1000));
                worldDataRequest.southWest = new PlayableLocationLatLng() { latitude = southWest.Lat, longitude = southWest.Lng };
                worldDataRequest.northEast = new PlayableLocationLatLng() { latitude = northEast.Lat, longitude = northEast.Lng };
                StartCoroutine(GameServerWrapper.Instance.PostWorldData(worldDataRequest, null, null));
            }

            // Stop service if there is no need to query location updates continuously
            //Input.location.Stop();
        }

        #endregion

        [SerializeField] private bool enableDynamicCulling = false;

        #region Class Implementation - Public
        /// <summary>
        /// Example of OnLoaded event listener.
        /// </summary>
        /// <remarks>
        /// The communication between the game and the MapsSDK is done through APIs and event listeners.
        /// </remarks>
        public void OnLoaded(MapLoadedArgs args)
        {
            if (enableDynamicCulling)
            {
                Camera.main.GetComponent<DynamicOcclusionSystem>().enabled = true;
            }

            GameManager.Instance.PlayerLogicManager.Pet = NetworkClient.localPlayer.gameObject;
            instantiatedPet = GameManager.Instance.PlayerLogicManager.Pet;
            //Camera.main.transform.parent.parent = instantiatedPitufo.transform;
            mapLight.transform.position = instantiatedPet.transform.position;
            mapLight.transform.parent = instantiatedPet.transform;
            GameManager.Instance.CameraController.SwitchTarget(GameManager.Instance.PlayerLogicManager.Pet);
            GameManager.Instance.CameraController.SetCameraPosition();
            onAvatarInstantiated?.Invoke();
            GameManager.Instance.PlayerLogicManager.Pet.name = "Pitufo";

            onMapLoaded?.Invoke();
        }

        public void AddAvatarInstantiatedCallback(Action callback)
        {
            onAvatarInstantiated += callback;
        }

        public void UpdateMapLoadProgress(MapLoadProgressArgs args)
        {
            LoadingManager.Instance.UpdateProgress(LoadingManager.Type.Map, args.Progress);
        }

        public void HandleModeledStructures(DidCreateModeledStructureArgs args)
        {
            // Spawn TokyoTower GameObject
            if (args.GameObject.name.Contains("ChIJcx2EkL2LGGARv0gV3HSFqQo"))
            {
                GameObject tokyoTower = Instantiate(tokyoTowerPrefab, args.GameObject.transform.position, tokyoTowerPrefab.transform.rotation);
                tokyoTower.layer = 24;
                Destroy(args.GameObject);
            }
        }

        public void GenerateExtrudedStructures(WillCreateExtrudedStructureArgs args)
        {
            if (((Vector3.Distance(args.MapFeature.Shape.BoundingBox.center, Camera.main.transform.position) > 800) && args.MapFeature.Shape.BoundingBox.extents.x * args.MapFeature.Shape.BoundingBox.extents.y < 400)
                )//|| args.MapFeature.Shape.BoundingBox.extents.x * args.MapFeature.Shape.BoundingBox.extents.y < 30)
            {
                args.Cancel = true;
            }
            else
            {
                ExtrudedStructureStyle.Builder builder = args.Style.AsBuilder();
                //builder.RoofMaterial = mapSettings.FlatMaterial;
                //builder.WallMaterial = mapSettings.FlatMaterial;
                //builder.ApplyFixedHeight = true;
                //builder.FixedHeight = 0.5f;

                if (args.MapFeature.GameObjectName().Contains("ChIJyd9mRPKKGGARzDT_WVTcbZg"))
                {
                    builder.ApplyFixedHeight = false;
                    builder.WallMaterial = mapSettings.HouseWallMaterials[1];
                    builder.RoofMaterial = mapSettings.RoofMaterials[1];
                }

                if (args.MapFeature.Shape.BoundingBox.extents.y > 15)//15)
                {
                    UnityEngine.Random.InitState(DateTime.UtcNow.Millisecond);
                    int randomWall = UnityEngine.Random.Range(0, 6);
                    builder.WallMaterial = mapSettings.BuildingWallMaterials[randomWall];

                    UnityEngine.Random.InitState(DateTime.UtcNow.Millisecond);
                    int randomRoof = UnityEngine.Random.Range(0, 6);
                    builder.RoofMaterial = mapSettings.RoofMaterials[randomRoof];
                    builder.ApplyFixedHeight = false;
                }
                else
                {
                    if (true)
                    {
                        UnityEngine.Random.InitState(DateTime.UtcNow.Millisecond);
                        int randomWall = UnityEngine.Random.Range(0, mapSettings.HouseWallMaterials.Length);
                        builder.WallMaterial = mapSettings.HouseWallMaterials[randomWall];

                        UnityEngine.Random.InitState(DateTime.UtcNow.Millisecond);
                        int randomRoof = UnityEngine.Random.Range(3, 6);
                        builder.RoofMaterial = mapSettings.RoofMaterials[randomRoof];

                        builder.FixedHeight = Mathf.Min(args.MapFeature.Shape.BoundingBox.extents.y, 20);
                        builder.ApplyFixedHeight = true;
                    }
                    else
                    {
                        //builder.RoofMaterial = mapSettings.CommunityBaseRoofMaterial;
                    }
                }
                args.Style = builder.Build();
            }
        }

        int triangleCounter = 0;

        public void HandleCreatedStructures(DidCreateExtrudedStructureArgs args)
        {
            if (args.GameObject.GetComponent<MeshRenderer>().bounds.extents.y > 1)
            {
                args.GameObject.tag = "Groundable";
                args.GameObject.AddComponent<MeshCollider>();
                args.GameObject.GetComponent<MeshCollider>().material = mapSettings.SlideyPhysicMaterial;
                //args.GameObject.GetComponent<MeshCollider>().convex = true;
            }

            // Buildings with a height over 2 are climbable and enterable
            if (args.GameObject.GetComponent<MeshRenderer>().bounds.extents.y > 2)
            {
                args.GameObject.tag = "Climbable";

                #region Building Interior Code
                // -------------------------------------------
                // Code for creating a second mesh inside the structure with an inverted collider to make sure the player cannot walk through walls once inside
                // Remove the collider on the mesh
                foreach (Collider c in args.GameObject.GetComponents<Collider>())
                {
                    Destroy(c);
                }

                // Clone the mesh at a slightly smaller scale
                GameObject innerMesh = Instantiate(args.GameObject);
                innerMesh.name = args.GameObject.name + "(interior)";
                innerMesh.transform.parent = args.GameObject.transform;
                innerMesh.transform.position = args.GameObject.transform.position;
                innerMesh.transform.localScale = new Vector3(0.999f, 0.999f, 0.999f);

                // Assign inner mesh to DynamicCulling layer
                //innerMesh.layer = 9;

                // Invert the clonded mesh's triangles
                Mesh mesh = innerMesh.GetComponent<MeshFilter>().mesh;
                var indices = mesh.triangles;
                var triangleCount = indices.Length / 3;

                for (var i = 0; i < triangleCount; i++)
                {
                    var tmp = indices[i * 3];
                    indices[i * 3] = indices[i * 3 + 1];
                    indices[i * 3 + 1] = tmp;
                }

                mesh.triangles = indices;
                innerMesh.GetComponent<MeshFilter>().mesh = mesh;

                // Create a new MeshCollider for the inverted mesh, causing it to be inverted as well, effectively trapping the player inside the structure
                innerMesh.AddComponent<MeshCollider>();
                innerMesh.GetComponent<MeshCollider>().material = mapSettings.SlideyPhysicMaterial;

                // Change inner mesh material to insideWallMaterial
                Material[] materials = innerMesh.GetComponent<MeshRenderer>().materials;
                materials[0] = insideWallTextureMaterial;
                materials[1] = insideFloorTextureMaterial;
                innerMesh.GetComponent<MeshRenderer>().materials = materials;

                // Create floors for the building interiors
                GameObject floor = Instantiate(args.GameObject, args.GameObject.transform.position, args.GameObject.transform.rotation, args.GameObject.transform);
                floor.name = "floor";
                floor.transform.localScale = new Vector3(floor.transform.localScale.x, 0.00001f, floor.transform.localScale.z);
                materials = floor.GetComponent<MeshRenderer>().materials;
                materials[0] = null;
                materials[1] = insideFloorTextureMaterial;
                floor.GetComponent<MeshRenderer>().materials = materials;
                // -------------------------------------------
                #endregion

                #region Building Door Generation Code
                // -------------------------------------------
                // Code for generating doors on building walls
                // Create a Transform to use as a vector for a raycast that checks for suitable positions on the building mesh to place a door
                Vector3 previousDoorNormal = new Vector3(Mathf.Infinity, 0, 0);

                for (int i = 0; i < 4; i++)
                {
                    doorRaycaster.transform.position = args.GameObject.transform.position;
                    doorRaycaster.transform.rotation = args.GameObject.transform.rotation * Quaternion.AngleAxis(i * 90, Vector3.up);

                    RaycastHit raycastHit;

                    if (Physics.Raycast(new Ray(doorRaycaster.transform.position, doorRaycaster.transform.forward), out raycastHit))
                    {
                        if (Vector3.Dot(previousDoorNormal, raycastHit.normal) > 0.9f ||
                            raycastHit.collider.gameObject.CompareTag("Door"))
                        {
                            continue;
                        }

                        if (triangleCounter < 5)
                        {

                            triangleCounter++;
                        }

                        previousDoorNormal = raycastHit.normal;

                        // Make sure the door is placed at the bottom center of the wall by getting the middle point of the triangle making up the bottom half (defined by the second and third vertices)
                        // *** (there's an easy with some buildings protruding into other buildings, will have to fix that)
                        if (mesh.triangles.Length <= raycastHit.triangleIndex * 3)
                        {
                            continue;
                        }

                        Vector3 p1 = raycastHit.collider.transform.TransformPoint(mesh.vertices[mesh.triangles[raycastHit.triangleIndex * 3 + 1]]);
                        Vector3 p2 = raycastHit.collider.transform.TransformPoint(mesh.vertices[mesh.triangles[raycastHit.triangleIndex * 3 + 2]]);
                        Vector3 triangleCenterPosition = (p1 + p2) / 2;
                        doorRaycaster.transform.position = triangleCenterPosition;
                        doorRaycaster.transform.rotation = Quaternion.LookRotation(raycastHit.normal);

                        // Instantiate the door GameObject
                        GameObject doorGameObject = Instantiate(doorPrefab, new Vector3(doorRaycaster.transform.position.x, 0.6f, doorRaycaster.transform.position.z), doorRaycaster.transform.rotation, args.GameObject.transform);
                        doorGameObject.transform.Find("DoorLight").gameObject.layer = 21;
                        //DoorScript doorScript = doorGameObject.GetComponent<DoorScript>();
                        //doorScript.BuildingInteriorGameObject = innerMesh;
                        //doorScript.FloorGameObject = floor;

                        //// Disable inner mesh so that we can activate it when the player actually enters the building;
                        //innerMesh.SetActive(false);

                        //// Disable floor so that we can activate it when the player actually enters the building;
                        //floor.SetActive(false);
                    }
                    // -------------------------------------------
                }
                #endregion
            }

            // Add building exteriors to Minimap layer
            args.GameObject.layer = 24;
        }

        public void GenerateSegments(WillCreateSegmentArgs args)
        {
            SegmentStyle.Builder builder = args.Style.AsBuilder();
            builder.Width = 5;
            builder.Material = mapSettings.SegmentMaterial;
            builder.BorderMaterial = mapSettings.SegmentBorderMaterial;
            builder.BorderWidth = 1;
            args.Style = builder.Build();
        }

        public void HandleCreatedSegments(DidCreateSegmentArgs args)
        {
            args.GameObject.layer = 23;
            args.GameObject.transform.GetChild(0).gameObject.layer = 23;
        }

        public void HandleCreatedRegions(DidCreateRegionArgs args)
        {
            args.GameObject.layer = 25;
        }

        public void HandleCreatedAreaWaters(DidCreateAreaWaterArgs args)
        {
            args.GameObject.layer = 26;
        }

        public void LoadMapRegion()
        {
            Debug.Log("Loading map region");
            mapsService.MakeMapLoadRegion().AddCircle(Camera.main.transform.position, 1200).Load(mapSettings.MapOptions);
            WorldDataRequest worldDataRequest = new WorldDataRequest();
            LatLng southWest = mapsService.Projection.FromVector3ToLatLng(instantiatedPet.transform.position + new Vector3(-1000, 0, -1000));
            LatLng northEast = mapsService.Projection.FromVector3ToLatLng(instantiatedPet.transform.position + new Vector3(1000, 0, 1000));
            worldDataRequest.southWest = new PlayableLocationLatLng() { latitude = southWest.Lat, longitude = southWest.Lng };
            worldDataRequest.northEast = new PlayableLocationLatLng() { latitude = northEast.Lat, longitude = northEast.Lng };
            StartCoroutine(GameServerWrapper.Instance.PostWorldData(worldDataRequest, null, null));
        }
        #endregion
    }
}

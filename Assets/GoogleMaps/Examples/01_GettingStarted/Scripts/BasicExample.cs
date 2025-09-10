using Google.Maps.Coord;
using Google.Maps.Event;
using Google.Maps.Examples.Shared;
using UnityEngine;
using System.Collections;

namespace Google.Maps.Examples
{

    /// <summary>
    /// This example demonstrates a basic usage of the Maps SDK for Unity.
    /// </summary>
    /// <remarks>
    /// By default, this script loads the Statue of Liberty. If a new lat/lng is set in the Unity
    /// inspector before pressing start, that location will be loaded instead.
    /// </remarks>
    [RequireComponent(typeof(MapsService))]
    public class BasicExample : MonoBehaviour
    {
        [SerializeField] private GameObject pitufo;

        [Tooltip("LatLng to load (must be set before hitting play).")]
        public LatLng LatLng = new LatLng(40.6892199, -74.044601);

        private MapsService mapsService;
        private GameObject instantiatedPitufo;

        private void Awake()
        {
            Input.location.Start();
        }

        /// <summary>
        /// Use <see cref="MapsService"/> to load geometry.
        /// </summary>
        private void Start()
        {
            // Get required MapsService component on this GameObject.
            mapsService = GetComponent<MapsService>();

            if (Application.isEditor)
            {
                // Set real-world location to load.
                mapsService.InitFloatingOrigin(LatLng);
                instantiatedPitufo = Instantiate(pitufo, Vector3.zero, pitufo.transform.rotation, null);
                Camera.main.transform.parent.parent = instantiatedPitufo.transform;
            }
            else
            {
                // Set real-world location to load.
                mapsService.InitFloatingOrigin(LatLng);
                StartCoroutine(SpawnPitufo(mapsService));
            }

            // Register a listener to be notified when the map is loaded.
            mapsService.Events.MapEvents.Loaded.AddListener(OnLoaded);

            // Load map with default options.
            mapsService.LoadMap(ExampleDefaults.DefaultBounds, ExampleDefaults.DefaultGameObjectOptions);
        }

        IEnumerator SpawnPitufo(MapsService mapsService)
        {
            // First, check if user has location service enabled
            if (!Input.location.isEnabledByUser)
                yield break;

            // Start service before querying location
            Input.location.Start();

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
                LatLng pitufoLatLng = new LatLng(Input.location.lastData.latitude, Input.location.lastData.longitude);
                //mapsService.InitFloatingOrigin(pitufoLatLng);
                instantiatedPitufo = Instantiate(pitufo, mapsService.Projection.FromLatLngToVector3(pitufoLatLng), pitufo.transform.rotation, null);
                instantiatedPitufo.transform.Rotate(Vector3.up, Input.compass.magneticHeading);
                Camera.main.transform.parent.parent = instantiatedPitufo.transform;
            }

            // Stop service if there is no need to query location updates continuously
            //Input.location.Stop();
        }

        private void Update()
        {
            if (!Application.isEditor && instantiatedPitufo != null)
            {
                instantiatedPitufo.transform.position = mapsService.Projection.FromLatLngToVector3(new LatLng(Input.location.lastData.latitude, Input.location.lastData.longitude));
                instantiatedPitufo.transform.GetChild(0).transform.rotation = Quaternion.Euler(0, -Input.compass.magneticHeading, 0);
            }
        }

        /// <summary>
        /// Example of OnLoaded event listener.
        /// </summary>
        /// <remarks>
        /// The communication between the game and the MapsSDK is done through APIs and event listeners.
        /// </remarks>
        public void OnLoaded(MapLoadedArgs args)
        {
            // The Map is loaded - you can start/resume gameplay from that point.
            // The new geometry is added under the GameObject that has MapsService as a component.
        }
    }
}

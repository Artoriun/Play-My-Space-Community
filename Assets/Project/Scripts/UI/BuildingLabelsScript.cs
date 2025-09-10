namespace PlayMySpace.PMSC.UI
{
    using UnityEngine;
    using Google.Maps.Event;
    using Google.Maps.Examples;
    using Google.Maps.Examples.Shared;
    using PlayMySpace.PMSC.Managers;
    using TMPro;

    /// <summary>
    /// RoadLabelsScript.cs
    /// 
    /// Handles the creation of Road Labels whenever new segments are generated in the game world.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class BuildingLabelsScript : MonoBehaviour
    {
        #region Class Members
        [SerializeField] private Labeller labeller;
        [SerializeField] private GameObject buildingLabelPrefab;
        [SerializeField] private Canvas buildingLabelsCanvas;
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            GameManager.Instance.MapManager.MapsService.Events.ExtrudedStructureEvents.DidCreate.AddListener(OnExtrudedStructureCreated);
            GameManager.Instance.MapManager.MapsService.Events.ModeledStructureEvents.DidCreate.AddListener(OnModeledStructureCreated);

        }
        #endregion

        #region Class Implementation - Private
        void OnExtrudedStructureCreated(DidCreateExtrudedStructureArgs args)
        {
            GameObject buildingLabel = Instantiate(buildingLabelPrefab, args.GameObject.transform.position + new Vector3(0, 99, 0), Quaternion.identity, buildingLabelsCanvas.transform);
            buildingLabel.name = args.MapFeature.Metadata.Name;
            buildingLabel.GetComponent<TextMeshPro>().text = args.MapFeature.Metadata.Name;
            //CreateBuildingLabel(args.GameObject, args.MapFeature.Metadata.PlaceId, args.MapFeature.Metadata.Name);
        }

        void OnModeledStructureCreated(DidCreateModeledStructureArgs args)
        {
            GameObject buildingLabel = Instantiate(buildingLabelPrefab, args.GameObject.transform.position + new Vector3(0, 99, 0), Quaternion.identity, buildingLabelsCanvas.transform);
            buildingLabel.name = args.MapFeature.Metadata.Name;
            buildingLabel.GetComponent<TextMeshPro>().text = args.MapFeature.Metadata.Name;
            //CreateBuildingLabel(args.GameObject, args.MapFeature.Metadata.PlaceId, args.MapFeature.Metadata.Name);
        }

        /// <summary>
        /// Creates a label for a building.
        /// </summary>
        /// <param name="buildingGameObject">The GameObject of the building.</param>
        /// <param name="placeId">The place ID of the building.</param>
        /// <param name="displayName">The name to display on the label for the building.</param>
        void CreateBuildingLabel(GameObject buildingGameObject, string placeId, string displayName)
        {
            if (!labeller.enabled)
                return;

            // Ignore uninteresting names.
            if (displayName.Equals("ExtrudedStructure") || displayName.Equals("ModeledStructure"))
            {
                return;
            }

            Label label = labeller.NameObject(buildingGameObject, placeId, displayName);
            if (label != null)
            {
                MapsGamingExamplesUtils.PlaceUIMarker(buildingGameObject, label.transform);
            }
        }
        #endregion
    }
}
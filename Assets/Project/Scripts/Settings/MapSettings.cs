namespace PlayMySpace.PMSC.Settings
{
    using Google.Maps;
    using Google.Maps.Feature.Style;
    using UnityEngine;

    /// <summary>
    /// MapSettings.cs
    /// 
    /// Material/shader settings for the map.
    /// 
    /// By Peter de Keijzer
    /// </summary>
    public class MapSettings : MonoBehaviour
    {
        #region Class Members
        private GameObjectOptions mapOptions;

        [SerializeField] private Bounds defaultBounds = new Bounds(Vector3.zero, new Vector3(800, 0, 800));

        [Header("Materials")]
        [SerializeField] private Material[] houseWallMaterials;
        [SerializeField] private Material[] buildingWallMaterials;
        [SerializeField] private Material[] roofMaterials;
        [SerializeField] private Material flatMaterial;
        [SerializeField] private Material eventHubRoofMaterial;
        [SerializeField] private Material communityBaseRoofMaterial;
        [SerializeField] private Material regionMaterial;
        [SerializeField] private Material waterMaterial;
        [SerializeField] private Material segmentMaterial;
        [SerializeField] private Material segmentBorderMaterial;
        [SerializeField] private Material intersectionMaterial;

        [Header("Physic Materials")]
        [SerializeField] private PhysicMaterial slideyPhysicMaterial;

        [Header("Prefabs")]
        [SerializeField] private GameObject konbini;

        private Material chosenWallMaterial;
        #endregion

        #region Class Accessors
        public PhysicMaterial SlideyPhysicMaterial
        {
            get { return slideyPhysicMaterial; }
        }

        public GameObjectOptions MapOptions
        {
            get
            {
                return mapOptions;
            }
        }

        public Bounds DefaultBounds
        {
            get
            {
                return defaultBounds;
            }
        }

        public Material EventHubRoofMaterial
        {
            get
            {
                return eventHubRoofMaterial;
            }
        }

        public Material CommunityBaseRoofMaterial
        {
            get
            {
                return communityBaseRoofMaterial;
            }
        }

        public Material[] HouseWallMaterials
        {
            get
            {
                return houseWallMaterials;
            }
        }

        public Material[] BuildingWallMaterials
        {
            get
            {
                return buildingWallMaterials;
            }
        }

        public Material[] RoofMaterials
        {
            get
            {
                return roofMaterials;
            }
        }

        public Material FlatMaterial
        {
            get
            {
                return flatMaterial;
            }
        }

        public Material SegmentMaterial
        {
            get
            {
                return segmentMaterial;
            }
        }

        public Material SegmentBorderMaterial
        {
            get
            {
                return segmentBorderMaterial;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            InitializeMap();
        }
        #endregion

        #region Class Implementation - Private
        private void InitializeMap()
        {


            // Create style for buildings made from extruded shapes (most buildings).
            ExtrudedStructureStyle extrudedStructureStyle =
                new ExtrudedStructureStyle
                    .Builder
                { WallMaterial = buildingWallMaterials[0], RoofMaterial = roofMaterials[2] }
                    .Build();

            // Create style for buildings with detailed vertex/triangle data (such as the Statue of
            // Liberty).
            ModeledStructureStyle modeledStructureStyle =
                new ModeledStructureStyle.Builder { Material = buildingWallMaterials[0] }.Build();

            // Create style for regions (such as parks).
            RegionStyle regionStyle = new RegionStyle.Builder { FillMaterial = regionMaterial }.Build();

            // Create style for bodies of water (such as oceans).
            AreaWaterStyle areaWaterStyle =
                new AreaWaterStyle.Builder { FillMaterial = waterMaterial }.Build();

            // Create style for lines of water (such as narrow rivers).
            LineWaterStyle lineWaterStyle =
                new LineWaterStyle.Builder { Material = waterMaterial }.Build();

            // Create style for segments (such as roads).
            SegmentStyle segmentStyle =
                new SegmentStyle.Builder
                {
                    Material = segmentMaterial,
                    IntersectionMaterial = intersectionMaterial,
                    Width = 7.0f
                }.Build();

            // Collect styles into a form that can be given to map loading function.
            mapOptions = new GameObjectOptions
            {
                ExtrudedStructureStyle = extrudedStructureStyle,
                ModeledStructureStyle = modeledStructureStyle,
                RegionStyle = regionStyle,
                AreaWaterStyle = areaWaterStyle,
                LineWaterStyle = lineWaterStyle,
                SegmentStyle = segmentStyle,
            };
        }
        #endregion
    }
}

namespace PlayMySpace.PMSC.Managers
{
    using System.Collections;
    using UnityEngine;
    using Framework.Patterns;

    /// <summary>
    /// WorldManager.cs
    /// 
    /// Handles the loading and updating of all objects and data on the world map.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class WorldManager : Singleton<WorldManager>
    {
        #region Class Members
        [SerializeField] int maximumCollectibles = 4;
        [SerializeField] private GameObject[] collectiblePrefabs;

        int spawnedCollectibles = 0;
        #endregion

        #region Class Accessors
        public int SpawnedCollectibles
        {
            get
            {
                return spawnedCollectibles;
            }
            set
            {
                spawnedCollectibles = value;
            }
        }
        #endregion

        #region MonoBehaviour Stuff
        protected override void Awake()
        {
            base.Awake();
            LoadingManager.Instance.AddCallback(StartSpawningCollectibles);
        }
        #endregion

        #region Class Implementation - Private
        private IEnumerator SpawnCollectiblesCoroutine()
        {
            while (true && maximumCollectibles > 0)
            {
                while (spawnedCollectibles < 1)
                {
                    float spawnChance = 0.5f;

                    for (int i = 0; i < collectiblePrefabs.Length; i++)
                    {
                        if (spawnedCollectibles >= maximumCollectibles)
                        {
                            break;
                        }

                        float spawnNumber = Random.Range(0, 1f);

                        if (spawnNumber < spawnChance)
                        {
                            Random.InitState(System.DateTime.Now.Millisecond);
                            Instantiate(collectiblePrefabs[i],
                                GameManager.Instance.PlayerLogicManager.Pet.transform.position +
                                new Vector3(
                                    Random.insideUnitCircle.x < 0 ? Mathf.Min((Random.insideUnitCircle.x - 0.5f) * 80, -20) : Mathf.Max((Random.insideUnitCircle.x - 0.5f) * 80, 20),
                                    0,
                                    Random.insideUnitCircle.y < 0 ? Mathf.Min((Random.insideUnitCircle.y - 0.5f) * 80, -20) : Mathf.Max((Random.insideUnitCircle.y - 0.5f) * 80, 20)),
                                collectiblePrefabs[i].transform.rotation,
                                null);
                            spawnedCollectibles++;
                        }

                        yield return new WaitForSeconds(0.05f);
                    }
                }

                yield return new WaitForSeconds(60);
            }
        }

        private void StartSpawningCollectibles()
        {
            StartCoroutine(SpawnCollectiblesCoroutine());
        }
        #endregion

        #region Class Implementation - Public
        #endregion
    }
}

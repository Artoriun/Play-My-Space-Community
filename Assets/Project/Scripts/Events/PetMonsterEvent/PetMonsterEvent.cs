namespace PlayMySpace.PMSC.Events
{
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using PlayMySpace.PMSC.Managers;
    using PlayMySpace.PMSC.Caches;
    using Mirror;

    /// <summary>
    /// PetMonsterEvent.cs
    /// 
    /// Triggers the PetMonsterEvent.
    /// 
    /// Copyright © 2021 Play My Space
    /// </summary>
    public class PetMonsterEvent : NetworkBehaviour
    {
        #region Class Members
        [Header("Prefabs")]
        [SerializeField] private GameObject angelPrefab;
        [SerializeField] private GameObject[] devilPrefabs;
        [SerializeField] private Transform petMonsterCharacters;
        [SerializeField] private GameObject haloPrefab;
        [SerializeField] private GameObject petMonsterEventZone;
        [Space(10)]

        [SerializeField] private float lobbyTimer;
        [SerializeField] private float startingTimer;
        [SerializeField] private float[] roundTimers;
        [Space(10)]

        [SyncVar] private float currentTimer = 0;
        [SyncVar] private int spiritsRescued = 0;
        [SyncVar] private int halosCollected = 0;
        [SyncVar] private int numberOfSpawns;
        [SyncVar] private int spawnedSpirits;
        [SyncVar] private int spawnedHalos;

        public enum PMEPhase { Lobby, Starting, ConvertDevils, CollectHalos, Ending };
        public PMEPhase CurrentPhase = PMEPhase.Lobby;

        private List<(NetworkConnectionToClient, string)> participants = new List<(NetworkConnectionToClient, string)>();
        private GameObject eventPanel, eventMessageTopPanel, eventMessageLeftPanel;
        private TextMeshProUGUI eventTimerText, eventMessageTop, eventMessageLeft, endingMessage, lobbyMessage, lobbyTimerText, lobbyTimerStartingMessage;
        private Button eventJoinButton;
        #endregion

        #region Class Accessors
        public List<(NetworkConnectionToClient, string)> Participants
        {
            get { return participants; }
        }

        public Transform PetMonsterCharacters
        {
            get { return petMonsterCharacters; }
        }
        #endregion

        #region MonoBehaviour Stuff
        private void Awake()
        {
            eventPanel = GameManager.Instance.EventPanel;
            eventMessageTopPanel = eventPanel.transform.Find("EventMessageTopPanel").gameObject;
            eventMessageLeftPanel = eventPanel.transform.Find("EventMessageLeftPanel").gameObject;
            eventTimerText = eventPanel.transform.Find("EventMessageTimerPanel").Find("EventTimer").GetComponent<TextMeshProUGUI>();
            lobbyTimerText = eventPanel.transform.Find("EventMessageTimerPanel").Find("LobbyTimer").GetComponent<TextMeshProUGUI>();
            lobbyTimerStartingMessage = eventPanel.transform.Find("EventMessageTimerPanel").Find("LobbyTimerStartingMessage").GetComponent<TextMeshProUGUI>();
            eventMessageTop = eventMessageTopPanel.transform.Find("EventMessageTop").GetComponent<TextMeshProUGUI>();
            eventMessageLeft = eventMessageLeftPanel.transform.Find("EventMessageLeft").GetComponent<TextMeshProUGUI>();
            endingMessage = eventMessageTopPanel.transform.Find("EndingMessage").GetComponent<TextMeshProUGUI>();
            lobbyMessage = eventPanel.transform.Find("EventMessageCenterPanel").Find("LobbyMessage").GetComponent<TextMeshProUGUI>();
            eventJoinButton = eventPanel.transform.Find("EventMessageCenterPanel").Find("EventJoinButton").GetComponent<Button>();
            eventJoinButton.gameObject.SetActive(true);
            eventJoinButton.onClick.AddListener(EventJoinButtonCallback);
            eventPanel.SetActive(true);
        }

        private void Update()
        {
            if (isServer)
            {
                if (CurrentPhase == PMEPhase.Lobby)
                {
                    if (currentTimer > 0)
                    {
                        lobbyTimerText.text = TimerToString(currentTimer);
                        lobbyMessage.text = UpdateLobbyMessage();

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            UpdateLobbyMessagesTargetRpc(n.Item1, lobbyMessage.text);
                        }

                        currentTimer -= Time.deltaTime;
                    }
                    else
                    {
                        CurrentPhase = PMEPhase.Starting;
                        currentTimer = startingTimer;
                        lobbyTimerStartingMessage.text = "Get ready to go!";

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            SetStartingMessageTargetRpc(n.Item1);
                        }
                    }
                }
                else if (CurrentPhase == PMEPhase.Starting)
                {
                    if (currentTimer > 0)
                    {
                        lobbyTimerText.text = TimerToString(currentTimer);

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            UpdateStartingGUITargetRpc(n.Item1);
                        }

                        currentTimer -= Time.deltaTime;
                    }
                    else
                    {
                        CurrentPhase = PMEPhase.ConvertDevils;
                        StartRound1();
                    }
                }
                else if (CurrentPhase == PMEPhase.ConvertDevils)
                {
                    if (currentTimer > 0)
                    {
                        eventTimerText.text = TimerToString(currentTimer);

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            UpdateRoundTimerTextTargetRpc(n.Item1);
                        }

                        currentTimer -= Time.deltaTime;

                        if (spiritsRescued == spawnedSpirits)
                        {
                            currentTimer = 0;
                        }
                    }
                    else
                    {
                        PMECharacterBehaviorScript[] characters = petMonsterCharacters.GetComponentsInChildren<PMECharacterBehaviorScript>();

                        foreach (PMECharacterBehaviorScript character in characters)
                        {
                            if (!character.Converted)
                            {
                                character.Disappear();
                            }
                        }

                        StartCoroutine(Round2Coroutine(spiritsRescued * 2));
                    }
                }
                else if (CurrentPhase == PMEPhase.CollectHalos)
                {
                    if (currentTimer > 0)
                    {
                        eventTimerText.text = TimerToString(currentTimer);

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            UpdateRoundTimerTextTargetRpc(n.Item1);
                        }

                        currentTimer -= Time.deltaTime;

                        if (halosCollected == spawnedHalos)
                        {
                            currentTimer = 0;
                        }
                    }
                    else
                    {
                        eventTimerText.text = "";

                        PMEHaloBehaviorScript[] halos = petMonsterCharacters.GetComponentsInChildren<PMEHaloBehaviorScript>();

                        foreach (PMEHaloBehaviorScript halo in halos)
                        {
                            halo.Disappear();
                        }

                        CurrentPhase = PMEPhase.Ending;
                        eventMessageLeftPanel.SetActive(false);
                        eventTimerText.gameObject.SetActive(false);
                        StartCoroutine(EndingCoroutine(7.5f));

                        foreach ((NetworkConnectionToClient, string) n in participants)
                        {
                            SetEndingTargetRpc(n.Item1, 5);
                        }
                    }
                }
            }
        }
        #endregion

        #region Class Implementation - Private
        private void EventJoinButtonCallback()
        {
            JoinEventCmd(NetworkClient.localPlayer, PlayerDataCache.Instance.PlayerData.name);
            eventJoinButton.gameObject.SetActive(false);
            eventPanel.SetActive(true);
        }

        private IEnumerator DisableAfterTime(GameObject g, float time)
        {
            yield return new WaitForSeconds(10);
            g.SetActive(false);
        }

        private string TimerToString(float timer)
        {
            if (timer <= 0)
            {
                return "0:00";
            }

            int seconds = Mathf.FloorToInt(timer % 60);
            return Mathf.FloorToInt(timer / 60) + ":" + (seconds < 10 ? "0" + seconds : seconds.ToString());
        }

        private IEnumerator Round1Coroutine(int numberOfSpawns)
        {
            spawnedSpirits = numberOfSpawns;

            WaitForSeconds wait = new WaitForSeconds(0.10f);

            for (int i = 0; i < numberOfSpawns; i++)
            {
                PMEAngelBehaviorScript angel = Instantiate(angelPrefab,
                                               transform.position,
                                               Quaternion.identity,
                                               petMonsterCharacters)
                                               .GetComponent<PMEAngelBehaviorScript>();
                angel.transform.rotation = Quaternion.LookRotation(new Vector3(angel.transform.position.x, 0, angel.transform.position.z) - new Vector3(transform.position.x, 0, transform.position.z));
                angel.transform.position += new Vector3(0, 160, 0) + Quaternion.AngleAxis((360 / numberOfSpawns) * i, Vector3.up) * transform.forward * Random.Range(100, 180);
                NetworkServer.Spawn(angel.gameObject);

                PMEDevilBehaviorScript devil = Instantiate(devilPrefabs[Random.Range(0, devilPrefabs.Length)],
                                                           angel.transform.position + angel.transform.forward * Random.Range(20, 40),
                                                           Quaternion.identity,
                                                           petMonsterCharacters)
                                                           .GetComponent<PMEDevilBehaviorScript>();
                devil.transform.rotation = Quaternion.LookRotation(angel.transform.position - devil.transform.position);
                NetworkServer.Spawn(devil.gameObject);

                angel.VerticalDistance = Random.Range(10, 40);
                angel.Direction = Random.Range(0, 2) == 0 ? -1 : 1;
                devil.Direction = Random.Range(0, 2) == 0 ? -1 : 1;
                angel.SetBehavior(PMECharacterBehaviorScript.PMECharacterBehavior.DevilSpiralAngel, devil, this);
                devil.SetBehavior(PMECharacterBehaviorScript.PMECharacterBehavior.DevilSpiralAngel, angel, this);
                SetAngelDevilPositionsRotationsClientRpc(angel, angel.transform.position, angel.transform.rotation, angel.Direction, angel.VerticalDistance,
                                                         devil, devil.transform.position, devil.transform.rotation, devil.Direction, numberOfSpawns, i);

                yield return wait;
            }
        }

        private IEnumerator Round2Coroutine(int numberOfSpawns)
        {
            eventTimerText.text = "";
            eventMessageLeft.text = "";

            while (petMonsterCharacters.childCount > 0)
            {
                yield return null;
            }

            CurrentPhase = PMEPhase.CollectHalos;
            currentTimer = roundTimers[1];
            numberOfSpawns = 20;
            spawnedHalos = numberOfSpawns;

            foreach ((NetworkConnectionToClient, string) n in participants)
            {
                SetRound2GUITargetRpc(n.Item1);
            }

            WaitForSeconds wait = new WaitForSeconds(0.5f);

            for (int i = 0; i < numberOfSpawns; i++)
            {
                Vector3 location = transform.position + new Vector3(0, Random.Range(120, 170), 0) + Quaternion.AngleAxis((360 / numberOfSpawns) * i, Vector3.up) * transform.forward * (i % 2 == 0 ? Random.Range(160, 190) : Random.Range(110, 140));
                PMEHaloBehaviorScript halo = Instantiate(haloPrefab, transform.position, Quaternion.identity, petMonsterCharacters).GetComponent<PMEHaloBehaviorScript>();
                halo.PetMonsterEvent = this;
                NetworkServer.Spawn(halo.gameObject, gameObject);
                SetGameObjectPositionClientRpc(halo.gameObject, transform.position);
                halo.Location = location;
                halo.MoveToLocation();
                HaloMoveToLocationClientRpc(halo);

                yield return wait;
            }
        }

        private IEnumerator EndingCoroutine(float waitTime)
        {
            endingMessage.gameObject.SetActive(true);

            yield return new WaitForSeconds(waitTime);

            endingMessage.gameObject.SetActive(false);
            eventPanel.SetActive(false);
            Destroy(gameObject);
        }

        private string UpdateLobbyMessage()
        {
            string result = "Participants:";

            for (int i = 0; i < participants.Count; i++)
            {
                result += "\n" + (i + 1) + ". " + participants[i].Item2;
            }

            return result;
        }

        private void StartRound1()
        {
            // Set all variables
            spiritsRescued = 0;
            halosCollected = 0;
            CurrentPhase = PMEPhase.ConvertDevils;
            currentTimer = roundTimers[0];
            eventMessageLeftPanel.SetActive(true);
            eventMessageTopPanel.SetActive(true);
            lobbyTimerStartingMessage.gameObject.SetActive(false);
            lobbyTimerText.gameObject.SetActive(false);
            lobbyMessage.gameObject.SetActive(false);
            eventTimerText.gameObject.SetActive(true);

            foreach ((NetworkConnectionToClient, string) n in participants)
            {
                SetRound1GUITargetRpc(n.Item1);
            }

            StartCoroutine(Round1Coroutine(numberOfSpawns));
        }
        #endregion

        #region Class Implementation - Public
        public void StartEvent(int numberOfSpawns)
        {
            this.numberOfSpawns = numberOfSpawns;
            EventZoneActiveClientRpc(true);
            eventPanel.SetActive(true);

            foreach ((NetworkConnection, string) n in participants)
            {
                SetLobbyGUITargetRpc(n.Item1);
            }

            CurrentPhase = PMEPhase.Lobby;
            currentTimer = lobbyTimer;
        }

        public void UpdateSpiritsRescued()
        {
            spiritsRescued++;
            eventMessageLeft.text = spiritsRescued + " spirits rescued";
        }

        public void UpdateHalosCollected()
        {
            halosCollected++;
            eventMessageLeft.text = halosCollected + " halos collected";
        }
        #endregion

        #region Network Commands
        [ClientRpc]
        private void SetAngelDevilPositionsRotationsClientRpc(PMEAngelBehaviorScript angel, Vector3 angelPosition, Quaternion angelRotation, int angelDirection, float angelVerticalDistance,
                                                              PMEDevilBehaviorScript devil, Vector3 devilPosition, Quaternion devilRotation, int devilDirection, int numberOfSpawns, int i)
        {
            angel.transform.position = angelPosition;
            angel.transform.rotation = angelRotation;
            devil.transform.position = devilPosition;
            devil.transform.rotation = devilRotation;
            angel.VerticalDistance = angelVerticalDistance;
            angel.Direction = angelDirection;
            devil.Direction = devilDirection;
            angel.SetBehavior(PMECharacterBehaviorScript.PMECharacterBehavior.DevilSpiralAngel, devil, this);
            devil.SetBehavior(PMECharacterBehaviorScript.PMECharacterBehavior.DevilSpiralAngel, angel, this);
        }


        [ClientRpc]
        private void SetGameObjectPositionClientRpc(GameObject go, Vector3 position)
        {
            go.transform.position = position;
        }

        [ClientRpc]
        private void EventZoneActiveClientRpc(bool active)
        {
            petMonsterEventZone.SetActive(active);
        }

        [ClientRpc]
        private void HaloMoveToLocationClientRpc(PMEHaloBehaviorScript halo)
        {
            halo.MoveToLocation();
        }

        [TargetRpc]
        private void UpdateRoundTimerTextTargetRpc(NetworkConnection n)
        {
            eventTimerText.text = TimerToString(currentTimer);
        }

        [TargetRpc]
        private void UpdateLobbyMessagesTargetRpc(NetworkConnection conn, string lobbyMessageText)
        {
            lobbyTimerText.text = TimerToString(currentTimer);
            lobbyMessage.text = lobbyMessageText;
        }

        [TargetRpc]
        private void SetLobbyGUITargetRpc(NetworkConnection conn)
        {
            eventPanel.SetActive(true);
        }

        [TargetRpc]
        private void UpdateStartingGUITargetRpc(NetworkConnection conn)
        {
            lobbyTimerText.text = TimerToString(currentTimer);
        }

        [TargetRpc]
        private void SetStartingMessageTargetRpc(NetworkConnection conn)
        {
            lobbyTimerStartingMessage.text = "Get ready to go!";
        }

        [TargetRpc]
        private void SetRound1GUITargetRpc(NetworkConnection conn)
        {
            eventMessageLeftPanel.SetActive(true);
            eventMessageTopPanel.SetActive(true);
            lobbyTimerStartingMessage.gameObject.SetActive(false);
            lobbyTimerText.gameObject.SetActive(false);
            lobbyMessage.gameObject.SetActive(false);
            eventTimerText.gameObject.SetActive(true);
            eventMessageTop.text = "Kind spirits are being bullied by naughty spirits!\nStop the bullies by touching them!";
            eventMessageTop.gameObject.SetActive(true);
            StartCoroutine(DisableAfterTime(eventMessageTop.gameObject, 4));
        }

        [TargetRpc]
        private void SetRound2GUITargetRpc(NetworkConnection conn)
        {
            eventMessageTop.text = "The spirits have left you halos as a reward!\nCollect as many as possible!";
            eventMessageLeft.text = halosCollected + " halos collected";
            eventMessageTop.gameObject.SetActive(true);
            StartCoroutine(DisableAfterTime(eventMessageTop.gameObject, 4));
        }

        [TargetRpc]
        private void SetEndingTargetRpc(NetworkConnection conn, float waitTime)
        {
            eventMessageLeftPanel.SetActive(false);
            eventTimerText.gameObject.SetActive(false);
            StartCoroutine(EndingCoroutine(waitTime));
        }

        [Command]
        private void JoinEventCmd(NetworkIdentity n, string name)
        {
            (NetworkConnectionToClient, string) t = (n.connectionToClient, name);
            participants.Add(t);
        }
        #endregion
    }
}

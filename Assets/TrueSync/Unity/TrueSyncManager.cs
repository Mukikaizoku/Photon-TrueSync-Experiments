using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace TrueSync {
    /**
     * @brief Manages creation of player prefabs and lockstep execution.
     **/
    [AddComponentMenu("")]
    public class TrueSyncManager : MonoBehaviour {

        private const float JitterTimeFactor = 0.001f;

        private const string serverSettingsAssetFile = "TrueSyncGlobalConfig";

        private enum StartState { BEHAVIOR_INITIALIZED, FIRST_UPDATE, STARTED };

        private StartState startState;

        /** 
         * @brief Player prefabs to be instantiated in each machine.
         **/
        public GameObject[] playerPrefabs;

        public static TrueSyncConfig _TrueSyncGlobalConfig;

        public static TrueSyncConfig TrueSyncGlobalConfig {
            get {
                if (_TrueSyncGlobalConfig == null) {
                    _TrueSyncGlobalConfig = (TrueSyncConfig) Resources.Load(serverSettingsAssetFile, typeof(TrueSyncConfig));
                }

                return _TrueSyncGlobalConfig;
            }
        }

        public static TrueSyncConfig TrueSyncCustomConfig = null;

        public TrueSyncConfig customConfig;

        private Dictionary<int, List<GameObject>> gameOjectsSafeMap = new Dictionary<int, List<GameObject>>();

        /**
         * @brief Instance of the lockstep engine.
         **/
        private AbstractLockstep lockstep;

        private FP lockedTimeStep;

        /**
         * @brief A list of {@link TrueSyncBehaviour} not linked to any player.
         **/
        private List<TrueSyncManagedBehaviour> generalBehaviours;

        /**
         * @brief A dictionary holding a list of {@link TrueSyncBehaviour} belonging to each player.
         **/
        private Dictionary<byte, List<TrueSyncManagedBehaviour>> behaviorsByPlayer;

        /**
         * @brief The coroutine scheduler.
         **/
        private CoroutineScheduler scheduler;

        /**
         * @brief List of {@link TrueSyncBehaviour} that should be included next update.
         **/
        private List<TrueSyncManagedBehaviour> queuedBehaviours = new List<TrueSyncManagedBehaviour>();

        private Dictionary<ITrueSyncBehaviour, TrueSyncManagedBehaviour> mapBehaviorToManagedBehavior = new Dictionary<ITrueSyncBehaviour, TrueSyncManagedBehaviour>();

        /**
         * @brief Returns the deltaTime between two simulation calls.
         **/
        public static FP DeltaTime {
            get {
                if (instance == null) {
                    return 0;
                }

                return instance.lockstep.deltaTime;
            }
        }

        /**
         * @brief Returns the time elapsed since the beginning of the simulation.
         **/
        public static FP Time {
            get {
                if (instance == null || instance.lockstep == null) {
                    return 0;
                }

                return instance.lockstep.time;
            }
        }

        /**
         * @brief Returns the number of the last simulated tick.
         **/
        public static int Ticks {
            get {
                if (instance == null || instance.lockstep == null) {
                    return 0;
                }

                return instance.lockstep.Ticks;
            }
        }

        /**
         * @brief Returns the last safe simulated tick.
         **/
        public static int LastSafeTick {
            get {
                if (instance == null || instance.lockstep == null) {
                    return 0;
                }

                return instance.lockstep.LastSafeTick;
            }
        }

        /** 
         * @brief Returns the simulated gravity.
         **/
        public static TSVector Gravity {
            get {
                if (instance == null) {
                    return TSVector.zero;
                }

                return instance.ActiveConfig.gravity3D;
            }
        }

        /** 
         * @brief Returns the list of players connected.
         **/
        public static List<TSPlayerInfo> Players {
            get {
                if (instance == null || instance.lockstep == null) {
                    return null;
                }

                List<TSPlayerInfo> allPlayers = new List<TSPlayerInfo>();
                foreach (TSPlayer tsp in instance.lockstep.Players.Values) {
                    if (!tsp.dropped) {
                        allPlayers.Add(tsp.playerInfo);
                    }
                }

                return allPlayers;
            }
        }

        /** 
         * @brief Returns the local player.
         **/
        public static TSPlayerInfo LocalPlayer {
            get {
                if (instance == null || instance.lockstep == null) {
                    return null;
                }

                return instance.lockstep.LocalPlayer.playerInfo;
            }
        }

        /** 
         * @brief Returns the active {@link TrueSyncConfig} used by the {@link TrueSyncManager}.
         **/
        public static TrueSyncConfig Config {
            get {
                if (instance == null) {
                    return null;
                }

                return instance.ActiveConfig;
            }
        }

        private static TrueSyncManager instance;

        private TrueSyncConfig ActiveConfig {
            get {
                if (TrueSyncCustomConfig != null) {
                    customConfig = TrueSyncCustomConfig;
                    TrueSyncCustomConfig = null;
                }

                if (customConfig != null) {
                    return customConfig;
                }

                return TrueSyncGlobalConfig;
            }
        }

        void Awake() {
            TrueSyncConfig currentConfig = ActiveConfig;
            lockedTimeStep = currentConfig.lockedTimeStep;

            StateTracker.Init();

            if (currentConfig.physics2DEnabled || currentConfig.physics3DEnabled) {
                PhysicsManager.New(currentConfig);
                PhysicsManager.instance.LockedTimeStep = lockedTimeStep;
                PhysicsManager.instance.Init();
            }
        }

        void Start() {
            instance = this;
            Application.runInBackground = true;

            if (ReplayRecord.replayMode == ReplayMode.LOAD_REPLAY) {
                ReplayRecord replayRecord = ReplayRecord.replayToLoad;
                if (replayRecord == null) {
                    Debug.LogError("Replay Record can't be loaded");
                    gameObject.SetActive(false);
                    return;
                }
            }

            bool isOfflineMode = false;         // MIK EDIT
            ICommunicator communicator = null;
            if (!PhotonNetwork.connected || !PhotonNetwork.inRoom) {
                Debug.LogWarning("You are not connected to Photon. TrueSync will start in offline mode.");
                isOfflineMode = true;           // MIK EDIT
            } else {
                communicator = new PhotonTrueSyncCommunicator(PhotonNetwork.networkingPeer);
            }

            TrueSyncConfig activeConfig = ActiveConfig;

            lockstep = AbstractLockstep.NewInstance(
                lockedTimeStep,
                communicator,
                PhysicsManager.instance,
                activeConfig.syncWindow,
                activeConfig.panicWindow,
                activeConfig.rollbackWindow,
                OnGameStarted,
                OnGamePaused,
                OnGameUnPaused,
                OnGameEnded,
                OnPlayerDisconnection,
                OnStepUpdate,
                GetLocalData
            );

            if (activeConfig.showStats) {
                this.gameObject.AddComponent<TrueSyncStats>().Lockstep = lockstep;
            }

            scheduler = new CoroutineScheduler(lockstep);

            // ORIGINAL SECTION from PHOTON TRUESYNC
            /*
            if (ReplayRecord.replayMode != ReplayMode.LOAD_REPLAY) {
                if (communicator == null) {
                    lockstep.AddPlayer(0, "Local_Player", true);
                } else {
                    List<PhotonPlayer> players = new List<PhotonPlayer>(PhotonNetwork.playerList);
                    players.Sort(UnityUtils.playerComparer);

                    foreach (PhotonPlayer p in players) {
                        lockstep.AddPlayer((byte)p.ID, p.NickName, p.IsLocal);
                    }
                }
            }
             */

            //#####################################################
            //          Region modified by Mik
            //#####################################################

            if (ReplayRecord.replayMode != ReplayMode.LOAD_REPLAY)
            {
                if (communicator == null)
                {
                    Debug.Log("Offline: Adding local players.");
                    lockstep.AddPlayer(0, "Local_Player", true);
                    //lockstep.AddPlayer(1, "Computer", true);
                }
                else
                {
                    List<PhotonPlayer> players = new List<PhotonPlayer>(PhotonNetwork.playerList);
                    players.Sort(UnityUtils.playerComparer);

                    foreach (PhotonPlayer p in players)
                    {
                        lockstep.AddPlayer((byte)p.ID, p.NickName, p.IsLocal);
                    }
                }
            }

            //#####################################################
            //          Region end
            //#####################################################
            Debug.Log("Total number of players: " + lockstep.Players.Count);   // MIK EDIT 


            generalBehaviours = new List<TrueSyncManagedBehaviour>();
            foreach (TrueSyncBehaviour tsb in FindObjectsOfType<TrueSyncBehaviour>()) {
                generalBehaviours.Add(NewManagedBehavior(tsb));
            }

            initBehaviors(isOfflineMode);                       // MIK EDIT
            initGeneralBehaviors(generalBehaviours, false);

            PhysicsManager.instance.OnRemoveBody(OnRemovedRigidBody);

            startState = StartState.BEHAVIOR_INITIALIZED;
        }

        private TrueSyncManagedBehaviour NewManagedBehavior(ITrueSyncBehaviour trueSyncBehavior) {
            TrueSyncManagedBehaviour result = new TrueSyncManagedBehaviour(trueSyncBehavior);
            mapBehaviorToManagedBehavior[trueSyncBehavior] = result;

            return result;
        }

        private void initBehaviors(bool offlineMode) {           // MIK EDIT
            behaviorsByPlayer = new Dictionary<byte, List<TrueSyncManagedBehaviour>>();

            foreach (TSPlayer p in lockstep.Players.Values) {
                List<TrueSyncManagedBehaviour> behaviorsInstatiated = new List<TrueSyncManagedBehaviour>();

                foreach (GameObject prefab in playerPrefabs) {
                    GameObject prefabInst = Instantiate(prefab);
                    InitializeGameObject(prefabInst, prefabInst.transform.position.ToTSVector(), prefabInst.transform.rotation.ToTSQuaternion());

                    TrueSyncBehaviour[] behaviours = prefabInst.GetComponentsInChildren<TrueSyncBehaviour>();
                    foreach (TrueSyncBehaviour behaviour in behaviours) {
                        behaviour.owner = p.playerInfo;
                        behaviour.localOwner = lockstep.LocalPlayer.playerInfo;
                        behaviour.numberOfPlayers = lockstep.Players.Count;
                        if (offlineMode)                                                        // MIK EDIT
                        {
                            //GameSyncManager gameSyncManger = (GameSyncManager)behaviour;        // MIK EDIT
                            //gameSyncManger.isOfflineMode = true;                                // MIK EDIT
                        } 

                        behaviorsInstatiated.Add(NewManagedBehavior(behaviour));
                    }
                }

                behaviorsByPlayer.Add(p.ID, behaviorsInstatiated);
            }
        }

        private void initGeneralBehaviors(IEnumerable<TrueSyncManagedBehaviour> behaviours, bool realOwnerId) {
            List<TSPlayer> playersList = new List<TSPlayer>(lockstep.Players.Values);
            List<TrueSyncManagedBehaviour> itemsToRemove = new List<TrueSyncManagedBehaviour>();

            foreach (TrueSyncManagedBehaviour tsmb in behaviours) {
                if (!(tsmb.trueSyncBehavior is TrueSyncBehaviour)) {
                    continue;
                }

                TrueSyncBehaviour bh = (TrueSyncBehaviour)tsmb.trueSyncBehavior;

                if (realOwnerId) {
                    bh.ownerIndex = bh.owner.Id;
                } else {
                    if (bh.ownerIndex >= 0 && bh.ownerIndex < playersList.Count) {
                        bh.ownerIndex = playersList[bh.ownerIndex].ID;
                    }
                }

                if (behaviorsByPlayer.ContainsKey((byte)bh.ownerIndex)) {
                    bh.owner = lockstep.Players[(byte)bh.ownerIndex].playerInfo;

                    behaviorsByPlayer[(byte)bh.ownerIndex].Add(tsmb);
                    itemsToRemove.Add(tsmb);
                } else {
                    bh.ownerIndex = -1;
                }

                bh.localOwner = lockstep.LocalPlayer.playerInfo;
                bh.numberOfPlayers = lockstep.Players.Count;
            }

            foreach (TrueSyncManagedBehaviour bh in itemsToRemove) {
                generalBehaviours.Remove(bh);
            }
        }

        private void CheckQueuedBehaviours() {
            if (queuedBehaviours.Count > 0) {
                generalBehaviours.AddRange(queuedBehaviours);
                initGeneralBehaviors(queuedBehaviours, true);

                foreach (TrueSyncManagedBehaviour tsmb in queuedBehaviours) {
                    tsmb.SetGameInfo(lockstep.LocalPlayer.playerInfo, lockstep.Players.Count);
                    tsmb.OnSyncedStart();
                }

                queuedBehaviours.Clear();
            }
        }

        void Update() {
            if (lockstep != null && startState != StartState.STARTED) {
                if (startState == StartState.BEHAVIOR_INITIALIZED) {
                    startState = StartState.FIRST_UPDATE;
                } else if (startState == StartState.FIRST_UPDATE) {
                    lockstep.RunSimulation(true);
                    startState = StartState.STARTED;
                }
            }
        }

        /**
         * @brief Run/Unpause the game simulation.
         **/
        public static void RunSimulation() {
            if (instance != null && instance.lockstep != null) {
                instance.lockstep.RunSimulation(false);
            }
        }

        /**
         * @brief Pauses the game simulation.
         **/
        public static void PauseSimulation() {
            if (instance != null && instance.lockstep != null) {
                instance.lockstep.PauseSimulation();
            }
        }

        /**
         * @brief End the game simulation.
         **/
        public static void EndSimulation() {
            if (instance != null && instance.lockstep != null) {
                instance.lockstep.EndSimulation();
            }
        }

        /**
         * @brief Update all coroutines created.
         **/
        public static void UpdateCoroutines() {
            if (instance != null && instance.lockstep != null) {
                instance.scheduler.UpdateAllCoroutines();
            }
        }

        /**
         * @brief Starts a new coroutine.
         * 
         * @param coroutine An IEnumerator that represents the coroutine.
         **/
        public static void SyncedStartCoroutine(IEnumerator coroutine) {
            if (instance != null && instance.lockstep != null) {
                instance.scheduler.StartCoroutine(coroutine);
            }
        }

        /**
         * @brief Instantiate a new prefab in a deterministic way.
         * 
         * @param prefab GameObject's prefab to instantiate.
         **/
        public static GameObject SyncedInstantiate(GameObject prefab) {
            return SyncedInstantiate(prefab, prefab.transform.position.ToTSVector(), prefab.transform.rotation.ToTSQuaternion());
        }

        /**
         * @brief Instantiates a new prefab in a deterministic way.
         * 
         * @param prefab GameObject's prefab to instantiate.
         * @param position Position to place the new GameObject.
         * @param rotation Rotation to set in the new GameObject.
         **/
        public static GameObject SyncedInstantiate(GameObject prefab, TSVector position, TSQuaternion rotation) {
            if (instance != null && instance.lockstep != null) {
                GameObject go = GameObject.Instantiate(prefab, position.ToVector(), rotation.ToQuaternion()) as GameObject;
                AddGameObjectOnSafeMap(go);

                foreach (MonoBehaviour bh in go.GetComponentsInChildren<MonoBehaviour>()) {
                    if (bh is ITrueSyncBehaviour) {
                        instance.queuedBehaviours.Add(instance.NewManagedBehavior((ITrueSyncBehaviour)bh));
                    }
                }

                InitializeGameObject(go, position, rotation);

                return go;
            }

            return null;
        }

        private static void AddGameObjectOnSafeMap(GameObject go) {
            Dictionary<int, List<GameObject>> safeMap = instance.gameOjectsSafeMap;

            int currentTick = TrueSyncManager.Ticks + 1;
            if (!safeMap.ContainsKey(currentTick)) {
                safeMap.Add(currentTick, new List<GameObject>());
            }

            safeMap[currentTick].Add(go);
        }

        private static void CheckGameObjectsSafeMap() {
            Dictionary<int, List<GameObject>> safeMap = instance.gameOjectsSafeMap;

            int currentTick = TrueSyncManager.Ticks + 1;
            if (safeMap.ContainsKey(currentTick)) {
                List<GameObject> gos = safeMap[currentTick];
                for (int i = 0, l = gos.Count; i < l; i++) {
                    GameObject go = gos[i];
                    if (go != null) {
                        Renderer rend = go.GetComponent<Renderer>();
                        if (rend != null) {
                            rend.enabled = false;
                        }

                        GameObject.Destroy(go);
                    }
                }

                gos.Clear();
            }

            safeMap.Remove(TrueSyncManager.LastSafeTick);
        }

        private static void InitializeGameObject(GameObject go, TSVector position, TSQuaternion rotation) {
            ICollider[] tsColliders = go.GetComponentsInChildren<ICollider>();
            if (tsColliders != null) {
                foreach (ICollider tsCollider in tsColliders) {
                    PhysicsManager.instance.AddBody(tsCollider);
                }
            }

            TSTransform rootTSTransform = go.GetComponent<TSTransform>();
            if (rootTSTransform != null) {
                rootTSTransform.Initialize();

                rootTSTransform.position = position;
                rootTSTransform.rotation = rotation;
            }

            TSTransform[] tsTransforms = go.GetComponentsInChildren<TSTransform>();
            if (tsTransforms != null) {
                foreach (TSTransform tsTransform in tsTransforms) {
                    if (tsTransform != rootTSTransform) {
                        tsTransform.Initialize();
                    }
                }
            }

            TSTransform2D rootTSTransform2D = go.GetComponent<TSTransform2D>();
            if (rootTSTransform2D != null) {
                rootTSTransform2D.Initialize();

                rootTSTransform2D.position = new TSVector2(position.x, position.y);
                rootTSTransform2D.rotation = rotation.ToQuaternion().eulerAngles.z;
            }

            TSTransform2D[] tsTransforms2D = go.GetComponentsInChildren<TSTransform2D>();
            if (tsTransforms2D != null) {
                foreach (TSTransform2D tsTransform2D in tsTransforms2D) {
                    if (tsTransform2D != rootTSTransform2D) {
                        tsTransform2D.Initialize();
                    }
                }
            }
        }

        /**
         * @brief Instantiates a new prefab in a deterministic way.
         * 
         * @param prefab GameObject's prefab to instantiate.
         * @param position Position to place the new GameObject.
         * @param rotation Rotation to set in the new GameObject.
         **/
        public static GameObject SyncedInstantiate(GameObject prefab, TSVector2 position, TSQuaternion rotation) {
            return SyncedInstantiate(prefab, new TSVector(position.x, position.y, 0), rotation);
        }

        /**
         * @brief Destroys a GameObject in a deterministic way.
         * 
         * The method {@link #DestroyTSRigidBody} is called and attached TrueSyncBehaviors are disabled.
         * 
         * @param rigidBody Instance of a {@link TSRigidBody}
         **/
        public static void SyncedDestroy(GameObject gameObject) {
            if (instance != null && instance.lockstep != null) {
                SyncedDisableBehaviour(gameObject);

                TSCollider[] tsColliders = gameObject.GetComponentsInChildren<TSCollider>();
                if (tsColliders != null) {
                    foreach (TSCollider tsCollider in tsColliders) {
                        DestroyTSRigidBody(tsCollider.gameObject, tsCollider.Body);
                    }
                }

                TSCollider2D[] tsColliders2D = gameObject.GetComponentsInChildren<TSCollider2D>();
                if (tsColliders2D != null) {
                    foreach (TSCollider2D tsCollider2D in tsColliders2D) {
                        DestroyTSRigidBody(tsCollider2D.gameObject, tsCollider2D.Body);
                    }
                }
            }
        }

        /**
         * @brief Disables 'OnSyncedInput' and 'OnSyncUpdate' calls to every {@link ITrueSyncBehaviour} attached.
         **/
        public static void SyncedDisableBehaviour(GameObject gameObject) {
            foreach (MonoBehaviour tsb in gameObject.GetComponentsInChildren<MonoBehaviour>()) {
                if (tsb is ITrueSyncBehaviour && instance.mapBehaviorToManagedBehavior.ContainsKey((ITrueSyncBehaviour)tsb)) {
                    instance.mapBehaviorToManagedBehavior[(ITrueSyncBehaviour)tsb].disabled = true;
                }
            }
        }

        /**
         * @brief The related GameObject is firstly set to be inactive then in a safe moment it will be destroyed.
         * 
         * @param rigidBody Instance of a {@link TSRigidBody}
         **/
        private static void DestroyTSRigidBody(GameObject tsColliderGO, IBody body) {
            tsColliderGO.gameObject.SetActive(false);
            instance.lockstep.Destroy(body);
        }

        /**
         * @brief Registers an implementation of {@link ITrueSyncBehaviour} to be included in the simulation.
         * 
         * @param trueSyncBehaviour Instance of an {@link ITrueSyncBehaviour}
         **/
        public static void RegisterITrueSyncBehaviour(ITrueSyncBehaviour trueSyncBehaviour) {
            if (instance != null && instance.lockstep != null) {
                instance.queuedBehaviours.Add(instance.NewManagedBehavior(trueSyncBehaviour));
            }
        }

        /**
         * @brief Register a {@link TrueSyncIsReady} delegate to that returns true if the game can proceed or false otherwise.
         * 
         * @param IsReadyChecker A {@link TrueSyncIsReady} delegate
         **/
        public static void RegisterIsReadyChecker(TrueSyncIsReady IsReadyChecker) {
            if (instance != null && instance.lockstep != null) {
                instance.lockstep.GameIsReady += IsReadyChecker;
            }
        }

        /**
         * @brief Removes objets related to a provided player.
         * 
         * @param playerId Target player's id.
         **/
        public static void RemovePlayer(int playerId) {
            if (instance != null && instance.lockstep != null) {
                foreach (TrueSyncManagedBehaviour tsmb in instance.behaviorsByPlayer[(byte)playerId]) {
                    tsmb.disabled = true;

                    TSCollider[] tsColliders = ((TrueSyncBehaviour)tsmb.trueSyncBehavior).gameObject.GetComponentsInChildren<TSCollider>();
                    if (tsColliders != null) {
                        foreach (TSCollider tsCollider in tsColliders) {
                            if (!tsCollider.Body.TSDisabled) {
                                DestroyTSRigidBody(tsCollider.gameObject, tsCollider.Body);
                            }
                        }
                    }

                    TSCollider2D[] tsCollider2Ds = ((TrueSyncBehaviour)tsmb.trueSyncBehavior).gameObject.GetComponentsInChildren<TSCollider2D>();
                    if (tsCollider2Ds != null) {
                        foreach (TSCollider2D tsCollider2D in tsCollider2Ds) {
                            if (!tsCollider2D.Body.TSDisabled) {
                                DestroyTSRigidBody(tsCollider2D.gameObject, tsCollider2D.Body);
                            }
                        }
                    }
                }
            }
        }

        private FP tsDeltaTime = 0;

        void FixedUpdate() {
            if (lockstep != null) {
                tsDeltaTime += UnityEngine.Time.deltaTime;

                if (tsDeltaTime >= (lockedTimeStep - JitterTimeFactor)) {
                    tsDeltaTime = 0;

                    instance.scheduler.UpdateAllCoroutines();
                    lockstep.Update();
                }
            }
        }

        void GetLocalData(InputData playerInputData) {
            TrueSyncInput.CurrentInputData = playerInputData;

            if (behaviorsByPlayer.ContainsKey(playerInputData.ownerID)) {
                foreach (TrueSyncManagedBehaviour bh in behaviorsByPlayer[playerInputData.ownerID]) {
                    if (bh != null && !bh.disabled) {
                        bh.OnSyncedInput();
                    }
                }
            }

            TrueSyncInput.CurrentInputData = null;
        }

        void OnStepUpdate(InputData[] allInputData) {
            CheckGameObjectsSafeMap();

            TrueSyncInput.GetAllInputs().Clear();

            if (generalBehaviours != null) {
                foreach (TrueSyncManagedBehaviour bh in generalBehaviours) {
                    if (bh != null && !bh.disabled) {
                        bh.OnPreSyncedUpdate();
                        instance.scheduler.UpdateAllCoroutines();
                    }
                }
            }

            foreach (InputData playerInputData in allInputData) {
                if (behaviorsByPlayer.ContainsKey(playerInputData.ownerID)) {
                    foreach (TrueSyncManagedBehaviour bh in behaviorsByPlayer[playerInputData.ownerID]) {
                        if (bh != null && !bh.disabled) {
                            bh.OnPreSyncedUpdate();
                            instance.scheduler.UpdateAllCoroutines();
                        }
                    }
                }
            }

            TrueSyncInput.GetAllInputs().AddRange(allInputData);

            TrueSyncInput.CurrentSimulationData = null;
            if (generalBehaviours != null) {
                foreach (TrueSyncManagedBehaviour bh in generalBehaviours) {
                    if (bh != null && !bh.disabled) {
                        bh.OnSyncedUpdate();
                        instance.scheduler.UpdateAllCoroutines();
                    }
                }
            }

            foreach (InputData playerInputData in allInputData) {
                if (behaviorsByPlayer.ContainsKey(playerInputData.ownerID)) {
                    TrueSyncInput.CurrentSimulationData = playerInputData;

                    foreach (TrueSyncManagedBehaviour bh in behaviorsByPlayer[playerInputData.ownerID]) {
                        if (bh != null && !bh.disabled) {
                            bh.OnSyncedUpdate();
                            instance.scheduler.UpdateAllCoroutines();
                        }
                    }
                }

                TrueSyncInput.CurrentSimulationData = null;
            }

            CheckQueuedBehaviours();
        }

        private void OnRemovedRigidBody(IBody body) {
            GameObject go = PhysicsManager.instance.GetGameObject(body);

            if (go != null) {
                List<TrueSyncBehaviour> behavioursToRemove = new List<TrueSyncBehaviour>(go.GetComponentsInChildren<TrueSyncBehaviour>());
                RemoveFromTSMBList(queuedBehaviours, behavioursToRemove);
                RemoveFromTSMBList(generalBehaviours, behavioursToRemove);

                foreach (List<TrueSyncManagedBehaviour> listBh in behaviorsByPlayer.Values) {
                    RemoveFromTSMBList(listBh, behavioursToRemove);
                }
            }
        }

        private void RemoveFromTSMBList(List<TrueSyncManagedBehaviour> tsmbList, List<TrueSyncBehaviour> behaviours) {
            List<TrueSyncManagedBehaviour> toRemove = new List<TrueSyncManagedBehaviour>();
            foreach (TrueSyncManagedBehaviour tsmb in tsmbList) {
                if ((tsmb.trueSyncBehavior is TrueSyncBehaviour) && behaviours.Contains((TrueSyncBehaviour)tsmb.trueSyncBehavior)) {
                    toRemove.Add(tsmb);
                }
            }

            foreach (TrueSyncManagedBehaviour tsmb in toRemove) {
                tsmbList.Remove(tsmb);
            }
        }

        void OnPlayerDisconnection(byte playerId) {
            GenericOnGameCall("TrueSyncManagedBehaviour.OnPlayerDisconnection", new object[] { (int)playerId });
        }

        void OnGameStarted() {
            GenericOnGameCall("TrueSyncManagedBehaviour.OnSyncedStart");
            CheckQueuedBehaviours();
        }

        void OnGamePaused() {
            GenericOnGameCall("TrueSyncManagedBehaviour.OnGamePaused");
        }

        void OnGameUnPaused() {
            GenericOnGameCall("TrueSyncManagedBehaviour.OnGameUnPaused");
        }

        void OnGameEnded() {
            GenericOnGameCall("TrueSyncManagedBehaviour.OnGameEnded");
        }

        void GenericOnGameCall(string callbackName) {
            GenericOnGameCall(callbackName, null);
        }

        void GenericOnGameCall(string callbackName, object[] parameter) {
            if (generalBehaviours != null) {
                foreach (TrueSyncManagedBehaviour bh in generalBehaviours) {
                    UnityUtils.methodInfoByName[callbackName].Invoke(bh, parameter);
                    instance.scheduler.UpdateAllCoroutines();
                }
            }

            foreach (List<TrueSyncManagedBehaviour> behaviors in behaviorsByPlayer.Values) {
                foreach (TrueSyncManagedBehaviour bh in behaviors) {
                    UnityUtils.methodInfoByName[callbackName].Invoke(bh, parameter);
                    instance.scheduler.UpdateAllCoroutines();
                }
            }
        }

        void OnApplicationQuit() {
            EndSimulation();
        }

    }

}
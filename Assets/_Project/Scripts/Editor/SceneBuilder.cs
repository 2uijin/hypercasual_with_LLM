#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using PrisonLife.Config;
using PrisonLife.Core;
using PrisonLife.Desk;
using PrisonLife.Factory;
using PrisonLife.Items;
using PrisonLife.Jail;
using PrisonLife.NPC;
using PrisonLife.Player;
using PrisonLife.Quarry;
using PrisonLife.UI;
using PrisonLife.Upgrades;
using PrisonLife.Zones;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PrisonLife.EditorTools
{
    public static class SceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/GameConfig.asset";
        private const string RockPrefabPath = "Assets/_Project/Prefabs/Rock.prefab";
        private const string WorkerPrefabPath = "Assets/_Project/Prefabs/Worker.prefab";
        private const string CivilianPrefabPath = "Assets/_Project/Prefabs/Civilian.prefab";
        private const string PolicePrefabPath = "Assets/_Project/Prefabs/Police.prefab";

        // Materials shared across build.
        private static Material _matGround, _matFence, _matMachine, _matDesk, _matJail, _matRock, _matBuyPad;

        [MenuItem("PrisonLife/Build Scene")]
        public static void BuildMainScene()
        {
            EnsureTags();
            EnsureFolders();
            EnsureMaterials();
            var config = EnsureConfig();

            // Create prefabs first.
            var rockPrefab = EnsureRockPrefab();
            var civilianPrefab = EnsureCivilianPrefab();
            var workerPrefab = EnsureWorkerPrefab();
            var policePrefab = EnsurePolicePrefab();

            // New scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Lighting.
            var lightGo = new GameObject("DirectionalLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ground.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, 0, 0);
            ground.transform.localScale = new Vector3(8f, 1f, 8f); // 80x80m
            SetMaterial(ground, _matGround);

            // Root holders.
            var world = new GameObject("World").transform;
            var systems = new GameObject("Systems").transform;
            var uiRoot = new GameObject("UI").transform;

            // GameManager.
            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(systems, false);
            var gm = gmGo.AddComponent<GameManager>();
            SerializedObject so = new SerializedObject(gm);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("startingCash").intValue = 0;
            so.ApplyModifiedProperties();

            // ---- Build areas ----
            var quarryRoot = new GameObject("Quarry").transform; quarryRoot.SetParent(world, false);
            var factoryRoot = new GameObject("Factory").transform; factoryRoot.SetParent(world, false);
            var deskRoot = new GameObject("Desk").transform; deskRoot.SetParent(world, false);
            var jailRoot = new GameObject("Jail").transform; jailRoot.SetParent(world, false);
            var buyPadsRoot = new GameObject("BuyPads").transform; buyPadsRoot.SetParent(world, false);

            // Quarry at -18, 0, 0 — widened to fit 8x16 grid
            quarryRoot.position = new Vector3(-18f, 0f, 0f);
            var quarryFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            quarryFloor.name = "QuarryFloor";
            quarryFloor.transform.SetParent(quarryRoot, false);
            quarryFloor.transform.localScale = new Vector3(14f, 0.2f, 24f);
            quarryFloor.transform.localPosition = new Vector3(0, -0.1f, 0);
            SetMaterial(quarryFloor, MakeMat(new Color(0.85f, 0.75f, 0.45f)));

            // Quarry zone trigger (detects player for vehicle mode + worker handoff).
            var quarryZoneGo = new GameObject("QuarryZone");
            quarryZoneGo.transform.SetParent(quarryRoot, false);
            quarryZoneGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var quarryZoneCol = quarryZoneGo.AddComponent<BoxCollider>();
            quarryZoneCol.isTrigger = true;
            quarryZoneCol.size = new Vector3(14f, 3f, 24f);
            var quarryZone = quarryZoneGo.AddComponent<QuarryZone>();

            var spawnerGo = new GameObject("RockSpawner");
            spawnerGo.transform.SetParent(quarryRoot, false);
            spawnerGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var quarryMarker = spawnerGo.AddComponent<ZoneMarker>();
            quarryMarker.Configure(ZoneMarker.Kind.Quarry, "[채석장] 바위 채굴", null, new Vector2(12f, 22f));
            var spawner = spawnerGo.AddComponent<RockSpawner>();
            var ss = new SerializedObject(spawner);
            ss.FindProperty("rockPrefab").objectReferenceValue = rockPrefab.GetComponent<Rock>();
            ss.FindProperty("grid").vector2IntValue = new Vector2Int(8, 16);
            ss.FindProperty("spacing").vector2Value = new Vector2(1.4f, 1.4f);
            ss.ApplyModifiedProperties();

            // Factory at 0, 0, 0
            factoryRoot.position = new Vector3(0f, 0f, 0f);
            var machineBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            machineBody.name = "MachineBody";
            machineBody.transform.SetParent(factoryRoot, false);
            machineBody.transform.localScale = new Vector3(3f, 2f, 3f);
            machineBody.transform.localPosition = new Vector3(0f, 1f, 0f);
            SetMaterial(machineBody, _matMachine);

            // Conveyor.
            var conveyor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            conveyor.name = "Conveyor";
            conveyor.transform.SetParent(factoryRoot, false);
            conveyor.transform.localScale = new Vector3(2f, 0.2f, 1f);
            conveyor.transform.localPosition = new Vector3(2.5f, 0.5f, 0f);
            SetMaterial(conveyor, MakeMat(new Color(0.3f, 0.3f, 0.3f)));

            // Machine deposit zone — component sits on the trigger go so OnTrigger* fires.
            var depositZoneGo = new GameObject("ProcessingMachine");
            depositZoneGo.transform.SetParent(factoryRoot, false);
            depositZoneGo.transform.localPosition = new Vector3(-2.2f, 0.5f, 0f);
            var depositCol = depositZoneGo.AddComponent<BoxCollider>();
            depositCol.isTrigger = true;
            depositCol.size = new Vector3(2f, 2f, 2f);
            var machine = depositZoneGo.AddComponent<ProcessingMachine>();
            var machineMarker = depositZoneGo.AddComponent<ZoneMarker>();
            machineMarker.Configure(ZoneMarker.Kind.Deposit, "[기계] 바위 투입", depositCol);

            // Shelf for handcuffs.
            var shelfGo = new GameObject("HandcuffShelf");
            shelfGo.transform.SetParent(factoryRoot, false);
            shelfGo.transform.localPosition = new Vector3(4.2f, 0.7f, 0f);
            var shelfPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelfPlatform.transform.SetParent(shelfGo.transform, false);
            shelfPlatform.transform.localScale = new Vector3(1.2f, 0.15f, 1f);
            shelfPlatform.transform.localPosition = Vector3.zero;
            SetMaterial(shelfPlatform, MakeMat(new Color(0.5f, 0.35f, 0.2f)));
            var stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(shelfGo.transform, false);
            stackRoot.localPosition = new Vector3(0f, 0.1f, 0f);

            var shelf = shelfGo.AddComponent<HandcuffShelf>();
            var shelfSo = new SerializedObject(shelf);
            shelfSo.FindProperty("stackRoot").objectReferenceValue = stackRoot;
            shelfSo.ApplyModifiedProperties();

            // Pickup zone around shelf.
            var pickupGo = new GameObject("HandcuffPickupZone");
            pickupGo.transform.SetParent(factoryRoot, false);
            pickupGo.transform.localPosition = new Vector3(4.2f, 0.5f, 1.2f);
            var pickupCol = pickupGo.AddComponent<BoxCollider>();
            pickupCol.isTrigger = true;
            pickupCol.size = new Vector3(1.5f, 2f, 1.5f);
            var pickup = pickupGo.AddComponent<HandcuffPickupZone>();
            var pSo = new SerializedObject(pickup);
            pSo.FindProperty("shelf").objectReferenceValue = shelf;
            pSo.ApplyModifiedProperties();
            var pickupMarker = pickupGo.AddComponent<ZoneMarker>();
            pickupMarker.Configure(ZoneMarker.Kind.Pickup, "[선반] 수갑 픽업", pickupCol);

            var mSo = new SerializedObject(machine);
            mSo.FindProperty("depositZone").objectReferenceValue = depositCol;
            mSo.FindProperty("shelf").objectReferenceValue = shelf;
            mSo.ApplyModifiedProperties();

            // Desk at +10
            deskRoot.position = new Vector3(10f, 0f, 0f);
            var deskBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deskBody.name = "DeskBody";
            deskBody.transform.SetParent(deskRoot, false);
            deskBody.transform.localScale = new Vector3(2.5f, 0.15f, 1.2f);
            deskBody.transform.localPosition = new Vector3(0, 0.9f, 0);
            SetMaterial(deskBody, _matDesk);

            var deskHandcuffRoot = new GameObject("DeskHandcuffStack").transform;
            deskHandcuffRoot.SetParent(deskRoot, false);
            deskHandcuffRoot.localPosition = new Vector3(-0.6f, 1.0f, 0);
            var deskCashRoot = new GameObject("DeskCashStack").transform;
            deskCashRoot.SetParent(deskRoot, false);
            deskCashRoot.localPosition = new Vector3(2.0f, 0.2f, 0);

            var deskGo = new GameObject("ProcessingDesk");
            deskGo.transform.SetParent(deskRoot, false);
            var deskCol = deskGo.AddComponent<BoxCollider>();
            deskCol.isTrigger = true;
            deskCol.size = new Vector3(3f, 2f, 3f);
            deskCol.center = new Vector3(0, 1f, 1.5f);
            var desk = deskGo.AddComponent<ProcessingDesk>();
            var dSo = new SerializedObject(desk);
            dSo.FindProperty("depositZone").objectReferenceValue = deskCol;
            dSo.FindProperty("handcuffStackRoot").objectReferenceValue = deskHandcuffRoot;
            dSo.FindProperty("cashStackRoot").objectReferenceValue = deskCashRoot;
            dSo.ApplyModifiedProperties();
            var deskMarker = deskGo.AddComponent<ZoneMarker>();
            deskMarker.Configure(ZoneMarker.Kind.Deposit, "[데스크] 수갑 투입", deskCol);

            // Cash pickup zone beside desk.
            var cashPickGo = new GameObject("CashPickupZone");
            cashPickGo.transform.SetParent(deskRoot, false);
            cashPickGo.transform.localPosition = new Vector3(2.0f, 0.2f, 0);
            var cashCol = cashPickGo.AddComponent<BoxCollider>();
            cashCol.isTrigger = true;
            cashCol.size = new Vector3(1.5f, 2f, 1.5f);
            var cashPick = cashPickGo.AddComponent<CashPickupZone>();
            var cpSo = new SerializedObject(cashPick);
            cpSo.FindProperty("desk").objectReferenceValue = desk;
            cpSo.ApplyModifiedProperties();
            var cashMarker = cashPickGo.AddComponent<ZoneMarker>();
            cashMarker.Configure(ZoneMarker.Kind.Pickup, "[현금] 픽업", cashCol);

            // Civilian queue slots.
            var spawnPt = new GameObject("CivSpawn").transform; spawnPt.SetParent(deskRoot, false);
            spawnPt.localPosition = new Vector3(0, 0, 8f);
            var exitPt = new GameObject("CivExit").transform; exitPt.SetParent(deskRoot, false);
            exitPt.localPosition = new Vector3(0, 0, -2f);
            var exitPt2 = new GameObject("CivExit2").transform; exitPt2.SetParent(deskRoot, false);
            exitPt2.localPosition = new Vector3(0, 0, -5f);
            var processPt = new GameObject("ProcessPoint").transform; processPt.SetParent(deskRoot, false);
            processPt.localPosition = new Vector3(0, 0, 1.5f);

            var queueSlots = new Transform[6];
            for (int i = 0; i < queueSlots.Length; i++)
            {
                var s = new GameObject($"QueueSlot_{i}").transform;
                s.SetParent(deskRoot, false);
                s.localPosition = new Vector3(0, 0, 1.5f + (i + 1) * 1.2f);
                queueSlots[i] = s;
            }

            // Jail at +10, 0, -10
            jailRoot.position = new Vector3(10f, 0f, -12f);
            var jailFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            jailFloor.name = "JailFloor";
            jailFloor.transform.SetParent(jailRoot, false);
            jailFloor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            jailFloor.transform.localPosition = new Vector3(0, 0.05f, 0);
            SetMaterial(jailFloor, _matJail);

            // Jail walls (simple 4-sided frame).
            float w = 8f, h = 2.5f, t = 0.2f;
            Vector3[] wallPos = { new Vector3(0, h * 0.5f, -w * 0.5f), new Vector3(0, h * 0.5f, w * 0.5f), new Vector3(-w * 0.5f, h * 0.5f, 0), new Vector3(w * 0.5f, h * 0.5f, 0) };
            Vector3[] wallScale = { new Vector3(w, h, t), new Vector3(w, h, t), new Vector3(t, h, w), new Vector3(t, h, w) };
            for (int i = 0; i < 4; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.SetParent(jailRoot, false);
                wall.transform.localPosition = wallPos[i];
                wall.transform.localScale = wallScale[i];
                SetMaterial(wall, MakeMat(new Color(0.75f, 0.75f, 0.8f, 0.55f)));
                if (i == 1) { // front - gap for entry
                    wall.transform.localScale = new Vector3(w * 0.4f, h, t);
                    wall.transform.localPosition = new Vector3(-w * 0.3f, h * 0.5f, w * 0.5f);
                    var wall2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall2.name = $"Wall_{i}b";
                    wall2.transform.SetParent(jailRoot, false);
                    wall2.transform.localScale = new Vector3(w * 0.4f, h, t);
                    wall2.transform.localPosition = new Vector3(w * 0.3f, h * 0.5f, w * 0.5f);
                    SetMaterial(wall2, MakeMat(new Color(0.75f, 0.75f, 0.8f, 0.55f)));
                }
            }

            var entryPt = new GameObject("EntryPoint").transform;
            entryPt.SetParent(jailRoot, false); entryPt.localPosition = new Vector3(0, 0, w * 0.5f - 0.3f);
            var waitingPt = new GameObject("WaitingOutsidePoint").transform;
            waitingPt.SetParent(jailRoot, false); waitingPt.localPosition = new Vector3(0, 0, w * 0.5f + 2f);

            // Cell slots (grid inside).
            int slotsCount = 40;
            var cellSlots = new Transform[slotsCount];
            int perRow = 10;
            for (int i = 0; i < slotsCount; i++)
            {
                var s = new GameObject($"CellSlot_{i}").transform;
                s.SetParent(jailRoot, false);
                int row = i / perRow;
                int col = i % perRow;
                s.localPosition = new Vector3((col - (perRow - 1) * 0.5f) * 0.7f, 0, (row * 0.9f) - 2.5f);
                cellSlots[i] = s;
            }

            var jailGo = new GameObject("JailCell");
            jailGo.transform.SetParent(jailRoot, false);
            var jail = jailGo.AddComponent<JailCell>();
            var jailSo = new SerializedObject(jail);
            jailSo.FindProperty("entryPoint").objectReferenceValue = entryPt;
            var slotsProp = jailSo.FindProperty("cellSlots");
            slotsProp.arraySize = cellSlots.Length;
            for (int i = 0; i < cellSlots.Length; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cellSlots[i];
            jailSo.FindProperty("waitingOutsidePoint").objectReferenceValue = waitingPt;
            jailSo.ApplyModifiedProperties();

            // Wire CivilianQueue.
            var queueGo = new GameObject("CivilianQueue");
            queueGo.transform.SetParent(deskRoot, false);
            var queue = queueGo.AddComponent<CivilianQueue>();
            var qSo = new SerializedObject(queue);
            qSo.FindProperty("desk").objectReferenceValue = desk;
            qSo.FindProperty("spawnPoint").objectReferenceValue = spawnPt;
            qSo.FindProperty("processPoint").objectReferenceValue = processPt;
            qSo.FindProperty("jail").objectReferenceValue = jail;
            qSo.FindProperty("civilianPrefab").objectReferenceValue = civilianPrefab.GetComponent<CivilianNPC>();
            var slotsArr = qSo.FindProperty("queueSlots");
            slotsArr.arraySize = queueSlots.Length;
            for (int i = 0; i < queueSlots.Length; i++) slotsArr.GetArrayElementAtIndex(i).objectReferenceValue = queueSlots[i];
            var exitArr = qSo.FindProperty("exitPath");
            var exitTransforms = new Transform[] { exitPt, exitPt2 };
            exitArr.arraySize = exitTransforms.Length;
            for (int i = 0; i < exitTransforms.Length; i++) exitArr.GetArrayElementAtIndex(i).objectReferenceValue = exitTransforms[i];
            qSo.ApplyModifiedProperties();

            // BuyPads.
            BuyPad padDrill = CreateBuyPad(buyPadsRoot, "Pad_Drill", new Vector3(-8f, 0.1f, 3f), UpgradeIds.Drill, config.priceDrill, null, BuyPadUnlocker.UnlockCondition.OnFirstCash);
            BuyPad padVehicle = CreateBuyPad(buyPadsRoot, "Pad_Vehicle", new Vector3(-12f, 0.1f, 3f), UpgradeIds.Vehicle, config.priceVehicle, UpgradeIds.Drill, BuyPadUnlocker.UnlockCondition.OnPrerequisitePurchased);
            BuyPad padWorkers = CreateBuyPad(buyPadsRoot, "Pad_Workers", new Vector3(-10f, 0.1f, 5f), UpgradeIds.Workers, config.priceWorkers, UpgradeIds.Drill, BuyPadUnlocker.UnlockCondition.OnPrerequisitePurchased);
            BuyPad padJail = CreateBuyPad(buyPadsRoot, "Pad_Jail", new Vector3(10f, 0.1f, -5f), UpgradeIds.JailExpand, config.priceJailExpand, null, BuyPadUnlocker.UnlockCondition.OnJailFull, jail);
            BuyPad padPolice = CreateBuyPad(buyPadsRoot, "Pad_Police", new Vector3(6f, 0.1f, 0f), UpgradeIds.Police, config.pricePolice, UpgradeIds.Workers, BuyPadUnlocker.UnlockCondition.OnPrerequisitePurchased);

            // Machine deposit stand point (for workers).
            var machineDepositPoint = new GameObject("MachineDepositPoint").transform;
            machineDepositPoint.SetParent(factoryRoot, false);
            machineDepositPoint.localPosition = new Vector3(-2.2f, 0.5f, 0f);

            // Worker spawner.
            var workerSpawnerGo = new GameObject("WorkerSpawner");
            workerSpawnerGo.transform.SetParent(systems, false);
            var workerSpawner = workerSpawnerGo.AddComponent<WorkerSpawner>();
            var wsSo = new SerializedObject(workerSpawner);
            wsSo.FindProperty("workerPrefab").objectReferenceValue = workerPrefab.GetComponent<WorkerNPC>();
            wsSo.FindProperty("spawner").objectReferenceValue = spawner;
            wsSo.FindProperty("machine").objectReferenceValue = machine;
            var spawnPts = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var p = new GameObject($"WorkerSpawn_{i}").transform;
                p.SetParent(quarryRoot, false);
                p.localPosition = new Vector3(i * 1.5f - 1.5f, 0, -12.5f);
                spawnPts[i] = p;
            }
            var wsArr = wsSo.FindProperty("spawnPoints");
            wsArr.arraySize = spawnPts.Length;
            for (int i = 0; i < spawnPts.Length; i++) wsArr.GetArrayElementAtIndex(i).objectReferenceValue = spawnPts[i];
            wsSo.ApplyModifiedProperties();

            // Police spawner.
            var shelfStand = new GameObject("ShelfStand").transform;
            shelfStand.SetParent(factoryRoot, false); shelfStand.localPosition = new Vector3(4.2f, 0.5f, 1.2f);
            var deskStand = new GameObject("DeskStand").transform;
            deskStand.SetParent(deskRoot, false); deskStand.localPosition = new Vector3(0, 0, 0.8f);
            var policeSpawn = new GameObject("PoliceSpawn").transform;
            policeSpawn.SetParent(factoryRoot, false); policeSpawn.localPosition = new Vector3(5f, 0.5f, 3f);

            var policeSpawnerGo = new GameObject("PoliceSpawner");
            policeSpawnerGo.transform.SetParent(systems, false);
            var policeSpawner = policeSpawnerGo.AddComponent<PoliceSpawner>();
            var psSo = new SerializedObject(policeSpawner);
            psSo.FindProperty("policePrefab").objectReferenceValue = policePrefab.GetComponent<PoliceCarrierNPC>();
            psSo.FindProperty("spawnPoint").objectReferenceValue = policeSpawn;
            psSo.FindProperty("shelf").objectReferenceValue = shelf;
            psSo.FindProperty("shelfStand").objectReferenceValue = shelfStand;
            psSo.FindProperty("desk").objectReferenceValue = desk;
            psSo.FindProperty("deskStand").objectReferenceValue = deskStand;
            psSo.ApplyModifiedProperties();

            // Player.
            var player = BuildPlayer();
            player.transform.position = new Vector3(-3f, 1f, 3f);

            // Mining tool wire.
            var miningTool = player.AddComponent<MiningTool>();
            var miningSo = new SerializedObject(miningTool);
            miningSo.FindProperty("player").objectReferenceValue = player.GetComponent<PlayerController>();
            miningSo.FindProperty("spawner").objectReferenceValue = spawner;
            miningSo.ApplyModifiedProperties();

            // Camera.
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.75f, 0.95f);
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow>();
            var camSo = new SerializedObject(follow);
            camSo.FindProperty("target").objectReferenceValue = player.transform;
            camSo.FindProperty("offset").vector3Value = new Vector3(0f, 14f, -10f);
            camSo.FindProperty("smooth").floatValue = 6f;
            camSo.ApplyModifiedProperties();

            // UI.
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(uiRoot, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0.5f;

            var evSysGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            evSysGo.transform.SetParent(uiRoot, false);

            // Cash label.
            var cashGo = new GameObject("CashLabel", typeof(RectTransform));
            cashGo.transform.SetParent(canvasGo.transform, false);
            var cashRt = (RectTransform)cashGo.transform;
            cashRt.anchorMin = cashRt.anchorMax = new Vector2(1f, 1f);
            cashRt.pivot = new Vector2(1f, 1f);
            cashRt.anchoredPosition = new Vector2(-30, -30);
            cashRt.sizeDelta = new Vector2(260, 80);
            var cashBgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            cashBgGo.transform.SetParent(cashRt, false);
            var cbgRt = (RectTransform)cashBgGo.transform;
            cbgRt.anchorMin = Vector2.zero; cbgRt.anchorMax = Vector2.one;
            cbgRt.offsetMin = Vector2.zero; cbgRt.offsetMax = Vector2.zero;
            cashBgGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);
            var cashTextGo = new GameObject("Text", typeof(RectTransform));
            cashTextGo.transform.SetParent(cashRt, false);
            var cashText = cashTextGo.AddComponent<TextMeshProUGUI>();
            var ctRt = (RectTransform)cashTextGo.transform;
            ctRt.anchorMin = Vector2.zero; ctRt.anchorMax = Vector2.one;
            ctRt.offsetMin = Vector2.zero; ctRt.offsetMax = Vector2.zero;
            cashText.text = "$ 0";
            cashText.alignment = TextAlignmentOptions.Center;
            cashText.fontSize = 42;
            cashText.color = new Color(0.2f, 0.5f, 0.2f);

            // Jail label (world overlay via TMP-UI fixed position mimic). Create as simple UI text top-left.
            var jailLabelGo = new GameObject("JailLabel", typeof(RectTransform));
            jailLabelGo.transform.SetParent(canvasGo.transform, false);
            var jrt = (RectTransform)jailLabelGo.transform;
            jrt.anchorMin = jrt.anchorMax = new Vector2(0f, 1f);
            jrt.pivot = new Vector2(0f, 1f);
            jrt.anchoredPosition = new Vector2(30, -30);
            jrt.sizeDelta = new Vector2(220, 60);
            var jailBg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            jailBg.transform.SetParent(jrt, false);
            var jbRt = (RectTransform)jailBg.transform;
            jbRt.anchorMin = Vector2.zero; jbRt.anchorMax = Vector2.one;
            jbRt.offsetMin = Vector2.zero; jbRt.offsetMax = Vector2.zero;
            jailBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            var jailTextGo = new GameObject("Text", typeof(RectTransform));
            jailTextGo.transform.SetParent(jrt, false);
            var jailText = jailTextGo.AddComponent<TextMeshProUGUI>();
            var jtRt = (RectTransform)jailTextGo.transform;
            jtRt.anchorMin = Vector2.zero; jtRt.anchorMax = Vector2.one;
            jtRt.offsetMin = Vector2.zero; jtRt.offsetMax = Vector2.zero;
            jailText.text = "0 / 20";
            jailText.color = Color.white;
            jailText.fontSize = 36;
            jailText.alignment = TextAlignmentOptions.Center;

            // Joystick.
            var joystickGo = new GameObject("Joystick", typeof(RectTransform));
            joystickGo.transform.SetParent(canvasGo.transform, false);
            var joyRt = (RectTransform)joystickGo.transform;
            joyRt.anchorMin = joyRt.anchorMax = new Vector2(0.5f, 0f);
            joyRt.pivot = new Vector2(0.5f, 0f);
            joyRt.anchoredPosition = new Vector2(0, 80);
            joyRt.sizeDelta = new Vector2(260, 260);
            var joyBg = joystickGo.AddComponent<Image>();
            joyBg.color = new Color(1, 1, 1, 0.35f);
            joyBg.raycastTarget = true;
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(joyRt, false);
            var hRt = (RectTransform)handleGo.transform;
            hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.5f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.sizeDelta = new Vector2(90, 90);
            handleGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);
            var joystick = joystickGo.AddComponent<JoystickInput>();
            var joySo = new SerializedObject(joystick);
            joySo.FindProperty("background").objectReferenceValue = joyRt;
            joySo.FindProperty("handle").objectReferenceValue = hRt;
            joySo.ApplyModifiedProperties();

            // Sound button.
            var soundGo = new GameObject("SoundButton", typeof(RectTransform), typeof(Image), typeof(Button));
            soundGo.transform.SetParent(canvasGo.transform, false);
            var sRt = (RectTransform)soundGo.transform;
            sRt.anchorMin = sRt.anchorMax = new Vector2(0f, 0f);
            sRt.pivot = new Vector2(0f, 0f);
            sRt.anchoredPosition = new Vector2(30, 30);
            sRt.sizeDelta = new Vector2(100, 100);
            soundGo.GetComponent<Image>().color = new Color(0.2f, 0.4f, 1f, 1f);

            // HUD controller.
            var hudGo = new GameObject("HudController");
            hudGo.transform.SetParent(canvasGo.transform, false);
            var hud = hudGo.AddComponent<HudController>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("cashLabel").objectReferenceValue = cashText;
            hudSo.FindProperty("jailLabel").objectReferenceValue = jailText;
            hudSo.FindProperty("jail").objectReferenceValue = jail;
            hudSo.FindProperty("soundButton").objectReferenceValue = soundGo.GetComponent<Button>();
            hudSo.ApplyModifiedProperties();

            // Wire player joystick.
            var pSoc = new SerializedObject(player.GetComponent<PlayerController>());
            pSoc.FindProperty("joystick").objectReferenceValue = joystick;
            pSoc.ApplyModifiedProperties();

            // No Cell bubble near jail.
            var noCellGo = new GameObject("NoCellIndicator");
            noCellGo.transform.SetParent(jailRoot, false);
            noCellGo.transform.localPosition = new Vector3(0, 3.2f, w * 0.5f + 0.5f);
            var bubbleGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bubbleGo.name = "Bubble";
            bubbleGo.transform.SetParent(noCellGo.transform, false);
            bubbleGo.transform.localScale = new Vector3(2.2f, 0.8f, 1f);
            if (bubbleGo.TryGetComponent<Collider>(out var bCol)) Object.DestroyImmediate(bCol);
            SetMaterial(bubbleGo, MakeMat(Color.white));
            var tmpLabelGo = new GameObject("Label", typeof(RectTransform));
            tmpLabelGo.transform.SetParent(noCellGo.transform, false);
            tmpLabelGo.transform.localPosition = new Vector3(0, 0, -0.05f);
            var tmpLbl = tmpLabelGo.AddComponent<TextMeshPro>();
            tmpLbl.text = "no cel1!";
            tmpLbl.alignment = TextAlignmentOptions.Center;
            tmpLbl.fontSize = 4;
            tmpLbl.color = Color.black;
            var noCell = noCellGo.AddComponent<NoCellBubble>();
            var nSo = new SerializedObject(noCell);
            nSo.FindProperty("jail").objectReferenceValue = jail;
            nSo.FindProperty("bubble").objectReferenceValue = bubbleGo;
            nSo.FindProperty("label").objectReferenceValue = tmpLbl;
            nSo.ApplyModifiedProperties();
            bubbleGo.SetActive(false);

            // Save scene.
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Add to build settings.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"[SceneBuilder] Built {ScenePath}");
        }

        private static BuyPad CreateBuyPad(Transform parent, string name, Vector3 pos, string id, int price, string prereq,
            BuyPadUnlocker.UnlockCondition unlock = BuyPadUnlocker.UnlockCondition.OnPrerequisitePurchased, JailCell jailRef = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.8f, 1.5f, 1.8f);
            col.center = new Vector3(0, 0.75f, 0);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            visual.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            if (visual.TryGetComponent<Collider>(out var vc)) Object.DestroyImmediate(vc);
            SetMaterial(visual, _matBuyPad);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.text = $"$ {price}";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 4;
            label.color = new Color(0.2f, 0.2f, 0.2f);
            labelGo.AddComponent<WorldLabelBillboard>();

            var pad = go.AddComponent<BuyPad>();
            var s = new SerializedObject(pad);
            s.FindProperty("upgradeId").stringValue = id;
            s.FindProperty("price").intValue = price;
            s.FindProperty("costLabel").objectReferenceValue = label;
            s.FindProperty("padVisual").objectReferenceValue = visual;
            s.ApplyModifiedProperties();

            var marker = go.AddComponent<ZoneMarker>();
            marker.Configure(ZoneMarker.Kind.Buy, $"[구매] {name.Replace("Pad_", "")}", col);

            // Pad starts disabled; a sibling Unlocker activates it when the condition is met.
            go.SetActive(false);

            var unlockerGo = new GameObject(name + "_Unlocker");
            unlockerGo.transform.SetParent(parent, false);
            var unlocker = unlockerGo.AddComponent<BuyPadUnlocker>();
            var us = new SerializedObject(unlocker);
            us.FindProperty("target").objectReferenceValue = go;
            us.FindProperty("condition").enumValueIndex = (int)unlock;
            if (!string.IsNullOrEmpty(prereq)) us.FindProperty("prerequisiteId").stringValue = prereq;
            if (jailRef != null) us.FindProperty("jailRef").objectReferenceValue = jailRef;
            us.ApplyModifiedProperties();

            return pad;
        }

        // --------- Prefab builders ---------

        private static GameObject EnsureRockPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(RockPrefabPath);
            if (existing != null) return existing;
            var go = new GameObject("Rock");
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var col = visual.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            SetMaterial(visual, _matRock);
            go.AddComponent<Rock>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, RockPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        [MenuItem("PrisonLife/Rebuild Civilian Prefab")]
        public static void RebuildCivilianPrefab()
        {
            EnsureFolders();
            EnsureMaterials();
            BuildCivilianPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject EnsureCivilianPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CivilianPrefabPath);
            if (existing != null) return existing;
            return BuildCivilianPrefab();
        }

        private static GameObject BuildCivilianPrefab()
        {
            var go = new GameObject("Civilian");
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(model, false);
            body.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            SetMaterial(body, EnsureSavedMat("Civilian_Body", new Color(0.9f, 0.9f, 0.9f)));
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.height = 1.6f;
            col.radius = 0.3f;

            // Speech bubble above head (hidden by default).
            var bubble = new GameObject("SpeechBubble");
            bubble.transform.SetParent(go.transform, false);
            bubble.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            bubble.AddComponent<PrisonLife.UI.WorldLabelBillboard>();

            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Bg";
            Object.DestroyImmediate(bg.GetComponent<Collider>());
            bg.transform.SetParent(bubble.transform, false);
            bg.transform.localScale = new Vector3(0.55f, 0.35f, 0.05f);
            bg.transform.localPosition = Vector3.zero;
            SetMaterial(bg, EnsureSavedMat("Bubble_Bg", new Color(1f, 1f, 1f)));

            var tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tail.name = "Tail";
            Object.DestroyImmediate(tail.GetComponent<Collider>());
            tail.transform.SetParent(bubble.transform, false);
            tail.transform.localScale = new Vector3(0.12f, 0.12f, 0.05f);
            tail.transform.localPosition = new Vector3(0f, -0.22f, 0f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            SetMaterial(tail, EnsureSavedMat("Bubble_Bg", new Color(1f, 1f, 1f)));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(bubble.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.04f);
            textGo.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "x1";
            tm.fontSize = 48;
            tm.color = new Color(0.1f, 0.1f, 0.1f);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 1f;
            var tr = textGo.GetComponent<MeshRenderer>();
            if (tr != null) tr.sortingOrder = 10;

            bubble.SetActive(false);

            var npc = go.AddComponent<CivilianNPC>();
            var so = new SerializedObject(npc);
            so.FindProperty("model").objectReferenceValue = model;
            so.FindProperty("body").objectReferenceValue = body.GetComponent<Renderer>();
            so.FindProperty("speechBubble").objectReferenceValue = bubble;
            so.FindProperty("speechText").objectReferenceValue = tm;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, CivilianPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        [MenuItem("PrisonLife/Rebuild Worker Prefab")]
        public static void RebuildWorkerPrefab()
        {
            EnsureFolders();
            EnsureMaterials();
            BuildWorkerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject EnsureWorkerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(WorkerPrefabPath);
            if (existing != null) return existing;
            return BuildWorkerPrefab();
        }

        private static GameObject BuildWorkerPrefab()
        {
            var go = new GameObject("Worker");
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(model, false);
            body.transform.localScale = new Vector3(0.7f, 0.8f, 0.7f);
            body.transform.localPosition = new Vector3(0, 0.8f, 0);
            SetMaterial(body, EnsureSavedMat("Worker_Body", new Color(0.95f, 0.5f, 0.1f)));
            var hat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hat.name = "Hat";
            hat.transform.SetParent(model, false);
            hat.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            hat.transform.localPosition = new Vector3(0, 1.55f, 0);
            SetMaterial(hat, EnsureSavedMat("Worker_Hat", new Color(1f, 0.85f, 0.1f)));
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.8f, 0);
            col.height = 1.6f;
            col.radius = 0.3f;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            var stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(go.transform, false);
            stackRoot.localPosition = new Vector3(0, 1.6f, 0.3f);
            var stack = go.AddComponent<CarryStack>();
            var ss = new SerializedObject(stack);
            ss.FindProperty("stackRoot").objectReferenceValue = stackRoot;
            ss.ApplyModifiedProperties();
            var worker = go.AddComponent<WorkerNPC>();
            var ws = new SerializedObject(worker);
            ws.FindProperty("model").objectReferenceValue = model;
            ws.FindProperty("stack").objectReferenceValue = stack;
            ws.ApplyModifiedProperties();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, WorkerPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        [MenuItem("PrisonLife/Rebuild Police Prefab")]
        public static void RebuildPolicePrefab()
        {
            EnsureFolders();
            EnsureMaterials();
            BuildPolicePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject EnsurePolicePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PolicePrefabPath);
            if (existing != null) return existing;
            return BuildPolicePrefab();
        }

        private static GameObject BuildPolicePrefab()
        {
            var go = new GameObject("Police");
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);

            // Colors (saved as assets so the prefab keeps references after reload)
            var uniformBlue = EnsureSavedMat("Police_Uniform", new Color(0.15f, 0.3f, 0.75f));
            var pantsNavy   = EnsureSavedMat("Police_Pants",   new Color(0.08f, 0.12f, 0.35f));
            var skin        = EnsureSavedMat("Police_Skin",    new Color(0.95f, 0.8f, 0.65f));
            var hatNavy     = EnsureSavedMat("Police_Hat",     new Color(0.06f, 0.1f, 0.3f));
            var badgeGold   = EnsureSavedMat("Police_Badge",   new Color(1f, 0.85f, 0.2f));
            var beltBlack   = EnsureSavedMat("Police_Belt",    new Color(0.08f, 0.08f, 0.08f));

            // Legs (Cube each)
            var legL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legL.name = "LegL";
            legL.transform.SetParent(model, false);
            legL.transform.localScale = new Vector3(0.22f, 0.6f, 0.25f);
            legL.transform.localPosition = new Vector3(-0.15f, 0.3f, 0f);
            SetMaterial(legL, pantsNavy);
            var legR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            legR.name = "LegR";
            legR.transform.SetParent(model, false);
            legR.transform.localScale = new Vector3(0.22f, 0.6f, 0.25f);
            legR.transform.localPosition = new Vector3(0.15f, 0.3f, 0f);
            SetMaterial(legR, pantsNavy);

            // Torso (Capsule)
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(model, false);
            torso.transform.localScale = new Vector3(0.55f, 0.45f, 0.45f);
            torso.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            SetMaterial(torso, uniformBlue);

            // Belt (thin Cube)
            var belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
            belt.name = "Belt";
            belt.transform.SetParent(model, false);
            belt.transform.localScale = new Vector3(0.6f, 0.08f, 0.5f);
            belt.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            SetMaterial(belt, beltBlack);

            // Badge (small Cube)
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Badge";
            badge.transform.SetParent(model, false);
            badge.transform.localScale = new Vector3(0.12f, 0.12f, 0.05f);
            badge.transform.localPosition = new Vector3(0.15f, 1.15f, 0.22f);
            SetMaterial(badge, badgeGold);

            // Arms (Capsule)
            var armL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            armL.name = "ArmL";
            armL.transform.SetParent(model, false);
            armL.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
            armL.transform.localPosition = new Vector3(-0.38f, 1.0f, 0f);
            SetMaterial(armL, uniformBlue);
            var armR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            armR.name = "ArmR";
            armR.transform.SetParent(model, false);
            armR.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
            armR.transform.localPosition = new Vector3(0.38f, 1.0f, 0f);
            SetMaterial(armR, uniformBlue);

            // Head (Sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(model, false);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            head.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            SetMaterial(head, skin);

            // Hat brim (flat Cylinder) + crown (Cylinder)
            var hatBrim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatBrim.name = "HatBrim";
            hatBrim.transform.SetParent(model, false);
            hatBrim.transform.localScale = new Vector3(0.48f, 0.03f, 0.48f);
            hatBrim.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            SetMaterial(hatBrim, hatNavy);
            var hatCrown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatCrown.name = "HatCrown";
            hatCrown.transform.SetParent(model, false);
            hatCrown.transform.localScale = new Vector3(0.33f, 0.1f, 0.33f);
            hatCrown.transform.localPosition = new Vector3(0f, 1.92f, 0f);
            SetMaterial(hatCrown, hatNavy);

            // Hat badge front (tiny Cube)
            var hatBadge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hatBadge.name = "HatBadge";
            hatBadge.transform.SetParent(model, false);
            hatBadge.transform.localScale = new Vector3(0.1f, 0.06f, 0.04f);
            hatBadge.transform.localPosition = new Vector3(0f, 1.95f, 0.3f);
            SetMaterial(hatBadge, badgeGold);

            // Strip child colliders (primitives come with colliders by default).
            foreach (var c in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);

            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.0f, 0);
            col.height = 2.0f;
            col.radius = 0.35f;

            // Kinematic Rigidbody so trigger zones detect this NPC as a moving body.
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            var stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(go.transform, false);
            stackRoot.localPosition = new Vector3(0, 1.3f, 0.35f);
            var stack = go.AddComponent<CarryStack>();
            var ss = new SerializedObject(stack);
            ss.FindProperty("stackRoot").objectReferenceValue = stackRoot;
            ss.ApplyModifiedProperties();

            var p = go.AddComponent<PoliceCarrierNPC>();
            var ps = new SerializedObject(p);
            ps.FindProperty("model").objectReferenceValue = model;
            ps.FindProperty("stack").objectReferenceValue = stack;
            ps.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PolicePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildPlayer()
        {
            var go = new GameObject("Player");
            go.tag = "Player";
            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(model, false);
            body.transform.localScale = new Vector3(0.75f, 0.9f, 0.75f);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            if (body.TryGetComponent<Collider>(out var pb)) Object.DestroyImmediate(pb);
            SetMaterial(body, MakeMat(new Color(0.15f, 0.3f, 0.85f)));
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Hat";
            hat.transform.SetParent(model, false);
            hat.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            hat.transform.localPosition = new Vector3(0, 1.75f, 0);
            if (hat.TryGetComponent<Collider>(out var hb)) Object.DestroyImmediate(hb);
            SetMaterial(hat, MakeMat(new Color(0.1f, 0.2f, 0.7f)));

            var drill = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drill.name = "DrillVisual";
            drill.transform.SetParent(model, false);
            drill.transform.localPosition = new Vector3(0, 1.3f, -0.4f);
            drill.transform.localRotation = Quaternion.Euler(90, 0, 0);
            drill.transform.localScale = new Vector3(0.2f, 0.4f, 0.2f);
            if (drill.TryGetComponent<Collider>(out var dc)) Object.DestroyImmediate(dc);
            SetMaterial(drill, MakeMat(new Color(0.7f, 0.7f, 0.7f)));
            drill.SetActive(false);

            var vehicle = new GameObject("VehicleVisual");
            vehicle.transform.SetParent(go.transform, false);
            vehicle.transform.localPosition = new Vector3(0, 0f, 0);

            var vehicleBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicleBody.name = "Body";
            vehicleBody.transform.SetParent(vehicle.transform, false);
            vehicleBody.transform.localScale = new Vector3(3.2f, 1.2f, 4.2f);
            vehicleBody.transform.localPosition = new Vector3(0, 0.9f, 0);
            if (vehicleBody.TryGetComponent<Collider>(out var vbc)) Object.DestroyImmediate(vbc);
            SetMaterial(vehicleBody, MakeMat(new Color(1f, 0.85f, 0.2f)));

            var vehicleCab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicleCab.name = "Cab";
            vehicleCab.transform.SetParent(vehicle.transform, false);
            vehicleCab.transform.localScale = new Vector3(2.4f, 1.2f, 1.6f);
            vehicleCab.transform.localPosition = new Vector3(0, 2.1f, -0.5f);
            if (vehicleCab.TryGetComponent<Collider>(out var vcc)) Object.DestroyImmediate(vcc);
            SetMaterial(vehicleCab, MakeMat(new Color(0.95f, 0.7f, 0.15f)));

            var vehicleDrill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicleDrill.name = "FrontDrill";
            vehicleDrill.transform.SetParent(vehicle.transform, false);
            vehicleDrill.transform.localScale = new Vector3(3.0f, 0.6f, 0.8f);
            vehicleDrill.transform.localPosition = new Vector3(0, 0.6f, 2.3f);
            if (vehicleDrill.TryGetComponent<Collider>(out var vdc)) Object.DestroyImmediate(vdc);
            SetMaterial(vehicleDrill, MakeMat(new Color(0.35f, 0.35f, 0.38f)));

            vehicle.SetActive(false);

            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 0.9f, 0);
            cc.height = 1.8f;
            cc.radius = 0.35f;

            // Stacks.
            var rockStackRoot = new GameObject("RockStackRoot").transform;
            rockStackRoot.SetParent(go.transform, false);
            rockStackRoot.localPosition = new Vector3(0, 1.8f, -0.4f);
            var rockStack = go.AddComponent<CarryStack>();
            var rs = new SerializedObject(rockStack);
            rs.FindProperty("stackRoot").objectReferenceValue = rockStackRoot;
            rs.ApplyModifiedProperties();

            var handcuffStackRoot = new GameObject("HandcuffStackRoot").transform;
            handcuffStackRoot.SetParent(go.transform, false);
            handcuffStackRoot.localPosition = new Vector3(0, 1.8f, 0.4f);
            var handcuffStack = go.AddComponent<CarryStack>();
            var hs = new SerializedObject(handcuffStack);
            hs.FindProperty("stackRoot").objectReferenceValue = handcuffStackRoot;
            hs.ApplyModifiedProperties();

            var cashStackRoot = new GameObject("CashStackRoot").transform;
            cashStackRoot.SetParent(go.transform, false);
            cashStackRoot.localPosition = new Vector3(0.45f, 1.8f, 0f);
            var cashStack = go.AddComponent<CarryStack>();
            var cs = new SerializedObject(cashStack);
            cs.FindProperty("stackRoot").objectReferenceValue = cashStackRoot;
            cs.ApplyModifiedProperties();

            var pc = go.AddComponent<PlayerController>();
            var pcSo = new SerializedObject(pc);
            pcSo.FindProperty("rockStack").objectReferenceValue = rockStack;
            pcSo.FindProperty("handcuffStack").objectReferenceValue = handcuffStack;
            pcSo.FindProperty("cashStack").objectReferenceValue = cashStack;
            pcSo.FindProperty("modelRoot").objectReferenceValue = model;
            pcSo.FindProperty("drillVisual").objectReferenceValue = drill.transform;
            pcSo.FindProperty("vehicleVisual").objectReferenceValue = vehicle.transform;
            pcSo.ApplyModifiedProperties();

            // Mining equipment (pickaxe + Drill prefabs).
            var me = go.AddComponent<MiningEquipment>();
            var pickaxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/pickaxe.prefab");
            var drillPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Drill.prefab");
            var meSo = new SerializedObject(me);
            meSo.FindProperty("player").objectReferenceValue = pc;
            meSo.FindProperty("attachRoot").objectReferenceValue = model;
            if (pickaxePrefab != null) meSo.FindProperty("pickaxePrefab").objectReferenceValue = pickaxePrefab;
            if (drillPrefab != null)   meSo.FindProperty("drillPrefab").objectReferenceValue = drillPrefab;
            meSo.ApplyModifiedProperties();
            return go;
        }

        [MenuItem("PrisonLife/Wire Mining Equipment")]
        public static void WireMiningEquipment()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) { Debug.LogWarning("[Mining] No active scene."); return; }

            PlayerController pc = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                pc = root.GetComponentInChildren<PlayerController>(true);
                if (pc != null) break;
            }
            if (pc == null) { Debug.LogWarning("[Mining] PlayerController not found."); return; }

            var pickaxePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/pickaxe.prefab");
            var drillPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Drill.prefab");
            if (pickaxePrefab == null || drillPrefab == null)
            {
                Debug.LogWarning("[Mining] Missing pickaxe.prefab or Drill.prefab in Assets/_Project/Prefabs.");
                return;
            }

            var playerGo = pc.gameObject;
            var me = playerGo.GetComponent<MiningEquipment>();
            if (me == null) me = playerGo.AddComponent<MiningEquipment>();
            Transform model = playerGo.transform.Find("Model");

            var so = new SerializedObject(me);
            so.FindProperty("player").objectReferenceValue = pc;
            so.FindProperty("attachRoot").objectReferenceValue = model != null ? model : playerGo.transform;
            so.FindProperty("pickaxePrefab").objectReferenceValue = pickaxePrefab;
            so.FindProperty("drillPrefab").objectReferenceValue = drillPrefab;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(playerGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Mining] MiningEquipment wired on Player.");
        }

        private static GameConfig EnsureConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (existing != null) return existing;
            var config = ScriptableObject.CreateInstance<GameConfig>();
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.CreateAsset(config, ConfigPath);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static void EnsureFolders()
        {
            string[] folders = { "Assets/_Project/Scenes", "Assets/_Project/Prefabs", "Assets/_Project/ScriptableObjects", "Assets/_Project/Materials" };
            foreach (var f in folders) Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
        }

        private static void EnsureMaterials()
        {
            _matGround = LoadOrCreateMat("Ground", new Color(0.85f, 0.75f, 0.45f));
            _matFence = LoadOrCreateMat("Fence", new Color(0.6f, 0.6f, 0.6f));
            _matMachine = LoadOrCreateMat("Machine", new Color(0.2f, 0.45f, 0.85f));
            _matDesk = LoadOrCreateMat("Desk", new Color(0.55f, 0.4f, 0.2f));
            _matJail = LoadOrCreateMat("Jail", new Color(0.55f, 0.55f, 0.55f));
            _matRock = LoadOrCreateMat("Rock", new Color(0.3f, 0.3f, 0.33f));
            _matBuyPad = LoadOrCreateMat("BuyPad", new Color(1f, 1f, 1f));
            AssetDatabase.SaveAssets();
        }

        private static Material LoadOrCreateMat(string name, Color c)
        {
            string path = $"Assets/_Project/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) { m.color = c; return m; }
            m = MakeMat(c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material MakeMat(Color c)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader);
            m.color = c;
            return m;
        }

        // ---------------- Jail detail builder ----------------

        [MenuItem("PrisonLife/Rebuild Jail Detail")]
        public static void RebuildJailDetail()
        {
            EnsureMaterials();
            var jail = Object.FindObjectOfType<JailCell>(true);
            if (jail == null) { Debug.LogError("No JailCell found in scene."); return; }
            var jailRoot = jail.transform.parent != null ? jail.transform.parent : jail.transform;

            // Preserve user-edited base structures (names ending in "_Base") and beds (Bed_*).
            // Only wipe slots, entry/waiting points, and old expansion props that we'll regenerate.
            var keep = jail.gameObject;
            for (int i = jailRoot.childCount - 1; i >= 0; i--)
            {
                var child = jailRoot.GetChild(i).gameObject;
                if (child == keep) continue;
                string nm = child.name;
                if (nm.EndsWith("_Base")) continue;
                if (nm.StartsWith("Bar_Base_")) continue;
                if (nm.StartsWith("Bed_")) continue;
                Object.DestroyImmediate(child);
            }

            BuildJailDetail(jailRoot, jail, forceNewComponent: false);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Jail detail rebuilt.");
        }

        /// <summary>
        /// Builds/refreshes jail furniture. Reads base floor/wall dimensions from existing scene
        /// objects (preserving any user edits). If base structures are missing, they are created.
        /// Beds are decorative only (not prisoner slots); slots are a separate grid on the floor.
        /// Expansion doubles jail depth in the z direction (extends backward).
        /// </summary>
        private static void BuildJailDetail(Transform jailRoot, JailCell jail, bool forceNewComponent)
        {
            const float wallH = 2.6f;
            const float wallT = 0.15f;

            var matWall = MakeMat(new Color(0.78f, 0.78f, 0.82f));
            var matBar = MakeMat(new Color(0.3f, 0.3f, 0.32f));
            var matBeam = MakeMat(new Color(0.45f, 0.35f, 0.22f));
            var matBedFrame = MakeMat(new Color(0.28f, 0.2f, 0.14f));
            var matMattress = MakeMat(new Color(0.95f, 0.88f, 0.78f));
            var matPillow = MakeMat(new Color(0.98f, 0.98f, 0.98f));
            var matBlanket = MakeMat(new Color(0.2f, 0.38f, 0.65f));
            var matBlanket2 = MakeMat(new Color(0.65f, 0.2f, 0.25f));
            var matFloor = MakeMat(new Color(0.45f, 0.45f, 0.48f));
            var matFloorExp = MakeMat(new Color(0.5f, 0.45f, 0.4f));

            GameObject MkCube(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = pos;
                go.transform.localScale = scale;
                SetMaterial(go, mat);
                return go;
            }

            // ---------- Base floor (preserve existing, otherwise create default) ----------
            var floorBase = jailRoot.Find("JailFloor_Base");
            float floorW, baseDepth, baseCenterZ;
            if (floorBase != null)
            {
                floorW = floorBase.localScale.x;
                baseDepth = floorBase.localScale.z;
                baseCenterZ = floorBase.localPosition.z;
            }
            else
            {
                floorW = 8.2f;
                baseDepth = 6.0f;
                baseCenterZ = -3.93f;
                var f = MkCube("JailFloor_Base", jailRoot,
                    new Vector3(0f, 0.05f, baseCenterZ),
                    new Vector3(floorW, 0.1f, baseDepth), matFloor);
                floorBase = f.transform;
            }

            float baseBackZ = baseCenterZ - baseDepth * 0.5f;
            float baseFrontZ = baseCenterZ + baseDepth * 0.5f;

            // Fill in any missing base structures using dimensions derived from the floor.
            if (jailRoot.Find("BackWall_Base") == null)
                MkCube("BackWall_Base", jailRoot, new Vector3(0f, wallH * 0.5f, baseBackZ), new Vector3(floorW, wallH, wallT), matWall);
            if (jailRoot.Find("LeftWall_Base") == null)
                MkCube("LeftWall_Base", jailRoot, new Vector3(-floorW * 0.5f, wallH * 0.5f, baseCenterZ), new Vector3(wallT, wallH, baseDepth), matWall);
            if (jailRoot.Find("RightWall_Base") == null)
                MkCube("RightWall_Base", jailRoot, new Vector3(floorW * 0.5f, wallH * 0.5f, baseCenterZ), new Vector3(wallT, wallH, baseDepth), matWall);
            if (jailRoot.Find("Pillar_BL_Base") == null)
                MkCube("Pillar_BL_Base", jailRoot, new Vector3(-floorW * 0.5f, (wallH + 0.3f) * 0.5f, baseBackZ), new Vector3(0.28f, wallH + 0.3f, 0.28f), matWall);
            if (jailRoot.Find("Pillar_BR_Base") == null)
                MkCube("Pillar_BR_Base", jailRoot, new Vector3(floorW * 0.5f, (wallH + 0.3f) * 0.5f, baseBackZ), new Vector3(0.28f, wallH + 0.3f, 0.28f), matWall);
            if (jailRoot.Find("CeilingBeam_Base") == null)
                MkCube("CeilingBeam_Base", jailRoot, new Vector3(0f, wallH + 0.05f, baseCenterZ), new Vector3(floorW + 0.2f, 0.15f, 0.2f), matBeam);

            // Front bars (only if no Bar_Base_* exist yet).
            bool hasBars = false;
            for (int i = 0; i < jailRoot.childCount; i++)
                if (jailRoot.GetChild(i).name.StartsWith("Bar_Base_")) { hasBars = true; break; }
            if (!hasBars)
            {
                int barCount = 14;
                float gapHalf = 0.7f;
                for (int i = 0; i < barCount; i++)
                {
                    float x = (i / (float)(barCount - 1) - 0.5f) * (floorW - 0.6f);
                    if (Mathf.Abs(x) < gapHalf) continue;
                    var bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bar.name = $"Bar_Base_{i}";
                    bar.transform.SetParent(jailRoot, false);
                    bar.transform.localPosition = new Vector3(x, wallH * 0.5f, baseFrontZ);
                    bar.transform.localScale = new Vector3(0.06f, wallH * 0.5f, 0.06f);
                    SetMaterial(bar, matBar);
                }
                MkCube("EntryHeader_Base", jailRoot, new Vector3(0f, wallH - 0.15f, baseFrontZ), new Vector3(gapHalf * 2f + 0.2f, 0.3f, 0.1f), matBar);
            }

            // ---------- Expansion: base floor/walls stretch 2x in z (anchored at front entrance);
            // back wall + back pillars shift backward by baseDepth. Only an extra ceiling beam
            // is added as a scale-in prop in the new (expanded) section. ----------
            float expOnlyDepth = baseDepth;                  // doubles total jail depth
            float expCenterZ = baseBackZ - expOnlyDepth * 0.5f;
            float expBackZ = baseBackZ - expOnlyDepth;

            var expansionStructures = new List<GameObject>();
            expansionStructures.Add(MkCube("CeilingBeam_Exp", jailRoot,
                new Vector3(0f, wallH + 0.05f, expCenterZ),
                new Vector3(floorW + 0.2f, 0.15f, 0.2f), matBeam));

            // Mark which base structures should stretch / shift on expansion.
            var stretchTargets = new List<Transform>();
            var floorBaseTr = jailRoot.Find("JailFloor_Base");
            var leftWallTr = jailRoot.Find("LeftWall_Base");
            var rightWallTr = jailRoot.Find("RightWall_Base");
            if (floorBaseTr != null) stretchTargets.Add(floorBaseTr);
            if (leftWallTr != null) stretchTargets.Add(leftWallTr);
            if (rightWallTr != null) stretchTargets.Add(rightWallTr);

            var shiftTargets = new List<Transform>();
            var backWallTr = jailRoot.Find("BackWall_Base");
            var pillarBLTr = jailRoot.Find("Pillar_BL_Base");
            var pillarBRTr = jailRoot.Find("Pillar_BR_Base");
            if (backWallTr != null) shiftTargets.Add(backWallTr);
            if (pillarBLTr != null) shiftTargets.Add(pillarBLTr);
            if (pillarBRTr != null) shiftTargets.Add(pillarBRTr);

            // ---------- Entry / waiting points ----------
            var entryPt = new GameObject("EntryPoint").transform;
            entryPt.SetParent(jailRoot, false);
            entryPt.localPosition = new Vector3(0f, 0f, baseFrontZ - 0.4f);

            var waitingPt = new GameObject("WaitingOutsidePoint").transform;
            waitingPt.SetParent(jailRoot, false);
            waitingPt.localPosition = new Vector3(0f, 0f, baseFrontZ + 1.8f);

            // ---------- Standing slots (prisoner positions — NOT tied to beds) ----------
            // 5 cols × 4 rows in base (20 slots), plus 5 cols × 4 rows in expansion (20 more).
            const int slotCols = 5;
            const int slotRowsPerSection = 4;
            const int slotsCount = 40;
            var cellSlots = new Transform[slotsCount];
            float slotXStep = floorW / (slotCols + 1);

            for (int sec = 0; sec < 2; sec++) // 0 = base, 1 = expansion
            {
                float secCenterZ = sec == 0 ? baseCenterZ : expCenterZ;
                float secDepth = sec == 0 ? baseDepth : expOnlyDepth;
                float zStep = secDepth / (slotRowsPerSection + 1);
                for (int row = 0; row < slotRowsPerSection; row++)
                {
                    for (int col = 0; col < slotCols; col++)
                    {
                        int idx = sec * 20 + row * slotCols + col;
                        float x = (col - (slotCols - 1) * 0.5f) * slotXStep;
                        float z = secCenterZ - secDepth * 0.5f + zStep * (row + 1);
                        var slot = new GameObject($"Slot_{idx}").transform;
                        slot.SetParent(jailRoot, false);
                        slot.localPosition = new Vector3(x, 0.1f, z);
                        cellSlots[idx] = slot;
                    }
                }
            }

            // ---------- Decorative beds (few per section, along side walls) ----------
            // 3 beds on left + 3 on right per section = 6 base + 6 expansion.
            var extraBeds = new List<GameObject>();

            GameObject BuildBed(string name, Transform parent, Vector3 pos, bool leftWall, int colorVariant)
            {
                var bed = new GameObject(name);
                bed.transform.SetParent(parent, false);
                bed.transform.localPosition = pos;
                // Orient bed so its long axis faces inward (rotate 90° when against side walls).
                bed.transform.localRotation = Quaternion.Euler(0f, leftWall ? 90f : -90f, 0f);

                MkCube("Frame", bed.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.6f, 0.2f, 1.5f), matBedFrame);
                MkCube("Leg_FL", bed.transform, new Vector3(-0.26f, 0.09f, -0.68f), new Vector3(0.08f, 0.18f, 0.08f), matBedFrame);
                MkCube("Leg_FR", bed.transform, new Vector3(0.26f, 0.09f, -0.68f), new Vector3(0.08f, 0.18f, 0.08f), matBedFrame);
                MkCube("Leg_BL", bed.transform, new Vector3(-0.26f, 0.09f, 0.68f), new Vector3(0.08f, 0.18f, 0.08f), matBedFrame);
                MkCube("Leg_BR", bed.transform, new Vector3(0.26f, 0.09f, 0.68f), new Vector3(0.08f, 0.18f, 0.08f), matBedFrame);
                MkCube("Mattress", bed.transform, new Vector3(0f, 0.33f, 0f), new Vector3(0.55f, 0.1f, 1.4f), matMattress);
                MkCube("Pillow", bed.transform, new Vector3(0f, 0.42f, -0.55f), new Vector3(0.42f, 0.08f, 0.25f), matPillow);
                MkCube("Blanket", bed.transform, new Vector3(0f, 0.4f, 0.25f), new Vector3(0.52f, 0.06f, 0.85f),
                    colorVariant == 0 ? matBlanket : matBlanket2);
                return bed;
            }

            void PlaceBeds(int section)
            {
                float secCenterZ = section == 0 ? baseCenterZ : expCenterZ;
                float secDepth = section == 0 ? baseDepth : expOnlyDepth;
                float leftX = -floorW * 0.5f + 0.45f;
                float rightX = floorW * 0.5f - 0.45f;
                int bedsPerSide = 3;
                float zStart = secCenterZ - secDepth * 0.5f + secDepth / (bedsPerSide + 1);
                float zStep = secDepth / (bedsPerSide + 1);
                string prefix = section == 0 ? "Bed_Base_" : "Bed_Exp_";
                for (int i = 0; i < bedsPerSide; i++)
                {
                    float z = zStart + zStep * i;
                    string lname = prefix + "L" + i;
                    string rname = prefix + "R" + i;
                    // Reuse existing beds (preserve user-edited transforms); only create if missing.
                    var leftExisting = jailRoot.Find(lname);
                    var rightExisting = jailRoot.Find(rname);
                    GameObject left = leftExisting != null ? leftExisting.gameObject
                        : BuildBed(lname, jailRoot, new Vector3(leftX, 0f, z), true, i % 2);
                    GameObject right = rightExisting != null ? rightExisting.gameObject
                        : BuildBed(rname, jailRoot, new Vector3(rightX, 0f, z), false, (i + 1) % 2);
                    if (section == 1)
                    {
                        extraBeds.Add(left);
                        extraBeds.Add(right);
                    }
                }
            }
            PlaceBeds(0);
            PlaceBeds(1);

            // Hide expansion (structures + extra beds) initially — JailCell animates them in.
            foreach (var s in expansionStructures)
            {
                s.SetActive(false);
                s.transform.localScale = Vector3.zero;
            }
            foreach (var b in extraBeds)
            {
                b.SetActive(false);
                b.transform.localScale = Vector3.zero;
            }

            // ---------- Wire JailCell ----------
            var so = new SerializedObject(jail);
            so.FindProperty("entryPoint").objectReferenceValue = entryPt;
            var slotsProp = so.FindProperty("cellSlots");
            slotsProp.arraySize = cellSlots.Length;
            for (int i = 0; i < cellSlots.Length; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = cellSlots[i];
            var extraProp = so.FindProperty("extraBeds");
            extraProp.arraySize = extraBeds.Count;
            for (int i = 0; i < extraBeds.Count; i++) extraProp.GetArrayElementAtIndex(i).objectReferenceValue = extraBeds[i];
            var expProp = so.FindProperty("expansionStructures");
            expProp.arraySize = expansionStructures.Count;
            for (int i = 0; i < expansionStructures.Count; i++) expProp.GetArrayElementAtIndex(i).objectReferenceValue = expansionStructures[i];
            var stretchProp = so.FindProperty("stretchZTargets");
            stretchProp.arraySize = stretchTargets.Count;
            for (int i = 0; i < stretchTargets.Count; i++) stretchProp.GetArrayElementAtIndex(i).objectReferenceValue = stretchTargets[i];
            var shiftProp = so.FindProperty("shiftBackZTargets");
            shiftProp.arraySize = shiftTargets.Count;
            for (int i = 0; i < shiftTargets.Count; i++) shiftProp.GetArrayElementAtIndex(i).objectReferenceValue = shiftTargets[i];
            so.FindProperty("waitingOutsidePoint").objectReferenceValue = waitingPt;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(jail);
        }

        private static Material EnsureSavedMat(string name, Color c)
        {
            const string dir = "Assets/_Project/Materials";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                    AssetDatabase.CreateFolder("Assets", "_Project");
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }
            string path = dir + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (existing.color != c) { existing.color = c; EditorUtility.SetDirty(existing); }
                return existing;
            }
            var m = MakeMat(c);
            m.name = name;
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void SetMaterial(GameObject go, Material m)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        private static void EnsureTags()
        {
            AddTag("Player");
        }

        private static void AddTag(string tag)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif

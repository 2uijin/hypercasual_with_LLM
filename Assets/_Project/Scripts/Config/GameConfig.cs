using UnityEngine;

namespace PrisonLife.Config
{
    [CreateAssetMenu(menuName = "PrisonLife/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Player")]
        public float playerMoveSpeed = 5f;
        public float playerRotateSpeed = 720f;
        public float playerVehicleSpeed = 7f;

        [Header("Carry")]
        public int rockCarryCapacity = 12;
        public int rockCarryCapacityDrill = 24;
        public int rockCarryCapacityVehicle = 40;
        public float stackItemHeight = 0.15f;
        public float stackSideOffset = 0.18f;
        public int stackRowSize = 1;

        [Header("Mining")]
        public float rockRespawnSeconds = 5f;
        public float miningRangeBasic = 1.6f;
        public float miningIntervalPickaxe = 0.45f;
        public float miningIntervalDrill = 0.12f;
        public float miningIntervalVehicle = 0.07f;

        [Header("Processing Machine")]
        public float machineRockIntakeInterval = 0.15f;
        public float machineSecondsPerHandcuff = 0.6f;
        public int machineMaxShelfStack = 40;

        [Header("Desk")]
        public float deskHandcuffDepositInterval = 0.1f;
        public float civilianPurchaseDuration = 0.8f;
        public int handcuffsPerCivilianMin = 1;
        public int handcuffsPerCivilianMax = 4;
        public int cashPerHandcuff = 6;
        public int deskQueueSlots = 6;

        [Header("Jail")]
        public int jailBaseCapacity = 20;
        public int jailExpandedCapacity = 40;

        [Header("Economy / Upgrade Prices")]
        public int priceDrill = 20;
        public int priceVehicle = 50;
        public int priceWorkers = 50;
        public int priceJailExpand = 50;
        public int pricePolice = 50;

        [Header("Upgrade Buy Rate (cash/sec)")]
        public float buyPadDrainPerSecond = 25f;

        [Header("NPC")]
        public float npcMoveSpeed = 2.8f;
        public int workerCountPerPurchase = 3;
        public float workerMiningInterval = 0.9f;
        public int workerCarryCapacity = 10;
        public float policeCarrierSpeed = 3.2f;
        public int policeCarryAmount = 6;
        public float civilianSpawnInterval = 1.5f;
    }
}

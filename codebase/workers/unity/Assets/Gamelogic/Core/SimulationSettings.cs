using UnityEngine;

namespace Assets.Gamelogic.Core
{
    public static class SimulationSettings
    {
        public static readonly float numDrones = 100;
        public static readonly float squareSize = 900;

        // DRONE CONSTANTS START //

        public const float DroneUpdateInterval = 0.25F;

        public const float MaxDroneSpeed = 20;

        public const int MaxTargetRequestFailures = 5;

        public static float DroneRadius = 0.5f;

        public static float MaxRequestWaitTime = 10f;

        // DRONE CONSTANTS END //

        // GLOBAL LAYER START //

        public static readonly float ControllerUpdateInterval = 0.1f;
        public static readonly float DroneSpawnInterval = 2f;

        public static readonly int BIT_SIZE = 10; // meters that each bit in the grid corresponds to, OG 25 in AATC 
        public const int SIZE_OF_A_STEP = 1; // used when setting bits from a no fly zone

        public static readonly float RoutingShortCircuitThreshold = 50;

        public static readonly float MinimumDroneHeight = 10;
        public static readonly float SuggestedDroneHeight = 80;
        public static readonly float MaximumDroneHeight = 120;

        public static readonly int NFZ_PADDING_RAW = 50;
        public static readonly int NFZ_PADDING = NFZ_PADDING_RAW / BIT_SIZE;

        // GLOBAL LAYER END //

        // REACTIVE LAYER START //

        //constants initially genetically discovered for 10 m/s speed
        //public static float RepulsionConst = 446.78f;
        public static float RepulsionConst = 223.39f;
        //public static float RepulsionConst = 111.7f;
        //public static float AttractionConst = 1.0038f;
        public static float AttractionConst = 2.0076f;
        public static float InfuentialDistanceConstant = 532;
        public static float ReturnConstant = 0.689f;

        public const float SafeDistance = 10;

        // REACTIVE LAYER END //

        public static readonly uint ControllerRows = 4;
        public static readonly uint ControllerColumns = 4;
        public static readonly uint ControllerCount = ControllerRows * ControllerColumns;

        public static readonly uint MaxDroneCount = (uint) numDrones;
        public static readonly uint MaxDroneCountPerController = (uint) (MaxDroneCount / ControllerCount);

        public static readonly string PlayerPrefabName = "Player";
        public static readonly string PlayerCreatorPrefabName = "PlayerCreator";
        public static readonly string CubePrefabName = "Cube";

        public static readonly string ControllerPrefabName = "Controller";
        public static readonly string DronePrefabName = "Drone";
        public static readonly string NfzNodePrefabName = "NfzNode";

        public static readonly float HeartbeatCheckIntervalSecs = 3;
        public static readonly uint TotalHeartbeatsBeforeTimeout = 3;
        public static readonly float HeartbeatSendingIntervalSecs = 3;

        public static readonly int TargetClientFramerate = 60;
        public static readonly int TargetServerFramerate = 60;
        public static readonly int FixedFramerate = 20;

        public static readonly float PlayerCreatorQueryRetrySecs = 4;
        public static readonly float PlayerEntityCreationRetrySecs = 4;

        public static readonly string DefaultSnapshotPath = Application.dataPath + "/../../../snapshots/default.snapshot";
    }
}

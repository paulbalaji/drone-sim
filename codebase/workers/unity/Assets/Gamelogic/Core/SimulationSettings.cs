using UnityEngine;
using Improbable;

namespace Assets.Gamelogic.Core
{
    public static class SimulationSettings
    {
        public static readonly float numDrones = 100;
        public static readonly float squareSize = 400;

        public static readonly float maxX = 15750; //31500m width
        public static readonly float maxZ = 7000; //14000m height

        //profiling, comment out when not used
        //public static readonly float maxX = 400; //31500m width
        //public static readonly float maxZ = 400; //14000m height

        // METRICS START //

        public static readonly float SchedulerMetricsInterval = 60f;
        public static readonly float ControllerMetricsInterval = 60f;

        // METRICS END //

        // CONTROLLER START //

        public static readonly float RequestHandlerInterval = 5f;

        public static readonly float MinimumDeliveryDistance = 250;

        public static readonly Vector3f ControllerArrivalOffset = new Vector3f(-75, 0, 75);
        public static readonly Vector3f ControllerDepartureOffset = new Vector3f(75, 0, -75);
        public static readonly Vector3f ControllerRunwayDelta = ControllerDepartureOffset - ControllerArrivalOffset;

        // CONTROLLER END //

        // DRONE CONSTANTS START //

        public const float DroneUpdateInterval = 1;

        public const float MaxDroneSpeed = 20;

        public const int MaxTargetRequestFailures = 5;

        public static float DroneRadius = 0.5f;

        public static float MaxRequestWaitTime = 10f;

        public static int DroneETAConstant = 3;

        // DRONE CONSTANTS END //

        // SCHEDULER CONSTANTS START //

        public static EntityId SchedulerEntityId = new EntityId(1);
        public static readonly float SchedulerInterval = 5f;

        // SCHEDULER CONSTANTS END //


        // GLOBAL LAYER START //

        public static readonly float ControllerUpdateInterval = 0.2f;
        public static readonly float ControllerWaitTime = 10f;
        public static readonly float DroneSpawnerSpacing = 2f;
        public static readonly float DroneSpawnInterval = DroneSpawnerSpacing * ControllerCount;

        public static readonly int BIT_SIZE = 25; // meters that each bit in the grid corresponds to, OG 25 in AATC 
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
        public static float RepulsionConst = 446.78f;
        //public static float RepulsionConst = 223.39f;
        //public static float RepulsionConst = 111.7f;
        public static float AttractionConst = 1.0038f;
        //public static float AttractionConst = 2.0076f;
        public static float InfuentialDistanceConstant = 532;
        //public static float InfuentialDistanceConstant = 133;
        public static float ReturnConstant = 0.689f;

        public const float SafeDistance = 10;

        // REACTIVE LAYER END //

        public static readonly uint ControllerRows = 2;
        public static readonly uint ControllerColumns = 2;
        public static readonly uint ControllerCount = ControllerRows * ControllerColumns;

        public static readonly uint MaxDroneCount = (uint) numDrones;
        public static readonly uint MaxDroneCountPerController = (uint) (MaxDroneCount / ControllerCount);

        public static readonly string PlayerPrefabName = "Player";
        public static readonly string PlayerCreatorPrefabName = "PlayerCreator";
        public static readonly string CubePrefabName = "Cube";

        public static readonly string ControllerPrefabName = "Controller";
        public static readonly string DronePrefabName = "Drone";
        public static readonly string NfzNodePrefabName = "NfzNode";
        public static readonly string SchedulerPrefabName = "Scheduler";

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

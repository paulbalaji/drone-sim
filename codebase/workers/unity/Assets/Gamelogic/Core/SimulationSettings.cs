using UnityEngine;
using Improbable;

namespace Assets.Gamelogic.Core
{
    public static class SimulationSettings
    {
        public static readonly uint ControllerRows = 2;
        public static readonly uint ControllerColumns = 2;
        public static readonly uint ControllerCount = ControllerRows * ControllerColumns;

        public static readonly float numDrones = 100;
        public static readonly float squareSize = 400;

        //London Large
        //public static readonly float maxX = 15750; //31500m width
        //public static readonly float maxZ = 7000; //14000m height
		//public static readonly float OrderGenerationInterval = 0.4f;
		//public static readonly uint MaxDroneCountPerController = 20;
        //public static readonly uint MaxDeliveryRequestQueueSize = 50;

		//London Small
		public static readonly float maxX = 2400; //4800m width
        public static readonly float maxZ = 1900; //3800m height
		public static readonly float OrderGenerationInterval = 4f;
		public static readonly uint MaxDroneCountPerController = 15;
        public static readonly uint MaxDeliveryRequestQueueSize = 40;

        //profiling, comment out when not used
        //public static readonly float maxX = 400; //31500m width
        //public static readonly float maxZ = 400; //14000m height

        // METRICS //

		public static readonly float OrderGenerationMetricsInterval = 60f;
        public static readonly float ControllerMetricsInterval = 60f;

        // ORDER GENERATOR CONSTANTS //

		public static EntityId OrderGeneratorEntityId = new EntityId(1);

		public static readonly int NumPackageTypes = 6;

		public static readonly float packageWeightSmallLetter = 8f;
        public static readonly float packageWeightLargeLetter = 25f;
        public static readonly float packageWeightSmallEnvelope = 20f;
        public static readonly float packageWeightStandardEnvelope = 40f;
        public static readonly float packageWeightLargeEnvelope = 40f;
        public static readonly float packageWeightStandardParcel = 100f;
        
		// CONTROLLER //

		public static readonly float DronePadRadius = 10f;

		//TODO: make this constant equal to the max time that a drone can last on one full charge
		public static readonly float DroneMapPruningInterval = 300f;

        public static readonly float RequestHandlerInterval = 10f; //5
        public static readonly float DroneSpawnInterval = 5f;

        public static readonly float MinimumDeliveryDistance = 250;

		public static readonly float MaximumTimePerWaypoint = 300f;

        public static readonly Vector3f ControllerArrivalOffset = new Vector3f(-75, 0, 75);
        public static readonly Vector3f ControllerDepartureOffset = new Vector3f(75, 0, -75);
        public static readonly Vector3f ControllerRunwayDelta = ControllerDepartureOffset - ControllerArrivalOffset;

		// MISC CONSTANTS //

		public const float CostPerWh = 0.0055f; //pence

		public const float DroneReplacementCost = 400; //pounds

		public const float KilometreToMiles = 0.621371f; //1 km = 0.621371 miles
		public const float TruckMilesPerGallon = 13.1f;
		public const float FuelCostPerGallon = 5.687f; //pounds
		public const float TruckCostConstant = 2 * KilometreToMiles * TruckMilesPerGallon * FuelCostPerGallon;

		// DRONE CONSTANTS //

        //Energy unit is Wh
		public const float DroneEnergyMove = DronePowerMove * DroneMoveInterval / 3600;
		public const float DroneEnergyHover = DronePowerHover * DroneMoveInterval / 3600;

		public const float DronePowerMove = 8355f;
		public const float DronePowerHover = 670f;

        public const float DroneUpdateInterval = 1f;
		public const float DroneMoveInterval = 0.25f;

        public const float MaxDroneSpeed = 10;

        public const int MaxTargetRequestFailures = 5;

        public static float DroneRadius = 0.5f;

        public static float MaxRequestWaitTime = 10f;

        public static int DroneETAConstant = 10;

		public const float MaxDroneBattery = 10000; // mAh battery

		public static float MaxDronePayload = MaxDronePayloadGrams / 1000; // kg
		public static float MaxDronePayloadGrams = 2300f; // g

        // GLOBAL LAYER //

        public static readonly float ControllerUpdateInterval = 0.2f;
        public static readonly float ControllerWaitTime = 10f;

        public static readonly int BIT_SIZE = 25; // meters that each bit in the grid corresponds to, OG 25 in AATC 
        public const int SIZE_OF_A_STEP = 1; // used when setting bits from a no fly zone

        public static readonly float RoutingShortCircuitThreshold = 50;

        public static readonly float MinimumDroneHeight = 10;
        public static readonly float SuggestedDroneHeight = 80;
        public static readonly float MaximumDroneHeight = 120;

        public static readonly int NFZ_PADDING_RAW = 50;
        public static readonly int NFZ_PADDING = NFZ_PADDING_RAW / BIT_SIZE;

        // REACTIVE LAYER //

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

        // GENERAL CONSTANTS //

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

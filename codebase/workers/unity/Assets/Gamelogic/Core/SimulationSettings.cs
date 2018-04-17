using UnityEngine;

namespace Assets.Gamelogic.Core
{
    public static class SimulationSettings
    {
        public static readonly float numDrones = 60;
        public static readonly float squareSize = 200;

        public static int BIT_SIZE = 5; // meters that each bit in the grid corresponds to
        public const int SIZE_OF_A_STEP = 1; // used when setting bits from a no fly zone

        public static readonly float RoutingShortCircuitThreshold = 50;

        public static readonly float MinimumDroneHeight = 10;
        public static readonly float SuggestedDroneHeight = 100;
        public static readonly float MaximumDroneHeight = 120;

        public static readonly uint ControllerCount = 1;

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

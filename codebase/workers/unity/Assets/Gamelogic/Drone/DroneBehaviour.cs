using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;

[WorkerType(WorkerPlatform.UnityWorker)]
public class DroneBehaviour : MonoBehaviour
{
    [Require]
    private Position.Writer PositionWriter;

    [Require]
    private DroneData.Writer DroneDataWriter;

    private bool simulate = false;

    private Vector3 target;
    private float speed;
    private float radius;
    private Vector3 direction;

    private float nextRequestTime = 0f;

    private float latestArrivalTime = 0f;

    private APF apf;

    private int failCount = 0;

    private void OnEnable()
    {
        //register for direction/speed updates
        DroneDataWriter.TargetUpdated.Add(OnTargetUpdate);
        DroneDataWriter.DirectionUpdated.Add(OnDirectionUpdate);

        //get latest component values
        target = DroneDataWriter.Data.target.ToVector3();
        speed = DroneDataWriter.Data.speed;
        radius = speed * SimulationSettings.DroneUpdateInterval;

        apf = gameObject.GetComponent<APF>();

        simulate = true;

        UnityEngine.Random.InitState((int)gameObject.EntityId().Id);
        InvokeRepeating("DroneTick", Random.Range(0, SimulationSettings.DroneUpdateInterval), SimulationSettings.DroneUpdateInterval);
    }

    private void OnDisable()
    {
        simulate = false;

        //deregister for direction/speed updates
        DroneDataWriter.TargetUpdated.Remove(OnTargetUpdate);
    }

    private void OnTargetUpdate(Vector3f newTarget)
    {
        target = newTarget.ToVector3();
    }

    void OnDirectionUpdate(Vector3f newDirection)
    {
        direction = newDirection.ToUnityVector();
    }

    void DroneTick()
	{
        if (simulate)
        {
            SendPositionUpdate();

            if (DroneDataWriter.Data.droneStatus == DroneStatus.MOVE)
            {
                apf.Recalculate();
            }

            if (DroneDataWriter.Data.targetPending == TargetPending.WAITING)
            {
                requestNewTarget();
            }

            float distanceToTarget = Vector3.Distance(target, transform.position);
            if (DroneDataWriter.Data.targetPending == TargetPending.REQUEST || distanceToTarget < radius)
            {
                requestNewTarget();
            }
        }
	}

	private void FixedUpdate()
	{
        if (simulate)
        {
            if (DroneDataWriter.Data.droneStatus == DroneStatus.MOVE)
            {
                transform.position += direction * DroneDataWriter.Data.speed * Time.fixedDeltaTime;
            }
        }
	}

    private void requestNewTarget()
    {
        //Debug.LogWarning("requesting new target");

        if (Time.time < nextRequestTime)
        {
            return;
        }

        nextRequestTime = Time.time + SimulationSettings.MaxRequestWaitTime;

        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.WAITING));
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            Controller.Commands.RequestNewTarget.Descriptor,
            new TargetRequest(gameObject.EntityId()),
            DroneDataWriter.Data.designatedController)
                 .OnSuccess((response) => requestTargetSuccess(response))
                 .OnFailure((response) => requestTargetFailure(response.ErrorMessage));
    }

    void requestTargetSuccess(TargetResponse response)
    {
        if (response.success == TargetResponseCode.WRONG_CONTROLLER)
        {
            requestTargetFailure("Command sent to wrong controller.");
            return;
        }

        failCount = 0;

        if (response.success == TargetResponseCode.JOURNEY_COMPLETE)
        {
            //Debug.LogWarning("Drone has completed its journey. Shutting Down.");
            simulate = false;
            return;
        }

        //Debug.LogWarning("DRONE New Target Received");
        latestArrivalTime = Time.time + (SimulationSettings.DroneETAConstant * Vector3.Distance(transform.position, response.newTarget.ToUnityVector()) / speed);

        DroneDataWriter.Send(new DroneData.Update()
                             .SetPreviousTarget(DroneDataWriter.Data.target)
                             .SetTarget(response.newTarget)
                             .SetTargetPending(TargetPending.RECEIVED)
                             .SetDroneStatus(DroneStatus.MOVE));
    }

    private void requestTargetFailure(string errorMessage)
    {
        failCount++;
        Debug.LogError("Failed to request new target. Fail #" + failCount + " with error: " + errorMessage);

        if (failCount > SimulationSettings.MaxTargetRequestFailures)
        {
            Debug.LogError("Too many failures. Drone Self-Destructing.");
            SelfDestruct();
        }

        DroneDataWriter.Send(new DroneData.Update()
                             .SetTargetPending(TargetPending.REQUEST)
                             .SetDroneStatus(DroneStatus.HOVER));
    }

    private void SelfDestruct()
    {
        SpatialOS.Commands.DeleteEntity(PositionWriter, transform.gameObject.EntityId());
    }

    private void SendPositionUpdate()
    {
        //Debug.LogWarning("update position function");
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }
}

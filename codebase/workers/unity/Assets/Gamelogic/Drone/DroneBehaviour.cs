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
        //register command
        DroneDataWriter.CommandReceiver.OnReceiveNewTarget.RegisterAsyncResponse(ReceivedNewTarget);

        //register for direction/speed updates
        DroneDataWriter.TargetUpdated.Add(OnTargetUpdate);
        DroneDataWriter.DirectionUpdated.Add(OnDirectionUpdate);

        //get latest component values
        target = DroneDataWriter.Data.target.ToVector3();
        speed = DroneDataWriter.Data.speed;
        radius = speed * SimulationSettings.DroneUpdateInterval;

        apf = gameObject.GetComponent<APF>();

        simulate = true;

        InvokeRepeating("DroneTick", SimulationSettings.DroneUpdateInterval, SimulationSettings.DroneUpdateInterval);
    }

    private void OnDisable()
    {
        simulate = false;

        //deregister command
        DroneDataWriter.CommandReceiver.OnReceiveNewTarget.DeregisterResponse();

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
            if (DroneDataWriter.Data.droneStatus == DroneStatus.MOVE)
            {
                SendPositionUpdate();
                apf.Recalculate();
            }

            if (DroneDataWriter.Data.targetPending == TargetPending.WAITING && Time.time > nextRequestTime)
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
        if (DroneDataWriter.Data.droneStatus == DroneStatus.MOVE)
        {
            transform.position += direction * DroneDataWriter.Data.speed * Time.fixedDeltaTime;
        }
	}

	//private void requestNewFlightPlan()
    //{
    //    SpatialOS.Commands.SendCommand(
    //        PositionWriter,
    //        Controller.Commands.RegeneratePath.Descriptor,
    //        new RegenPathRequest(
    //            gameObject.EntityId(),
    //            transform.position.ToSpatialVector3f()),
    //        DroneDataWriter.Data.designatedController)
    //             .OnFailure((response) => Debug.LogError("Unable to request new path for Drone ID: " + gameObject.EntityId()));

    //    DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.WAITING));
    //}

    private void requestNewTarget()
    {
        //Debug.LogWarning("requesting new target");

        nextRequestTime = Time.time + SimulationSettings.MaxRequestWaitTime;

        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.WAITING));
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            Controller.Commands.RequestNewTarget.Descriptor,
            new TargetRequest(
                gameObject.EntityId(),
                transform.position.ToSpatialVector3f()),
            DroneDataWriter.Data.designatedController)
                 .OnFailure((response) => requestTargetFailure(response.ErrorMessage));
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

        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.REQUEST));
    }

    private void SelfDestruct()
    {
        SpatialOS.Commands.DeleteEntity(PositionWriter, transform.gameObject.EntityId());
    }

    void ReceivedNewTarget(Improbable.Entity.Component.ResponseHandle<DroneData.Commands.ReceiveNewTarget, NewTargetRequest, NewTargetResponse> handle)
    {
        handle.Respond(new NewTargetResponse());
        if (handle.Request.target.y < 0)
        {
            requestTargetFailure("Controller failed to pathfind.");
            return;
        }

        //Debug.LogWarning("DRONE New Target Received");
        latestArrivalTime = Time.time + (SimulationSettings.DroneETAConstant * Vector3.Distance(transform.position, handle.Request.target.ToUnityVector()) / speed);

        DroneDataWriter.Send(new DroneData.Update()
                             .SetPreviousTarget(DroneDataWriter.Data.target)
                             .SetTarget(handle.Request.target)
                             .SetTargetPending(TargetPending.RECEIVED)
                             .SetDroneStatus(DroneStatus.MOVE));
    }

    private void SendPositionUpdate()
    {
        //Debug.LogWarning("update position function");
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }
}

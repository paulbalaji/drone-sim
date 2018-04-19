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

    private float nextActionTime = 0f;

    private APF apf;

    private void OnEnable()
    {
        //register command
        DroneDataWriter.CommandReceiver.OnReceiveNewTarget.RegisterAsyncResponse(ReceivedNewTarget);

        //register for direction/speed updates
        DroneDataWriter.TargetUpdated.Add(OnTargetUpdate);

        //get latest component values
        target = DroneDataWriter.Data.target.ToVector3();
        speed = DroneDataWriter.Data.speed;
        radius = speed * SimulationSettings.DroneUpdateInterval;

        apf = gameObject.GetComponent<APF>();

        simulate = true;
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

	private void Start()
	{
        InvokeRepeating("DroneTick", SimulationSettings.DroneUpdateInterval, SimulationSettings.DroneUpdateInterval);
	}

    void DroneTick()
	{
        if (simulate && DroneDataWriter.Data.targetPending != TargetPending.WAITING)
        {
            float distanceToTarget = Vector3.Distance(target, transform.position);
            if (DroneDataWriter.Data.targetPending == TargetPending.REQUEST || distanceToTarget < radius)
            {
                requestNewTarget();
            }
            else
            {
                apf.Recalculate();
            }
        }
	}

    private void requestNewTarget()
    {
        //Debug.LogWarning("requesting new target");

        Improbable.Collections.Option<Vector3f> requestTarget = new Improbable.Collections.Option<Vector3f>();

        if (DroneDataWriter.Data.snapshot)
        {
            requestTarget = new Improbable.Collections.Option<Vector3f>(DroneDataWriter.Data.target);
        }

        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.WAITING));
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            Controller.Commands.RequestNewTarget.Descriptor,
            new TargetRequest(
                gameObject.EntityId(),
                transform.position.ToSpatialVector3f(),
                requestTarget),
            new EntityId(1))
                 .OnFailure((response) => requestTargetFailure(response.ErrorMessage));
    }

    private void requestTargetFailure(string errorMessage)
    {
        Debug.LogError("Failed to request new target, with error: " + errorMessage);
        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.REQUEST));
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

        DroneDataWriter.Send(new DroneData.Update()
                             .SetTarget(handle.Request.target)
                             .SetTargetPending(TargetPending.RECEIVED)
                             .SetSnapshot(false));
    }

    private void updatePosition()
    {
        //Debug.LogWarning("update position function");
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }
}

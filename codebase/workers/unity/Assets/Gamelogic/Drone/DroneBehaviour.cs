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

    private void OnEnable()
    {
        //register command
        DroneDataWriter.CommandReceiver.OnReceiveNewTarget.RegisterAsyncResponse(ReceivedNewTarget);

        //register for direction/speed updates
        DroneDataWriter.TargetUpdated.Add(OnTargetUpdate);
        DroneDataWriter.SpeedUpdated.Add(OnSpeedUpdated);
        DroneDataWriter.RadiusUpdated.Add(OnRadiusUpdated);

        //get latest component values
        target = DroneDataWriter.Data.target.ToVector3();
        speed = DroneDataWriter.Data.speed;
        radius = DroneDataWriter.Data.radius;

        simulate = true;
    }

    private void OnDisable()
    {
        simulate = false;

        //deregister command
        DroneDataWriter.CommandReceiver.OnReceiveNewTarget.DeregisterResponse();

        //deregister for direction/speed updates
        DroneDataWriter.TargetUpdated.Remove(OnTargetUpdate);
        DroneDataWriter.SpeedUpdated.Remove(OnSpeedUpdated);
        DroneDataWriter.RadiusUpdated.Remove(OnRadiusUpdated);
    }

    private void OnTargetUpdate(Vector3f newTarget)
    {
        target = newTarget.ToVector3();
    }

    private void OnSpeedUpdated(float newSpeed)
    {
        speed = newSpeed;
    }

    private void OnRadiusUpdated(float newRadius)
    {
        radius = newRadius;
    }

	private void Start()
	{
        InvokeRepeating("DroneTick", SimulationSettings.DroneUpdateInterval, SimulationSettings.DroneUpdateInterval);
	}

    void DroneTick()
	{
        if (simulate && DroneDataWriter.Data.targetPending != TargetPending.WAITING)
        {
            Vector3 direction = target - transform.position;
            float distance = direction.magnitude;

            if (DroneDataWriter.Data.targetPending == TargetPending.REQUEST || direction.magnitude < radius)
            {
                requestNewTarget();
            }
            else
            {
                direction.Normalize();
                transform.position += direction * Mathf.Max(speed, distance) * SimulationSettings.DroneUpdateInterval;
                updatePosition();
            }
        }
	}

    private void requestNewTarget()
    {
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

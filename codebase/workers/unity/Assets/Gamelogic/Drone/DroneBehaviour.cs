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

    Vector3 target;
    float speed;
    float radius;

    private void OnEnable()
    {
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

	void FixedUpdate()
	{
        if (simulate && DroneDataWriter.Data.targetPending != TargetPending.WAITING)
        {
            if (DroneDataWriter.Data.targetPending == TargetPending.REQUEST || withinTargetRange(target)) {
                requestNewTarget();
            } else {
                Vector3 direction = target - transform.position;
                direction.Normalize();
                transform.position += direction * speed * Time.deltaTime;
                updatePosition();
            }
        }
	}

    private bool withinTargetRange(Vector3 target)
    {
        //Debug.LogError("target range function");
        return Mathf.Pow(transform.position.x - target.x, 2) + Mathf.Pow(transform.position.z - target.z, 2) < Mathf.Pow(radius, 2);
    }

    private void requestNewTarget()
    {
        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.WAITING));
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            Controller.Commands.RequestNewTarget.Descriptor,
            new TargetRequest(gameObject.EntityId(), transform.position.ToSpatialVector3f()), new EntityId(1))
                 .OnSuccess((TargetResponse response) => requestTargetSuccess(response.target))
                 .OnFailure((response) => requestTargetFailure(response.ErrorMessage));
    }

    private void requestTargetSuccess(Vector3f newTarget)
    {
        //Debug.LogError("update target function");
        DroneDataWriter.Send(new DroneData.Update().SetTarget(newTarget).SetTargetPending(TargetPending.RECEIVED));
    }

    private void requestTargetFailure(string errorMessage)
    {
        Debug.LogError("Failed to request new target, with error: " + errorMessage);
        DroneDataWriter.Send(new DroneData.Update().SetTargetPending(TargetPending.REQUEST));
    }

    private void updatePosition()
    {
        //Debug.LogError("update position function");
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }
}

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
        if (simulate)
        {
            if (withinTargetRange(DroneDataWriter.Data.target.ToVector3())) {
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
        return Mathf.Pow(transform.position.x - target.x, 2) + Mathf.Pow(transform.position.z - target.z, 2) < Mathf.Pow(radius, 2);
    }

    private void requestNewTarget()
    {
        SpatialOS.Commands.SendCommand(PositionWriter, Controller.Commands.SetNewTarget.Descriptor, new TargetRequest(), new EntityId(1))
                 .OnSuccess((TargetResponse response) => updateTarget(response.target))
                 .OnFailure((response) => Debug.LogError("Failed to request new target, with error: " + response.ErrorMessage));
    }

    private void updateTarget(Vector3f newTarget)
    {
        DroneDataWriter.Send(new DroneData.Update().SetTarget(newTarget));
    }

    private void updatePosition()
    {
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }
}

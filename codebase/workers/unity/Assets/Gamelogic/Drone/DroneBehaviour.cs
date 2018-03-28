using Improbable;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

namespace Assets.Gamelogic.Drone
{
    [WorkerType(WorkerPlatform.UnityWorker)]
    public class DroneBehaviour : MonoBehaviour
    {
        [Require]
        private Position.Writer PositionWriter;

        [Require]
        private DroneData.Writer DroneDataWriter;

        private bool simulate = false;

        private void OnEnable()
        {
            //register for direction/speed updates
            DroneDataWriter.TargetUpdated.Add(OnTargetUpdate);
            DroneDataWriter.SpeedUpdated.Add(OnSpeedUpdated);

            simulate = true;
        }

        private void OnDisable()
        {
            simulate = false;

            //deregister for direction/speed updates
            DroneDataWriter.TargetUpdated.Remove(OnTargetUpdate);
            DroneDataWriter.SpeedUpdated.Remove(OnSpeedUpdated);
        }

        private void OnTargetUpdate(Vector3f newTarget)
        {
            
        }

        private void OnSpeedUpdated(float speed)
        {
            
        }

		void FixedUpdate()
		{
            if (simulate)
            {
                Vector3 target = DroneDataWriter.Data.target.ToVector3();
                Vector3 current = transform.position;

                if (Mathf.Pow(current.x - target.x, 2) + Mathf.Pow(current.z - target.z, 2) < Mathf.Pow(0.5f, 2)) {
                    updateTarget();
                } else {
                    Vector3 direction = target - current;
                    direction.Normalize();
                    transform.position += direction * DroneDataWriter.Data.speed * Time.deltaTime;
                    updatePosition();
                }
            }
		}

        private void updateTarget()
        {
            DroneDataWriter.Send(new DroneData.Update().SetTarget(new Vector3f(Random.Range(-10, 10), 0, Random.Range(-10, 10))));
        }

        private void updatePosition()
        {
            PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
        }
	}

    public static class Vector3Extensions
    {
        public static Coordinates ToCoordinates(this Vector3 vector3)
        {
            return new Coordinates(vector3.x, vector3.y, vector3.z);
        }

        public static Vector3 ToVector3(this Vector3f vector3f)
        {
            return new Vector3(vector3f.x, vector3f.y, vector3f.z);
        }
    }
}

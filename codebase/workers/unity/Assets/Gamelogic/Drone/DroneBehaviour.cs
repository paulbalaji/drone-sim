using Improbable;
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

        private Vector3 target = new Vector3(0, 0, 0);
        private float speed = 0.5f;

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

		void FixedUpdate()
		{
            Vector3 direction = target - transform.position;
            direction.Normalize();

            transform.position += direction * speed * Time.deltaTime;
            //Debug.Log("fixed update: " + direction + "");

            updatePosition();
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
    }
}

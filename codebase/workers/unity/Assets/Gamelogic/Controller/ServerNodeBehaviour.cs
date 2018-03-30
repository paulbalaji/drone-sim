using Improbable;

using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

namespace Assets.Gamelogic.Drone
{
    [WorkerType(WorkerPlatform.UnityWorker)]
    public class ServerNodeBehaviour : MonoBehaviour
    {
        private void OnEnable()
        {
            //register for updates
        }

        private void OnDisable()
        {
            //deregister for updates
        }

        void FixedUpdate()
        {
            
        }
    }
}

using Improbable;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

namespace Assets.Gamelogic.Controller
{
    [WorkerType(WorkerPlatform.UnityWorker)]
    public class Controller : MonoBehaviour
    {

        private void OnEnable()
        {
            //register stuff
        }

        private void OnDisable()
        {
            //deregister stuff
        }

        void FixedUpdate()
        {
            //do stuff
        }
    }
}

using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Visualizer;
using UnityEngine;

[WorkerType(WorkerPlatform.UnityWorker)]
public class ControllerBehaviour : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    private void OnEnable()
    {
        //register stuff
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);
    }

    private void OnDisable()
    {
        //deregister stuff
        ControllerWriter.CommandReceiver.OnRequestNewTarget.RegisterAsyncResponse(CalculateNewTarget);
    }

    void CalculateNewTarget(Improbable.Entity.Component.ResponseHandle<Controller.Commands.RequestNewTarget, TargetRequest, TargetResponse> handle)
    {
        //calculate new target here
        float size = SimulationSettings.squareSize;
        Vector3f newTarget = new Vector3f(Random.Range(-size, size), 0, Random.Range(-size, size));

        //Debug.LogError("returning new target");

        //respond to drone with new target
        handle.Respond(new TargetResponse(newTarget));
    }

    void FixedUpdate()
    {
        //do stuff
    }
}

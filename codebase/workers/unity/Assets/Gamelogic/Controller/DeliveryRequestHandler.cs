using Assets.Gamelogic.Core;
using Improbable;
using Improbable.Drone;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using System;
using System.Collections.Generic;
using UnityEngine;

[WorkerType(WorkerPlatform.UnityWorker)]
public class DeliveryRequestHandler : MonoBehaviour
{
    [Require]
    private Controller.Writer ControllerWriter;

    [Require]
    private DeliveryHandler.Writer DeliveryHandlerWriter;

    DroneTranstructor droneTranstructor;

    Queue<DeliveryRequest> queue;

	private void Start()
	{
        InvokeRepeating("DeliveryHandlerTick", SimulationSettings.RequestHandlerInterval, SimulationSettings.RequestHandlerInterval);
	}

	private void OnEnable()
    {
        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();

        queue = new Queue<DeliveryRequest>(DeliveryHandlerWriter.Data.requestQueue);

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(EnqueueDeliveryRequest);

        UnityEngine.Random.InitState((int) gameObject.EntityId().Id);
    }

	private void OnDisable()
	{
        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

    void UpdateRequestQueue()
    {
        DeliveryHandlerWriter.Send(new DeliveryHandler.Update().SetRequestQueue(new Improbable.Collections.List<DeliveryRequest>(queue.ToArray())));
    }

	void EnqueueDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        handle.Respond(new DeliveryResponse());
        queue.Enqueue(handle.Request);
    }

    void DeliveryHandlerTick()
    {
        if (ControllerWriter.Data.initialised)
        {
            if (queue.Count > 0)
            {
                DeliveryRequest request = queue.Dequeue();
                droneTranstructor.CreateDrone(
                    transform.position.ToCoordinates(),
                    request.destination,
                    SimulationSettings.MaxDroneSpeed);
            }
        }
    }
}

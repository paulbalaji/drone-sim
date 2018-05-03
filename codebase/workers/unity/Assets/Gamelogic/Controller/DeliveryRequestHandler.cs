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

    private void OnEnable()
    {
        droneTranstructor = gameObject.GetComponent<DroneTranstructor>();

        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.RegisterAsyncResponse(HandleDeliveryRequest);

        UnityEngine.Random.InitState((int) gameObject.EntityId().Id);
    }

	private void OnDisable()
	{
        DeliveryHandlerWriter.CommandReceiver.OnRequestDelivery.DeregisterResponse();
	}

	void HandleDeliveryRequest(Improbable.Entity.Component.ResponseHandle<DeliveryHandler.Commands.RequestDelivery, DeliveryRequest, DeliveryResponse> handle)
    {
        handle.Respond(new DeliveryResponse());

        if (ControllerWriter.Data.initialised)
        {
            droneTranstructor.CreateDrone(
                transform.position.ToCoordinates(),
                handle.Request.destination,
                SimulationSettings.MaxDroneSpeed);
        }
    }
}

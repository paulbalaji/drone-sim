using System;
using System.Collections.Generic;
using Assets.Gamelogic.Core;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;

public class ReactiveLayerBehaviour : MonoBehaviour
{
    [Require]
    private ReactiveLayer.Writer ReactiveLayerWriter;

    private Bitmap bitmap;

	private void OnEnable()
	{
        ReactiveLayerWriter.CommandReceiver.OnGetNearestObstacle.RegisterAsyncResponse(GetNearestObstacle);

        bitmap = gameObject.GetComponent<Bitmap>();
	}

    void GetNearestObstacle(Improbable.Entity.Component.ResponseHandle<ReactiveLayer.Commands.GetNearestObstacle, ObstacleRequest, ObstacleResponse> handle)
    {
        Vector3f nearestNoFlyZone = bitmap.nearestNoFlyZonePoint(handle.Request.location);
        APFObstacleType obstacleType = APFObstacleType.NO_FLY_ZONE;
        if (nearestNoFlyZone.y < 0)
        {
            obstacleType = APFObstacleType.NONE;
        }
        handle.Respond(new ObstacleResponse(new APFObstacle(obstacleType, nearestNoFlyZone)));
    }
}

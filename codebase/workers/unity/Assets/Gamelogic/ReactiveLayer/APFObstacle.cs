using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Collections;
using Improbable.Controller;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Core;
using UnityEngine;
using Assets.Gamelogic.Core;

public class APFObstacle
{
    public APFObstacleType type;
    public float distance;

    public APFObstacle(APFObstacleType type, float distance)
    {
        this.type = type;
        this.distance = distance;
    }

    public APFObstacleType getType()
    {
        return type;
    }

    public float getDistance()
    {
        return type == APFObstacleType.NONE ? float.PositiveInfinity : distance;
    }
}
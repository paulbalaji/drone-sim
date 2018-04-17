using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Collections;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Core;
using UnityEngine;
using Assets.Gamelogic.Core;

public class APF
{
    public static float RepulsionConst = SimulationSettings.RepulsionConst;

    public static float AttractionConst = SimulationSettings.AttractionConst;

    public static float VelocityConst = SimulationSettings.VelocityConst;

    public static float p0 = SimulationSettings.p0;

    public static float ReturnConstant = SimulationSettings.ReturnConstant;

    private static Vector3f calculateGradient(Vector3f dp, Vector3f lp, Vector3f goal)
    {
        float potentialAtDrone = calculateTotalPotential(dp, lp, goal);
        Vector3f xDpos = new Vector3f(dp.x + 1, dp.y, dp.z);
        Vector3f yDpos = new Vector3f(dp.x, dp.y + 1, dp.z);
        Vector3f zDpos = new Vector3f(dp.x, dp.y, dp.z + 1);

        float xD = calculateTotalPotential(xDpos, lp, goal) - potentialAtDrone;
        float yD = calculateTotalPotential(yDpos, lp, goal) - potentialAtDrone;
        float zD = calculateTotalPotential(zDpos, lp, goal) - potentialAtDrone;
        // TODO: Add modifying factor here in order to disencourage changes in altitude

        return new Vector3f(xD, yD, zD);
    }

    private static float calculateTotalPotential(Vector3f dronePosition, Vector3f lastPosition, Vector3f goal)
    {
        //Environment env = Environment.GetInstance();
        // TODO: Pass in environment in some way rather than getting it here.

        //Calculate uAttract = pAttract * dGoal
        float distanceToGoal = Vector3.Distance(goal.ToUnityVector(), dronePosition.ToUnityVector());
        float uAttract = AttractionConst * distanceToGoal;

        //Calculate uRepel = dObst <= dInfluence ? (pRepel / dObst-dSafe) : 0
        //APFObstacle nearestObstacle = env.distanceToNearestObstacleFrom(drone, dronePosition);
        APFObstacle nearestObstacle = new APFObstacle(0, 0);
        float distanceToNearestObstacle = nearestObstacle.getDistance();
        float uRepel = distanceToNearestObstacle <= p0
            ? Mathf.Pow(RepulsionConst / (distanceToNearestObstacle - SimulationSettings.SafeDistance), 2)
            : 0;

        //Calculate uReturn = pReturn * dLast
        float distanceToLastPosition = Vector3.Distance(lastPosition.ToUnityVector(), dronePosition.ToUnityVector());
        float uReturn = ReturnConstant * distanceToLastPosition;

        return uAttract + uRepel + uReturn;
    }

    public static Vector3f recommendVelocity(Vector3f currentPosition, Vector3f lastPosition, Vector3f goal)
    {
        // recommended velocity = -1 * normalized gradient
        // remember: we want to go against the gradient
        return -1 * calculateGradient(currentPosition, lastPosition, goal).Normalized();
    }
}
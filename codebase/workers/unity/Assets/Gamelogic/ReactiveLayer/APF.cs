using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Collections;
using Improbable.Drone;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using UnityEngine;
using Assets.Gamelogic.Core;

public class APF : MonoBehaviour
{
    [Require]
    private DroneData.Writer DroneDataWriter;

    [Require]
    private Position.Writer PositionWriter;

    public static float RepulsionConst;

    public static float AttractionConst;

    public static float InfuentialDistanceConstant;

    public static float ReturnConstant;

    private APFObstacle dummyObstacle;

    private float safeDistance;

    private bool droneCalcReady;

    private APFObstacle nearestStaticObstacle;
    private APFObstacle nearestDrone;
    private float nearestDroneDistance;

	private void OnEnable()
	{
        RepulsionConst = SimulationSettings.RepulsionConst;
        AttractionConst = SimulationSettings.AttractionConst;
        InfuentialDistanceConstant = SimulationSettings.InfuentialDistanceConstant;
        ReturnConstant = SimulationSettings.ReturnConstant;
        droneCalcReady = false;
        dummyObstacle = new APFObstacle(APFObstacleType.NONE, new Vector3f(0, -1, 0));
        nearestDrone = new APFObstacle(APFObstacleType.DRONE, new Vector3f(0, -1, 0));

        safeDistance = DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
        nearestDroneDistance = 2 * safeDistance;
	}

    private Vector3 calculateGradient()
    {
        Vector3f dp = transform.position.ToSpatialVector3f();
        Vector3f goal = DroneDataWriter.Data.target;

        float potentialAtDrone = calculateTotalPotential(dp, goal);
        Vector3f xDpos = new Vector3f(dp.x + 1, dp.y, dp.z);
        Vector3f yDpos = new Vector3f(dp.x, dp.y + 1, dp.z);
        Vector3f zDpos = new Vector3f(dp.x, dp.y, dp.z + 1);

        float xD = calculateTotalPotential(xDpos, goal) - potentialAtDrone;
        float yD = calculateTotalPotential(yDpos, goal) - potentialAtDrone;
        float zD = calculateTotalPotential(zDpos, goal) - potentialAtDrone;
        // TODO: Add modifying factor here in order to disencourage changes in altitude

        return new Vector3(xD, yD, zD);
    }

    private float calculateTotalPotential(Vector3f dronePosition, Vector3f goal)
    {
        APFObstacle nearestObstacle = GetNearestObstacle(dronePosition.ToUnityVector());

        //Calculate uAttract = pAttract * dGoal
        float distanceToGoal = Vector3.Distance(goal.ToUnityVector(), dronePosition.ToUnityVector());
        float uAttract = AttractionConst * distanceToGoal;

        float uRepel;
        if (nearestObstacle.type == APFObstacleType.NONE)
        {
            uRepel = 0;
        }
        else
        {
            float distanceToNearestObstacle = Vector3.Distance(dronePosition.ToUnityVector(), nearestObstacle.position.ToUnityVector());
            uRepel = distanceToNearestObstacle < InfuentialDistanceConstant
                ? uRepel = RepulsionConst / (distanceToNearestObstacle - safeDistance)
                : 0;
        }

        Vector3f previousTarget = DroneDataWriter.Data.previousTarget;
        float uRet = previousTarget.y < 0
           ? 0
           : ReturnConstant * Vector3.Distance(dronePosition.ToUnityVector(), DroneDataWriter.Data.previousTarget.ToUnityVector());

        return uAttract + uRepel + uRet;
    }

    private void CheckForNearbyDrones(Vector3 dronePosition)
    {
        nearestDrone.type = APFObstacleType.NONE;
        nearestDrone.position = new Vector3f(0, -1, 0);
        nearestDroneDistance = safeDistance;

        Collider[] hitColliders = Physics.OverlapSphere(dronePosition, safeDistance);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.EntityId() != gameObject.EntityId())
            {
                if (Vector3.Distance(hitCollider.transform.position, dronePosition) < nearestDroneDistance)
                {
                    nearestDrone.position = hitCollider.transform.position.ToSpatialVector3f();
                    nearestDrone.type = APFObstacleType.DRONE;
                }
            }
        }
    }

    private APFObstacle GetNearestObstacle(Vector3 dronePosition)
    {
        // check for nearest dynamic obstacle, currently just looking for other drones
        CheckForNearbyDrones(dronePosition);

        if (nearestStaticObstacle.type == APFObstacleType.NONE)
        {
            if (nearestDrone.type == APFObstacleType.NONE)
            {
                // no nearby obstacles so use whatever empty info you want
                return nearestStaticObstacle;
            }
            else
            {
                // only drone nearby so use drone info
                //Debug.LogError("using drone as nearest obstacle (not an error)");
                return nearestDrone;
            }
        }
        else
        {
            // if obstacle is NFZ, update obstacle height to match drone
            if (nearestStaticObstacle.type == APFObstacleType.NO_FLY_ZONE)
            {
                nearestStaticObstacle.position.y = dronePosition.y;
            }

            if (nearestDrone.type == APFObstacleType.NONE)
            {
                // obstacle but no drone, so send obstacle info
                return nearestStaticObstacle;
            }
            else
            {
                // both obstacle and drone exist, so send info of whichever is closest to self
                if (Vector3.Distance(dronePosition, nearestStaticObstacle.position.ToUnityVector()) > nearestDroneDistance)
                {
                    // if distance to obstacle > distance to nearest drone, send drone info
                    //Debug.LogError("using drone as nearest obstacle (not an error)");
                    return nearestDrone;
                }
                else
                {
                    // distance to obstacle < distance to nearest drone, send obstacle info
                    return nearestStaticObstacle;
                }
            }
        }
    }

    private void MoveDrone(APFObstacle obstacle)
    {
        nearestStaticObstacle = obstacle;
        Vector3 direction = -1 * calculateGradient().normalized;
        transform.position += direction * DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }

    private void BackupMove()
    {
        MoveDrone(dummyObstacle);
    }

    public void Recalculate()
    {
        //get nearest static obstacle, currently just NFZs
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            ReactiveLayer.Commands.GetNearestObstacle.Descriptor,
            new ObstacleRequest(transform.position.ToSpatialVector3f()),
            new EntityId(1))
                 .OnSuccess((response) => MoveDrone(response.obstacle))
                 .OnFailure((response) => BackupMove());
    }
}
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

    public static float p0;

    public static float ReturnConstant;

    private APFObstacle dummyObstacle;

    private float safeDistance;

    private bool droneCalcReady;

    private APFObstacle nearestDrone;
    private float nearestDroneDistance;

	private void OnEnable()
	{
        RepulsionConst = SimulationSettings.RepulsionConst;
        AttractionConst = SimulationSettings.AttractionConst;
        p0 = SimulationSettings.p0;
        ReturnConstant = SimulationSettings.ReturnConstant;
        droneCalcReady = false;
        dummyObstacle = new APFObstacle(APFObstacleType.NONE, new Vector3f(0, -1, 0));
        nearestDrone = new APFObstacle(APFObstacleType.DRONE, new Vector3f(0, -1, 0));

        safeDistance = DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
        nearestDroneDistance = 2 * safeDistance;
	}

    private Vector3 calculateGradient(APFObstacle obstacle)
    {
        Vector3f dp = transform.position.ToSpatialVector3f();
        Vector3f goal = DroneDataWriter.Data.target;

        float potentialAtDrone = calculateTotalPotential(dp, goal, obstacle);
        Vector3f xDpos = new Vector3f(dp.x + 1, dp.y, dp.z);
        Vector3f yDpos = new Vector3f(dp.x, dp.y + 1, dp.z);
        Vector3f zDpos = new Vector3f(dp.x, dp.y, dp.z + 1);

        float xD = calculateTotalPotential(xDpos, goal, obstacle) - potentialAtDrone;
        float yD = calculateTotalPotential(yDpos, goal, obstacle) - potentialAtDrone;
        float zD = calculateTotalPotential(zDpos, goal, obstacle) - potentialAtDrone;
        // TODO: Add modifying factor here in order to disencourage changes in altitude

        return new Vector3(xD, yD, zD);
    }

    private float calculateTotalPotential(Vector3f dronePosition, Vector3f goal, APFObstacle nearestObstacle)
    {
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
            uRepel = distanceToNearestObstacle < safeDistance
                ? uRepel = RepulsionConst / (distanceToNearestObstacle - safeDistance)
                : 0;
        }

        float uRet = ReturnConstant * Vector3.Distance(transform.position, DroneDataWriter.Data.previousTarget.ToUnityVector());

        return uAttract + uRepel + uRet;
    }

    private void MoveDrone(APFObstacle obstacle)
    {
        Vector3 gradient = new Vector3();

        //get nearest dynamic obstacle, currently just other drones
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, safeDistance);
        nearestDrone.type = APFObstacleType.NONE;
        nearestDrone.position = new Vector3f(0, -1, 0);
        nearestDroneDistance = safeDistance;
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.EntityId() != gameObject.EntityId())
            {
                if (Vector3.Distance(hitCollider.transform.position, transform.position) < nearestDroneDistance)
                {
                    nearestDrone.position = hitCollider.transform.position.ToSpatialVector3f();
                    nearestDrone.type = APFObstacleType.DRONE;
                }
            }
        }

        if (obstacle.type == APFObstacleType.NONE)
        {
            if (nearestDrone.type == APFObstacleType.NONE)
            {
                // no nearby obstacles so use whatever empty info you want
                gradient = calculateGradient(obstacle).normalized;
            }
            else
            {
                // only drone nearby so use drone info
                Debug.LogError("using drone as nearest obstacle (not an error)");
                gradient = calculateGradient(nearestDrone).normalized;
            }
        }
        else
        {
            // if obstacle is NFZ, update obstacle height to match drone
            if (obstacle.type == APFObstacleType.NO_FLY_ZONE)
            {
                obstacle.position.y = transform.position.y;
            }

            if (nearestDrone.type == APFObstacleType.NONE)
            {
                // obstacle but no drone, so send obstacle info
                gradient = calculateGradient(obstacle).normalized;
            }
            else 
            {
                // both obstacle and drone exist, so send info of whichever is closest to self
                if (Vector3.Distance(transform.position, obstacle.position.ToUnityVector()) > nearestDroneDistance)
                {
                    // if distance to obstacle > distance to nearest drone, send drone info
                    Debug.LogError("using drone as nearest obstacle (not an error)");
                    gradient = calculateGradient(nearestDrone).normalized;

                } else {
                    // distance to obstacle < distance to nearest drone, send obstacle info
                    gradient = calculateGradient(obstacle).normalized;
                }
            }
        }

        //if (obstacle.type == APFObstacleType.NO_FLY_ZONE)
        //{
        //    obstacle.position.y = transform.position.y;
        //}
        //gradient = calculateGradient(obstacle).normalized;
        Vector3 direction = -1 * gradient;
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
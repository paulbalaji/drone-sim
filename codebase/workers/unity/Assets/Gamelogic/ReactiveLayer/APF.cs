using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Collections;
using Improbable.Controller;
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

    public static float DroneRadius;

    private APFObstacle dummyObstacle;

    private float radiusOfInfluence;

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
        DroneRadius = SimulationSettings.DroneRadius;
        droneCalcReady = false;
        dummyObstacle = new APFObstacle(APFObstacleType.NONE, new Vector3f(0, -1, 0));
        nearestDrone = new APFObstacle(APFObstacleType.DRONE, new Vector3f(0, -1, 0));

        radiusOfInfluence = 2 * DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
        nearestDroneDistance = radiusOfInfluence;
	}

    private Vector3 CalculateDirection()
    {
        Vector3f dp = transform.position.ToSpatialVector3f();
        Vector3f goal = DroneDataWriter.Data.target;

        APFObstacle nearest = GetNearestObstacle(dp.ToUnityVector(), true);
        if (nearest.type == APFObstacleType.NONE)
        {
            Vector3 direction = goal.ToUnityVector() - dp.ToUnityVector();
            return direction.normalized;
        }

        float potentialAtDrone = calculateTotalPotential(dp, goal, nearest);
        Vector3f xDpos = new Vector3f(dp.x + 1, dp.y, dp.z);
        Vector3f yDpos = new Vector3f(dp.x, dp.y + 1, dp.z);
        Vector3f zDpos = new Vector3f(dp.x, dp.y, dp.z + 1);

        float xD = calculateTotalPotential(xDpos, goal, GetNearestObstacle(xDpos.ToUnityVector(), false)) - potentialAtDrone;
        float yD = calculateTotalPotential(yDpos, goal, GetNearestObstacle(yDpos.ToUnityVector(), false)) - potentialAtDrone;
        float zD = calculateTotalPotential(zDpos, goal, GetNearestObstacle(zDpos.ToUnityVector(), false)) - potentialAtDrone;
        // TODO: Add modifying factor here in order to disencourage changes in altitude

        return -1 * new Vector3(xD, yD, zD).normalized;
    }

    private float calculateTotalPotential(Vector3f dronePosition, Vector3f goal, APFObstacle nearestObstacle, bool collisionDetection = false)
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
            uRepel = distanceToNearestObstacle < InfuentialDistanceConstant
                ? uRepel = RepulsionConst / (distanceToNearestObstacle - DroneRadius)
                : 0;
        }

        Vector3f previousTarget = DroneDataWriter.Data.previousTarget;
        float uRet = previousTarget.y < 0
           ? 0
           : ReturnConstant * Vector3.Distance(dronePosition.ToUnityVector(), DroneDataWriter.Data.previousTarget.ToUnityVector());

        return uAttract + uRepel + uRet;
    }

    private void CheckForNearbyDrones(Vector3 dronePosition, bool collisionDetection)
    {
        nearestDrone.type = APFObstacleType.NONE;
        nearestDrone.position = new Vector3f(0, -1, 0);
        nearestDroneDistance = radiusOfInfluence;

        Collider[] hitColliders = Physics.OverlapSphere(dronePosition, radiusOfInfluence);
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.EntityId() != gameObject.EntityId())
            {
                
                float currentDroneDistance = Vector3.Distance(hitCollider.transform.position, dronePosition);

                if (collisionDetection && currentDroneDistance < 1)
                {
                    Debug.LogWarning("DRONE REPORTING COLLISION");
                    SpatialOS.Commands.SendCommand(
                        PositionWriter,
                        Controller.Commands.Collision.Descriptor,
                        new CollisionRequest(gameObject.EntityId(), hitCollider.gameObject.EntityId()),
                        new EntityId(1))
                             .OnSuccess((response) => Debug.LogWarning("DRONE COLLISION."))
                             .OnFailure((response) => Debug.LogError("UNABLE TO REPORT DRONE COLLISION."));
                    return;
                }

                if (currentDroneDistance < nearestDroneDistance)
                {
                    nearestDrone.position = hitCollider.transform.position.ToSpatialVector3f();
                    nearestDrone.type = APFObstacleType.DRONE;
                    nearestDroneDistance = currentDroneDistance;
                }
            }
        }
    }

    private APFObstacle GetNearestObstacle(Vector3 dronePosition, bool collisionDetection)
    {
        // check for nearest dynamic obstacle, currently just looking for other drones
        CheckForNearbyDrones(dronePosition, collisionDetection);

        // if no static obstacles, pass in drone obstacle
        // it'll resolve itself if drone is none later
        if (nearestStaticObstacle.type == APFObstacleType.NONE)
        {
            return nearestDrone;
        }

        float distanceToStaticObstacle = Vector3.Distance(dronePosition, nearestStaticObstacle.position.ToUnityVector());
        if (distanceToStaticObstacle > SimulationSettings.InfuentialDistanceConstant)
        {
            return nearestDrone;
        }

        // if obstacle is NFZ, update obstacle height to match drone
        if (nearestStaticObstacle.type == APFObstacleType.NO_FLY_ZONE)
        {
            nearestStaticObstacle.position.y = dronePosition.y;
        }

        // nearby obstacle but no nearby drones, so send obstacle info
        if (nearestDrone.type == APFObstacleType.NONE)
        {
            return nearestStaticObstacle;
        }
        else
        {
            // both obstacle and drone exist, so send info of whichever is closest to self
            // if static obstacle's distance > dynamic obstacle's distance, return dynamic obstacle's distance
            if (distanceToStaticObstacle > nearestDroneDistance)
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

    private void MoveDrone(APFObstacle obstacle)
    {
        nearestStaticObstacle = obstacle;
        Vector3 direction = CalculateDirection();
        DroneDataWriter.Send(new DroneData.Update().SetDirection(direction.ToSpatialVector3f()));
    }

    public void Recalculate()
    {
        //get nearest static obstacle, currently just NFZs
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            ReactiveLayer.Commands.GetNearestObstacle.Descriptor,
            new ObstacleRequest(transform.position.ToSpatialVector3f()),
            new EntityId(1),
            TimeSpan.FromSeconds(SimulationSettings.DroneUpdateInterval))
                 .OnSuccess((response) => MoveDrone(response.obstacle))
                 .OnFailure((response) => MoveDrone(dummyObstacle));
    }
}
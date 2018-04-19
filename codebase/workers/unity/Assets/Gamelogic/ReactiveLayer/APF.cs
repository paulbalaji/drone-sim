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

    public static float VelocityConst;

    public static float p0;

    public static float ReturnConstant;

    private ObstacleResponse dummyResponse;

    private float safeDistance;

	private void OnEnable()
	{
        RepulsionConst = SimulationSettings.RepulsionConst;
        AttractionConst = SimulationSettings.AttractionConst;
        VelocityConst = SimulationSettings.VelocityConst;
        p0 = SimulationSettings.p0;
        ReturnConstant = SimulationSettings.ReturnConstant;
        dummyResponse = new ObstacleResponse(new APFObstacle(APFObstacleType.NONE, new Vector3f(0, -1, 0)));

        safeDistance = DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
	}

	private Vector3 calculateGradient(ObstacleResponse response)
    {
        Vector3f dp = transform.position.ToSpatialVector3f();
        Vector3f goal = DroneDataWriter.Data.target;

        float potentialAtDrone = calculateTotalPotential(dp, goal, response.obstacle.position);
        Vector3f xDpos = new Vector3f(dp.x + 1, dp.y, dp.z);
        Vector3f yDpos = new Vector3f(dp.x, dp.y + 1, dp.z);
        Vector3f zDpos = new Vector3f(dp.x, dp.y, dp.z + 1);

        float xD = calculateTotalPotential(xDpos, goal, response.obstacle.position) - potentialAtDrone;
        float yD = calculateTotalPotential(yDpos, goal, response.obstacle.position) - potentialAtDrone;
        float zD = calculateTotalPotential(zDpos, goal, response.obstacle.position) - potentialAtDrone;
        // TODO: Add modifying factor here in order to disencourage changes in altitude

        return new Vector3(xD, yD, zD);
    }

    private float calculateTotalPotential(Vector3f dronePosition, Vector3f goal, Vector3f nearestObstacle)
    {
        //Calculate uAttract = pAttract * dGoal
        float distanceToGoal = Vector3.Distance(goal.ToUnityVector(), dronePosition.ToUnityVector());
        float uAttract = AttractionConst * distanceToGoal;

        float distanceToNearestObstacle = nearestObstacle.y < 0
            ? float.PositiveInfinity
            : Vector3.Distance(dronePosition.ToUnityVector(), nearestObstacle.ToUnityVector());
        float uRepel = distanceToNearestObstacle <= p0
            ? Mathf.Pow(RepulsionConst / (distanceToNearestObstacle - safeDistance), 2)
            : 0;

        return uAttract + uRepel;
    }

    private void MoveDrone(ObstacleResponse response)
    {
        Vector3 direction = -1 * calculateGradient(response).normalized;
        transform.position += direction * DroneDataWriter.Data.speed * SimulationSettings.DroneUpdateInterval;
        PositionWriter.Send(new Position.Update().SetCoords(transform.position.ToCoordinates()));
    }

    private void BackupMove()
    {
        MoveDrone(dummyResponse);
    }

    public void Recalculate()
    {
        SpatialOS.Commands.SendCommand(
            PositionWriter,
            ReactiveLayer.Commands.GetNearestObstacle.Descriptor,
            new ObstacleRequest(transform.position.ToSpatialVector3f()),
            new EntityId(1))
                 .OnSuccess((response) => MoveDrone(response))
                 .OnFailure((response) => BackupMove());
    }
}
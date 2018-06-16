using Assets.Gamelogic.Core;
using Improbable.Controller;
using Improbable.Orders;
using UnityEngine;


public static class TimeValueFunctions
{
	public static float DeliveryValue(float deliveryTime, PackageInfo packageInfo, TimeValueFunction tvf)
    {
		if (deliveryTime > SimulationSettings.DeliveryTimeLimit)
        {
            return 0;
        }

		float maxRevenue = PayloadGenerator.GetPackageCost(packageInfo) * Mathf.Pow(SimulationSettings.TierModifier, (int)tvf.tier);

		float penaltyStep = maxRevenue / (float)tvf.numSteps;
		if (tvf.numSteps == 0)
		{
			return maxRevenue;
		}

		int stepsHit = 0;
		float timeStep = SimulationSettings.TVFStepInterval;
		for (int i = 0; i < tvf.steps.Count; i++)
		{
			if (deliveryTime < timeStep)
			{
				break;
			}

			timeStep += SimulationSettings.TVFStepInterval;

			if (tvf.steps[i])
			{
				++stepsHit;
				if (stepsHit == tvf.numSteps)
				{
					return 0;
				}
			}
		}

		return maxRevenue - (penaltyStep * stepsHit);
    }
    
	public static float ExpectedProfit(float estimatedTime, float expectedDuration, PackageInfo packageInfo, TimeValueFunction tvf)
    {
        float income = DeliveryValue(estimatedTime, packageInfo, tvf);
        float costs = SimulationSettings.EnergyUseEstimationConstant * expectedDuration;
        return income - costs;
    }

	public static TimeValueFunction GenerateTypeA(DeliveryType deliveryType)
    {
        Improbable.Collections.List<bool> steps = new Improbable.Collections.List<bool>(SimulationSettings.TVFSteps);
        int numSteps = SimulationSettings.TVFSteps;
		for (int i = 0; i < steps.Capacity; i++)
        {
            steps.Add(true);
        }
		return new TimeValueFunction(steps, numSteps, deliveryType);
    }

	public static TimeValueFunction GenerateTypeB(DeliveryType deliveryType)
    {
        Improbable.Collections.List<bool> steps = new Improbable.Collections.List<bool>(SimulationSettings.TVFSteps);
        int numSteps = 2;
		for (int i = 0; i < steps.Capacity; i++)
        {
            steps.Add(i == 4 || i == 9);
        }
		return new TimeValueFunction(steps, numSteps, deliveryType);
    }
}

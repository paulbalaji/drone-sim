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
		int tvfStep = 0;
		for (float timeStep = SimulationSettings.TVFStepInterval; timeStep < deliveryTime; timeStep += SimulationSettings.TVFStepInterval)
		{
			if (tvf.steps[tvfStep])
			{
				stepsHit++;
				if (stepsHit == tvf.numSteps)
                {
					return 0;
                }
			}
			++tvfStep;
		}

		return maxRevenue - (penaltyStep * stepsHit);
    }

	public static TimeValueFunction GenerateTypeA(DeliveryType deliveryType)
    {
        Improbable.Collections.List<bool> steps = new Improbable.Collections.List<bool>(SimulationSettings.TVFSteps);
        int numSteps = SimulationSettings.TVFSteps;
        for (int i = 0; i < numSteps; i++)
        {
            steps.Add(true);
        }
		return new TimeValueFunction(steps, numSteps, deliveryType);
    }

	public static TimeValueFunction GenerateTypeB(DeliveryType deliveryType)
    {
        Improbable.Collections.List<bool> steps = new Improbable.Collections.List<bool>(SimulationSettings.TVFSteps);
        int numSteps = 2;
        for (int i = 0; i < numSteps; i++)
        {
            steps.Add(i == 4 || i == 9);
        }
		return new TimeValueFunction(steps, numSteps, deliveryType);
    }
}

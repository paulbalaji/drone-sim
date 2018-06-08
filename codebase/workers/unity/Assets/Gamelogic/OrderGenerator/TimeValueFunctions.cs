using Assets.Gamelogic.Core;
using Improbable.Controller;
using Improbable.Orders;
using UnityEngine;


public static class TimeValueFunctions
{
    public static int DeliveryValue(float deliveryTime, PackageInfo packageInfo)
    {
		int packageRevenue = PayloadGenerator.GetPackageCost(packageInfo);

        // under 30 mins ==> full
        if (deliveryTime < SimulationSettings.DeliveryTimeThreshold)
        {
            return packageRevenue;
        }
        else if (deliveryTime < SimulationSettings.DeliveryTimeLimit)
        {
            return packageRevenue / 2;
        }

        return 0;
    }
}

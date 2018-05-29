using Assets.Gamelogic.Core;
using Improbable.Controller;
using Improbable.Orders;
using UnityEngine;


public static class PayloadGenerator
{
	public static int GetPackageCost(PackageInfo packageInfo)
	{
		switch (packageInfo.type)
        {
            case PackageType.LETTER_SMALL:
                return 60;
            case PackageType.LETTER_LARGE:
                return 80;
            case PackageType.ENVELOPE_SMALL:
                return 109;
            case PackageType.ENVELOPE_STANDARD:
				if (packageInfo.weight < 101)
					return 121;
				if (packageInfo.weight < 251)
					return 134;
				return 154;
            case PackageType.ENVELOPE_LARGE:
                return 177;
            case PackageType.PARCEL:
				if (packageInfo.weight < 251)
					return 173;
				if (packageInfo.weight < 501)
                    return 179;
				if (packageInfo.weight < 1001)
                    return 184;
				if (packageInfo.weight < 1501)
                    return 226;
				if (packageInfo.weight < 2001)
                    return 248;
                return 332;
			default:
				//should never get here
				return 0;
        }
	}

	public static float GetPackagingWeight(PackageType packageType)
	{
		switch(packageType)
		{
			case PackageType.LETTER_SMALL:
				return SimulationSettings.packageWeightSmallLetter;
			case PackageType.LETTER_LARGE:
				return SimulationSettings.packageWeightLargeLetter;
			case PackageType.ENVELOPE_SMALL:
				return SimulationSettings.packageWeightSmallEnvelope;
			case PackageType.ENVELOPE_STANDARD:
				return SimulationSettings.packageWeightStandardEnvelope;
			case PackageType.ENVELOPE_LARGE:
				return SimulationSettings.packageWeightLargeEnvelope;
			case PackageType.PARCEL:
				return SimulationSettings.packageWeightStandardParcel;
		}

        //should never get to this 
		return 0;
	}

	public static PackageInfo GetNextPackage()
	{
		PackageType packageType = (PackageType) Random.Range((int)0, SimulationSettings.NumPackageTypes);
		float packageWeight;

		switch (packageType)
        {
            case PackageType.LETTER_SMALL:
				packageWeight = SmallLetter();
				break;
            case PackageType.LETTER_LARGE:
				packageWeight = LargeLetter();
                break;
            case PackageType.ENVELOPE_SMALL:
				packageWeight = SmallEnvelope();
                break;
            case PackageType.ENVELOPE_STANDARD:
				packageWeight = StandardEnvelope();
                break;
            case PackageType.ENVELOPE_LARGE:
				packageWeight = LargeEnvelope();
                break;
            case PackageType.PARCEL:
				packageWeight = StandardParcel();
                break;
			default:
				// should never get here
				packageWeight = 0;
				break;
        }

		return new PackageInfo(packageType, packageWeight);
	}

    private static float SmallLetter()
	{
		return Random.Range(SimulationSettings.packageWeightSmallLetter, 100f);
	}

	private static float LargeLetter()
    {
		return Random.Range(SimulationSettings.packageWeightLargeLetter, 250f);
    }

	private static float SmallEnvelope()
    {
		return Random.Range(SimulationSettings.packageWeightSmallEnvelope, 100f);
    }

	private static float StandardEnvelope()
    {
		return Random.Range(SimulationSettings.packageWeightStandardEnvelope, 500f);
    }

	private static float LargeEnvelope()
    {
		return Random.Range(SimulationSettings.packageWeightLargeEnvelope, 1000f);
    }

	private static float StandardParcel()
    {
		return Random.Range(SimulationSettings.packageWeightStandardParcel, SimulationSettings.MaxDronePayloadGrams);
    }
}

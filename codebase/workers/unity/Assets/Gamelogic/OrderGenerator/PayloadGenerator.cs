using Assets.Gamelogic.Core;
using Improbable.Controller;
using Improbable.Orders;
using UnityEngine;


public static class PayloadGenerator
{
	public static float GetPackageCost(PackageInfo packageInfo)
	{
		switch (packageInfo.type)
        {
            case PackageType.LETTER_SMALL:
                return 0.6f;
            case PackageType.LETTER_LARGE:
                return 0.8f;
            case PackageType.ENVELOPE_SMALL:
                return 1.09f;
            case PackageType.ENVELOPE_STANDARD:
				if (packageInfo.weight < 101)
					return 1.21f;
				if (packageInfo.weight < 251)
					return 1.34f;
				return 1.54f;
            case PackageType.ENVELOPE_LARGE:
                return 1.77f;
            case PackageType.PARCEL:
				if (packageInfo.weight < 251)
					return 1.73f;
				if (packageInfo.weight < 501)
                    return 1.79f;
				if (packageInfo.weight < 1001)
                    return 1.84f;
				if (packageInfo.weight < 1501)
                    return 2.26f;
				if (packageInfo.weight < 2001)
                    return 2.48f;
                return 3.32f;
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

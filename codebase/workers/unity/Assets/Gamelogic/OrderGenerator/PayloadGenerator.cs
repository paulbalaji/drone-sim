using Assets.Gamelogic.Core;
using Improbable.Controller;
using Improbable.Orders;
using System;

public static class PayloadGenerator
{
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
		Random random = new Random();
		PackageType packageType = (PackageType) random.Next(SimulationSettings.NumPackageTypes);
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
               
        }

		return new PackageInfo(packageType, packageWeight);
	}

    private static float SmallLetter()
	{
		
	}

	private static float LargeLetter()
    {

    }

	private static float SmallEnvelope()
    {

    }

	private static float StandardEnvelope()
    {

    }

	private static float LargeEnvelope()
    {

    }

	private static float StandardParcel()
    {

    }
}

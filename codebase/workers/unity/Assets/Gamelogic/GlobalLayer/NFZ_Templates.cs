using System;
using Improbable.Controller;

public static class NFZ_Templates
{
    private static float[] basicSquare = { 
        25, 25, 
        25, -25,
        -25, -25,
        -25, 25
    };

    private static float[] basicRectangle = {
        251, 21,
        251, -21,
        -251, -21,
        -251, 21
    };

    private static float[] basicEnclosure = {
        251, 61,
        251, -61,
        200, -61,
        200, -1,
        -200, -1,
        -200, -61,
        -251, -61,
        -251, 61
    };

    private static float[] topLeftSquare = {
        -800, 800,
        -600, 800,
        -600, 600,
        -800, 600
    };

    private static float[] rightRectangle = {
        300, 150,
        400, 150,
        400, -150,
        300, -150
    };

    public static Improbable.Controller.NoFlyZone GetNoFlyZone(NFZTemplate template)
    {
        Improbable.Controller.NoFlyZone nfz = new Improbable.Controller.NoFlyZone();
        nfz.vertices = new Improbable.Collections.List<Improbable.Vector3f>();

        float[] coords = getPoints(template);
        for (int i = 0; i < coords.Length; i += 2)
        {
            nfz.vertices.Add(new Improbable.Vector3f(coords[i], 0, coords[i+1]));
        }

        NoFlyZone.setBoundingBoxCoordinates(ref nfz);

        return nfz;
    }

    public static float[] getPoints(NFZTemplate template)
    {
        switch (template)
        {
            case NFZTemplate.RIGHT_RECTANGLE:
                return rightRectangle;
            case NFZTemplate.TOP_LEFT_SQUARE:
                return topLeftSquare;
            case NFZTemplate.BASIC_ENCLOSURE:
                return basicEnclosure;
            case NFZTemplate.BASIC_RECTANGLE:
                return basicRectangle;
            case NFZTemplate.BASIC_SQUARE:
            default:
                return basicSquare;
        }
    }
}

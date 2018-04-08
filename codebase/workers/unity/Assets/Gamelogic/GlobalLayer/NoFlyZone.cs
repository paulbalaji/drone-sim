using System;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;

public static class NoFlyZone
{
    private static bool isInPolygon(Improbable.Controller.NoFlyZone nfz, Improbable.Vector3d point)
    {
        bool isInside = false;

        for (int i = 0, j = nfz.vertices.Count - 1; i < nfz.vertices.Count; j = i++)
        {
            bool isInZRange = ((nfz.vertices[i].z > point.z) != (nfz.vertices[j].z > point.z));

            if (isInZRange &&
                (point.x < (nfz.vertices[j].x - nfz.vertices[i].x) * (point.z - nfz.vertices[i].z)
                 / (nfz.vertices[j].z - nfz.vertices[i].z) + nfz.vertices[i].x))
            {
                isInside = !isInside;
            }
        }
        return isInside;
    }

    public static bool hasCollidedWith(Improbable.Controller.NoFlyZone nfz, Improbable.Vector3d point)
    {
        return isInPolygon(nfz, point);
    }

    public static void setBoundingBoxCoordinates(ref Improbable.Controller.NoFlyZone nfz)
    {
        Vector3d BoundingBoxBottomLeft = nfz.vertices[0];
        Vector3d BoundingBoxTopRight = nfz.vertices[0];

        foreach (Improbable.Vector3d vertex in nfz.vertices)
        {
            if (vertex.x > BoundingBoxTopRight.x)
            {
                BoundingBoxTopRight.x = vertex.x;
            }
            if (vertex.x < BoundingBoxBottomLeft.x)
            {
                BoundingBoxBottomLeft.x = vertex.x;
            }
            if (vertex.z > BoundingBoxTopRight.z)
            {
                BoundingBoxTopRight.z = vertex.z;
            }
            if (vertex.z < BoundingBoxBottomLeft.z)
            {
                BoundingBoxBottomLeft.z = vertex.z;
            }
        }

        nfz.boundingBoxBottomLeft = BoundingBoxBottomLeft;
        nfz.boundingBoxTopRight = BoundingBoxTopRight;
    }

    public static bool isPointInTheBoundingBox(Improbable.Controller.NoFlyZone nfz, Improbable.Vector3d point)
    {
        bool res = false;
        if (point.x >= nfz.boundingBoxBottomLeft.x & point.x <= nfz.boundingBoxTopRight.x)
        {
            if (point.z >= nfz.boundingBoxBottomLeft.z & point.z <= nfz.boundingBoxTopRight.z)
            {
                res = true;
            }
        }
        return res;
    }
}

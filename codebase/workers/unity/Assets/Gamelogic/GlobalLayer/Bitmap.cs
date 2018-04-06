using System;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;

public class Bitmap : MonoBehaviour
{
    [Require]
    private BitmapComponent.Writer BitmapWriter;

    public static int BIT_SIZE = 25; // meters that each bit in the grid corresponds to
    const int SIZE_OF_A_STEP = 1; // used when setting bits from a no fly zone
    Improbable.Vector3d TopLeft;
    Improbable.Vector3d BottomRight;
    private int Width; // Meters
    private int Height; // Meters
    public Improbable.Collections.List<GridType> Grid;

    public Bitmap(Improbable.Vector3d topLeft, Improbable.Vector3d bottomRight)
    {
        if (topLeft.x > bottomRight.x || bottomRight.z < topLeft.z)
        {
            throw new Exception("Unsupported grid coordinates");
        }
        TopLeft = topLeft;
        BottomRight = bottomRight;
        Width = (int)Math.Ceiling(Math.Abs(bottomRight.x - topLeft.x));
        Height = (int)Math.Ceiling(Math.Abs(topLeft.z - bottomRight.z));
        Grid = createBitmapOfGivenSize(Width, Height);

        BitmapWriter.Send(new BitmapComponent.Update()
                          .SetTopLeft(TopLeft)
                          .SetBottomRight(BottomRight)
                          .SetWidth(Width)
                          .SetHeight(Height)
                          .SetGrid(Grid)
                          .SetInitialised(true));
    }

    private Improbable.Collections.List<GridType> createBitmapOfGivenSize(double width, double height)
    {
        int columns = (int)Math.Ceiling(width / BIT_SIZE);
        int rows = (int)Math.Ceiling(height / BIT_SIZE);
        return new Improbable.Collections.List<GridType>(rows * columns);
    }

    private void OnEnable()
    {
        BitmapWriter.ComponentUpdated.Add(HandleAction);

        if (BitmapWriter.Data.initialised)
        {
            TopLeft = BitmapWriter.Data.topLeft;
            BottomRight = BitmapWriter.Data.bottomRight;
            Width = BitmapWriter.Data.width;
            Height = BitmapWriter.Data.height;
            Grid = BitmapWriter.Data.grid;
        }
    }

    void HandleAction(BitmapComponent.Update obj)
    {
        if (obj.topLeft.HasValue)
        {
            TopLeft = obj.topLeft.Value;
        }

        if (obj.bottomRight.HasValue)
        {
            BottomRight = obj.bottomRight.Value;
        }

        if (obj.width.HasValue)
        {
            Width = obj.width.Value;
        }

        if (obj.height.HasValue)
        {
            Height = obj.height.Value;
        }

        if (obj.grid.HasValue)
        {
            Grid = obj.grid.Value;
        }
    }

    private void OnDisable()
    {
        BitmapWriter.ComponentUpdated.Remove(HandleAction);
    }

    public void updateWithNoFlyZones(List<NoFlyZone> zones)
    {
        zones.ForEach(addNoFlyZone);
    }

    public void addNoFlyZone(NoFlyZone noFlyZone)
    {
        Improbable.Vector3d[] vertices = noFlyZone.getVertices();
        Improbable.Vector3d previousWaypoint = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            Improbable.Vector3d currentWaypoint = vertices[i];
            setLine(previousWaypoint, currentWaypoint);
            previousWaypoint = currentWaypoint;
        }

        setLine(previousWaypoint, vertices[0]); // setting the final line
    }

    private void setGridCell(int x, int z, GridType value)
    {
        Grid[z * Width + x] = value;
        BitmapWriter.Send(new BitmapComponent.Update().SetGrid(Grid));
    }

    private GridType getGridCell(int x, int z)
    {
        return Grid[z * Width + x];
    }

    public bool isNearNoFlyZone(int x, int z)
    {
        return getGridCell(x, z) == GridType.NEAR;
    }

    public Improbable.Vector3d nearestNoFlyZonePoint(Improbable.Vector3d point)
    {
        // Find out where point is in the grid
        int[] gridCo = findGridCoordinatesOfPoint(point);
        int z = gridCo[0];
        int x = gridCo[1];

        GridLocation anchor = new GridLocation(z, x);

        double nearestDistance = Double.PositiveInfinity;
        GridLocation nearestLocation = new GridLocation(0, 0); // placeholder.
        bool foundNoFlyZone = false;

        for (int layer = 1, maxLayers = 6; !foundNoFlyZone && layer <= maxLayers; ++layer)
        {
            // Placeholders.
            int k, xi;

            // Loop from top left to top right.
            int zi = z - layer;
            if (0 <= zi && zi < Height)
            {
                for (k = -layer; k < layer; ++k)
                {
                    xi = x + k;
                    if (InBounds(xi, zi) && getGridCell(xi, zi) == GridType.IN)
                    {
                        foundNoFlyZone = true;
                        GridLocation candidate = new GridLocation(xi, zi);
                        double dist = anchor.distanceTo(candidate);
                        if (dist < nearestDistance)
                        {
                            nearestDistance = Math.Min(nearestDistance, dist);
                            nearestLocation = candidate;
                        }
                    }
                }
            }

            // Loop from top right to bottom right.
            xi = x + layer;
            if (0 <= xi && xi < Width)
            {
                for (k = -layer; k < layer; ++k)
                {
                    zi = z + k;
                    if (InBounds(xi, zi) && getGridCell(xi, zi) == GridType.IN)
                    {
                        foundNoFlyZone = true;
                        GridLocation candidate = new GridLocation(xi, zi);
                        double dist = anchor.distanceTo(candidate);
                        if (dist < nearestDistance)
                        {
                            nearestDistance = Math.Min(nearestDistance, dist);
                            nearestLocation = candidate;
                        }
                    }
                }
            }

            // Loop from bottom right to bottom left.
            if (0 <= zi && zi < Height)
            {
                zi = z + layer;
                for (k = layer; k > -layer; --k)
                {
                    xi = x + k;
                    if (InBounds(xi, zi) && getGridCell(xi, zi) == GridType.IN)
                    {
                        foundNoFlyZone = true;
                        GridLocation candidate = new GridLocation(xi, zi);
                        double dist = anchor.distanceTo(candidate);
                        if (dist < nearestDistance)
                        {
                            nearestDistance = Math.Min(nearestDistance, dist);
                            nearestLocation = candidate;
                        }
                    }
                }
            }

            // Loop from bottom left to top left.
            xi = x - layer;
            if (0 <= xi && xi < Width)
            {
                for (k = layer; k > -layer; --k)
                {
                    zi = z + k;
                    if (InBounds(xi, zi) && getGridCell(xi, zi) == GridType.IN)
                    {
                        foundNoFlyZone = true;
                        GridLocation candidate = new GridLocation(xi, zi);
                        double dist = anchor.distanceTo(candidate);
                        if (dist < nearestDistance)
                        {
                            nearestDistance = Math.Min(nearestDistance, dist);
                            nearestLocation = candidate;
                        }
                    }
                }
            }
        }
        if (!foundNoFlyZone)
        {
            return new Improbable.Vector3d(0, -1, 0);
        }

        //given set point, convert (x,y) -> cartesian
        Improbable.Vector3d nearPoint = getPointFromCoordinates(new int[] { nearestLocation.x, nearestLocation.z });
        //assert third element of vector is 0
        //TODO: assert nearPoint only has 2 non-zero elements
        //Console.WriteLine(nearestLocation);
        //compute euclidean distance between the two
        return nearPoint;
    }

    // Returns positive infinity if no point is found within a certain amount of layers.
    public double distanceToNoFlyZone(Improbable.Vector3d point)
    {
        Improbable.Vector3d p = nearestNoFlyZonePoint(point);
        if (p.y >= 0)
        {
            double w = p.x - point.x;
            double h = p.z - point.z;
            return Math.Sqrt(Math.Pow(w, 2) + Math.Pow(h, 2));
        }
        return Double.MaxValue;
    }

    public void setLine(Improbable.Vector3d startPoint, Improbable.Vector3d endPoint)
    {
        // setting the size of each step as we are walking along the line; this should be 1 meter
        Improbable.Vector3d incrementationVector = endPoint - startPoint;
        incrementationVector = incrementationVector.Normalized() * SIZE_OF_A_STEP;

        Improbable.Vector3d prevPoint = startPoint;
        int[] prevCoord = findAndSetPointInGrid(startPoint);
        while (!nextPointIsEndpoint(prevPoint, endPoint))
        {
            Improbable.Vector3d currentPoint = prevPoint + incrementationVector;
            int[] currCoord = findAndSetPointInGrid(currentPoint);

            // if diagonal, set the box lower to the diagonalization
            if (pointsAreDiagonal(prevCoord, currCoord))
            {
                int[] higherCoordinate = findHigherCoordinate(prevCoord, currCoord);
                setGridCell(higherCoordinate[0], higherCoordinate[1] + 1, GridType.NEAR);
            }

            prevPoint = currentPoint;
            prevCoord = currCoord;
        }

        findAndSetPointInGrid(endPoint);
    }

    public bool pointsAreDiagonal(int[] fstCoord, int[] sndCoord)
    {
        return (Math.Abs(fstCoord[0] - sndCoord[0]) == 1) && (Math.Abs(fstCoord[1] - sndCoord[1]) == 1);
    }

    /*
     * Returns the coordinate with bigger z value 
     */
    public int[] findHigherCoordinate(int[] fstCoord, int[] sndCoord)
    {
        return fstCoord[1] < sndCoord[1] ? fstCoord : sndCoord;
    }

    private bool nextPointIsEndpoint(Improbable.Vector3d currentPoint, Improbable.Vector3d endPoint)
    {
        double distanceToEndPoint = Math.Sqrt(Math.Pow(endPoint.x - currentPoint.x, 2) +
                                              Math.Pow(endPoint.z - currentPoint.z, 2));
        return distanceToEndPoint < SIZE_OF_A_STEP;
    }

    /*
     * Sets the corresponding bit in the grid and returns the coordinates
     */
    public int[] findAndSetPointInGrid(Improbable.Vector3d point)
    {
        int[] coord = findGridCoordinatesOfPoint(point);
        GridLocation loc = new GridLocation(coord[0], coord[1]);
        int NFZ_PADDING = 2;
        foreach (var n in GeneralNeighbours(loc, NFZ_PADDING))
        {
            if (getGridCell(n.x, n.z) != GridType.IN)
            {
                setGridCell(n.x, n.z, GridType.NEAR);
            }
        }
        setGridCell(coord[0], coord[1], GridType.IN);

        return coord;
    }

    /** 
     * x and y are coordinates in the grid
     * */
    public bool isNoFlyZone(int x, int z)
    {
        return getGridCell(x, z) == GridType.IN;
    }

    /**
     * 'point' has components in cartesian format
     */
    public bool isNoFlyZone(Improbable.Vector3d point)
    {
        int[] coord = findGridCoordinatesOfPoint(point);
        return isNoFlyZone(coord[0], coord[1]);
    }

    // Returns an int[] of the form {x, y}.
    public int[] findGridCoordinatesOfPoint(Improbable.Vector3d point)
    {
        int x = (int)Math.Floor((point.x - TopLeft.x) / BIT_SIZE);
        int z = (int)Math.Floor((point.z - TopLeft.z) / BIT_SIZE);
        if (x < 0 || x >= Width || z < 0 || z >= Height)
        {
            throw new Exception("Invalid bitmap index: x - " + x + ", z - " + z);
        }

        int[] result = { x, z };
        return result;
    }

    public Improbable.Vector3d getPointFromCoordinates(int[] coords)
    {
        // TODO do sanity check
        double x = TopLeft.x + coords[0] * BIT_SIZE;
        double z = TopLeft.z + coords[1] * BIT_SIZE;

        return new Improbable.Vector3d(x, 0, z);
    }

    public String toString()
    {
        string output = "";
        for (int i = 0; i < Height; i++)
        {
            for (int k = 0; k < Width; k++)
            {
                output += getGridCell(k ,i) != GridType.OUT ? getGridCell(k, i).ToString() : "_";
            }
            output += "\n";
        }
        return output;
    }


    public bool InBounds(int x, int z)
    {
        return 0 <= x && x < Width && 0 <= z && z < Height;
    }

    public HashSet<GridLocation> GeneralNeighbours(GridLocation current, int layers)
    {
        HashSet<GridLocation> set = new HashSet<GridLocation>();
        for (int i = -layers; i <= layers; ++i)
        {
            for (int j = -layers; j <= layers; ++j)
            {
                if ((i != 0 || j != 0) && InBounds(current.x + i, current.z + j)
                    && (getGridCell(current.x + i, current.z + j) == GridType.OUT))
                {
                    set.Add(new GridLocation(current.x + i, current.z + j));
                }
            }
        }
        return set;
    }

    public HashSet<GridLocation> Neighbours(GridLocation current)
    {
        return GeneralNeighbours(current, 1);
    }


    public bool lineOfSight(GridLocation a, GridLocation b)
    {
        int x0 = a.x;
        int z0 = a.z;
        int x1 = b.x;
        int z1 = b.z;
        int dz = z1 - z0;
        int dx = x1 - x0;
        int f = 0;
        int sz = 1, sx = 1;
        if (dz < 0)
        {
            dz = -dz;
            sz = -1;
        }
        if (dx < 0)
        {
            dx = -dx;
            sx = -1;
        }
        if (dx >= dz)
        {
            while (x0 != x1)
            {
                f += dz;
                if (f >= dx)
                {
                    if (getGridCell(x0 + ((sx - 1) / 2), z0 + ((sz - 1) / 2)) == GridType.IN)
                    {
                        return false;
                    }
                    z0 += sz;
                    f -= dx;
                }
                if (f != 0 && getGridCell(x0 + ((sx - 1) / 2), z0 + ((sz - 1) / 2)) == GridType.IN)
                {
                    return false;
                }
                if (dz == 0 && getGridCell(x0, z0 + ((sz - 1) / 2)) == GridType.IN && getGridCell(x0 - 1, z0 + ((sz - 1) / 2)) == GridType.IN)
                {
                    return false;
                }
                x0 += sx;
            }
        }
        else
        {
            while (z0 != z1)
            {
                f += dx;
                if (f >= dz)
                {
                    if (getGridCell(x0 + ((sx - 1) / 2), z0 + ((sz - 1) / 2)) == GridType.IN)
                    {
                        return false;
                    }
                    x0 += sx;
                    f -= dz;
                }
                if (f != 0 && getGridCell(x0 + ((sx - 1) / 2), z0 + ((sz - 1) / 2)) == GridType.IN)
                {
                    return false;
                }
                if (dx == 0 && getGridCell(x0, z0 + ((sz - 1) / 2)) == GridType.IN && getGridCell(x0 - 1, z0 + ((sz - 1) / 2)) == GridType.IN)
                {
                    return false;
                }
                z0 += sz;
            }
        }
        return true;
    }
}

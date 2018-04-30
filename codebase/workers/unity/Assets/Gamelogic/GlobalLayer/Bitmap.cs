using System;
using System.Collections.Generic;
using UnityEngine;
using Improbable;
using Improbable.Controller;
using Improbable.Unity;
using Improbable.Unity.Core;
using Improbable.Unity.Visualizer;
using Assets.Gamelogic.Core;

[WorkerType(WorkerPlatform.UnityWorker)]
public class Bitmap : MonoBehaviour
{
    [Require]
    private BitmapComponent.Writer BitmapWriter;

    public static int BIT_SIZE; // meters that each bit in the grid corresponds to
    static int SIZE_OF_A_STEP; // used when setting bits from a no fly zone
    Improbable.Vector3f TopLeft;
    Improbable.Vector3f BottomRight;
    private int Width; // Meters
    private int Height; // Meters
    private int GridWidth; // columns in the grid array
    private int GridHeight; // rows in the grid array
    public Improbable.Collections.Map<int, GridType> Grid;

    public void InitialiseBitmap(Improbable.Vector3f topLeft, Improbable.Vector3f bottomRight)
    {
        //Debug.LogWarning("first if");
        if (BitmapWriter.Data.initialised)
        {
            Debug.LogError("Bitmap already initialised");
            return;
        }

        //Debug.LogWarning("second if");
        if (topLeft.x > bottomRight.x || bottomRight.z > topLeft.z)
        {
            Debug.LogError("Unsupported grid coordinates");
            return;
        }

        //Debug.LogWarning("harmless shit");
        TopLeft = topLeft;
        BottomRight = bottomRight;
        Width = (int)Math.Ceiling(Math.Abs(bottomRight.x - topLeft.x));
        Height = (int)Math.Ceiling(Math.Abs(topLeft.z - bottomRight.z));

        //Debug.LogWarning("potential danger");
        createBitmapOfGivenSize(Width, Height);

        //Debug.LogWarning("init || topLeft: " + TopLeft);

        BitmapWriter.Send(new BitmapComponent.Update()
                          .SetTopLeft(TopLeft)
                          .SetBottomRight(BottomRight)
                          .SetWidth(Width)
                          .SetHeight(Height)
                          .SetGrid(Grid)
                          .SetGridWidth(GridWidth)
                          .SetGridHeight(GridHeight)
                          .SetInitialised(true));

        WaitUntil.Equals(BitmapWriter.Data.initialised, true);
    }

    private void createBitmapOfGivenSize(double width, double height)
    {
        GridWidth = (int)Math.Ceiling(width / BIT_SIZE);
        GridHeight = (int)Math.Ceiling(height / BIT_SIZE);
        Grid = new Improbable.Collections.Map<int, GridType>();
    }

    private void OnEnable()
    {
        BIT_SIZE = SimulationSettings.BIT_SIZE; // meters that each bit in the grid corresponds to
        SIZE_OF_A_STEP = SimulationSettings.SIZE_OF_A_STEP; // used when setting bits from a no fly zone

        //BitmapWriter.ComponentUpdated.Add(HandleAction);

        if (BitmapWriter.Data.initialised)
        {
            TopLeft = BitmapWriter.Data.topLeft;
            BottomRight = BitmapWriter.Data.bottomRight;
            Width = BitmapWriter.Data.width;
            Height = BitmapWriter.Data.height;
            GridHeight = BitmapWriter.Data.gridHeight;
            GridWidth = BitmapWriter.Data.gridWidth;
            Grid = BitmapWriter.Data.grid;
        }
    }

    private void OnDisable()
    {
        //BitmapWriter.ComponentUpdated.Remove(HandleAction);
    }

    //private void HandleAction(BitmapComponent.Update obj)
    //{
    //    if (obj.topLeft.HasValue)
    //    {
    //        Debug.LogError("hand || topLeft: " + TopLeft);
    //        TopLeft = obj.topLeft.Value;
    //    }

    //    if (obj.bottomRight.HasValue)
    //    {
    //        BottomRight = obj.bottomRight.Value;
    //    }

    //    if (obj.width.HasValue)
    //    {
    //        Width = obj.width.Value;
    //    }

    //    if (obj.height.HasValue)
    //    {
    //        Height = obj.height.Value;
    //    }

    //    if (obj.grid.HasValue)
    //    {
    //        Grid = obj.grid.Value;
    //    }

    //    if (obj.gridWidth.HasValue)
    //    {
    //        GridWidth = obj.gridWidth.Value;
    //    }

    //    if (obj.gridHeight.HasValue)
    //    {
    //        GridHeight = obj.gridHeight.Value;
    //    }
    //}

    public void updateWithNoFlyZones(List<Improbable.Controller.NoFlyZone> zones)
    {
        foreach(Improbable.Controller.NoFlyZone zone in zones)
        {
            addNoFlyZone(zone, false);
        }
        sendGridUpdate();
    }

    public void addNoFlyZone(Improbable.Controller.NoFlyZone noFlyZone, bool sendUpdate = true)
    {
        Improbable.Vector3f[] vertices = noFlyZone.vertices.ToArray();
        Improbable.Vector3f previousWaypoint = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            Improbable.Vector3f currentWaypoint = vertices[i];
            setLine(previousWaypoint, currentWaypoint);
            previousWaypoint = currentWaypoint;
        }

        setLine(previousWaypoint, vertices[0]); // setting the final line

        if (sendUpdate)
        {
            sendGridUpdate();
        }
    }

    private void setGridCell(int x, int z, GridType value)
    {
        int index = z * GridWidth + x;

        if(Grid.ContainsKey(index))
        {
            Grid.Remove(index);
        }

        Grid.Add(index, value);
    }

    private void sendGridUpdate()
    {
        BitmapWriter.Send(new BitmapComponent.Update().SetGrid(Grid));
    }

    private GridType getGridCell(int x, int z)
    {
        GridType gridCell;
        int index = z * GridWidth + x;

        if (Grid.TryGetValue(index, out gridCell))
        {
            return gridCell;
        }

        return GridType.OUT;
    }

    public bool isNearNoFlyZone(int x, int z)
    {
        int[] xz = findGridCoordinatesOfPoint(new Vector3f(x, 0, z));
        return getGridCell(xz[0], xz[1]) == GridType.NEAR;
    }

    public Improbable.Vector3f nearestNoFlyZonePoint(Improbable.Vector3f point)
    {
        // Find out where point is in the grid
        int[] gridCo = findGridCoordinatesOfPoint(point);
        int x = gridCo[0];
        int z = gridCo[1];

        GridLocation anchor = new GridLocation(x, z);

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
                    if (InGridBounds(xi, zi) && (getGridCell(xi, zi) == GridType.IN || getGridCell(xi, zi) == GridType.NEAR))
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
                    if (InGridBounds(xi, zi) && (getGridCell(xi, zi) == GridType.IN || getGridCell(xi, zi) == GridType.NEAR))
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
                    if (InGridBounds(xi, zi) && (getGridCell(xi, zi) == GridType.IN || getGridCell(xi, zi) == GridType.NEAR))
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
                    if (InGridBounds(xi, zi) && (getGridCell(xi, zi) == GridType.IN || getGridCell(xi, zi) == GridType.NEAR))
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
            return new Improbable.Vector3f(0, -1, 0);
        }

        //given set point, convert grid -> real world
        Improbable.Vector3f nearPoint = getPointFromGridCoordinates(new int[] { nearestLocation.x, nearestLocation.z });
        //assert vector.y is 0
        //TODO: assert nearPoint only has 2 non-zero elements
        return nearPoint;
    }

    // Returns positive infinity if no point is found within a certain amount of layers.
    public float distanceToNoFlyZone(Improbable.Vector3f point)
    {
        Improbable.Vector3f p = nearestNoFlyZonePoint(point);
        if (p.y >= 0)
        {
            float w = p.x - point.x;
            float h = p.z - point.z;
            return Mathf.Sqrt(Mathf.Pow(w, 2) + Mathf.Pow(h, 2));
        }
        return float.PositiveInfinity;
    }

    public void setLine(Improbable.Vector3f startPoint, Improbable.Vector3f endPoint)
    {
        // setting the size of each step as we are walking along the line; this should be 1 meter
        Improbable.Vector3f incrementationVector = endPoint - startPoint;
        incrementationVector = incrementationVector.Normalized() * SIZE_OF_A_STEP;

        Improbable.Vector3f prevPoint = startPoint;
        int[] prevCoord = findAndSetPointInGrid(startPoint);
        while (!nextPointIsEndpoint(prevPoint, endPoint))
        {
            Improbable.Vector3f currentPoint = prevPoint + incrementationVector;
            int[] currCoord = findAndSetPointInGrid(currentPoint);

            if (currCoord[0] != prevCoord[0] && currCoord[1] != prevCoord[1])
            {
                // if diagonal, set the box lower to the diagonalization
                if (gridPointsAreDiagonal(prevCoord, currCoord))
                {
                    int[] higherCoordinate = findHigherGridCoordinate(prevCoord, currCoord);
                    setGridCell(higherCoordinate[0], higherCoordinate[1] + 1, GridType.NEAR);
                }
            }

            prevPoint = currentPoint;
            prevCoord = currCoord;
        }

        findAndSetPointInGrid(endPoint);
    }

    public bool gridPointsAreDiagonal(int[] fstCoord, int[] sndCoord)
    {
        return (Math.Abs(fstCoord[0] - sndCoord[0]) == 1) && (Math.Abs(fstCoord[1] - sndCoord[1]) == 1);
    }

    /*
     * Returns the coordinate with bigger z value 
     */
    public int[] findHigherGridCoordinate(int[] fstCoord, int[] sndCoord)
    {
        return fstCoord[1] < sndCoord[1] ? fstCoord : sndCoord;
    }

    private bool nextPointIsEndpoint(Improbable.Vector3f currentPoint, Improbable.Vector3f endPoint)
    {
        double distanceToEndPoint = Math.Sqrt(Math.Pow(endPoint.x - currentPoint.x, 2) +
                                              Math.Pow(endPoint.z - currentPoint.z, 2));
        return distanceToEndPoint < SIZE_OF_A_STEP;
    }

    /*
     * Sets the corresponding bit in the grid and returns the coordinates
     */
    public int[] findAndSetPointInGrid(Improbable.Vector3f point)
    {
        int[] coord = findGridCoordinatesOfPoint(point);
        GridLocation loc = new GridLocation(coord[0], coord[1]);
        foreach (var n in GeneralNeighbours(loc, SimulationSettings.NFZ_PADDING))
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
    public bool isGridCellNoFlyZone(int x, int z)
    {
        return getGridCell(x, z) == GridType.IN;
    }

    /**
     * 'point' has components in cartesian format
     */
    public bool isNoFlyZone(Improbable.Vector3f point)
    {
        int[] coord = findGridCoordinatesOfPoint(point);
        return isGridCellNoFlyZone(coord[0], coord[1]);
    }

    // Returns an int[] of the form {x, y}.
    public int[] findGridCoordinatesOfPoint(Improbable.Vector3f point)
    {
        int x = (int)Math.Floor((point.x - TopLeft.x) / BIT_SIZE);
        int z = (int)Math.Floor((TopLeft.z - point.z) / BIT_SIZE);
        if (x < 0 || x >= GridWidth || z < 0 || z >= GridHeight)
        {
            Debug.LogError("Invalid bitmap index: x= " + x + ", z= " + z);
            Debug.LogError("G->P || point: " + point + " ,topLeft: " + TopLeft);
            return null;
        }

        int[] result = { x, z };
        return result;
    }

    // grid --> real world
    public Improbable.Vector3f getPointFromGridCoordinates(int[] coords)
    {
        // TODO do sanity check
        float x = TopLeft.x + coords[0] * BIT_SIZE;
        float z = TopLeft.z - coords[1] * BIT_SIZE;

        return new Improbable.Vector3f(x, 0, z);
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
        return TopLeft.x <= x && x <= BottomRight.x && TopLeft.z >= z && z >= BottomRight.z;
    }

    public bool InGridBounds(int x, int z)
    {
        return 0 <= x && x < GridWidth && 0 <= z && z < GridHeight;
    }

    public HashSet<GridLocation> GeneralNeighbours(GridLocation current, int layers)
    {
        HashSet<GridLocation> set = new HashSet<GridLocation>();
        for (int i = -layers; i <= layers; ++i)
        {
            for (int j = -layers; j <= layers; ++j)
            {
                if ((i != 0 || j != 0) && InGridBounds(current.x + i, current.z + j)
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

using System;

namespace AssemblyCSharp.Gamelogic.GlobalLayer
{
    public class NoFlyZone
    {
        private Improbable.Vector3d[] Vertices;
        private Improbable.Vector3d BoundingBoxBottomLeft;
        private Improbable.Vector3d BoundingBoxTopRight;

        public NoFlyZone(Improbable.Vector3d[] vertices)
        {
            setVertices(vertices);
        }

        private bool isInPolygon(Improbable.Vector3d point)
        {
            bool isInside = false;

            for (int i = 0, j = Vertices.Length - 1; i < Vertices.Length; j = i++)
            {
                bool isInZRange = ((Vertices[i].z > point.z) != (Vertices[j].z > point.z));

                if (isInZRange &&
                (point.x < (Vertices[j].x - Vertices[i].x) * (point.z - Vertices[i].z)
                 / (Vertices[j].z - Vertices[i].z) + Vertices[i].x))
                {
                    isInside = !isInside;
                }
            }
            return isInside;
        }

        public bool hasCollidedWith(Improbable.Vector3d point)
        {
            return isInPolygon(point);
        }

        public Improbable.Vector3d[] getVertices()
        {
            return Vertices;
        }

        public void setVertices(Improbable.Vector3d[] vertices)
        {
            Vertices = vertices;
        }

        public void setBoundingBoxCoordinates()
        {
            BoundingBoxBottomLeft = Vertices[0];
            BoundingBoxTopRight = Vertices[0];

            foreach (Improbable.Vector3d vertex in Vertices)
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
        }

        public bool isPointInTheBoundingBox(Improbable.Vector3d point)
        {
            bool res = false;
            if (point.x >= BoundingBoxBottomLeft.x & point.x <= BoundingBoxTopRight.x)
            {
                if (point.z >= BoundingBoxBottomLeft.z & point.z <= BoundingBoxTopRight.z)
                {
                    res = true;
                }
            }
            return res;
        }

        public Improbable.Vector3d getBoundingBoxBottomLeft()
        {
            return BoundingBoxBottomLeft;
        }

        public Improbable.Vector3d getBoundingBoxTopRight()
        {
            return BoundingBoxTopRight;
        }
    }
}

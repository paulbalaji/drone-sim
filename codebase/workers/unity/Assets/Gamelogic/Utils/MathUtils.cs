using Improbable;
using UnityEngine;

public static class MathUtils
{

    public static Quaternion ToUnityQuaternion(this Improbable.Core.Quaternion quaternion)
    {
        return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }

    public static Improbable.Core.Quaternion ToNativeQuaternion(this Quaternion quaternion)
    {
        return new Improbable.Core.Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
    }
}

public static class Vector3Extensions
{
    public static Coordinates ToCoordinates(this Vector3 vector3)
    {
        return new Coordinates(vector3.x, vector3.y, vector3.z);
    }

    public static Vector3 ToVector3(this Vector3f vector3f)
    {
        return new Vector3(vector3f.x, vector3f.y, vector3f.z);
    }

    public static Vector3f ToSpatialVector3f(this Coordinates coordinates)
    {
        return new Vector3f((float)coordinates.x, (float)coordinates.y, (float)coordinates.z);
    }
}
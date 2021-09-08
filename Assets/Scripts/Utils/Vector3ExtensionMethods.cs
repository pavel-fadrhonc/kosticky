using UnityEngine;
using System.Collections.Generic;

public static class Vector3ExtensionMethods
{
    public static Vector3 NormalizeIfLongerThanOne(this Vector3 vector)
    {
        return vector.sqrMagnitude > 1f ? vector.normalized : vector;
    }

    public static Vector3 GetZeroYNormalizedVector(this Vector3 vector)
    {
        Vector3 newVec = vector;
        newVec.y = 0f;
        newVec.Normalize();
        return newVec;
    }

    public static bool IsCloseToZeroVector(this Vector3 vector)
    {
        return Mathf.Abs(vector.x) < 0.01f && Mathf.Abs(vector.y) < 0.01f && Mathf.Abs(vector.z) < 0.01f;
    }

    /// <summary>
    /// Normalized the angle. between -180 and 180 degrees
    /// </summary>
    /// <param Name="eulerAngle">Euler angle.</param>
    public static Vector3 NormalizeAngle(this Vector3 eulerAngle)
    {
        var delta = eulerAngle;

        if (delta.x > 180) delta.x -= 360;
        else if (delta.x < -180) delta.x += 360;

        if (delta.y > 180) delta.y -= 360;
        else if (delta.y < -180) delta.y += 360;

        if (delta.z > 180) delta.z -= 360;
        else if (delta.z < -180) delta.z += 360;

        return new Vector3(delta.x, delta.y, delta.z); //round values to angle;
    }

    public static Vector2 ToVector2XZ(this Vector3 vect3)
    {
        return new Vector2(vect3.x, vect3.z);
    }

    public static Vector3 WithX(this Vector3 vector3, float x)
    {
        return new Vector3(x, vector3.y, vector3.z);
    }
    
    public static Vector3 WithY(this Vector3 vector3, float y)
    {
        return new Vector3(vector3.x, y, vector3.z);
    }
    
    public static Vector3 WithZ(this Vector3 vector3, float z)
    {
        return new Vector3(vector3.x, vector3.y, z);
    }        
}
using UnityEngine;

public static class AxisUtils
{
    public static int GetDominantAxis(Vector3 vector)
    {
        Vector3 absolute = new(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        if (absolute.x > absolute.y && absolute.x > absolute.z)
        {
            return 0;
        }

        return absolute.y > absolute.z ? 1 : 2;
    }

    public static Vector3 GetUnitAxis(int axis)
    {
        return axis switch
        {
            0 => Vector3.right,
            1 => Vector3.up,
            _ => Vector3.forward,
        };
    }

    public static float GetAxis(Vector3 vector, int axis)
    {
        return axis switch
        {
            0 => vector.x,
            1 => vector.y,
            _ => vector.z,
        };
    }

    public static void SetAxis(ref Vector3 vector, int axis, float value)
    {
        switch (axis)
        {
            case 0:
                vector.x = value;
                break;
            case 1:
                vector.y = value;
                break;
            default:
                vector.z = value;
                break;
        }
    }
}
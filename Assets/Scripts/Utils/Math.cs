using UnityEngine;

namespace Utils
{
    public static class Math
    {
        public static bool IsEqual(float a, float b)
        {
            if(Mathf.Abs(a - b) < 0.000001f)
            {
                return true;
            }
            return false;
        }

        public static Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float t)
        {
            t = Mathf.Clamp01(t);
            Matrix4x4 lerpMatrix = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                lerpMatrix.SetRow(i, Vector4.Lerp(from.GetRow(i), to.GetRow(i), t));
            }
            return lerpMatrix;
        }

        /// <summary>
        /// 对角度进行限制.
        /// </summary>
        public static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
    }
}
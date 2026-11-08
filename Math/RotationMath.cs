using System;
using System.Numerics;

namespace GimbalLockVisualizer.Math
{
    /// <summary>
    /// 萬向鎖數學核心與四元數解算器
    /// </summary>
    public static class RotationMath
    {
        public const float DegToRad = MathF.PI / 180f;
        public const float RadToDeg = 180f / MathF.PI;

        /// <summary>
        /// 計算 3x3 歐拉角旋轉矩陣 (依照 Y-X-Z 順序 / Yaw -> Pitch -> Roll)
        /// </summary>
        public static Matrix4x4 CalculateEulerMatrix(float yawDeg, float pitchDeg, float rollDeg)
        {
            float y = yawDeg * DegToRad;
            float p = pitchDeg * DegToRad;
            float r = rollDeg * DegToRad;

            // R = R_yaw(Y) * R_pitch(X) * R_roll(Z)
            Matrix4x4 mYaw = Matrix4x4.CreateRotationY(y);
            Matrix4x4 mPitch = Matrix4x4.CreateRotationX(p);
            Matrix4x4 mRoll = Matrix4x4.CreateRotationZ(r);

            // System.Numerics 是行優先矩陣，相乘為 mRoll * mPitch * mYaw 代表局部坐標系或反向變換
            // 這裡按標準物理階層外至內相乘:
            return mRoll * mPitch * mYaw;
        }

        /// <summary>
        /// 判斷是否處於萬向鎖狀態 (Pitch 接近 ±90 度)
        /// </summary>
        public static bool IsGimbalLocked(float pitchDeg, float threshold = 2.0f)
        {
            float norm = MathF.Abs(pitchDeg % 360f);
            if (norm > 180f) norm = 360f - norm;
            return MathF.Abs(norm - 90f) <= threshold;
        }

        /// <summary>
        /// 萬向鎖嚴重度 (0.0: 完全正常, 1.0: 嚴重死鎖)
        /// </summary>
        public static float GetLockSeverity(float pitchDeg)
        {
            float rad = pitchDeg * DegToRad;
            return MathF.Abs(MathF.Sin(rad));
        }

        /// <summary>
        /// 從歐拉角轉換為四元數 (Y-X-Z 階層)
        /// </summary>
        public static Quaternion EulerToQuaternion(float yawDeg, float pitchDeg, float rollDeg)
        {
            float y = yawDeg * DegToRad;
            float p = pitchDeg * DegToRad;
            float r = rollDeg * DegToRad;

            Quaternion qYaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
            Quaternion qPitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, p);
            Quaternion qRoll = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, r);

            // 結合順序: 外環 Yaw -> 中環 Pitch -> 內環 Roll
            return Quaternion.Normalize(qYaw * qPitch * qRoll);
        }

        /// <summary>
        /// 歐拉角線性插值 (Euler Lerp)
        /// </summary>
        public static (float Yaw, float Pitch, float Roll) EulerLerp(
            (float y, float p, float r) start,
            (float y, float p, float r) end,
            float t)
        {
            return (
                start.y + (end.y - start.y) * t,
                start.p + (end.p - start.p) * t,
                start.r + (end.r - start.r) * t
            );
        }

        /// <summary>
        /// 四元數球面線性插值 (Quaternion Slerp)
        /// 永遠在 4D 單位超球面上沿最短大圓弧旋轉，無任何奇點！
        /// </summary>
        public static Quaternion QuaternionSlerp(Quaternion start, Quaternion end, float t)
        {
            return Quaternion.Slerp(start, end, t);
        }
    }
}


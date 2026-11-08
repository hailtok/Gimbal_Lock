using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace GimbalLockVisualizer.Models
{
    /// <summary>
    /// 3D 多邊形面
    /// </summary>
    public class Polygon3D
    {
        public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
        public Color BaseColor { get; set; }
        public Vector3 Normal { get; set; }
        public float AverageZ { get; set; }
    }

    /// <summary>
    /// 3D 幾何模型生成器 (飛機、萬向環、坐標軸)
    /// </summary>
    public static class AircraftModel
    {
        /// <summary>
        /// 建立 3D 飛機多邊形面集合
        /// </summary>
        public static List<Polygon3D> CreateAircraft(float scale = 1.0f)
        {
            var list = new List<Polygon3D>();

            // 機身長度沿 Z 軸 (正 Z 為機頭，負 Z 為機尾)
            Vector3 nose = new Vector3(0, 0, 1.8f) * scale;
            Vector3 cabinTop = new Vector3(0, 0.35f, 0.4f) * scale;
            Vector3 fuselageRearTop = new Vector3(0, 0.2f, -1.2f) * scale;
            Vector3 fuselageRearBottom = new Vector3(0, -0.2f, -1.2f) * scale;
            Vector3 leftWingTip = new Vector3(-1.8f, 0, -0.6f) * scale;
            Vector3 rightWingTip = new Vector3(1.8f, 0, -0.6f) * scale;
            Vector3 wingRootFront = new Vector3(0, 0, 0.6f) * scale;
            Vector3 wingRootRear = new Vector3(0, 0, -1.0f) * scale;
            Vector3 tailFinTop = new Vector3(0, 0.9f, -1.3f) * scale;
            Vector3 tailFinRootFront = new Vector3(0, 0.15f, -0.6f) * scale;
            Vector3 tailFinRootRear = new Vector3(0, 0.15f, -1.4f) * scale;

            // 1. 機頭到駕駛艙
            list.Add(new Polygon3D
            {
                Vertices = new[] { nose, new Vector3(-0.3f, 0, 0.4f) * scale, cabinTop },
                BaseColor = Color.FromRgb(56, 189, 248) // 淺藍
            });
            list.Add(new Polygon3D
            {
                Vertices = new[] { nose, cabinTop, new Vector3(0.3f, 0, 0.4f) * scale },
                BaseColor = Color.FromRgb(3, 105, 161) // 深藍
            });

            // 2. 機身底部
            Vector3 belly = new Vector3(0, -0.3f, 0.2f) * scale;
            list.Add(new Polygon3D
            {
                Vertices = new[] { nose, belly, new Vector3(-0.3f, 0, 0.4f) * scale },
                BaseColor = Color.FromRgb(148, 163, 184)
            });
            list.Add(new Polygon3D
            {
                Vertices = new[] { nose, new Vector3(0.3f, 0, 0.4f) * scale, belly },
                BaseColor = Color.FromRgb(100, 116, 139)
            });

            // 3. 左主機翼 (三角翼)
            list.Add(new Polygon3D
            {
                Vertices = new[] { wingRootFront, leftWingTip, wingRootRear },
                BaseColor = Color.FromRgb(99, 102, 241) // 靛藍
            });
            // 4. 右主機翼
            list.Add(new Polygon3D
            {
                Vertices = new[] { wingRootFront, wingRootRear, rightWingTip },
                BaseColor = Color.FromRgb(79, 70, 229)
            });

            // 5. 垂直尾翼
            list.Add(new Polygon3D
            {
                Vertices = new[] { tailFinRootFront, tailFinTop, tailFinRootRear },
                BaseColor = Color.FromRgb(244, 63, 94) // 玫瑰紅尾翼
            });

            return list;
        }

        /// <summary>
        /// 生成環狀 3D 線段點陣 (Circle Ring)
        /// </summary>
        public static Vector3[] CreateRingPoints(float radius, int segments, char axis)
        {
            var pts = new Vector3[segments + 1];
            float step = (MathF.PI * 2f) / segments;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * step;
                float cos = MathF.Cos(angle) * radius;
                float sin = MathF.Sin(angle) * radius;

                pts[i] = axis switch
                {
                    'Y' => new Vector3(cos, 0, sin), // 水平環 (繞 Y 軸)
                    'X' => new Vector3(0, cos, sin), // 垂直環 (繞 X 軸)
                    'Z' => new Vector3(cos, sin, 0), // 垂直正面環 (繞 Z 軸)
                    _ => new Vector3(cos, sin, 0)
                };
            }

            return pts;
        }
    }
}


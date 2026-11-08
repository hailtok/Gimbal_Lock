using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GimbalLockVisualizer.Math;
using GimbalLockVisualizer.Models;

namespace GimbalLockVisualizer.Controls
{
    public enum ViewMode
    {
        SingleGimbal,
        Comparison
    }

    /// <summary>
    /// 高效能互動式 3D 向量畫布 (WPF Vector 3D Canvas)
    /// </summary>
    public class GimbalVisualizerControl : FrameworkElement
    {
        // 攝影機姿態
        private float _camYaw = -35f;
        private float _camPitch = 25f;
        private float _camDistance = 9.5f;
        private Point _lastMousePos;
        private bool _isDragging;

        // 當前歐拉角 (度)
        public float Yaw { get; set; } = 0f;
        public float Pitch { get; set; } = 0f;
        public float Roll { get; set; } = 0f;

        // 當前四元數
        public Quaternion CurrentQuaternion { get; set; } = Quaternion.Identity;

        // 比較模式專用參數
        public ViewMode Mode { get; set; } = ViewMode.SingleGimbal;
        public float InterpT { get; set; } = 0f;
        public (float y, float p, float r) InterpEulerStart { get; set; } = (0, 0, 0);
        public (float y, float p, float r) InterpEulerEnd { get; set; } = (180, 90, 180);
        public Quaternion InterpQuatStart { get; set; } = Quaternion.Identity;
        public Quaternion InterpQuatEnd { get; set; } = Quaternion.Identity;

        // 萬向環半徑
        private const float OuterRingRadius = 3.6f;
        private const float MiddleRingRadius = 2.8f;
        private const float InnerRingRadius = 2.0f;

        // 預置幾何線段
        private readonly Vector3[] _outerRingPts;
        private readonly Vector3[] _middleRingPts;
        private readonly Vector3[] _innerRingPts;
        private readonly List<Polygon3D> _aircraftPolys;

        // 畫筆與筆刷快取
        private readonly Pen _outerRingPen;
        private readonly Pen _middleRingPen;
        private readonly Pen _innerRingPen;
        private readonly Pen _lockLinePen;
        private readonly Pen _axisXPen;
        private readonly Pen _axisYPen;
        private readonly Pen _axisZPen;
        private readonly Pen _pivotPen;
        private readonly Brush _gridBrush;
        private readonly Typeface _typeface;

        public GimbalVisualizerControl()
        {
            ClipToBounds = true;
            Focusable = true;

            // 初始化幾何點
            _outerRingPts = AircraftModel.CreateRingPoints(OuterRingRadius, 72, 'Y');
            _middleRingPts = AircraftModel.CreateRingPoints(MiddleRingRadius, 72, 'X');
            _innerRingPts = AircraftModel.CreateRingPoints(InnerRingRadius, 72, 'Z');
            _aircraftPolys = AircraftModel.CreateAircraft(1.0f);

            // 畫筆
            _outerRingPen = new Pen(new SolidColorBrush(Color.FromRgb(16, 185, 129)), 3.5); // 綠色 Yaw
            _middleRingPen = new Pen(new SolidColorBrush(Color.FromRgb(239, 68, 68)), 3.5); // 紅色 Pitch
            _innerRingPen = new Pen(new SolidColorBrush(Color.FromRgb(59, 130, 246)), 3.5); // 藍色 Roll
            _lockLinePen = new Pen(new SolidColorBrush(Color.FromRgb(255, 0, 128)), 3.0)
            {
                DashStyle = new DashStyle(new double[] { 4, 3 }, 0)
            };

            _axisXPen = new Pen(new SolidColorBrush(Color.FromRgb(248, 113, 113)), 2.0);
            _axisYPen = new Pen(new SolidColorBrush(Color.FromRgb(74, 222, 128)), 2.0);
            _axisZPen = new Pen(new SolidColorBrush(Color.FromRgb(96, 165, 250)), 2.0);
            _pivotPen = new Pen(Brushes.WhiteSmoke, 2.0);
            _gridBrush = new SolidColorBrush(Color.FromArgb(40, 148, 163, 184));
            _typeface = new Typeface(new FontFamily("Segoe UI, Microsoft JhengHei"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

            // 滑鼠事件註冊
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
                _lastMousePos = e.GetPosition(this);
                CaptureMouse();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                // 重設視角
                _camYaw = -35f;
                _camPitch = 25f;
                _camDistance = 9.5f;
                InvalidateVisual();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point current = e.GetPosition(this);
                float dx = (float)(current.X - _lastMousePos.X);
                float dy = (float)(current.Y - _lastMousePos.Y);

                _camYaw += dx * 0.5f;
                _camPitch = System.Math.Clamp(_camPitch - dy * 0.5f, -85f, 85f);

                _lastMousePos = current;
                InvalidateVisual();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _camDistance = System.Math.Clamp(_camDistance - (e.Delta / 120f) * 0.8f, 3.5f, 22f);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 10 || h <= 10) return;

            // 背景繪製 (深邃太空深藍灰漸層)
            var bgBrush = new LinearGradientBrush(
                Color.FromRgb(10, 14, 23),
                Color.FromRgb(15, 23, 42),
                new Point(0, 0), new Point(0, 1)
            );
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));

            // 計算攝影機矩陣
            float camYawRad = _camYaw * RotationMath.DegToRad;
            float camPitchRad = _camPitch * RotationMath.DegToRad;

            Vector3 camPos = new Vector3(
                _camDistance * MathF.Cos(camPitchRad) * MathF.Sin(camYawRad),
                _camDistance * MathF.Sin(camPitchRad),
                _camDistance * MathF.Cos(camPitchRad) * MathF.Cos(camYawRad)
            );

            Matrix4x4 viewMat = Matrix4x4.CreateLookAt(camPos, Vector3.Zero, Vector3.UnitY);
            Matrix4x4 projMat = Matrix4x4.CreatePerspectiveFieldOfView(45f * RotationMath.DegToRad, (float)(w / h), 0.1f, 100f);
            Matrix4x4 viewProj = viewMat * projMat;

            // 繪製微弱地平網格 (地面深度參考)
            DrawGroundGrid(dc, viewProj, w, h);

            if (Mode == ViewMode.SingleGimbal)
            {
                DrawSingleGimbalSystem(dc, viewProj, w, h);
            }
            else
            {
                DrawComparisonSystem(dc, viewProj, w, h);
            }
        }

        private void DrawGroundGrid(DrawingContext dc, Matrix4x4 viewProj, double w, double h)
        {
            float yGround = -4.2f;
            float size = 8f;
            float step = 2f;
            Pen gridPen = new Pen(_gridBrush, 1.0);

            for (float x = -size; x <= size; x += step)
            {
                ProjectAndDrawLine(dc, viewProj, w, h, new Vector3(x, yGround, -size), new Vector3(x, yGround, size), gridPen);
            }
            for (float z = -size; z <= size; z += step)
            {
                ProjectAndDrawLine(dc, viewProj, w, h, new Vector3(-size, yGround, z), new Vector3(size, yGround, z), gridPen);
            }
        }

        private void DrawSingleGimbalSystem(DrawingContext dc, Matrix4x4 viewProj, double w, double h)
        {
            float yawRad = Yaw * RotationMath.DegToRad;
            float pitchRad = Pitch * RotationMath.DegToRad;
            float rollRad = Roll * RotationMath.DegToRad;

            // 嚴格依物理萬向環階層相乘變換:
            // 1. 外環: 僅受 Yaw 影響 (繞全域 Y 軸)
            Matrix4x4 mOuter = Matrix4x4.CreateRotationY(yawRad);

            // 2. 中環: 嵌套於外環內部，其自身轉軸為 X 軸 (繞 X 軸轉 pitchRad)
            Matrix4x4 mMiddle = Matrix4x4.CreateRotationX(pitchRad) * mOuter;

            // 3. 內環: 嵌套於中環內部，其自身轉軸為 Z 軸 (繞 Z 軸轉 rollRad)
            Matrix4x4 mInner = Matrix4x4.CreateRotationZ(rollRad) * mMiddle;

            // 4. 搭載物 (飛機): 固定於內環上
            Matrix4x4 mAircraft = mInner;

            // 是否觸發萬向鎖
            bool isLocked = RotationMath.IsGimbalLocked(Pitch);

            // A. 繪製基座立柱
            DrawBaseStand(dc, viewProj, w, h);

            // B. 繪製外環 (Yaw / 綠色)
            DrawTransformedRing(dc, viewProj, w, h, _outerRingPts, mOuter, _outerRingPen);
            DrawBearingPivots(dc, viewProj, w, h, OuterRingRadius, mOuter, 'X', Brushes.LightGreen);

            // C. 繪製中環 (Pitch / 紅色)
            DrawTransformedRing(dc, viewProj, w, h, _middleRingPts, mMiddle, _middleRingPen);
            DrawBearingPivots(dc, viewProj, w, h, MiddleRingRadius, mMiddle, 'Z', Brushes.LightCoral);

            // D. 繪製內環 (Roll / 藍色)
            DrawTransformedRing(dc, viewProj, w, h, _innerRingPts, mInner, _innerRingPen);
            DrawBearingPivots(dc, viewProj, w, h, InnerRingRadius, mInner, 'Y', Brushes.SkyBlue);

            // E. 繪製中心飛機多邊形
            DrawAircraftPolygons(dc, viewProj, w, h, mAircraft, 1.0f);

            // F. 繪製局部坐標軸
            DrawAxes(dc, viewProj, w, h, mAircraft, 2.2f);

            // G. 若處於萬向鎖狀態，繪製醒目的「軸向重疊共線射線」
            if (isLocked)
            {
                // 當 Pitch = 90度時，內環的轉軸(Z軸)轉向了全域 Y 軸方向，與外環轉軸重合！
                Vector3 lockAxisTop = new Vector3(0, 5.5f, 0);
                Vector3 lockAxisBottom = new Vector3(0, -5.5f, 0);
                ProjectAndDrawLine(dc, viewProj, w, h, lockAxisBottom, lockAxisTop, _lockLinePen);

                Point? pt = ProjectPoint(lockAxisTop, viewProj, w, h);
                if (pt.HasValue)
                {
                    var ft = new FormattedText("⚠️ 萬向鎖共線軸 (Yaw 軸與 Roll 軸完全重疊！)",
                        CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, 13, Brushes.Magenta, 1.0);
                    dc.DrawText(ft, new Point(pt.Value.X - ft.Width / 2, pt.Value.Y - 22));
                }
            }
        }

        private void DrawComparisonSystem(DrawingContext dc, Matrix4x4 viewProj, double w, double h)
        {
            // 雙機並列插值對比
            // 左側: 歐拉線性插值
            var curEuler = RotationMath.EulerLerp(InterpEulerStart, InterpEulerEnd, InterpT);
            Matrix4x4 mEulerAircraft =
                Matrix4x4.CreateRotationZ(curEuler.Roll * RotationMath.DegToRad) *
                Matrix4x4.CreateRotationX(curEuler.Pitch * RotationMath.DegToRad) *
                Matrix4x4.CreateRotationY(curEuler.Yaw * RotationMath.DegToRad) *
                Matrix4x4.CreateTranslation(-3.8f, 0, 0);

            // 右側: 四元數 Slerp 插值
            Quaternion curQuat = RotationMath.QuaternionSlerp(InterpQuatStart, InterpQuatEnd, InterpT);
            Matrix4x4 mQuatRot = Matrix4x4.CreateFromQuaternion(curQuat);
            Matrix4x4 mQuatAircraft = mQuatRot * Matrix4x4.CreateTranslation(3.8f, 0, 0);

            // 繪製左機 (歐拉角插值 - 紅色/橘色色調)
            DrawAircraftPolygons(dc, viewProj, w, h, mEulerAircraft, 1.1f, Color.FromRgb(239, 68, 68));
            DrawAxes(dc, viewProj, w, h, mEulerAircraft, 2.0f);

            // 繪製右機 (四元數 Slerp - 紫色/科技藍色調)
            DrawAircraftPolygons(dc, viewProj, w, h, mQuatAircraft, 1.1f, Color.FromRgb(168, 85, 247));
            DrawAxes(dc, viewProj, w, h, mQuatAircraft, 2.0f);

            // 標註文字
            Point? pLeft = ProjectPoint(new Vector3(-3.8f, 2.8f, 0), viewProj, w, h);
            if (pLeft.HasValue)
            {
                var ft = new FormattedText("❌ 歐拉角線性插值 (Euler Lerp)\n路徑經過 90° 俯仰奇點產生翻轉抽搐",
                    CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, 13, Brushes.Salmon, 1.0);
                dc.DrawText(ft, new Point(pLeft.Value.X - ft.Width / 2, pLeft.Value.Y));
            }

            Point? pRight = ProjectPoint(new Vector3(3.8f, 2.8f, 0), viewProj, w, h);
            if (pRight.HasValue)
            {
                var ft = new FormattedText("✨ 四元數球面插值 (Quaternion Slerp)\n永遠沿 4D 單位超球面最短大圓弧平滑旋轉",
                    CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, 13, Brushes.MediumPurple, 1.0);
                dc.DrawText(ft, new Point(pRight.Value.X - ft.Width / 2, pRight.Value.Y));
            }
        }

        private void DrawBaseStand(DrawingContext dc, Matrix4x4 viewProj, double w, double h)
        {
            Pen standPen = new Pen(new SolidColorBrush(Color.FromRgb(71, 85, 105)), 3.0);
            Vector3 bottomCenter = new Vector3(0, -4.2f, 0);
            Vector3 ringBottom = new Vector3(0, -OuterRingRadius - 0.2f, 0);

            // 繪製垂直立柱
            ProjectAndDrawLine(dc, viewProj, w, h, bottomCenter, ringBottom, standPen);

            // 繪製底盤三角撐
            Vector3 leg1 = bottomCenter + new Vector3(-1.4f, 0, -1.0f);
            Vector3 leg2 = bottomCenter + new Vector3(1.4f, 0, -1.0f);
            Vector3 leg3 = bottomCenter + new Vector3(0, 0, 1.6f);
            ProjectAndDrawLine(dc, viewProj, w, h, bottomCenter, leg1, standPen);
            ProjectAndDrawLine(dc, viewProj, w, h, bottomCenter, leg2, standPen);
            ProjectAndDrawLine(dc, viewProj, w, h, bottomCenter, leg3, standPen);
        }

        private void DrawTransformedRing(DrawingContext dc, Matrix4x4 viewProj, double w, double h, Vector3[] ringPts, Matrix4x4 transform, Pen pen)
        {
            var projectedPts = new Point[ringPts.Length];
            for (int i = 0; i < ringPts.Length; i++)
            {
                Vector3 worldPt = Vector3.Transform(ringPts[i], transform);
                Point? p = ProjectPoint(worldPt, viewProj, w, h);
                if (!p.HasValue) return;
                projectedPts[i] = p.Value;
            }

            StreamGeometry geom = new StreamGeometry();
            using (StreamGeometryContext ctx = geom.Open())
            {
                ctx.BeginFigure(projectedPts[0], false, true);
                ctx.PolyLineTo(projectedPts.Skip(1).ToList(), true, true);
            }
            geom.Freeze();
            dc.DrawGeometry(null, pen, geom);
        }

        private void DrawBearingPivots(DrawingContext dc, Matrix4x4 viewProj, double w, double h, float radius, Matrix4x4 transform, char axis, Brush brush)
        {
            Vector3 v1, v2;
            if (axis == 'X')
            {
                v1 = new Vector3(radius, 0, 0);
                v2 = new Vector3(-radius, 0, 0);
            }
            else if (axis == 'Z')
            {
                v1 = new Vector3(0, 0, radius);
                v2 = new Vector3(0, 0, -radius);
            }
            else
            {
                v1 = new Vector3(0, radius, 0);
                v2 = new Vector3(0, -radius, 0);
            }

            Vector3 w1 = Vector3.Transform(v1, transform);
            Vector3 w2 = Vector3.Transform(v2, transform);

            Point? p1 = ProjectPoint(w1, viewProj, w, h);
            Point? p2 = ProjectPoint(w2, viewProj, w, h);

            if (p1.HasValue) dc.DrawEllipse(brush, _pivotPen, p1.Value, 5, 5);
            if (p2.HasValue) dc.DrawEllipse(brush, _pivotPen, p2.Value, 5, 5);
        }

        private void DrawAircraftPolygons(DrawingContext dc, Matrix4x4 viewProj, double w, double h, Matrix4x4 transform, float scale, Color? overrideColor = null)
        {
            // 繪製前先依照 Z 深度排序 (Painter's Algorithm)
            var transformedPolys = new List<(Point[] screenPts, Color color, float depth)>();

            foreach (var poly in _aircraftPolys)
            {
                var screenPts = new Point[poly.Vertices.Length];
                float totalZ = 0;
                bool valid = true;

                for (int i = 0; i < poly.Vertices.Length; i++)
                {
                    Vector3 worldV = Vector3.Transform(poly.Vertices[i] * scale, transform);
                    totalZ += worldV.Z;
                    Point? p = ProjectPoint(worldV, viewProj, w, h);
                    if (!p.HasValue) { valid = false; break; }
                    screenPts[i] = p.Value;
                }

                if (valid)
                {
                    Color finalColor = overrideColor ?? poly.BaseColor;
                    transformedPolys.Add((screenPts, finalColor, totalZ / poly.Vertices.Length));
                }
            }

            // 由遠及近繪製
            foreach (var item in transformedPolys.OrderBy(p => p.depth))
            {
                StreamGeometry geom = new StreamGeometry();
                using (StreamGeometryContext ctx = geom.Open())
                {
                    ctx.BeginFigure(item.screenPts[0], true, true);
                    ctx.PolyLineTo(item.screenPts.Skip(1).ToList(), true, true);
                }
                geom.Freeze();

                var brush = new SolidColorBrush(item.color);
                var stroke = new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 1.0);
                dc.DrawGeometry(brush, stroke, geom);
            }
        }

        private void DrawAxes(DrawingContext dc, Matrix4x4 viewProj, double w, double h, Matrix4x4 transform, float len)
        {
            Vector3 origin = Vector3.Transform(Vector3.Zero, transform);
            Vector3 axX = Vector3.Transform(Vector3.UnitX * len, transform);
            Vector3 axY = Vector3.Transform(Vector3.UnitY * len, transform);
            Vector3 axZ = Vector3.Transform(Vector3.UnitZ * len, transform);

            ProjectAndDrawLine(dc, viewProj, w, h, origin, axX, _axisXPen);
            ProjectAndDrawLine(dc, viewProj, w, h, origin, axY, _axisYPen);
            ProjectAndDrawLine(dc, viewProj, w, h, origin, axZ, _axisZPen);
        }

        private void ProjectAndDrawLine(DrawingContext dc, Matrix4x4 viewProj, double w, double h, Vector3 v1, Vector3 v2, Pen pen)
        {
            Point? p1 = ProjectPoint(v1, viewProj, w, h);
            Point? p2 = ProjectPoint(v2, viewProj, w, h);
            if (p1.HasValue && p2.HasValue)
            {
                dc.DrawLine(pen, p1.Value, p2.Value);
            }
        }

        private Point? ProjectPoint(Vector3 v, Matrix4x4 viewProj, double w, double h)
        {
            Vector4 clip = Vector4.Transform(new Vector4(v, 1.0f), viewProj);
            if (clip.W <= 0.001f) return null; // 位於攝影機後方

            float ndcX = clip.X / clip.W;
            float ndcY = clip.Y / clip.W;

            double screenX = (ndcX + 1.0) * 0.5 * w;
            double screenY = (1.0 - ndcY) * 0.5 * h;

            return new Point(screenX, screenY);
        }
    }
}


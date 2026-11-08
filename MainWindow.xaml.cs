using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using GimbalLockVisualizer.Controls;
using GimbalLockVisualizer.Math;

namespace GimbalLockVisualizer
{
    /// <summary>
    /// 萬向鎖主視窗邏輯控制器
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _interpTimer;
        private readonly DispatcherTimer _cancelTestTimer;
        private float _interpT = 0f;
        private float _cancelTestStep = 0f;
        private bool _isInitialized = false;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _isInitialized = true;

                // 插值動畫計時器 (60 FPS)
                _interpTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                _interpTimer.Tick += OnInterpTimerTick;

                // 對消測試計時器
                _cancelTestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
                _cancelTestTimer.Tick += OnCancelTestTimerTick;

                Loaded += (s, e) =>
                {
                    UpdateState();
                };
            }
            catch (Exception ex)
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "init_error.txt");
                File.WriteAllText(logPath, ex.ToString());
                throw;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isInitialized) return;
            if (Canvas3D == null || SliderYaw == null || SliderPitch == null || SliderRoll == null) return;

            Canvas3D.Yaw = (float)SliderYaw.Value;
            Canvas3D.Pitch = (float)SliderPitch.Value;
            Canvas3D.Roll = (float)SliderRoll.Value;

            UpdateState();
        }

        private void UpdateState()
        {
            if (!_isInitialized) return;
            if (Canvas3D == null || TxtYawVal == null || TxtPitchVal == null || TxtRollVal == null ||
                TxtMatrixDisplay == null || TxtQuatDisplay == null || StatusBorder == null ||
                StatusDot == null || StatusText == null || WarningBox == null)
            {
                return;
            }

            float y = Canvas3D.Yaw;
            float p = Canvas3D.Pitch;
            float r = Canvas3D.Roll;

            // 更新數值文字
            TxtYawVal.Text = $"{y:0}°";
            TxtPitchVal.Text = $"{p:0}°";
            TxtRollVal.Text = $"{r:0}°";

            // 計算旋轉矩陣與四元數
            Matrix4x4 m = RotationMath.CalculateEulerMatrix(y, p, r);
            Quaternion q = RotationMath.EulerToQuaternion(y, p, r);
            Canvas3D.CurrentQuaternion = q;

            // 判斷萬向鎖狀態
            bool isLocked = RotationMath.IsGimbalLocked(p);

            if (isLocked)
            {
                WarningBox.Visibility = Visibility.Visible;
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(69, 10, 10));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                StatusText.Text = "⚠️ 萬向鎖觸發！(Roll 軸與 Yaw 軸共線重合)";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
            }
            else
            {
                WarningBox.Visibility = Visibility.Collapsed;
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(6, 78, 59));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(5, 150, 105));
                StatusDot.Fill = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                StatusText.Text = "系統正常 (具備 3 個完整旋轉自由度)";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
            }

            // 更新矩陣看板文字
            UpdateMatrixDisplay(m, y, p, r, isLocked);

            // 更新四元數看板文字
            UpdateQuaternionDisplay(q);

            Canvas3D.InvalidateVisual();
        }

        private void UpdateMatrixDisplay(Matrix4x4 m, float y, float p, float r, bool isLocked)
        {
            var sb = new StringBuilder();
            sb.AppendLine("┌                                     ┐");
            sb.AppendLine($"│  {m.M11,7:F3}  {m.M12,7:F3}  {m.M13,7:F3}  │");
            sb.AppendLine($"│  {m.M21,7:F3}  {m.M22,7:F3}  {m.M23,7:F3}  │");
            sb.AppendLine($"│  {m.M31,7:F3}  {m.M32,7:F3}  {m.M33,7:F3}  │");
            sb.AppendLine("└                                     ┘");
            sb.AppendLine();

            if (isLocked)
            {
                float sum = y + r;
                float diff = y - r;
                sb.AppendLine("⚠️ 奇點偵測 (Pitch ≈ ±90°):");
                sb.AppendLine("cos(Pitch) ≈ 0, 自由度退化為 (Yaw ± Roll)");
                sb.AppendLine($"目前合角 (Yaw + Roll) = {sum:0.0}°");
                sb.AppendLine($"目前差角 (Yaw - Roll) = {diff:0.0}°");
                sb.AppendLine("最外環與最內環轉軸重合，失去 1 個自由度！");
            }
            else
            {
                sb.AppendLine("正交旋轉矩陣 (Orthogonal Matrix)");
                sb.AppendLine("行列式 det(R) = 1.000");
                sb.AppendLine("具備完整 3 自由度空間姿態控制能力");
            }

            TxtMatrixDisplay.Text = sb.ToString();
        }

        private void UpdateQuaternionDisplay(Quaternion q)
        {
            float norm = MathF.Sqrt(q.W * q.W + q.X * q.X + q.Y * q.Y + q.Z * q.Z);
            var sb = new StringBuilder();
            sb.AppendLine($"w (標量) : {q.W,7:F4}");
            sb.AppendLine($"x (向偏) : {q.X,7:F4}");
            sb.AppendLine($"y (偏航) : {q.Y,7:F4}");
            sb.AppendLine($"z (翻滾) : {q.Z,7:F4}");
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"單位模長 ‖q‖ = {norm:F4} (無奇點)");

            TxtQuatDisplay.Text = sb.ToString();
        }

        #region 按鈕操作

        private void BtnLock90_Click(object sender, RoutedEventArgs e)
        {
            if (SliderPitch != null) SliderPitch.Value = 90;
        }

        private void BtnLockNeg90_Click(object sender, RoutedEventArgs e)
        {
            if (SliderPitch != null) SliderPitch.Value = -90;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            _cancelTestTimer?.Stop();
            _interpTimer?.Stop();
            if (SliderYaw != null) SliderYaw.Value = 0;
            if (SliderPitch != null) SliderPitch.Value = 0;
            if (SliderRoll != null) SliderRoll.Value = 0;
        }

        private void BtnCancelTest_Click(object sender, RoutedEventArgs e)
        {
            if (SliderPitch == null || SliderYaw == null || SliderRoll == null) return;

            // 先固定俯仰在 90 度
            SliderPitch.Value = 90;
            SliderYaw.Value = 0;
            SliderRoll.Value = 0;
            _cancelTestStep = 0;
            _cancelTestTimer?.Start();
        }

        private void OnCancelTestTimerTick(object? sender, EventArgs e)
        {
            _cancelTestStep += 1.5f;
            if (_cancelTestStep > 90f)
            {
                _cancelTestTimer?.Stop();
                return;
            }

            // 同時旋轉 Yaw 增加，Roll 減少
            if (SliderYaw != null) SliderYaw.Value = _cancelTestStep;
            if (SliderRoll != null) SliderRoll.Value = -_cancelTestStep;
        }

        #endregion

        #region 視圖切換與插值測試

        private void BtnViewSingle_Click(object sender, RoutedEventArgs e)
        {
            _interpTimer?.Stop();
            if (Canvas3D != null) Canvas3D.Mode = ViewMode.SingleGimbal;
            if (InterpControlBox != null) InterpControlBox.Visibility = Visibility.Collapsed;
            Canvas3D?.InvalidateVisual();
        }

        private void BtnViewCompare_Click(object sender, RoutedEventArgs e)
        {
            if (Canvas3D == null) return;
            Canvas3D.Mode = ViewMode.Comparison;
            if (InterpControlBox != null) InterpControlBox.Visibility = Visibility.Visible;

            // 設定極端跨越 Pitch 90 度的插值起迄點
            Canvas3D.InterpEulerStart = (0, 0, 0);
            Canvas3D.InterpEulerEnd = (179f, 90f, 179f);

            Canvas3D.InterpQuatStart = RotationMath.EulerToQuaternion(0, 0, 0);
            Canvas3D.InterpQuatEnd = RotationMath.EulerToQuaternion(179f, 90f, 179f);

            _interpT = 0f;
            Canvas3D.InterpT = 0f;
            if (ProgInterp != null) ProgInterp.Value = 0;
            if (TxtInterpProgress != null) TxtInterpProgress.Text = "0%";

            Canvas3D.InvalidateVisual();
        }

        private void BtnViewTheory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "【萬向鎖 (Gimbal Lock) 原理與四元數解決之道】\n\n" +
                "1. 核心成因：\n" +
                "   歐拉角依照固定先後順序連續相乘三組旋轉矩陣。當中間的俯仰軸 (Pitch) 旋轉到 ±90° 時，\n" +
                "   外層轉軸 (Yaw) 與最內層轉軸 (Roll) 在空間中會完全共線！\n" +
                "   此時 3 個自由度退化為 2 個自由度，無法在空間中直接繞第三維度旋轉。\n\n" +
                "2. 動畫與姿態控制中的致命問題：\n" +
                "   當關鍵影格在萬向鎖奇點附近進行線性插值 (Euler Lerp) 時，兩軸會發生數值突變，\n" +
                "   導致物體或人體骨骼產生瞬間 180° 的不自然翻轉與劇烈抽搐！\n\n" +
                "3. 終極解決方案：四元數 (Quaternion)\n" +
                "   四元數以四維超球面 (S³) 表示旋轉，具備無奇點的拓撲結構。\n" +
                "   使用四元數球面線性插值 (Quaternion.Slerp)，保證旋轉永遠沿著最短大圓弧平滑過渡，\n" +
                "   徹底根除了萬向鎖與軸向翻轉問題！\n\n" +
                "專案原始碼目錄下的 README.md 提供了完整的數學證明矩陣與實作細節。",
                "萬向鎖原理解析與教學",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnPlayInterp_Click(object sender, RoutedEventArgs e)
        {
            _interpT = 0f;
            if (Canvas3D != null) Canvas3D.InterpT = 0f;
            if (ProgInterp != null) ProgInterp.Value = 0;
            _interpTimer?.Start();
        }

        private void OnInterpTimerTick(object? sender, EventArgs e)
        {
            _interpT += 0.012f;
            if (_interpT >= 1.0f)
            {
                _interpT = 1.0f;
                _interpTimer?.Stop();
            }

            if (Canvas3D != null) Canvas3D.InterpT = _interpT;
            if (ProgInterp != null) ProgInterp.Value = _interpT;
            if (TxtInterpProgress != null) TxtInterpProgress.Text = $"{(_interpT * 100):0}%";
            Canvas3D?.InvalidateVisual();
        }

        #endregion
    }
}

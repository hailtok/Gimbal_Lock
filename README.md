# 萬向鎖 (Gimbal Lock) 3D 原理解析與四元數解算器 (C# / WPF)

本專案是一個基於 **C# / WPF (.NET 8)** 開發的原生桌面 3D 視覺化實驗系統。專門為深入理解三維旋轉中著名的 **萬向鎖 (Gimbal Lock)** 問題、其底層數學成因，以及如何使用 **四元數 (Quaternion)** 徹底解決此問題而設計。

---

## 📽️ 專案核心功能

### 1. 實體三層萬向環 3D 互動模擬 (`SingleGimbal View`)
- **三層嵌套物理環即時旋轉**：
  - **外環 (綠色 - Yaw / Y 軸偏航)**：繞全域 Y 軸旋轉。
  - **中環 (紅色 - Pitch / X 軸俯仰)**：嵌套於外環內部，轉軸為 X 軸。
  - **內環 (藍色 - Roll / Z 軸翻滾)**：嵌套於中環內部，轉軸為 Z 軸。
  - **中心搭載物 (3D 戰鬥機)**：固定於最內層環上，具備駕駛艙、三角翼與垂直尾翼，隨萬向環姿態即時轉向。
- **即時攝影機視角**：
  - 支援滑鼠左鍵自由拖曳旋轉 3D 視角、滑鼠滾輪自由縮放、滑鼠右鍵快速復位。
- **一鍵重現萬向鎖**：
  - 點擊「💥 一鍵觸發萬向鎖 (Pitch = 90°)」，俯仰環轉為 $90^\circ$。
  - 畫面即時亮起 **洋紅色同軸共線警示射線**，直觀看見 **綠色外環轉軸與藍色內環轉軸完全重疊**！
  - 狀態列彈出紅色危險警報：`⚠️ 萬向鎖觸發！(Roll 軸與 Yaw 軸共線重合，失去 1 個獨立自由度)`。
- **自由度退化實證測試**：
  - 點擊「🔄 實證：Yaw 與 Roll 對消測試」，系統會在 Pitch = $90^\circ$ 下同時以相反角速度轉動 Yaw 與 Roll。
  - 您可以親眼見證：**外環與內環都在劇烈轉動，但中心飛機在空間中的朝向完全靜止不動**！這直接證明了兩者在三維空間中只能控制同一個旋轉維度。

### 2. 歐拉角 vs 四元數插值動態對決 (`Comparison View`)
- **左側飛機 (紅色 - 歐拉角線性插值 Euler Lerp)**：
  - 旋轉路徑經過 Pitch = $90^\circ$ 奇點時，機身發生不可預測的劇烈抽搐、軸向突然翻折 180 度。
- **右側飛機 (紫色 - 四元數球面插值 Quaternion Slerp)**：
  - 永遠在四維超球面上沿最短大圓弧旋轉，角速度恆定均勻，軌跡優雅平滑，完全沒有奇點死鎖問題！
- 內建 60 FPS 平滑進度播放器與時間軸進度條。

### 3. 即時旋轉矩陣與四元數演算看板
- **即時 $3 \times 3$ 正交旋轉矩陣**：
  - 顯示目前姿勢所對應的旋轉矩陣數值，並動態判定行列式 $\det(R) = 1.000$。
  - 當進入死鎖時，動態標註 $\cos(\text{Pitch}) \to 0$ 導致參數退化為 $(\text{Yaw} \pm \text{Roll})$ 的數學奇點。
- **即時四元數數值監視器**：
  - 顯示標量 $w$ 與三維向量部 $(x, y, z)$，並實時計算單位模長 $\|q\| = 1.0000$。

---

## 🧠 萬向鎖的本質與數學證明

### 1. 什麼是萬向鎖 (Gimbal Lock)？
萬向節 (Gimbal) 是由相互垂直的旋轉軸所構成的機械機構。在三維空間中，使用三個角度——**偏航 (Yaw)**、**俯仰 (Pitch)**、**翻滾 (Roll)** 來描述旋轉的方式統稱為 **歐拉角 (Euler Angles)**。

> **致命弱點**：
> 歐拉角是有**先後順序**的（例如 $Y \to X \to Z$）。
> 當中間軸（俯仰 Pitch）旋轉到 **$\pm 90^\circ$** 時，最外層環的轉軸與最內層環的轉軸會轉移到**同一條直線上（Colinear 共線）**。
> 此時，三維空間原本獨立的 3 個自由度（Degrees of Freedom, DOF）**退化為 2 個**，導致物體在當下無法在三維空間中直接繞第三個垂直軸轉動，這就稱為 **萬向鎖 (Gimbal Lock)**。

---

### 2. 嚴格數學推導
設旋轉順序為先繞 $Y$ 軸轉 $\psi$ (Yaw)，再繞 $X$ 軸轉 $\theta$ (Pitch)，最後繞 $Z$ 軸轉 $\phi$ (Roll)：
$$R = R_y(\psi) \cdot R_x(\theta) \cdot R_z(\phi)$$

各自的基礎旋轉矩陣為：
$$R_y(\psi) = \begin{bmatrix} \cos\psi & 0 & \sin\psi \\ 0 & 1 & 0 \\ -\sin\psi & 0 & \cos\psi \end{bmatrix},\quad
R_x(\theta) = \begin{bmatrix} 1 & 0 & 0 \\ 0 & \cos\theta & -\sin\theta \\ 0 & \sin\theta & \cos\theta \end{bmatrix},\quad
R_z(\phi) = \begin{bmatrix} \cos\phi & -\sin\phi & 0 \\ \sin\phi & \cos\phi & 0 \\ 0 & 0 & 1 \end{bmatrix}$$

相乘展開後：
$$R = \begin{bmatrix}
\cos\psi\cos\phi + \sin\psi\sin\theta\sin\phi & -\cos\psi\sin\phi + \sin\psi\sin\theta\cos\phi & \sin\psi\cos\theta \\
\cos\theta\sin\phi & \cos\theta\cos\phi & -\sin\theta \\
-\sin\psi\cos\phi + \cos\psi\sin\theta\sin\phi & \sin\psi\sin\phi + \cos\psi\sin\theta\cos\phi & \cos\psi\cos\theta
\end{bmatrix}$$

**當俯仰角 $\theta = 90^\circ$（觸發萬向鎖）時**：
此時 $\cos(90^\circ) = 0$，$\sin(90^\circ) = 1$。矩陣簡化為：
$$R_{\theta=90^\circ} = \begin{bmatrix}
\cos\psi\cos\phi + \sin\psi\sin\phi & -\cos\psi\sin\phi + \sin\psi\cos\phi & 0 \\
0 & 0 & -1 \\
-\sin\psi\cos\phi + \cos\psi\sin\phi & \sin\psi\sin\phi + \cos\psi\cos\phi & 0
\end{bmatrix}$$

利用三角函數和角差角公式：
- $\cos\psi\cos\phi + \sin\psi\sin\phi = \cos(\psi - \phi)$
- $\sin\psi\cos\phi - \cos\psi\sin\phi = \sin(\psi - \phi)$

代入後得到：
$$R_{\theta=90^\circ} = \begin{bmatrix}
\cos(\psi - \phi) & \sin(\psi - \phi) & 0 \\
0 & 0 & -1 \\
-\sin(\psi - \phi) & \cos(\psi - \phi) & 0
\end{bmatrix}$$

> 📌 **數學結論**：
> 整個 $3 \times 3$ 旋轉矩陣中，偏航角 $\psi$ 與翻滾角 $\phi$ 完全綁定在同一項 **$(\psi - \phi)$** 之中！
> 這意味著增加 $\psi$ 與減少 $\phi$ 的效果完全相同（兩者共線），我們失去了獨立控制第三個旋轉維度的能力。這就是萬向鎖在數學上的**坐標奇點 (Coordinate Singularity)**。

---

## 🛡️ 如何解決萬向鎖？四元數 (Quaternion)

### 為什麼四元數能徹底根除萬向鎖？
四元數由 1 個實數標量與 3 個虛數向量構成：
$$q = w + x\mathbf{i} + y\mathbf{j} + z\mathbf{k} = [\cos(\theta/2),\ \mathbf{u}\sin(\theta/2)]$$
其中 $\mathbf{u}$ 為三維旋轉軸（單位向量），$\theta$ 為繞該軸旋轉的角度。

1. **四維超球面拓撲 ($S^3$)**：
   三維旋轉群 $SO(3)$ 在歐拉角的三維參數空間中必定存在奇點（拓撲學定理：任何用 3 個實數參數化 $SO(3)$ 的座標系都必定存在奇點）。
   而四元數將旋轉提升至四維空間的單位超球面 $S^3$，其幾何流形處處光滑，**完全沒有任何軸向重疊的奇點**！
2. **球面線性插值 (Slerp, Spherical Linear Interpolation)**：
   $$q(t) = \frac{\sin((1-t)\Omega)}{\sin\Omega} q_1 + \frac{\sin(t\Omega)}{\sin\Omega} q_2$$
   保證兩點姿態之間永遠沿著四維超球面的**最短大圓弧**平滑轉動，旋轉角速度恆定均勻，絕無歐拉角插值時的抽搐與跳變。

---

## 💻 C# 核心程式碼實作

### 1. 歐拉角轉四元數 (`RotationMath.cs`)
```csharp
using System;
using System.Numerics;

public static Quaternion EulerToQuaternion(float yawDeg, float pitchDeg, float rollDeg)
{
    float y = yawDeg * (MathF.PI / 180f);
    float p = pitchDeg * (MathF.PI / 180f);
    float r = rollDeg * (MathF.PI / 180f);

    Quaternion qYaw   = Quaternion.CreateFromAxisAngle(Vector3.UnitY, y);
    Quaternion qPitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, p);
    Quaternion qRoll  = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, r);

    // 依外環 -> 中環 -> 內環相乘結合
    return Quaternion.Normalize(qYaw * qPitch * qRoll);
}
```

### 2. 四元數平滑插值 (Slerp)
```csharp
public static Quaternion SmoothInterpolate(Quaternion startOrientation, Quaternion endOrientation, float t)
{
    // t 介於 0.0 ~ 1.0 之間
    return Quaternion.Slerp(startOrientation, endOrientation, t);
}
```

---

## 🚀 如何執行專案

### 方法一：Windows 一鍵啟動
直接雙擊專案目錄下的：
```bat
run.bat
```

### 方法二：透過 .NET CLI 指令啟動
在終端機中切換至 `G:\Gimbal_Lock`，執行：
```powershell
dotnet run
```

---

## 📁 專案檔案結構

```
G:\Gimbal_Lock\
├── GimbalLockVisualizer.csproj    # .NET 8 WPF 專案設定檔
├── App.xaml                       # 應用程式資源入口
├── App.xaml.cs                    # 應用程式後端
├── MainWindow.xaml                # 主視窗 UI 介面 (科技風暗色主題、控制面板、矩陣看板)
├── MainWindow.xaml.cs             # 萬向鎖判定、動畫計時器、事件控制邏輯
├── run.bat                        # Windows 一鍵啟動腳本
├── Controls/
│   └── GimbalVisualizerControl.cs # 高效 3D 向量畫布控件 (三層同心環、飛機渲染、共線指示線、軌道相機)
├── Math/
│   └── RotationMath.cs            # 歐拉角矩陣計算、萬向鎖嚴重度檢測、四元數 Slerp 算法
├── Models/
│   └── AircraftModel.cs           # 3D 戰鬥機多邊形面與萬向環幾何頂點生成器
└── README.md                      # 完整萬向鎖原理、數學證明與解決方案說明手冊
```


# Preview 三工程 代码审查与重构方案

> 审查范围：`src/Preview/DotNet.Drawing`（6901 行）、`src/Preview/DotNet.HalconUI`（7552 行）、`src/Preview/DotNet.HalconAlgo`（4000 行）
> 审查方式：逐文件通读 + 跨工程模式扫描
> 优先级：**P0 = 必现缺陷/数据损坏/资源泄漏**，**P1 = 潜在缺陷/易误用 API**，**P2 = 可维护性/一致性**

---

## 〇、结论摘要

| 类别 | 数量 | 代表问题 |
|---|---|---|
| P0 必现 Bug | 12 | `HalconHelper.GenContours` 无限自递归；`DispPolygon` 行列颠倒；可变全局单例 `Rect2d.Default` / `CvRegion.Empty` |
| P0 资源泄漏 | 31 处 | `new HObject(); GenEmptyObj(out x);` 覆盖丢句柄，分布于 10 个文件 |
| P1 设计缺陷 | 20+ | 分层倒置、巨型接口、三层纯转发、角度二次转换、坐标序 (X,Y)/(Row,Col) 混用 |
| P2 可维护性 | 大量 | 55 处 `catch{}`/`Console.WriteLine` 静默吞错、11 个空壳策略类、647 行整体注释掉的死代码文件 |

**最紧迫的三件事**：① 修 12 个必现 Bug；② 统一 HObject 所有权模型，消灭 31 处泄漏；③ 打断 `HalconAlgo → HalconUI` 的反向依赖。

---

## 一、架构级问题（P0，必须最先解决）

### A1. 分层倒置：算法层反向依赖 UI 层

- **位置**：`DotNet.HalconAlgo/DotNet.HalconAlgo.csproj`（`ProjectReference → DotNet.HalconUI`）、`DotNet.HalconAlgo/IParaStrategy.cs:1-3`
- **现象**：依赖拓扑为 `Drawing ← HalconUI ← HalconAlgo`。算法工程直接 `using DotNet.HalconUI; using System.Windows.Forms;`，`IParaStrategy` 的方法签名里出现 `HDisplayUI`、`TreeVisualizer`、`Control`、`VsControlModel`。
- **影响**：算法无法脱离 WinForms 单元测试；无法在无界面服务/多线程流水线中复用；UI 改动会连锁编译整个算法层。
- **方案**：
  1. 新建 `DotNet.Vision.Abstractions`（无 UI 依赖），下沉 `IHDisplay`（仅保留绘制原语）与 `IParaStrategy` 的**算法部分**；
  2. `IParaStrategy` 拆成小接口（见 A2），UI 相关实现移到 HalconUI 侧的适配器；
  3. 目标依赖方向：`Abstractions ← Drawing ← HalconAlgo`，`Abstractions ← HalconUI`，两者互不依赖。

### A2. `IParaStrategy` 是 15 成员的巨型接口（违反 ISP）

- **位置**：`DotNet.HalconAlgo/IParaStrategy.cs`
- **现象**：一个接口同时承担 参数解析 / 算法执行 / ROI 绘制 / 树节点生成 / WinForms 控件双向同步 / 模板设置。
- **方案**：按职责拆分——
  - `IAlgoStrategy`：`Algorithm`、`Name`、`RunIndex`、`Execute(AlgoContext)`
  - `IOutputProvider`：`ResolveOutput(path)` / `TryResolveOutput<T>(path, out T)`
  - `IRoiEditable`：`DrawROI` / `DispROI` / `SetTemplate`
  - `IParaBinding`：`DispPara` / `SavePara`（进一步改为声明式绑定，见 C4）
  - `ITreeNodeProvider`：`GenTreeNode`

  策略类按需实现，不再被迫写空方法。

### A3. `HDisplayUI → HDisplayCore → HDisplay` 三层纯转发

- **位置**：`HDisplayUI.cs`（527 行）、`HWindows/HDisplayCore.cs`（378 行）、`HWindows/HDisplay/HDisplay.cs`（971 行）
- **现象**：`IHDisplay` 有约 60 个成员，三层各手写一遍转发。中间层 `HDisplayCore` 除 `Size`/`Centre`/`MouseDown`/`MouseDouble` 外**未增加任何行为**。约 1000 行以上是纯样板。
- **影响**：每加一个绘制方法要改三处；漏改一处即行为分叉（现已出现，见 B4）。
- **方案**：
  1. 删除 `HDisplayCore`，由 `HDisplayUI` 直接组合 `HDisplay` + `HWindowMouse`；
  2. `HDisplayUI` **不再实现** `IHDisplay`，改为暴露 `public IHDisplay Display { get; }`（组合优于假继承）；
  3. `IHDisplay` 按 A4 瘦身后，转发量自然降到可接受范围。

### A4. `IHDisplay` 的 46 个 `Disp*` 重载

- **实测规模**：接口共 **66 个成员**，其中 `Disp*` 方法 **46 个**，带 `string color` 参数的重载 **22 个**。
- **现象**：几乎每个图元都有「带 color」「不带 color」两版，唯一差别是首行 `SetColor(color)`；`size` 参数在重载间时而 `double` 时而 `int`。
- **方案**：
  - 统一为 `void Disp<T>(T shape, DrawStyle? style = null)`，`DrawStyle` 封装 `Color`/`Size`/`LineWidth`/`DrawMode`；
  - 颜色从 `string` 改为 `HColor` 强类型（枚举或 `readonly struct`），消除魔法字符串；
  - 保留少量薄兼容重载并标 `[Obsolete]`，分批迁移。

---

## 二、必现 Bug 清单（P0）

### B1. `HalconHelper.GenContours` 无限自递归 → 栈溢出

- **位置**：`DotNet.Drawing/HalconHelper.cs`

```csharp
public static void GenContours(List<Point2d> points, out HObject contour)
{
    GenContours(points, out contour);   // 调用的是自己
}
```

- **修复**：改为 `controller.GenContours(points, out contour);`
- **根因**：`HalconHelper` 是 `HalconController` 的纯静态镜像，两份 API 逐字复制，复制时漏改前缀。
- **根治**：删除 `HalconHelper` 整个类，调用方改用 `HalconController`（或反之保留一个）。这是**同一份逻辑维护两遍**的典型代价。

### B2. `DispCvRegion` 多边形分支行列颠倒

- **位置**：`HWindows/HDisplay/HDisplay.cs`，`DispCvRegion` 的 `RectEnum.Polygon` 分支

```csharp
HOperatorSet.DispPolygon(_hWindow, hRegion.PolygonX, hRegion.PolygonY);
```

- **证据**：同文件 `DrawPolygonInto` 中 `hRegion.PolygonX = columns; hRegion.PolygonY = rows;`，而 Halcon `disp_polygon(Window, Row, Column)` 首参为 Row。
- **现象**：多边形 ROI 显示时 X/Y 互换，画面完全错位。
- **修复**：`HOperatorSet.DispPolygon(_hWindow, hRegion.PolygonY, hRegion.PolygonX);`

### B3. `DrawRegion` / `DrawRegionMod` 缺失 `Ring` 分支

- **位置**：`HWindows/HDisplay/HDisplay.cs`
- **现象**：`RectEnum` 含 `Ring`，`DispCvRegion` 也实现了 Ring 显示，但 `DrawRegion(CvRegion)`（:589）、`DrawRegionMod(CvRegion)`（:665）、`DrawRegion(RectEnum, out HObject)`（:793）三个方法的 `switch` 都没有 Ring 分支且无 `default` → 用户选择圆环 ROI 时**静默无反应**，无任何提示。
- **修复**：补齐 Ring 分支；并为所有 `switch (RectEnum)` 加 `default: throw new NotSupportedException(...)`，让遗漏在编码期暴露。

### B4. `DispLine(CvLine, int radius)` 绕过颜色缓存 → 颜色错乱

- **位置**：`HWindows/HDisplay/HDisplay.cs`

```csharp
public void DispLine(CvLine line, int radius, string color)
{
    SetColor(color);                       // 更新 _color 缓存
    _hWindow.DispLine(...);
    _hWindow.SetColor(HColor.Red);         // 直接调窗口，_color 缓存未更新！
    _hWindow.DispCircle(...);
}
```

- **现象**：`SetColor` 有「颜色相同则跳过 PInvoke」的快速路径。此处窗口实际颜色已是 Red 而缓存仍是 `color`，后续调用 `SetColor(HColor.Red)` 会被缓存命中跳过 → **后续所有图元错误地继续用红色绘制**。
- **修复**：把 `_hWindow.SetColor(...)` 全部改为走 `SetColor(...)`，把 `_color` 设为唯一写入口；并在 `Fun_ZoomImage`/`ClearWindow` 等重置窗口状态的位置使缓存失效。

### B5. `CvCoord.Angle` 二次单位转换

- **位置（共 4 处，均需删除）**：`HWindows/HDisplay/HDisplay.cs:417`、`:422`（`DispCross(CvCoord, ...)` 两个重载）；`DotNet.Drawing/HalconController.cs:120`、`:121`（`VectorAngleToRigid(CvCoord...)`）

```csharp
_hWindow.DispCross(coord.Y, coord.X, size, coord.Angle.ToRadians());
```

- **证据**：`CvMode/CvCoord.cs:37-40` 中 `AngleDegrees => Angle * 180.0 / Math.PI`，`Direction => new(Math.Cos(Angle), Math.Sin(Angle))` —— **`Angle` 本身就是弧度**。
- **现象**：再乘 π/180，坐标系十字与刚体变换角度错误（约缩小 57 倍）。
- **修复**：删除上述 4 处 `.ToRadians()`。
- **不要误删**：`HalconController.cs:189`、`:214` 的 `angleDiff` 属于从未使用的死代码（见 D9），应连同整段删除而非仅去掉转换；`image/RotateImageStrategy.cs:87` 的 `baseAglDeg.ToRadians()` 是**正确**的（`baseAglDeg` 确为角度制），必须保留。
- **根治**：引入强类型 `readonly struct Angle`（内部存弧度，提供 `FromDegrees`/`FromRadians`/`Degrees`/`Radians`），从类型上杜绝单位混淆。

### B6. 可变全局单例被外部污染

- **位置**：`DotNet.Drawing/OpenCvSharp/Rect2d.cs`、`DotNet.Drawing/CvMode/CvRegion.cs`

```csharp
public static readonly Rect2d Default = new Rect2d();     // Rect2d 是可变 class
public static readonly CvRegion Empty = new CvRegion();   // 且实现 IDisposable

public static Rect2d Intersect(Rect2d a, Rect2d b)
{
    ...
    return Default;   // 不相交时返回全局共享引用
}
```

- **现象**：调用方拿到 `Default` 后调 `Inflate()` 或赋 `X/Y/Width/Height`，即**永久污染全局单例**；`CvRegion.Empty` 被任意一处 `Dispose()` 后，全进程的 `Empty` 都变成已释放状态。
- **修复**：
  - `Intersect` 不相交时返回 `new Rect2d()`；
  - `Default` 改为 `public static Rect2d Default => new Rect2d();`（属性，每次新实例），或将 `Rect2d` 改为不可变；
  - `CvRegion.Empty` 直接删除。

### B7. `CvRegion.Clone()` 丢失 `HoRegion`

- **位置**：`DotNet.Drawing/CvMode/CvRegion.cs` + `DotNet.Drawing/CvMode/TransExpV2.cs`

```csharp
public CvRegion Clone() => TransExpV2<CvRegion, CvRegion>.Trans(this);
// TransExpV2 内部：foreach (var item in typeof(TOut).GetProperties())  ← 只枚举属性
public HObject HoRegion;   // 是字段，不是属性 → 被跳过
```

- **现象**：克隆出的 `CvRegion` 的 `HoRegion` 为 `null`，后续显示/运算 NRE 或静默不显示。
- **修复**：`Clone()` 手写实现，显式 `HoRegion = this.HoRegion?.CopyObj(1, -1)`（深拷贝，明确所有权）；`TransExpV2` 补 `GetFields()`，或明确标注「仅复制属性」并在 XML 注释中警示。

### B8. `SerializeConvert` 用 `FileMode.OpenOrCreate` 写文件 → JSON 损坏

- **位置**：`DotNet.Drawing/Serialize/SerializeConvert.cs`

```csharp
using (var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
{ var date = JsonSerializeToBytes(obj); fileStream.Write(date, 0, date.Length); fileStream.Close(); }
```

- **现象**：`OpenOrCreate` **不截断**。新内容比旧文件短时旧尾部残留 → 产出 `{...}}]` 之类的非法 JSON，配置文件永久损坏。
- **修复**：改为 `FileMode.Create`；`using` 内的 `fileStream.Close()` 冗余，删除。
- **加固**：改为「写临时文件 → `File.Replace`」的原子写入，避免掉电或异常写坏配置。

### B9. 31 处 HObject 句柄泄漏（同一模式）

- **模式**：

```csharp
HObject x = new HObject();          // 创建句柄 #1
HOperatorSet.GenEmptyObj(out x);    // 句柄 #1 丢失，永不释放
```

- **分布**（10 个文件）：`contour/FitArcMidpointStrategy.cs`、`contour/FitLineStrategy.cs`、`matching/GenericModelStrategy.cs`、`matching/NccModelStrategy.cs`、`matching/ScaledModelStrategy.cs`、`matching/ShapeModelStrategy.cs`、`region/CreateROIStrategy.cs`、`region/MergeRegionStrategy.cs`、`DotNet.Drawing/Extension/RegionExtension.cs`、`DotNet.Drawing/HalconController.cs`
- **影响**：长时间运行的机台程序 Halcon 非托管内存持续增长，最终 `HALCON error: out of memory`。
- **修复**：
  1. 统一删除 `new HObject()` 初始化，只保留 `HOperatorSet.GenEmptyObj(out x)`；
  2. 更彻底的做法：封装 `sealed class HObjectHandle : IDisposable` 或 `using var x = HObjectScope.Empty();`，让所有权在类型上可见；
  3. 加集成测试，断言流程前后 `HOperatorSet.CountObj` 平衡。
- **同类问题**：`RegionExtension.RebuildRegion` 的 Ring 分支 `GenEmptyObj(out circle1/circle2)` 生成的对象随即被 `GenCircle(out ...)` 覆盖；`FileImageStrategy.Init` 与 `RotateImage` 构造函数中 `GenEmptyObj` 覆盖字段初始化器 `= new HObject()`。

### B10. `FitLineStrategy.Close()` 释放后仍被使用

- **位置**：`DotNet.HalconAlgo/contour/FitLineStrategy.cs`

```csharp
public override void Close(HDisplayUI display) { inPara.HoRect.Dispose(); }
```

- **现象**：`CvRegion.Dispose()` 会把 `HoRegion` 置 null 且 `_disposed = true`。若用户关闭工具页后再次打开同一策略实例，`DrawROI`/`Fun_action` 必然 NRE。
- **修复**：`Close` 只释放**运行期临时对象**，不销毁配置态的 `inPara.HoRect`；策略实例生命周期结束才由 `Dispose()` 释放（可参考 `FitArcMidpointStrategy` 的 `Close → Dispose` + `_disposed` 幂等写法，但同样需先确认策略实例不会被复用）。

### B11. `MergeRegionStrategy` 是假实现却返回成功

- **位置**：`DotNet.HalconAlgo/region/MergeRegionStrategy.cs`

```csharp
var region = strategys.ResolveFrom<CvRegion>(inPara.RegionIn);  // 解析后完全未使用
var coord  = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);    // 解析后完全未使用
int srcCnt = 0;                                                  // 只统计来源数量
if (inPara.DispText) { /* 只显示文本 */ }
return true;                                                     // 未做任何合并，却报告成功
```

- **现象**：流程中配置了「区域合并」，实际什么也没发生，下游拿到的仍是原始区域，且**无任何错误提示**。另外 `regionGet`/`imgReduce` 创建后在 `finally` 直接释放，纯粹浪费。
- **修复**：要么用 `HOperatorSet.Union2` 真正实现合并并注册输出，要么删除该策略并从 `AlgoEnum` 移除。**绝不能保留返回 true 的空实现。**

### B12. `HDisplayUI` 在 `HandleDestroyed` 中释放 → 句柄重建后使用已释放对象

- **位置**：`HDisplayUI.cs` 构造函数中 `HandleDestroyed += UI_HandleDestroyed;`
- **现象**：WinForms 的 `HandleDestroyed` 在**句柄重建**时也会触发（更换 `Parent`、修改 `Dock`/`RightToLeft`、TabPage 切换等），此时控件并未销毁。`display` 被提前 `Dispose`，之后控件重建句柄却不会重新创建 `display` → 所有显示功能静默失效。
- **补充证据**：`UI_HandleDestroyed`（:139-155）内已先解绑 `HMouseDown/Up/Wheel/Move` 与 `HandleDestroyed` 自身，因此句柄重建后**不会再有第二次机会重新初始化**，`display` 永久处于已释放状态。
- **修复**：`HDisplayUI.Designer.cs:14` 已存在 `protected override void Dispose(bool disposing)`，把释放逻辑整体迁移到该方法的 `if (disposing)` 分支即可（构造函数中的 `HandleDestroyed += UI_HandleDestroyed;` 一并删除）。不要用 `HandleDestroyed` 做资源释放。

---

## 三、设计缺陷清单（P1）

### C1. 坐标序 (X, Y) 与 (Row, Column) 在同一 API 族内混用

- **位置**：`HWindows/HDisplay/IHDisplay.cs:68-77`、`HWindows/HDisplay/HDisplay.cs` 的 `DispPoint` 重载族（共 **10 个重载**，坐标语义分裂成两派）

```csharp
void DispPoint(double crossX, double crossY, ...);               // (X, Y)
void DispPoint(double[] rowPoints, double[] columnPoints, ...);  // (Row, Col) = (Y, X) —— 顺序反了
```

- **影响**：调用方极易传反，且编译器无法发现（都是 `double`）。`DispText(message, FontX, FontY, ...)` 内部转成 `(FontY, FontX)`，靠一行注释维系语义，同类风险。
- **方案**：所有公开 API 统一以 `Point2d`（X/Y 语义）为准，Halcon 的 (Row, Col) 转换**只在最内层一处**完成；数组重载改为 `IReadOnlyList<Point2d>`。

### C2. 40 多个 `Disp*` 重载缺少防护，与其它方法防护级别不一致

- **位置**：`HWindows/HDisplay/HDisplay.cs`
- **现象**：`SetDraw`/`DispImage`/`DispGenRegion`/`DrawRegion` 都有 `IsWindowUsable()` + `try/catch`；而全部 `DispPoint`/`DispCross`/`DispLine`/`DispArrow`/`DispCircle`/`DispRegion`/`DispRectangle2` **一个防护都没有**，窗口已释放时直接抛 Halcon 异常打穿上层调用栈。
- **另**：`DispPoint(double[], double[], string color, int)` 先 `SetColor(color)` 再校验数组长度，**副作用先于校验发生**；长度不等时静默 `return`。
- **方案**：按 A4 收敛重载后，在唯一实现入口做一次防护；校验一律前置；长度不等应抛 `ArgumentException` 而非静默返回。

### C3. `DrawHelper`：阻塞式模态循环 + 全局单例状态

- **位置**：`HWindows/HMouse/DrawHelper.cs`（1639 行，全工程最大文件）

```csharp
private void BlockUntilDone()
{
    while (!_completed && !_cancelled) { Application.DoEvents(); System.Threading.Thread.Sleep(10); }
}
private static DrawHelper _active;   // 全局可变静态，无 lock / volatile
```

- **问题**：
  1. **`Application.DoEvents()` 重入**：绘制过程中用户可点击任意按钮、关闭窗体、再次发起绘制。代码注释里已承认该风险，并用 `_active` 身份校验、`Interlocked.Exchange(ref h._ended, 1)` 打补丁，但根因未除；
  2. **无超时、无取消令牌**：若宿主忘记转发鼠标事件，循环**永不退出**，UI 完全假死，只能靠静态 `CancelDraw()` 从外部打断；
  3. **多窗口不安全**：第二个 `HWindowControl` 发起绘制会顶掉第一个的 `_active`；
  4. **1639 行单类**承担 状态机 + 命中测试 + 几何计算 + 绘制 + 窗口参数存取 + 背景截图，严重违反 SRP；
  5. 12 个静态入口是 `CancelDraw(); Begin(); try{ BlockUntilDone(); 拷贝字段 } finally{ End(h); }` 的逐字复制；
  6. 类是 `public` 但全部实例成员私有、无公开构造，实质是「绘图会话」，命名为 `DrawHelper` 具误导性，且未实现 `IDisposable`；
  7. **单文件内 21 处空 `catch`**，占整个 `DotNet.HalconUI` 31 处的 2/3 —— 绘制流程中的 Halcon 错误几乎全部被吞掉。
- **方案**（风险较高，建议单独排期，分三步）：
  1. **短期**：给 `BlockUntilDone` 加超时（如 5 分钟）与 `CancellationToken`；`_active` 加 `volatile` 并改为「每 `HWindow` 一个会话」的字典，支持多窗口；
  2. **中期**：拆为 `DrawSession`（状态机 + 几何）+ `DrawRenderer`（绘制）+ `DrawInteraction`（命中测试）；类改 `sealed` 并实现 `IDisposable`；12 个静态入口收敛到一个泛型模板方法；
  3. **长期**：改为 `Task<DrawResult> DrawAsync(...)` + `TaskCompletionSource`，由鼠标事件驱动完成，彻底消灭 `DoEvents`。

### C4. `DispPara`/`SavePara` 靠魔法字符串手工双向同步

- **位置**：各策略类（`contour/FitLineStrategy.cs` 等）

```csharp
VsControls["cmb_100"] ... VsControls["ckb_disp0"] ... VsControls["CB_FontX"]
```

- **规模**：全 `DotNet.HalconAlgo` 共 **144 处** `VsControls["..."]` 字面量索引。
- **问题**：控件 key 是 `"cmb_100"`、`"cmb_101"` 这类**纯序号、无语义**的字符串（同一文件里 `cmb_100`~`cmb_110` 连续排布，无法从名字判断对应哪个参数）；`DispPara` 与 `SavePara` 必须手工保持镜像，漏改一处即参数静默丢失；`SavePara` 用 `VsControls["..."]` 直接索引，key 缺失即 `KeyNotFoundException` 崩溃。
- **方案**：
  1. 索引一律改为 `TryGetValue`，缺失时记录警告而非崩溃；
  2. 中期改为**声明式绑定**：参数类属性上标注 `[VsControl("阈值", ControlType.TrackBar, Min = 0, Max = 255)]`，由 `VsControlFactory` 反射生成控件并双向绑定，`DispPara`/`SavePara` 从各策略中彻底消失。

### C5. 中文 UI 文案被当作业务状态持久化

- **位置**：`contour/FitLineStrategy.cs`（`"由黑到白"`、`"图像中心"`、`"默认"`）、`contour/FitArcMidpointStrategy.cs`（`circPointOrder.S == "positive"`）、`image/RotateImageStrategy.cs`（`case "坐标系Y轴"`）

```csharp
internal string GetTransition
{ get { if (Transition == "由黑到白") return "positive"; ... return ""; } }  // 未知值返回空串 → Halcon 报错
```

- **问题**：① 改一个界面文案就会让所有已保存配置失效；② 无法做多语言；③ 未知值回落为 `""` 会让 Halcon 在运行时报参数错误，而非在配置期就拒绝。
- **方案**：业务状态改用 `enum`（如 `TransitionMode.DarkToLight`），序列化存枚举名；界面显示走 `Description` 特性或资源文件；转换函数对未知值 `throw`。

### C6. 泛型 `ResolveOutput<T>` 对 null 强转 struct → NRE

- **位置**：`DotNet.HalconAlgo/IParaStrategy.cs`

```csharp
public T ResolveOutput<T>(string[] path) => (T)ResolveOutput(path);
public static T ResolveFrom<T>(this IList<IParaStrategy> strategies, string fullPath, ...) => (T)ResolveFrom(...);
```

- **现象**：路径不存在时 `ResolveOutput` 返回 `null`，若 `T` 是 `CvCoord`/`Point2d` 等值类型则抛 `NullReferenceException`，错误信息与真实原因（路径拼错）完全无关。`RotateImageStrategy` 中的 `strategys.ResolveFrom<CvCoord>(inPara.CoordIn)` 已存在此风险。
- **方案**：改为 `bool TryResolveOutput<T>(string[] path, out T value)`；保留的强转版本在失败时抛携带路径信息的 `AlgoOutputNotFoundException`。

### C7. `Fun_action(HObject, IHDisplay)` 传 `strategys = null`

- **实测范围**：共 **7 个文件**存在此调用 —— `contour/FitLineStrategy.cs:46`、`image/LineRotImageStrategy.cs:29`、`image/RotateImageStrategy.cs:29`、`matching/GenericModelStrategy.cs:41`、`matching/NccModelStrategy.cs:41`、`matching/ScaledModelStrategy.cs`、`matching/ShapeModelStrategy.cs`。且这 7 个文件的另一重载**全部实际调用了 `strategys.`**（各 2~4 处），即 7 处全是真实崩溃点，无一例外。

```csharp
public override bool Fun_action(HObject ho_Image, IHDisplay display)
{ display.SetImage(ho_Image); return Fun_action(display, null); }   // ← null
```

- **现象**：另一重载在 `ImageIn`/`RegionIn`/`CoordIn` 不为 `"默认"` 时会 `strategys.ResolveFrom(...)` → NRE。即「单张图快速验证」路径在配置了上游输入时必崩。
- **方案**：传 `Array.Empty<IParaStrategy>()`；`ResolveFrom` 对空集合返回「未找到」而非崩溃；或让两个重载合并为 `Execute(AlgoContext ctx)`，由 `ctx` 保证非空。

### C8. 算法层弹 WinForms 对话框

- **位置（共 3 处）**：`DotNet.Drawing/Serialize/JsonConvertHObject.cs:31`、`:52`；`DotNet.HalconAlgo/image/FileImageStrategy.cs:134`。三处都是 `catch { MessageBox.Show(ex.Message); }` —— 弹完框后**不重新抛出**，调用方以为成功。
- **问题**：库/算法层弹模态框——在服务端或后台线程中会阻塞或抛异常；且吞掉异常导致调用方以为成功。
- **方案**：库层只抛异常或写日志，由最外层 UI 决定如何呈现。

### C9. `RotateImageStrategy` 角度归一化边界不连续

- **位置**：`image/RotateImageStrategy.cs`

```csharp
baseAglDeg = baseAglDeg % 360;      // 手工重复实现归一化
case "坐标系Y轴":
    if (baseAglDeg > 0) baseAglDeg = 90 - baseAglDeg;
    else if (baseAglDeg < 0) baseAglDeg = -90 - baseAglDeg;
    // baseAglDeg == 0 时不做任何处理 → 与两侧极限（90 / -90）不连续
```

- **方案**：`== 0` 明确归入某一分支并写清文档；统一改用 `MathHelper.NormalizeAngle`（修好 C13 之后）。

### C10. ROI 跟随坐标系时「只平移不旋转」

- **位置**：`region/CreateROIStrategy.cs`、`contour/FitLineStrategy.cs`、`contour/FitArcMidpointStrategy.cs`
- **现象**：
  - `CreateROIStrategy` 跟随坐标只调用 `HalconHelper.TransRegion(tmplPoint, inCoord.Center, ...)` 的 `Point2d` 重载 → 仅平移；且 `inPara.Coord = new CvCoord(new Point2d(column, row))` **丢弃角度**；
  - `FitLineStrategy` 中 `fixAgl = inPara.HoRect.Phi`，跟随分支下未做任何角度变换。
- **影响**：产品有旋转时，ROI 位置对了但方向错，测量结果系统性偏差。
- **方案**：统一走 `VectorAngleToRigid` 生成完整刚体变换矩阵（含旋转），ROI 与测量矩形的 `Phi` 一并变换；`CvCoord` 全程保留角度。

### C11. `CreateROIStrategy` 输出的区域与画面不一致

- **位置**：`region/CreateROIStrategy.cs`
- **现象**：`RegisterOutput("区域", () => inPara.HoRect)` 输出的是**未经坐标变换的原始 ROI**，而实际显示用的 `regionGet`（已变换）在 `finally` 中被释放。下游策略拿到的区域与用户在画面上看到的不是同一个。
- **修复**：把变换后的区域保存到 `inPara`（并接管所有权），`RegisterOutput` 指向它。
- **另**：树节点把「角度」声明为 `OutEnum.Array`，而 `ResolveOutput` 实际返回 `double` → 类型声明与实际不符。

### C12. 圆弧拟合 Stage 1 用直线拟合粗滤（算法设计缺陷）

- **位置**：`contour/FitArcMidpointStrategy.cs`

```csharp
lineGate = Math.Max(maxErr * 3.0, 15.0);   // 硬编码 15.0 像素
```

- **现象**：圆弧曲率较大（接近半圆）时，用直线拟合的残差本身就很大，`lineGate` 会把**正确的边缘点**当离群点剔除。
- **方案**：Stage 1 直接用圆拟合 + `atukey` 稳健权重，或按弦长/半径估算自适应门限；硬编码 `15.0` 提升为可配置参数。
- **另**：`FitArcMidpointStrategy` 用 `HOperatorSet.GetImageSize` 取图像尺寸，而同构的 `FitLineStrategy` 用 `display.HoWidth/HoHeight` —— 两者口径不一致，窗口与图像尺寸不符时结果会分叉。

### C13. `MathHelper` 的容差与角度归一化

- **位置**：`DotNet.Drawing/CvMode/MathHelper.cs`

```csharp
public const double Tolerance = 1e-9;   // 注释称"适用于像素级精度"，实际远小于像素
public static double NormalizeAngle(double angle)
{ while (angle >= Math.PI) angle -= TwoPi; while (angle < -Math.PI) angle += TwoPi; return angle; }
public static double SmoothStep(double a, double b, double t)
{ t = Clamp01((t - a) / (b - a)); return t * t * (3 - 2 * t); }   // 返回 0..1，与 (a,b) 语义不符
```

- **关键事实（核验后更正）**：`MathHelper` **已经定义了三档容差** —— `Tolerance = 1e-9`、`LooseTolerance = 1e-6`、`PixelTolerance = 0.01`，但经全库扫描，**后两档从未被任何代码使用**（0 处引用）。`AreEqual(a, b)` 的无参重载一律落到最严格的 `1e-9`，`CvArrow`/`CvCircle`/`CvCoord`/`CvLine`/`CvRegion` 的全部 `Equals`、`IsFullCircle`、`IsDegenerate`、共线判定、平行判定共 20 余处都在用它。所以问题不是「缺少分档」，而是**分档形同虚设**。
- **问题**：
  1. **`CvLine.Contains` 主动降级到最严格容差**：`ContainsPoint(Point2d, double tolerance = 0.01)` 默认值本是合理的像素级 0.01，但 `Contains(point) => ContainsPoint(point, MathHelper.Tolerance)`（:204）显式传入 `1e-9`，比默认严格 7 个数量级，实际结果**恒为 false**；而同一文件的 `IsOnBoundary(point)`（:209）用的是默认 0.01 —— 两个语义相近的方法行为相差 7 个数量级；
  2. `NormalizeAngle` 用 `while` 逐步加减 `TwoPi`，对极大输入（如 1e18）近乎挂起，应改为 `angle - TwoPi * Math.Floor((angle + Math.PI) / TwoPi)`；`NormalizeAnglePositive` / `NormalizeAngleDegrees` 同样需要检查；
  3. `SmoothStep(a, b, t)` 命名与参数语义和实现不符（返回 0..1 而非 a..b 之间的插值），易误用。
- **方案**：
  1. `CvLine.Contains` 改为 `ContainsPoint(point, MathHelper.PixelTolerance)`；
  2. 逐处审查 20 余个 `AreEqual` 调用点，几何量（坐标、长度、半径）改用 `PixelTolerance`，纯数值恒等判定保留 `Tolerance`，让三档真正各司其职；
  3. 量级敏感的判定改**相对容差**（见 C14）；
  4. 重写 `NormalizeAngle` 系列；`SmoothStep` 更名为 `SmoothStepBetween` 并在 XML 注释中写明返回 0..1。

### C14. 绝对容差用于量级敏感的判定

- **位置**：`CvMode/CvCircle.cs` 的 `FromThreePoints`（`AreEqual(d, 0)` 判共线，`d` 量级为**坐标平方**）、`CvMode/CvLine.cs` 的 `TryIntersect`（`AreEqual(cross, 0)` 判平行）、`OpenCvSharp/Point2d.cs` 的 `operator /`（`|scalar| < 1e-9` 判除零）
- **现象**：大坐标（如 4000×3000 图像）下 `d` 轻易达到 1e7 量级，`1e-9` 的绝对容差**永远判不出共线**；反之 `operator /` 会把合法的小缩放因子误判为除零并抛 `DivideByZeroException`。
- **方案**：归一化后比较（如 `|cross| < tol * |a| * |b|`）；除法只在 `scalar == 0` 时抛异常。

### C15. `Rect2d` 的可变性与契约问题

- **位置**：`DotNet.Drawing/OpenCvSharp/Rect2d.cs`、`DotNet.Drawing/CvMode/CvRegion.cs`
- **问题**：
  1. `Inflate()` 直接改字段，**绕过构造函数的非负校验** → 可产生负宽高；
  2. `Contains(double, double)` 用右开区间，`Contains(Rect2d)` 用闭区间 → 边界语义不一致；
  3. `Top`/`Left` 有 setter 而 `Bottom`/`Right` 只读 → 语义不对称；
  4. `[StructLayout(LayoutKind.Sequential)]` + `public const int SizeOf` 加在**可被继承的 class** 上（`CvRegion : Rect2d`），非 blittable，是从 struct 移植过来的遗留物；
  5. `CvRegion.Equals(Rect2d? obj) => obj is CvRegion other && Equals(other)` **破坏对称性**：`rect.Equals(region)` 与 `region.Equals(rect)` 结果不同，放进 `HashSet`/字典行为未定义。
- **方案**：`Rect2d` 改为不可变（`Inflate` 返回新实例）；统一边界语义并写清 XML 注释；删除 `StructLayout`/`SizeOf`；`CvRegion` 改为**组合** `Rect2d` 而非继承（`CvRegion` 有 `HoRegion`、`IDisposable`、多边形点集，与「矩形」不构成 is-a 关系）。

### C16. `HWindowImage.HoImage` 悬挂引用风险

- **位置**：`HWindows/HWindowImage.cs` + `HWindows/HDisplay/HDisplay.cs`
- **现象**：`HDisplay.DispImage` 每次执行 `_hoImage.Dispose(); CopyImage(image, out _hoImage);`，而 `HWindowImage.HoImage` 仍指向**上一个已释放的对象**，直到 `Fun_DispImage` 把它覆盖。若这中间控件 `Resize` 触发 `Fun_ReDisplay()` → 使用已释放句柄。
- **另**：`HWindowImage` 的 XML 注释称「所有权由 `HDisplayCore` 持有」，实际持有者是 `HDisplay`，注释与代码不符。
- **方案**：图像所有权集中到一处（建议 `HWindowImage`），`HDisplay` 只传引用；或 `HWindowImage` 自持一份 `CopyObj` 副本。释放顺序：先置空引用再 `Dispose`。

### C17. `VsControlModel` 只有 `Value` 会触发通知

- **位置**：`VsControl/VsControlModel.cs`

```csharp
public object Value { get { return _value; } set { SetField(ref _value, value); } }  // 有通知
public bool Visible { get; set; }        // 无通知
public bool Enabled { get; set; }        // 无通知
public bool DropDownStyle { get; set; }  // 无通知
```

- **现象**：绑定建立后，代码里改 `Visible`/`Enabled` **不会同步到控件**，界面看起来「没反应」。
- **另**：`Value` 用 `object` 承载 string/bool/int，类型安全完全靠约定；`Type` 是 `string` 而非枚举。
- **方案**：三个属性改用 `SetField`；`VsControlModel` 改为泛型 `VsControlModel<T>` 或按控件类型派生子类；`Type` 改枚举。

### C18. `HDisplayUI` 的鼠标分发缺分支 + 每次事件全图重绘

- **位置**：`HDisplayUI.cs`

```csharp
private void OnMouseDown(object sender, HMouseEventArgs e)
{
    ReDispImage();                       // 每次鼠标事件全图重绘
    switch (drawType)
    {
        case DrawEnum.None: ...  case DrawEnum.DispRect: ...  case DrawEnum.DispModel: ...
        // 缺 DrawEnum.Erase 与 DrawEnum.Synthethic → 选中这两种模式时鼠标完全无响应
    }
}
```

- **实测范围**：`OnMouseDown`（:100-102）、`OnMouseUp`（:111-113）、`OnMouseWheel`（:122-124）、`OnMouseMove`（:133-135）**四个方法的 switch 全部只有 3 个分支**，无一个 `default`。`DrawEnum` 有 5 个值，即 `Erase`（`EraseRectMouse` 确有实现，被 `HEditModelUI` 使用）在此完全接不到鼠标事件。

- **另**：`public DrawEnum drawType` 与 `public ShowDelegate OnShow` 都是**公开可变字段**（后者还不是 `event`，外部可直接覆盖或调用）。
- **方案**：`switch` 加 `default` 兜底；字段改属性/`event`；重绘改为「脏区域」策略，或由各 handler 自行决定是否需要 `ReDispImage`。

---

## 四、可维护性问题（P2）

### D1. 55 处静默吞错

- `DotNet.HalconUI`：**31 处**空 `catch`（含仅有注释的 catch）+ **24 处** `Console.WriteLine`；`DotNet.Drawing` 与 `DotNet.HalconAlgo` 无空 catch，但各有 2 处 / 1 处库层 `MessageBox.Show`（见 C8）。
- **问题**：`Console.WriteLine` 在 WinForms 发布版本中**无人看得到**；`catch { }` 让 Halcon 错误彻底消失，现场问题无法定位。
- **方案**：引入 `ILogger` 抽象（NLog/Serilog 或自建轻量实现），分级记录并落盘；`catch { }` 只允许出现在 `Dispose` 路径，且必须写注释说明理由。

### D2. 异常包装丢失 `InnerException`

- **位置**：`DotNet.Drawing/HalconController.cs` 共 7 处 `throw new Exception`，其中**同一文件内两种写法并存**：

```csharp
// 正确（4 处）：:320 :359 :553 :588
throw new Exception($"保存图像失败：{ex.Message}", ex);
// 错误（3 处）：:407 :446 :499 —— 丢 InnerException，把堆栈拼进消息字符串
throw new Exception($"保存图像: {ex.Message}\n{ex.StackTrace}");
```

- **问题**：错误的 3 处丢弃原始异常类型与 `InnerException`，调试信息实际变少；同一文件两种写法并存说明是逐次复制粘贴积累的，无统一约定。
- **方案**：3 处错误写法改为 `throw new Exception(msg, ex)`；进一步统一为自定义 `HalconOperationException(context, ex)`，或在无需补充上下文时直接 `throw;`。

### D3. 11 个空壳策略类（死代码）

`FitCircleStrategy`、`CreateCoordStrategy`、`LineOffsetStrategy`、`BarCodeStrategy`、`QRCodeStrategy`、`CaptureWinStrategy`、`IndexImageStrategy`、`RGBToGrayStrategy`、`SaveImageStrategy`、`SaveRegionImgStrategy`、`SaveRegionStrategy` —— 均为 12~16 行的 `internal class Xxx { }`。

其中 `IndexImageStrategy` 的命名空间为 `DotNet.HalconAlgo.Algorithms.image`，与其余 `DotNet.HalconAlgo` **不一致**。

- **方案**：全部删除；确有规划的记录到本文档「待实现」清单，不留空壳。

### D4. 647 行整体注释掉的死文件

- **位置**：`HWindows/HMouse/Handlers/SynthethicHandler.cs`

```csharp
//    public class SynthethicHandler : IDrawHandler     // IDrawHandler 接口已不存在
```

全文件 647 行**无一行有效代码**（非注释非空行 = 0）。`DrawEnum.Synthethic` 枚举值除定义外无任何引用；配套的 `DrawSynthethicArgs`（`HWindows/HMouse/DrawEvent.cs:35`）同样只有定义、无任何使用点。

- **方案**：删除文件与枚举值。历史版本在 Git 里，无需以注释形式保留。

### D5. 大段结构性重复代码

| 重复处 | 规模 |
|---|---|
| `HalconHelper` vs `HalconController` | 整个类逐字复制两份 |
| `HDisplay.DispImage(HObject)` vs `DispImage(HObject, bool)` | 约 40 行逐字重复 |
| `HDisplay.DispLine(Point2d, Point2d, int)` 与其 color 版本 | 约 30 行逐字重复 |
| `IHDisplay` 全部 Disp 方法的「带/不带 color」两版 | 40+ 方法 ×2 |
| `FitLineStrategy` vs `FitArcMidpointStrategy` 的边缘查找 + 三段拟合 | 约 60 行结构性重复 |
| `matching/` 四个 Model 策略 | 大量共性逻辑未提取 |
| `DrawHelper` 的 12 个静态入口 | 模板逐字复制 |

- **方案**：见 A3/A4；算法侧提取 `EdgeMeasurePipeline`（`gen_measure_rectangle2` → `measure_pos` → 点集）与 `RobustFitPipeline`（Stage1/2/3 稳健拟合）两个可复用组件。
- **另**：`FitLineStrategy` 的 Stage 2 每移除一个最差点就全量重拟合，复杂度 O(n²)，点数多时明显卡顿，提取时一并优化为增量更新。

### D6. 全局可变静态配置

- **位置**：`DotNet.HalconAlgo/AlgoPaths.cs`

```csharp
public static string ProjectDir = "Config";
public static string SchemeDir = Path.Combine(ProjectDir, "Scheme");  // 静态初始化时定死
public static string JobDir    = Path.Combine(SchemeDir, "Job");
public static string System    = Path.Combine(ProjectDir, "System.json");   // 同样在类型初始化时定死
public static bool UIBlock = true;
```

- **问题**：`ProjectDir` 是可变字段，但派生路径在**类型初始化时**就算好了 —— 运行期改 `ProjectDir`，`SchemeDir`/`JobDir` **不会联动**，产生难查的路径错乱。
- **方案**：改为 `public static string SchemeDir => Path.Combine(ProjectDir, "Scheme");`（表达式属性）；更好的做法是引入 `AlgoOptions` 实例并通过构造注入。

### D7. `HashCode` 定义在 `namespace System`

- **位置**：`DotNet.Drawing/CvMode/HashCode.cs` —— `internal struct HashCode` 放在 `namespace System`。
- **问题**：占用 BCL 命名空间，未来目标框架升级到自带 `System.HashCode` 的版本会产生歧义；`internal` 意味着每个引用程序集都要各自复制一份。
- **方案**：移到 `DotNet.Drawing.Internal` 命名空间，或直接引用 `Microsoft.Bcl.HashCode` NuGet 包。

### D8. 硬编码路径与魔法数

- `HalconController`：`@"D:\Picture\SaveOriginalImages"`、`@"D:\Picture\SaveCropWindow"` 作为默认参数写死；`Directory.CreateDirectory(Path.GetDirectoryName(filePath))` 未判 `null`；`SaveImage(HObject, string)` 中生成的时间戳变量**未被使用**；三个 `SaveImage` 重载叠加默认参数导致调用歧义。
- `HDisplay.DispCvRegion`：所有分支的 `+ 0.5` / `+ 1` 像素偏移，且 Row 加 0.5、Col 加 1，不对称，无任何注释解释来源。
- `FitArcMidpointStrategy`：`lineGate` 的 `15.0`。
- **方案**：路径改为配置项（默认取 `AppDomain.CurrentDomain.BaseDirectory`）；像素偏移提为命名常量并注释推导过程；删除未使用变量；重载改为参数对象或去掉默认值。

### D9. 死方法 / 未使用计算

- **位置**：`DotNet.Drawing/HalconController.cs` 的 `GetTransformedCoord(Point2d, Point2d, CvCoord, out CvCoord)`

```csharp
double angleDiff = (pointTrans.Angle - point.Angle).ToRadians();  // 计算后从未使用
double deltaX = pointTrans.X - point.X;                           // 未使用
double deltaY = pointTrans.Y - point.Y;                           // 未使用
double cosTheta = Math.Cos(0);   // 恒等于 1，硬编码 0
double sinTheta = Math.Sin(0);   // 恒等于 0
```

- **现象**：函数名为「取变换后坐标」，实际**不做任何旋转**（角度写死为 0），是半成品。
- **方案**：补全实现或删除；开启 CS0219（未使用变量）警告为错误以防再犯。

### D10. 其它一致性问题

| 问题 | 位置 |
|---|---|
| `DotNet.HalconAlgo` 未启用 `Nullable`（另两个工程已启用） | `DotNet.HalconAlgo.csproj` |
| `HalconHelper.GetPolygons` 返回 `List<Point2d>`，`HalconController` 同名方法返回 `List<Point2d>?` | 可空注解不一致 |
| `CvRegion` 的 XML 文档通篇写 `InRegion`，实际字段名为 `HoRegion` | `CvMode/CvRegion.cs` |
| `DrawModelUIArgs` 注释称「全部只读属性」，实际 `ModelPath`/`Result` 有 setter；两个 `HObject` 属性所有权不明 | `HWindows/HMouse/DrawEvent.cs` |
| `FitArcMidpointRenderData` 注释称「发布后视为只读」，实际全是 public 可变字段；`Dispose()` 不幂等 | `contour/FitArcMidpointRender.cs` |
| `Point2d` 有 `Rotate`/`RotateAround` 却未实现 `ICvRotatable<Point2d>` | `OpenCvSharp/Point2d.cs` |
| `JsonConvertHObject.CanConvert` 直接 `throw new NotImplementedException()` | `Serialize/JsonConvertHObject.cs` |
| `HTupleExtension.NotNull` 语义可疑（EMPTY 时返回 `Length > 0`），且未处理 `null` | `Extension/HTupleExtension.cs` |
| `StringExtension.ConvertToWesternDigit` 对 `char.GetNumericValue` 结果取 `FirstOrDefault()`，大于 9 的 Unicode 数字（如「十」= 10）只取到首字符 `'1'` | `Extension/StringExtension.cs` |
| `CvCircle.BoundingBox` 对圆弧把圆心一并纳入 → 包围盒偏大 | `CvMode/CvCircle.cs` |
| `CvCircle.SamplePoints(count)` 对圆弧用 `span / count`，不含终点 | `CvMode/CvCircle.cs` |
| `CvCircle.Scale` 只缩放半径不动圆心，与 `Point2d.Scale`（相对原点）语义不一致 | `CvMode/CvCircle.cs` |
| `record` 的 `with` 表达式可绕过构造函数的 `radius < 0` 校验 | `CvMode/CvCircle.cs` |
| `FitLineStrategy.Line` 是引用类型 `record`，默认 `null`；`RegisterOutput("直线/起点", () => inPara.Line.Start)` 在未执行时 NRE | `contour/FitLineStrategy.cs` |
| `RegionExtension.RebuildRegion` 中 `hRegion.HoRegion.Dispose()` 未判 null；Polygon 分支 `PolygonX/PolygonY` 为 null 会抛；`GenCoordsRegion` 未对 `hRegion` 判空 | `Extension/RegionExtension.cs` |
| `HWindowMouse` 用 `DateTime.Now.Ticks` 判双击（受系统时间调整影响），未用 `SystemInformation.DoubleClickTime`；连续三击会被判成两次双击 | `HWindows/HWindowMouse.cs` |
| `MouseDown`/`MouseDouble` 是公开标志位，本类从不复位，依赖外部清除（隐式协议） | `HWindows/HWindowMouse.cs` |
| `HWindowImage.Fun_ZoomImage` 实际在做**控件布局**（改 Width/Height/Location），命名误导且会再次触发 `Resize`；`HWindowControl_Resize` 中 `catch { }` 完全空吞 | `HWindows/HWindowImage.cs` |
| `HDisplay.SetImage` 用 `NullReferenceException` 表达业务错误，应为 `ObjectDisposedException`/`ArgumentNullException` | `HWindows/HDisplay/HDisplay.cs` |
| `FileImageStrategy` 的 `Index`/`ImagePaths` 是隐式实例状态、非线程安全；`FileImage` 持 public `HObject` 字段却不实现 `IDisposable`；`catch { throw; }` 是无意义噪音 | `image/FileImageStrategy.cs` |
| `IHDisplay` 中 `DispRegion(CvRegion)` 与 `DispCvRegion(CvRegion)` 签名相同、命名不同、语义不明 | `HWindows/HDisplay/IHDisplay.cs` |

---

## 五、值得保留的良好设计（重构时作为范本推广）

`contour/FitArcMidpointRender.cs` 是三个工程中**唯一**做到计算与渲染彻底分离的实现，建议作为所有策略类的改造模板：

```csharp
// 单槽发布 + 所有权原子转移，天然线程安全
public FitArcMidpointRenderData TakeRenderData() => Interlocked.Exchange(ref _pendingRenderData, null);
private void PublishRenderData(FitArcMidpointRenderData data)
    => Interlocked.Exchange(ref _pendingRenderData, data)?.Dispose();
```

可取之处：① `ComputeFit` 纯计算、`DrawTo(IHDisplay)` 是唯一绘制入口；② 样式与可见性开关在计算时快照，绘制可在任意线程进行；③ `IDisposable` 明确 `HObject` 所有权；④ `FitArcMidpointRenderFrame.Create` 的失败路径会连带释放 overlay。

**需要一并修正的两点**：`FitArcMidpointRenderData` 的字段应改为 `{ get; init; }` 或只读，让「发布后只读」从注释变成编译期约束；`Dispose()` 应加幂等标志。

---

## 六、分阶段实施路线图

### 阶段 1：止血（1~2 天，零架构改动，风险最低）

- [x] B1 `HalconHelper.GenContours` 自递归
- [x] B2 `DispPolygon` 行列颠倒
- [x] B4 `DispLine` 绕过颜色缓存
- [x] B5 去掉 4 处 `CvCoord.Angle.ToRadians()` 二次转换（HDisplay.cs:417,422 + HalconController.cs:120,121）
- [x] B8 `FileMode.OpenOrCreate` → `FileMode.Create`
- [x] B9 批量清理 31 处 `new HObject(); GenEmptyObj(out x);`
- [x] B12 `HandleDestroyed` → `Dispose(bool)`
- [x] D9 删除 `GetTransformedCoord` 中的死代码（或补全实现）
- **验收**：MSBuild 构建 `DotNet.VisionMaster` 已通过（2026-09-01）。
  - ⚠️ 待人工完成：跑一遍完整流程，用 `HOperatorSet.CountObj` 对比运行前后的对象数是否平衡。
  - ⚠️ `src/DotNet.HalconUI.Tests` 源码在工作区已不存在（仅剩 bin/obj，且从未入库），单元测试暂时无法运行。

### 阶段 2：契约与安全性（3~5 天）

- [x] B6 消除可变全局单例 `Rect2d.Default` / `CvRegion.Empty`
- [x] B7 手写 `CvRegion.Clone()`，明确 `HoRegion` 深拷贝
- [x] B3 / C18 补齐 `switch` 缺失分支，全部加 `default` 兜底
- [x] B10 `FitLineStrategy.Close` 不再销毁配置态对象
- [x] B11 `MergeRegionStrategy`：实现或删除，**禁止保留返回 true 的空实现**
- [x] C6 引入 `TryResolveOutput<T>`，替换所有强转
- [x] C7 `strategys` 传空集合替代 `null`
- [x] C13 / C14 修正容差体系与 `NormalizeAngle`
- [x] C15 `Rect2d` 不可变化；`CvRegion` 改继承为组合
- [x] C17 `VsControlModel` 三个属性改走 `SetField`
- [x] D1 引入 `ILogger`，替换全部 `catch { }` 与 `Console.WriteLine`
- [x] D3 / D4 删除 11 个空壳类与 647 行死文件
- [x] D6 `AlgoPaths` 派生路径改表达式属性
- [x] C2 全部 `Disp*` 补齐 `IsWindowUsable()` 防护；参数校验一律前置于 `SetColor` 等副作用之前
- [x] C8 移除库层的 3 处 `MessageBox.Show`（`FileImageStrategy.Init`、`JsonConvertHObject` ×2），改为抛异常或写日志
- [x] C9 `RotateImageStrategy` 的 `baseAglDeg == 0` 边界明确归入某一分支
- [x] D2 异常包装改为 `throw new XxxException(msg, ex)` 保留 `InnerException`
- [x] D8 硬编码路径 `D:\Picture\...` 改配置项；`DispCvRegion` 的 `+0.5`/`+1` 像素偏移提为命名常量并注明推导；删除未使用变量

### 阶段 3：API 收敛（1~2 周）

- [x] A4 `IHDisplay` 62 → 34 成员（`Disp*` 46 → 15）；统一为 `Disp(图元, DrawStyle)`，`string color` → `HColor` 强类型
- [x] C1 统一坐标序为 `Point2d`(X, Y)，Halcon (Row, Col) 转换只保留在最内层（`TransPixel` → `TransPoint`/`TransPoints`；算法层点集与 `FitArcMidpointRenderData` 改为 `List<Point2d>`）
- [x] B5 后续：引入 `readonly struct Angle`，从类型上消灭单位混淆（`CvCoord.Angle` 改为强类型，`AngleJsonConverter` 保持 JSON 落盘形状不变）
- [x] A3 删除 `HDisplayCore`；`HDisplayUI` 不再实现 `IHDisplay`，改为组合暴露 `Display` 属性
- [x] D5 提取 `EdgeMeasurePipeline` / `RobustFitPipeline`，消除 `FitLine` 与 `FitArcMidpoint` 的重复，同时修掉 O(n²) 重拟合（改为按残差降序分批剔除，重拟合轮数降到约 log₂n；**行为变更**：临界点取舍可能与旧实现不同，需现场回归）
- [x] B1 后续：删除 `HalconHelper`，统一到 `HalconController`（且改为静态无状态类）

### 阶段 4：分层与交互重构（2~4 周，需单独排期）

- [ ] A1 抽出 `DotNet.Vision.Abstractions`，打断 `HalconAlgo → HalconUI`
- [ ] A2 `IParaStrategy` 拆分为 5 个小接口
- [ ] C3 `DrawHelper` 三步走：加超时/取消 → 拆类 → `DrawAsync` 消灭 `DoEvents`
- [ ] C4 `DispPara`/`SavePara` 改为特性驱动的声明式绑定
- [ ] C5 中文文案与业务状态解耦（enum + 资源文件）
- [ ] C10 / C11 ROI 跟随坐标系补全旋转变换，输出与显示保持一致
- [ ] C12 圆弧拟合 Stage 1 改用圆拟合稳健权重
- [ ] C16 统一图像所有权，消除 `HWindowImage.HoImage` 悬挂引用

### 阶段 5：工程化收尾

- [ ] `DotNet.HalconAlgo` 启用 `Nullable`，与另两工程对齐
- [ ] 开启 `TreatWarningsAsErrors`（至少 CS0219 未使用变量、CS8618 不可空未初始化）
- [ ] D7 `HashCode` 迁出 `namespace System`（或改用 `Microsoft.Bcl.HashCode`）
- [ ] D10 逐条清理一致性问题表（注释与代码不符、可空注解不一致、`CanConvert` 抛 `NotImplementedException`、`ConvertToWesternDigit` 的 `FirstOrDefault` 截断、`CvCircle` 圆弧包围盒/采样/缩放语义等）
- [ ] 补齐几何计算（`MathHelper`/`CvCircle`/`CvLine`/`Rect2d`/`Point2d`/`CvCoord`）的单元测试——这部分无 Halcon 依赖，最容易测
- [ ] 加入 HObject 计数断言的集成测试，防止泄漏回归

---

## 七、验证方式

依据 `build-and-test-commands.md`：**以构建 `DotNet.VisionMaster` 为准**验证改动（`DotNet.HWindows` 原始即编译失败，不在本次范围内，也不处理）。注意 Halcon 需 x64 平台。

每个阶段结束后：

1. MSBuild 构建 `DotNet.VisionMaster` 通过；
2. 手工跑通「取像 → 建 ROI → 拟合直线 / 拟合圆弧中点 → 模板匹配」主流程；
3. 用 `HOperatorSet.CountObj` 在流程前后各采一次，确认 Halcon 对象数不增长；
4. 阶段 2 之后开始补单元测试，阶段 5 前几何部分测试覆盖率不低于 70%。

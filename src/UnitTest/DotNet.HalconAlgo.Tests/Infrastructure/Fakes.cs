using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Drawing;
using DotNet.HalconCore;
using HalconDotNet;

namespace DotNet.HalconAlgo.Tests
{
    /// <summary>
    /// 记录绘制调用的 <see cref="IHDisplay"/>：不接真实窗口，只供断言策略「画了什么」。
    /// </summary>
    internal sealed class FakeDisplay : IHDisplay
    {
        public readonly List<Drawn<string>> Texts = new List<Drawn<string>>();
        public readonly List<Drawn<Point2d>> Points = new List<Drawn<Point2d>>();
        public readonly List<Drawn<HObject>> Objects = new List<Drawn<HObject>>();
        public readonly List<Drawn<CvRegion>> Regions = new List<Drawn<CvRegion>>();
        public readonly List<Point2d> Rect2Centers = new List<Point2d>();
        public readonly List<CvArrow> Arrows = new List<CvArrow>();
        public readonly List<CvCoord> Coords = new List<CvCoord>();
        public int DispImageCount;

        public bool IsCross { get; set; }
        public bool Adaptive { get; set; }
        public double HoWidth { get; private set; }
        public double HoHeight { get; private set; }
        public Size2d HoSize => new Size2d(HoWidth, HoHeight);
        public Point2d HoCentre => new Point2d(HoWidth / 2, HoHeight / 2);

        /// <summary>不接管所有权：图像由测试自己释放。</summary>
        public HObject HoImage { get; set; }

        /// <summary>最近一条文本；没有则为 null。</summary>
        public string LastText => Texts.Count == 0 ? null : Texts[Texts.Count - 1].Item;

        public HColor GetColor() => HColor.Green;
        public void SetColor(HColor color) { }
        public void SetDraw(string mode) { }
        public void SetFontSize(HTuple size) { }

        public void SetImage(HObject image)
        {
            HoImage = image;
            if (image.NotNull())
            {
                HOperatorSet.GetImageSize(image, out HTuple w, out HTuple h);
                HoWidth = w.D;
                HoHeight = h.D;
            }
        }

        public void DispImage(HObject image) => DispImage(image, true);

        public void DispImage(HObject image, bool isSetPart)
        {
            DispImageCount++;
            SetImage(image);
        }

        public void ReDispImage() { }
        public void ClearWinDisp(HObject objectVal) { }

        public void Disp(Point2d point, DrawStyle style = null) => Points.Add(new Drawn<Point2d>(point, style));
        public void Disp(IReadOnlyList<Point2d> points, DrawStyle style = null)
        {
            foreach (var p in points) Points.Add(new Drawn<Point2d>(p, style));
        }
        public void Disp(CvCoord coord, DrawStyle style = null) => Coords.Add(coord);
        public void Disp(CvLine line, DrawStyle style = null) { }
        public void Disp(CvArrow arrow, DrawStyle style = null) => Arrows.Add(arrow);
        public void Disp(CvCircle circle, DrawStyle style = null) { }
        public void Disp(CvRegion region, DrawStyle style = null) => Regions.Add(new Drawn<CvRegion>(region, style));
        public void Disp(HObject region, DrawStyle style = null) => Objects.Add(new Drawn<HObject>(region, style));

        public void DispText(string message, Point2d position, DrawStyle style = null) => Texts.Add(new Drawn<string>(message, style, position));

        public void DispRect2(Point2d center, double phi, double length1, double length2, DrawStyle style = null)
            => Rect2Centers.Add(center);

        public void DispRegionOutline(CvRegion region, DrawStyle style = null) { }
        public void DispLineWithEndMarker(CvLine line, double markerRadius, DrawStyle style = null) { }
        public void DispSegmentWithCrosses(Point2d start, Point2d end, double armLength, DrawStyle style = null) { }

        public void DispGenRegion(CvRegion region) { }
        public void GenCoordsRegion(CvRegion region, List<CvCoord> coords) { }
        public Task<bool> DrawRegionAsync(CvRegion region) => Task.FromResult(false);
        public Task<bool> DrawRegionModAsync(CvRegion region) => Task.FromResult(false);
        public Task<HObject> DrawRegionAsync(RectEnum type) => Task.FromResult<HObject>(null);

        public void Dispose() { }
    }

    /// <summary>
    /// 可编排的 <see cref="IRoiHost"/>：「用户画框」由 <see cref="OnDraw"/> 模拟，返回值模拟确认 / 取消。
    /// </summary>
    internal sealed class FakeRoiHost : IRoiHost
    {
        public FakeDisplay FakeDisplay { get; } = new FakeDisplay();
        public IHDisplay Display => FakeDisplay;

        /// <summary>绘制时对区域做的修改（模拟用户拖出的几何）。</summary>
        public Action<CvRegion> OnDraw { get; set; }

        /// <summary>DrawRegionAsync / DrawRegionModAsync 的返回值：false 表示用户取消。</summary>
        public bool Confirm { get; set; } = true;

        public int DrawCount, DrawModCount, SetRectParaCount, SetModelParaCount;
        public RectEnum? TypeDuringDraw;
        public string DonePath;
        public ModelResult? DoneResult;

        public Task<bool> DrawRegionAsync(CvRegion hRegion)
        {
            DrawCount++;
            TypeDuringDraw = hRegion.Type;
            if (Confirm) OnDraw?.Invoke(hRegion);
            return Task.FromResult(Confirm);
        }

        public Task<bool> DrawRegionModAsync(CvRegion hRegion)
        {
            DrawModCount++;
            TypeDuringDraw = hRegion.Type;
            if (Confirm) OnDraw?.Invoke(hRegion);
            return Task.FromResult(Confirm);
        }

        public void SetRectPara(CvRegion shrRegion) => SetRectParaCount++;

        public void SetModelPara(HObject shrFindMode, HObject shrContour, CvCoord shrCoord) => SetModelParaCount++;

        public void DrawDone(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            DonePath = modelPath;
            DoneResult = result;
        }
    }

    /// <summary>字典驱动的 <see cref="IParaUiHost"/>：Show* 写入控件值，Get* 从同一字典读回。</summary>
    internal sealed class FakeUiHost : IParaUiHost
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public readonly Dictionary<string, bool> Checks = new Dictionary<string, bool>();
        public readonly Dictionary<string, string[]> Items = new Dictionary<string, string[]>();
        public TabPageEnum[] Tabs = new TabPageEnum[0];

        public FakeUiHost Set(string name, string value) { Values[name] = value; return this; }
        public FakeUiHost Check(string name, bool value) { Checks[name] = value; return this; }

        public void ShowTabs(params TabPageEnum[] tabsToShow) => Tabs = tabsToShow;
        public void ClearAll() { Values.Clear(); Checks.Clear(); Items.Clear(); }

        public void ShowLabel(string name, string text) => Values[name] = text;
        public void ShowButton(string name, bool visible) { }
        public void ShowTextBox(string name, string text) => Values[name] = text;
        public void ShowComboBox(string name, string text, bool enabled) => Values[name] = text;
        public void ShowComboBoxList(string name, string text, string[] items) { Values[name] = text; Items[name] = items; }
        public void ShowComboBoxDropDown(string name, string text, string[] items) { Values[name] = text; Items[name] = items; }
        public void ShowCheckBox(string name, string text, bool isChecked) => Checks[name] = isChecked;
        public void ShowGroupBox(string name) { }
        public void ShowRadioButton(string name, string text, bool visible, bool isChecked) => Checks[name] = isChecked;
        public void ShowTrackBar(string name, int value) => Values[name] = value.ToString();
        public void ShowTabPage(string name, string text, bool visible) { }

        public string GetString(string name) => Values.TryGetValue(name, out var v) ? v : null;
        public string GetString(string name, string fallback) => Values.TryGetValue(name, out var v) ? v : fallback;
        public bool GetBool(string name) => Checks.TryGetValue(name, out var v) && v;
        public bool GetBool(string name, bool fallback) => Checks.TryGetValue(name, out var v) ? v : fallback;
        public int GetInt(string name) => int.Parse(GetString(name));
        public int GetInt(string name, int fallback) => int.TryParse(GetString(name), out var v) ? v : fallback;
        public double GetDouble(string name) => double.Parse(GetString(name));
        public double GetDouble(string name, double fallback) => double.TryParse(GetString(name), out var v) ? v : fallback;
    }

    /// <summary>把树结构拍平成 "策略/节点/子节点" 路径，便于断言。</summary>
    internal sealed class FakeTree : ITreeVisualizer
    {
        public readonly List<string> Paths = new List<string>();

        public ITreeVisualizer Branch(string text, Action<ITreeBranch> config)
        {
            Paths.Add(text);
            config?.Invoke(new FakeBranch(this, text));
            return this;
        }

        public ITreeVisualizer Branches(params string[] texts)
        {
            Paths.AddRange(texts);
            return this;
        }

        private sealed class FakeBranch : ITreeBranch
        {
            private readonly FakeTree _tree;
            private readonly string _prefix;

            public FakeBranch(FakeTree tree, string prefix)
            {
                _tree = tree;
                _prefix = prefix;
            }

            public ITreeBranch Node(string text, OutEnum type, Action<ITreeBranch> config = null) => Branch(text, config);

            public ITreeBranch Branch(string text, Action<ITreeBranch> config)
            {
                var path = _prefix + "/" + text;
                _tree.Paths.Add(path);
                config?.Invoke(new FakeBranch(_tree, path));
                return this;
            }

            public ITreeBranch ReusePointStructure(string pointName) => this;

            public ITreeBranch CommonNodes() => this;
        }
    }

    /// <summary>上游策略替身：按路径直接登记输出值。</summary>
    internal sealed class StubStrategy : ParaStrategyBase<object>
    {
        public StubStrategy(string name) { Name = name; }

        public override AlgoEnum Algorithm => AlgoEnum.Undefined;
        public override string Name { get; set; }
        public override int RunIndex { get; set; }

        public StubStrategy Output(string path, object value)
        {
            RegisterOutput(path, () => value);
            return this;
        }

        public override void GenTreeNode(ITreeVisualizer tree) { }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys) => true;
        public override void DispPara(IParaUiHost ui) { }
        public override void SavePara(IParaUiHost ui) { }

        /// <summary>
        /// 模拟一个「坐标系」上游：TmplPoint 为示教原点，坐标系为当前位姿。
        /// </summary>
        public static StubStrategy Coord(string name, Point2d tmplPoint, CvCoord current)
            => new StubStrategy(name).Output("TmplPoint", tmplPoint).Output("坐标系", current);
    }

    /// <summary>捕获 <see cref="Log"/> 输出；Dispose 时恢复原 logger。</summary>
    internal sealed class CapturingLogger : ILogger, IDisposable
    {
        private readonly ILogger _previous;
        public readonly List<LogEntry> Entries = new List<LogEntry>();

        public CapturingLogger()
        {
            _previous = DotNet.Drawing.Log.Current;
            DotNet.Drawing.Log.Current = this;
        }

        public void Log(LogLevel level, string category, string message, Exception exception)
            => Entries.Add(new LogEntry { Level = level, Category = category, Message = message, Exception = exception });

        public IEnumerable<string> Messages(LogLevel level) => Entries.Where(e => e.Level == level).Select(e => e.Message);

        public void Dispose() => DotNet.Drawing.Log.Current = _previous;
    }

    internal sealed class LogEntry
    {
        public LogLevel Level;
        public string Category;
        public string Message;
        public Exception Exception;
    }

    /// <summary>一次绘制调用：绘制对象 + 样式（文本另带位置）。</summary>
    internal sealed class Drawn<T>
    {
        public Drawn(T item, DrawStyle style, Point2d position = default)
        {
            Item = item;
            Style = style;
            Position = position;
        }

        public T Item { get; }
        public DrawStyle Style { get; }
        public Point2d Position { get; }

        public string ColorName => Style?.Color.Name;
    }

    internal static class Strategies
    {
        public static List<IParaStrategy> Of(params IParaStrategy[] items) => new List<IParaStrategy>(items);
    }
}

using DotNet.Drawing;
using DotNet.HalconUI;
using DotNet.Vision.Abstractions;
using DotNet.HalconAlgo;
using HalconDotNet;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public partial class ParaForm : UserControl
    {
        int _index;
        List<IParaStrategy> _strategys;
        HDisplayUI _disPlay;
        HModelUI _hModel;
        ValueForm _form_Value;
        HEditModelUI _editModel;

        Dictionary<RadioButton, RectEnum> _rectDrawMap;
        Dictionary<RadioButton, RectEnum> _modelDrawMap;

        /// <summary>
        /// 交互绘制是否正在进行中，用于挡住重入。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 本窗体的 4 个绘制入口都是 <c>async void</c>，<c>await</c> 期间 UI 线程继续泵消息，
        /// 用户完全可以再点另一个按钮：新的绘制会 <c>CancelDraw</c> 掉上一次的会话，
        /// 于是两条协程交叉读写同一个策略对象与模板句柄（先 Dispose 再赋值），
        /// 轻则 ROI 参数错乱，重则访问已释放的 HObject。
        /// 只在 UI 线程读写，无需 Interlocked。
        /// </para>
        /// <para>
        /// <b>不变式</b>：同一 <c>HDisplayUI</c> 上任何时刻只能有一条绘制在飞，新增绘制入口时必须一起判它。
        /// 原因：<c>DrawHelper.RunAsync</c> 每次开头都 <c>CancelDraw(window)</c>，
        /// 而 <c>DrawSession.Finish</c> 的 <c>TrySetResult</c> 会<b>就地内联</b>旧会话调用方 await 之后的
        /// 全部代码，一路跑到旧 handler 的 <c>finally</c>。若允许在 <c>_drawBusy == true</c> 时再发起绘制，
        /// 旧续体的 <c>finally</c> 就会在新会话 <c>Begin</c> 之前把闸门清成 false，闸门形同虚设。
        /// <see cref="_drawEpoch"/> 让每一轮只清自己那一轮的标志，从机制上挡掉这种误清。
        /// </para>
        /// </remarks>
        bool _drawBusy;

        /// <summary>
        /// 绘制轮次。每发起一轮 +1，<c>finally</c> 凭它判断"这一轮还是不是我"。
        /// </summary>
        /// <remarks>
        /// <b>当前路径下不可达</b>：4 个入口开头都有 <c>if (_drawBusy) return;</c>，
        /// 第二个 handler 根本起不来，<c>finally</c> 里的 <c>_drawEpoch == epoch</c> 恒为真。
        /// 它是给"将来新增了不判 <see cref="_drawBusy"/> 的入口"准备的前瞻性护栏，
        /// 不是在修一个正在发生的 bug —— 改动这里前先看 <see cref="_drawBusy"/> 的不变式说明。
        /// </remarks>
        int _drawEpoch;

        public ParaForm(HDisplayUI displayUI)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            _disPlay = displayUI;
        }
        private void ParaForm_Load(object sender, EventArgs e)
        {
            _hModel = new HModelUI();
            _editModel = new HEditModelUI();
            _form_Value = new ValueForm(this.FindForm());

            // _editModel / _form_Value 没有父容器, Designer 的 Dispose(bool) 只管 components,
            // 谁也不会去释放它们; _editModel 还因 HEditForm_FormClosing 的 e.Cancel = true 永远关不掉,
            // 不显式释放的话它持有的 HObject 与 HWindow 会一直挂到进程退出。
            // 挂 HandleDestroyed 而不是重写 Dispose(bool): 后者已在 Designer 里定义, 同一方法不能定义两次。
            HandleDestroyed += ParaForm_HandleDestroyed;

            panel1.Controls.Add(_hModel);
            _disPlay.DrawDoneEvent += DrawDoneEvent;

            _rectDrawMap = new Dictionary<RadioButton, RectEnum>
            {
                { btn_rectRectangle, RectEnum.Rectangle },
                { btn_rectAffRect,   RectEnum.AffRect },
                { btn_rectCircle,    RectEnum.Circle },
                { btn_rectEllipse,   RectEnum.Ellipse },
                { btn_rectPolygon,   RectEnum.Polygon },
            };

            _modelDrawMap = new Dictionary<RadioButton, RectEnum>
            {
                { btn_modelRectangle, RectEnum.Rectangle },
                { btn_modelAffRect,   RectEnum.AffRect },
                { btn_modelCircle,    RectEnum.Circle },
                { btn_modelEllipse,   RectEnum.Ellipse },
                { btn_modelPolygon,   RectEnum.Polygon },
            };
        }

        /// <summary>
        /// 释放 <see cref="ParaForm_Load"/> 里自行 new、又没挂进控件树的两个窗体。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>_hModel</c> 进了 <c>panel1.Controls</c>，随父控件一起销毁，不用管；
        /// <c>_editModel</c> / <c>_form_Value</c> 没有父容器，只能在这里收口。
        /// </para>
        /// <para>
        /// <b>必须判 <see cref="System.Windows.Forms.Control.Disposing"/></b>：句柄重建
        /// (改 <c>RightToLeft</c>、被重新 Parent 等)同样会触发本事件，那时控件还活着，
        /// 释放了后面就没得用了。<c>Control.Dispose(bool)</c> 是先置 <c>STATE_DISPOSING</c>
        /// 再 <c>DestroyHandle()</c>，所以真正销毁时这里读到的 <c>Disposing</c> 一定是 true，
        /// 拿它当判据是准的。也正因为句柄可能重建，这里<b>不能</b>顺手退订本事件。
        /// </para>
        /// </remarks>
        private void ParaForm_HandleDestroyed(object sender, EventArgs e)
        {
            if (!Disposing && !IsDisposed) return;

            _disPlay.DrawDoneEvent -= DrawDoneEvent;   // Load 里订阅过, 不退订 HDisplayUI 会一直引着本控件

            try { _editModel?.Dispose(); }
            catch (Exception ex) { Log.Warn(nameof(ParaForm), "释放模板编辑窗失败.", ex); }

            try { _form_Value?.Dispose(); }
            catch (Exception ex) { Log.Warn(nameof(ParaForm), "释放数值窗失败.", ex); }
        }

        /// <summary>
        /// 交互绘制是否正在进行中。宿主在切换工具<b>之前</b>必须先问一次。
        /// </summary>
        /// <remarks>
        /// 绘制期间切换工具会改写 <c>DrawType</c>(`DispROI` → `SetRectPara`)，
        /// 进行中的会话就此收不到鼠标事件(见 <c>HDisplayUI.ResolveMouseHandler</c>)，一直卡到 5 分钟超时。
        /// 这道闸门必须由宿主在改动自身状态前拦下 —— 放在 <see cref="SelectPara"/> 里静默 return 是没用的：
        /// 宿主的 `ClearAll` / `DispPara` / `DispROI` 照跑不误，卡死照旧，还会让宿主与本窗体的索引脱节。
        /// </remarks>
        public bool IsDrawBusy => _drawBusy;

        public void SelectPara(int index, List<IParaStrategy> strategys)
        {
            _index = index;
            _strategys = strategys;
        }
        private void btn_100_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                    case AlgoEnum.LineRotImage:
                    case AlgoEnum.RotateImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_100.Text, OutEnum.Image);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_100.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void btn_101_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_101.Text, OutEnum.Region);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_101.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                    case AlgoEnum.LineRotImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_101.Text, OutEnum.Line);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_101.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_setPath_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button == btn_setPath)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                try { fbd.SelectedPath = cmb_ImageFolder.Text; } catch { }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    cmb_ImageFolder.Text = fbd.SelectedPath;
                }
            }
            else
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                try { fbd.SelectedPath = cmb_115.Text; } catch { }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    cmb_115.Text = fbd.SelectedPath;
                }
            }
        }
        private void btn_openPath_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button == btn_openPath)
            {
                try
                {
                    System.Diagnostics.Process.Start(cmb_ImageFolder.Text);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(cmb_115.Text);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }
        private void btn_setCoordIn_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.CreateROI:
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                    case AlgoEnum.RotateImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_CoordIn.Text, OutEnum.Coord);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_CoordIn.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private async void btn_drawRegion_Click(object sender, EventArgs e)
        {
            if (_drawBusy) return;
            _drawBusy = true;
            int epoch = ++_drawEpoch;
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                        btn_rectAffRect.Checked = true;
                        break;
                    default:
                        btn_rectRectangle.Checked = true;
                        break;
                }
                var drawType = _rectDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                if (strategy is IRoiEditable roi)
                    await roi.DrawROIAsync(_disPlay, drawType, true);
            }
            catch (Exception ex)
            {
                // 这里可能是在控件 Dispose 里被 CancelDraw 内联唤醒的
                // (见 HDisplayUI.ReleaseDisplayResources): 此时弹模态框会开一个消息循环,
                // 把控件销毁整个卡住, 用户还能在模态框上继续操作半销毁的界面。
                // 查 _disPlay.IsReleasing 而不是本控件的 IsDisposed/Disposing —— 那一刻两者都还没置位:
                // ParaForm 与 HDisplayUI 是兄弟控件, 且 Designer 的 Dispose(bool) 是先调
                // ReleaseDisplayResources、后调 base.Dispose(disposing)。
                if (!_disPlay.IsReleasing && !IsDisposed && !Disposing) MessageBox.Show(ex.Message);
                else Log.Warn(nameof(ParaForm), "绘制异常(显示控件正在释放, 不弹框).", ex);
            }
            finally { if (_drawEpoch == epoch) _drawBusy = false; }   // 只清自己那一轮
        }
        private async void but_editRegion_Click(object sender, EventArgs e)
        {
            if (_drawBusy) return;
            _drawBusy = true;
            int epoch = ++_drawEpoch;
            try
            {
                var strategy = _strategys[_index];
                _disPlay.ReDispImage();
                var drawType = _rectDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                if (strategy is IRoiEditable roi)
                    await roi.DrawROIAsync(_disPlay, drawType, false);
            }
            catch (Exception ex)
            {
                // 这里可能是在控件 Dispose 里被 CancelDraw 内联唤醒的
                // (见 HDisplayUI.ReleaseDisplayResources): 此时弹模态框会开一个消息循环,
                // 把控件销毁整个卡住, 用户还能在模态框上继续操作半销毁的界面。
                // 查 _disPlay.IsReleasing 而不是本控件的 IsDisposed/Disposing —— 那一刻两者都还没置位:
                // ParaForm 与 HDisplayUI 是兄弟控件, 且 Designer 的 Dispose(bool) 是先调
                // ReleaseDisplayResources、后调 base.Dispose(disposing)。
                if (!_disPlay.IsReleasing && !IsDisposed && !Disposing) MessageBox.Show(ex.Message);
                else Log.Warn(nameof(ParaForm), "绘制异常(显示控件正在释放, 不弹框).", ex);
            }
            finally { if (_drawEpoch == epoch) _drawBusy = false; }   // 只清自己那一轮
        }
        private async void btn_newModel_Click(object sender, EventArgs e)
        {
            if (_drawBusy) return;
            _drawBusy = true;
            int epoch = ++_drawEpoch;
            try
            {
                var strategy = _strategys[_index];
                var drawType = _modelDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                if (strategy is ITemplateEditable template)
                    await template.SetTemplateAsync(_disPlay, drawType, true);
            }
            catch (Exception ex)
            {
                // 这里可能是在控件 Dispose 里被 CancelDraw 内联唤醒的
                // (见 HDisplayUI.ReleaseDisplayResources): 此时弹模态框会开一个消息循环,
                // 把控件销毁整个卡住, 用户还能在模态框上继续操作半销毁的界面。
                // 查 _disPlay.IsReleasing 而不是本控件的 IsDisposed/Disposing —— 那一刻两者都还没置位:
                // ParaForm 与 HDisplayUI 是兄弟控件, 且 Designer 的 Dispose(bool) 是先调
                // ReleaseDisplayResources、后调 base.Dispose(disposing)。
                if (!_disPlay.IsReleasing && !IsDisposed && !Disposing) MessageBox.Show(ex.Message);
                else Log.Warn(nameof(ParaForm), "绘制异常(显示控件正在释放, 不弹框).", ex);
            }
            finally { if (_drawEpoch == epoch) _drawBusy = false; }   // 只清自己那一轮
        }

        private async void but_modifyModel_Click(object sender, EventArgs e)
        {
            if (_drawBusy) return;
            _drawBusy = true;
            int epoch = ++_drawEpoch;
            try
            {
                var strategy = _strategys[_index];
                var drawType = _modelDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                if (strategy is ITemplateEditable template)
                    await template.SetTemplateAsync(_disPlay, drawType, false);
            }
            catch (Exception ex)
            {
                // 这里可能是在控件 Dispose 里被 CancelDraw 内联唤醒的
                // (见 HDisplayUI.ReleaseDisplayResources): 此时弹模态框会开一个消息循环,
                // 把控件销毁整个卡住, 用户还能在模态框上继续操作半销毁的界面。
                // 查 _disPlay.IsReleasing 而不是本控件的 IsDisposed/Disposing —— 那一刻两者都还没置位:
                // ParaForm 与 HDisplayUI 是兄弟控件, 且 Designer 的 Dispose(bool) 是先调
                // ReleaseDisplayResources、后调 base.Dispose(disposing)。
                if (!_disPlay.IsReleasing && !IsDisposed && !Disposing) MessageBox.Show(ex.Message);
                else Log.Warn(nameof(ParaForm), "绘制异常(显示控件正在释放, 不弹框).", ex);
            }
            finally { if (_drawEpoch == epoch) _drawBusy = false; }   // 只清自己那一轮
        }
        private void but_editModel_Click(object sender, EventArgs e)
        {
            // 绘制期间不能打开编辑窗：它会把 ModeRect.HoRegion 的句柄交给 _editModel，
            // 而待完成的绘制稍后会 ReplaceRegion 释放这个旧句柄。
            // 与下面编辑窗那道闸门、以及宿主 SwitchStrategy 的提示保持一致: 都给明确反馈,
            // 不静默吞掉点击 —— 本窗体 4 个绘制按钮在绘制期间并不禁用, 用户很容易点到这里。
            if (_drawBusy)
            {
                MessageBox.Show("当前正在绘制 ROI / 模板，请先在图像上右键确认或取消后再打开模板编辑窗。");
                return;
            }

            // 编辑窗自己也可能正在绘制(它是非模态的 Show(), 主窗照样能点)：
            // DisplayModel 会把 DrawType 改成 DispModel, 把编辑窗里挂起的会话饿死到 5 分钟超时,
            // 顺带还会 Reset() + Dispose 掉它正在用的 shrFindMode。
            if (_editModel.IsDrawBusy)
            {
                MessageBox.Show("模板编辑窗正在绘制区域，请先在图像上右键确认或取消后再打开。");
                return;
            }

            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                        {
                            var inPara = ((ShapeModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.NccModel:
                        {
                            var inPara = ((NccModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.ScaledModel:
                        {
                            var inPara = ((ScaledModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.GenericModel:
                        {
                            var inPara = ((GenericModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DrawDoneEvent(object sender, DrawModelUIArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                        {
                            var inPara = ((ShapeModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.NccModel:
                        {
                            var inPara = ((NccModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.ScaledModel:
                        {
                            var inPara = ((ScaledModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.GenericModel:
                        {
                            var inPara = ((GenericModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

       
    }
}

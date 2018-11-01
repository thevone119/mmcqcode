using System.Drawing;
using System.Windows.Forms;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirScenes;
using Microsoft.DirectX.Direct3D;
using S = ServerPackets;
using C = ClientPackets;

namespace Client.MirControls
{
    /// <summary>
    /// 传奇的场景,主界面的意思,目前有3种不同的主界面，分别是注册登录界面，人物选择界面，游戏界面
    /// 这里的场景是没有图的，是个虚拟的东西
    /// </summary>
    public abstract class MirScene : MirControl
    {
        //登录的场景是静态的，不销毁？为什么在这里创建一个静态的登录场景，想不明白(其实不应该放在这里，应该放在全局的一个类里去,可能是这里使用得多一点吧？)
        //当前活动的场景
        public static MirScene ActiveScene = new LoginScene();
        //最后按下的键，左，右
        private static MouseButtons _buttons;
        //最后点击事件
        private static long _lastClickTime;
        //最后点击的控件
        private static MirControl _clickedControl;
        //private bool _redraw;

        protected MirScene()
        {
            DrawControlTexture = true;
            BackColour = Color.Magenta;//紫红色
            BackColour = Color.Gray;//灰色吧？
            Size = new Size(Settings.ScreenWidth, Settings.ScreenHeight);
        }

        public override sealed Size Size
        {
            get { return base.Size; }
            set { base.Size = value; }
        }


        public override void Draw()
        {
            if (IsDisposed || !Visible)
                return;

            OnBeforeShown();

            DrawControl();

            if (CMain.DebugBaseLabel != null && !CMain.DebugBaseLabel.IsDisposed)
                CMain.DebugBaseLabel.Draw();

            if (CMain.HintBaseLabel != null && !CMain.HintBaseLabel.IsDisposed)
                CMain.HintBaseLabel.Draw();

            OnShown();
        }

        protected override void CreateTexture()
        {
            if (ControlTexture != null && !ControlTexture.Disposed && Size != TextureSize)
                ControlTexture.Dispose();

            if (ControlTexture == null || ControlTexture.Disposed)
            {
                DXManager.ControlList.Add(this);
                ControlTexture = new Texture(DXManager.Device, Size.Width, Size.Height, 1, Usage.RenderTarget, Format.A8R8G8B8, Pool.Default);
                ControlTexture.Disposing += ControlTexture_Disposing;
                TextureSize = Size;
            }
            Surface oldSurface = DXManager.CurrentSurface;
            Surface surface = ControlTexture.GetSurfaceLevel(0);
            DXManager.SetSurface(surface);


            DXManager.Device.Clear(ClearFlags.Target, BackColour, 0, 0);

            BeforeDrawControl();
            DrawChildControls();
            AfterDrawControl();

            DXManager.Sprite.Flush();


            DXManager.SetSurface(oldSurface);
            TextureValid = true;
            surface.Dispose();

        }

        public override void OnMouseDown(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (MouseControl != null && MouseControl != this)
                MouseControl.OnMouseDown(e);
            else
                base.OnMouseDown(e);
        }
        public override void OnMouseUp(MouseEventArgs e)
        {
            if (!Enabled)
                return;
            if (MouseControl != null && MouseControl != this)
                MouseControl.OnMouseUp(e);
            else
                base.OnMouseUp(e);
        }
        public override void OnMouseMove(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (MouseControl != null && MouseControl != this && MouseControl.Moving)
                MouseControl.OnMouseMove(e);
            else
                base.OnMouseMove(e);
        }
        public override void OnMouseWheel(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (MouseControl != null && MouseControl != this)
                MouseControl.OnMouseWheel(e);
            else
                base.OnMouseWheel(e);
        }

        public override void OnMouseClick(MouseEventArgs e)
        {
            if (!Enabled)
                return;
            if (_buttons == e.Button)
            {
                if (_lastClickTime + SystemInformation.DoubleClickTime >= CMain.Time)
                {
                    OnMouseDoubleClick(e);
                    return;
                }
            }
            else
                _lastClickTime = 0;

            if (ActiveControl != null && ActiveControl.IsMouseOver(CMain.MPoint) && ActiveControl != this)
                ActiveControl.OnMouseClick(e);
            else
                base.OnMouseClick(e);

            _clickedControl = ActiveControl;

            _lastClickTime = CMain.Time;
            _buttons = e.Button;
        }
        public override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (!Enabled)
                return;
            _lastClickTime = 0;
            _buttons = MouseButtons.None;

            if (ActiveControl != null && ActiveControl.IsMouseOver(CMain.MPoint) && ActiveControl != this)
            {
                if (ActiveControl == _clickedControl)
                    ActiveControl.OnMouseDoubleClick(e);
                else
                    ActiveControl.OnMouseClick(e);
            }
            else
            {
                if (ActiveControl == _clickedControl)
                    base.OnMouseDoubleClick(e);
                else
                    base.OnMouseClick(e);
            }
        }

        public override void Redraw()
        {
            TextureValid = false;
        }
        /// <summary>
        /// 所有场景通用的处理
        /// 好像只有游戏场景才用到？
        /// </summary>
        /// <param name="p"></param>
        public virtual void ProcessPacket(Packet p)
        {
            switch (p.Index)
            {
                case (short)ServerPacketIds.Disconnect: // Disconnected
                    Disconnect((S.Disconnect) p);
                    Network.Disconnect();
                    break;
                case (short)ServerPacketIds.NewItemInfo:
                    NewItemInfo((S.NewItemInfo) p);
                    break;
                case (short)ServerPacketIds.NewQuestInfo:
                    NewQuestInfo((S.NewQuestInfo)p);
                    break;
                case (short)ServerPacketIds.NewRecipeInfo:
                    NewRecipeInfo((S.NewRecipeInfo)p);
                    break;
            }
        }

        private void NewItemInfo(S.NewItemInfo info)
        {
            GameScene.ItemInfoList.Add(info.Info);
        }

        private void NewQuestInfo(S.NewQuestInfo info)
        {
            GameScene.QuestInfoList.Add(info.Info);
        }

        private void NewRecipeInfo(S.NewRecipeInfo info)
        {
            GameScene.RecipeInfoList.Add(info.Info);

            GameScene.Bind(info.Info.Item);

            for (int j = 0; j < info.Info.Ingredients.Count; j++)
                GameScene.Bind(info.Info.Ingredients[j]);
        }

        private static void Disconnect(S.Disconnect p)
        {
            switch (p.Reason)
            {
                case 0:
                    MirMessageBox.Show("Disconnected: Server is shutting down.", true);
                    break;
                case 1:
                    MirMessageBox.Show("Disconnected: Another user logged onto your account.", true);
                    break;
                case 2:
                    MirMessageBox.Show("Disconnected: Packet Error.", true);
                    break;
                case 3:
                    MirMessageBox.Show("Disconnected: Server Crashed.", true);
                    break;
                case 4:
                    MirMessageBox.Show("Disconnected: Kicked by Admin.", true);
                    break;
                case 5:
                    MirMessageBox.Show("Disconnected: Maximum connections reached.", true);
                    break;
            }

            GameScene.LogTime = 0;
        }

        public abstract void Process();

        #region Disposable

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);

            if (!disposing) return;

            if (ActiveScene == this) ActiveScene = null;

            _buttons = 0;
            _lastClickTime = 0;
            _clickedControl = null;
        }

        #endregion
    }
}
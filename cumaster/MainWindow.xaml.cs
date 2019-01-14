using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using static global.funcs;

namespace baha_test
{
    public partial class MainWindow : Window
    {
        //main timer
        System.Timers.Timer MainTick;

        //stuffs for loading 
        Thread BahaLoadingThread;
        Boolean IsLoading = false, IsUpdateFromText = false;
        String
            threadUrl, threadFloor, threadRoomId,
            Warning, stepString;

        //stuffs for game system 
        GameHandler game;
        List<Step> GlobalStepList;
        int stepOffset = 0;

        //stuffs for UI
        Button
            previewButt; //to prevent the previewing button to be refreshed by loop
        Button[,] bts = new Button[20, 20];

        System.Timers.Timer ThemeChange;

        float 
            ThemeRatio = 1.0f,
            ThemeKey = 1.0f;

        Color DarkTheme_BG = Color.FromRgb(19, 19, 19);
        Color DarkTheme_Dark = Color.FromRgb(29, 29, 29);
        Color DarkTheme_Light = Color.FromRgb(120, 120, 120);
        Color DarkTheme_Text = Color.FromRgb(255, 255, 255);
        
        Color BrightTheme_BG = Color.FromRgb(250, 250, 255);
        Color BrightTheme_Dark = Color.FromRgb(100, 130, 135);
        Color BrightTheme_Light = Color.FromRgb(120, 160, 180);
        Color BrightTheme_Text = Color.FromRgb(0, 0, 0);

        String DragName = "";

        public MainWindow()
        {
            InitializeComponent();
            InitializeButtons();

            //initialize the game system
            game = new GameHandler(this,bts);

            //initialize the main timer
            MainTick = new System.Timers.Timer { Interval = 420 };
            MainTick.Elapsed += new ElapsedEventHandler(Loop);
            MainTick.Start();

            ThemeChange = new System.Timers.Timer { Interval = 50 };
            ThemeChange.Elapsed += new ElapsedEventHandler(ColorChange);

            //initialize the loading thread
            BahaLoadingThread = new Thread(LoadingThread);
            BahaLoadingThread.IsBackground = true;
            BahaLoadingThread.Start();
        }

        void InitializeButtons()
        {
            //style for buttons
            Style PieceStyle = FindResource("butS") as Style;

            //button array
            for (int x = 0; x < bts.GetLength(0); x++)
            {
                for (int y = 0; y < bts.GetLength(1); y++)
                {
                    //passing value to events setting later
                    int x_ = x, y_ = y;

                    //new button
                    bts[x, y] = new Button
                    {
                        Background = Brushes.Gray,
                        BorderBrush = null,
                        Opacity = 0.0,
                        Style = PieceStyle,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    };

                    //setting the evevnts of this button
                    bts[x, y].MouseEnter += (sender1, e1) => buttonMEnter(x_, y_);
                    bts[x, y].MouseLeave += (sender1, e1) => buttonMLeave(x_, y_);
                    bts[x, y].Click += (sender1, e1) => buttonClicked(x_, y_);

                    //add the button to the grid
                    grid.Children.Add(bts[x, y]);
                    Grid.SetRow(bts[x, y], y);
                    Grid.SetColumn(bts[x, y], x);

                    //strokes on the board
                    Rectangle rect = new Rectangle
                    {
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.1,
                        SnapsToDevicePixels = true
                    };

                    grid.Children.Add(rect);
                    Grid.SetRow(rect, y);
                    Grid.SetColumn(rect, x);
                }
            }
        }
        
        private void But_Click(object sender, RoutedEventArgs e)
        {
            if (!IsUpdateFromText)
            {
                //switch of loading thread
                IsLoading = !IsLoading;

                if (IsLoading)
                    this.Resources["LoadButColor"] = new SolidColorBrush(Colors.Red);
                else
                    this.Resources["LoadButColor"] = FindResource("LightColor");
            }
            else
                AnnounceText("請關閉文本更新。");

        }
        
        void Loop(object sender, EventArgs e)
        {
            Action delg = delegate ()
            {
                if (GlobalStepList != null)
                {
                    game.Update(GlobalStepList, previewButt, stepOffset);
                    
                    //show the Player we're waiting for
                    if (GlobalStepList.Last().id == 0){
                        nameboardA.Opacity = 0.3;
                        nameboardB.Opacity = 0.9;
                    }
                    else{
                        nameboardA.Opacity = 0.9;
                        nameboardB.Opacity = 0.3;
                    }
                }
                else{
                    nameboardA.Opacity = 0.3;
                    nameboardB.Opacity = 0.3;
                }

                announce.Opacity = 0.4;

                //update from text
                if (IsUpdateFromText)
                {
                    UpdateFromText();

                    //for the convenience to drag the command directly to the new line
                    if (admi.Text.Split('\n').Last() != "")
                        admi.Text += "\n";
                }

                //update the data for loading thread
                threadUrl = urlBar.Text;
                threadFloor = floorBox.Text;
                threadRoomId = roomID.Text;

                //update loading state
                if (Warning == "reading")
                {
                    if (LoadStateBox.Text == "讀取中")
                        LoadStateBox.Text = "讀取中.";
                    else if (LoadStateBox.Text == "讀取中.")
                        LoadStateBox.Text = "讀取中..";
                    else if (LoadStateBox.Text == "讀取中..")
                        LoadStateBox.Text = "讀取中...";
                    else
                        LoadStateBox.Text = "讀取中";
                }
                else if ((IsLoading && Warning == "") || !IsLoading)
                {
                    LoadStateBox.Text = "";

                    if (IsLoading)
                        admi.Text = stepString;
                }
                else
                    LoadStateBox.Text = Warning;
            };
            Dispatcher.BeginInvoke(delg);
        }

        private void ThemeBut_Click(object sender, RoutedEventArgs e)
        {
            ThemeRatio = ThemeRatio == 1.0 ? 0.0f : 1.0f;
            ThemeChange.Start();
        }

        void ColorChange(object sender, EventArgs e)
        {
            Action delg = delegate ()
            {
                ThemeKey =(ThemeKey+ThemeRatio)/ 2.0f;

                Double a = ThemeKey, b = 1 - a;

                this.Resources["BackgroundColor"] = Color.FromRgb(Convert.ToByte(a * DarkTheme_BG.R + b * BrightTheme_BG.R), Convert.ToByte(a * DarkTheme_BG.G + b * BrightTheme_BG.G), Convert.ToByte(a * DarkTheme_BG.B + b * BrightTheme_BG.B));
                SolidColorBrush DB = new SolidColorBrush();
                SolidColorBrush LB = new SolidColorBrush();
                SolidColorBrush TB = new SolidColorBrush();

                DB.Color = Color.FromRgb(Convert.ToByte(a * DarkTheme_Dark.R + b * BrightTheme_Dark.R), Convert.ToByte(a * DarkTheme_Dark.G + b * BrightTheme_Dark.G), Convert.ToByte(a * DarkTheme_Dark.B + b * BrightTheme_Dark.B));
                LB.Color = Color.FromRgb(Convert.ToByte(a * DarkTheme_Light.R + b * BrightTheme_Light.R), Convert.ToByte(a * DarkTheme_Light.G + b * BrightTheme_Light.G), Convert.ToByte(a * DarkTheme_Light.B + b * BrightTheme_Light.B));
                TB.Color = Color.FromRgb(Convert.ToByte(a * DarkTheme_Text.R + b * BrightTheme_Text.R), Convert.ToByte(a * DarkTheme_Text.G + b * BrightTheme_Text.G), Convert.ToByte(a * DarkTheme_Text.B + b * BrightTheme_Text.B));
                this.Resources["DarkColor"] = DB;
                this.Resources["LightColor"] = LB;
                this.Resources["TextColor"] = TB;

                if (!IsLoading)
                    this.Resources["LoadButColor"] = LB;

                if (!IsUpdateFromText)
                    this.Resources["TextLoadButColor"] = LB;

                if (Math.Abs(ThemeKey-ThemeRatio)<0.01)
                {
                    ThemeKey = ThemeRatio;
                    ThemeChange.Stop();
                }
            };
            Dispatcher.BeginInvoke(delg);
        }

        //preview the pieces
        void buttonMEnter(int x,int y)
        {
            if (game.board[x, y] == -1)
            {
                bts[x, y].Opacity = 1.0;
                previewButt = bts[x, y];
            }
        }

        void buttonMLeave(int x, int y)
        {
            if (game.board[x, y] == -1)
            {
                bts[x, y].Opacity = 0.0;
                previewButt = null;
            }
        }

        //click event for pieces on the board
        void buttonClicked(int x, int y)
        { 
            if(stepOffset!=0)
            {
                AnnounceText("現在的畫面不是最新結果。");
                cmd.Text = "";
            }
            else if (game.board[x, y] == -1)
            {
                char cx = Convert.ToChar(x+65), cy = Convert.ToChar(y + 65);

                if (WhichWebsite(threadUrl) == "GO" || IsUpdateFromText)
                {
                    cmd.Text = floorBox.Text + " -" + cx + cy + "-";
                    if(floorBox.Text.Length==0)
                    {
                        AnnounceText("小提醒!在上方空欄輸入，可以自動在指令加上名字喔!");
                    }
                }
                else
                    cmd.Text = "-" + cx + cy + "-";
            }
        }

        void AnnounceText(String str)
        {
            announce.Opacity = 0.65;
            announce.Text = str;
        }

        private void NameboardA_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragName = "";

            if (GlobalStepList != null && GlobalStepList.Count >= 1)
                DragName = GlobalStepList[0].name;
        }

        private void NameboardB_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragName = "";

            if (GlobalStepList != null && GlobalStepList.Count >= 2)
                DragName = GlobalStepList[1].name;
        }

        private void FloorBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DragName.Length > 0)
            {
                floorBox.Text = DragName;
                DragName = "";
            }
        }

        //offset adjusting
        private void ButR_Click(object sender, RoutedEventArgs e)
        {
            SetOffset(1);
        }

        private void ButL_Click(object sender, RoutedEventArgs e)
        {
            SetOffset(-1);
        }

        //mouse wheel control offset
        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int n = e.Delta / Math.Abs(e.Delta);
            SetOffset(n);
        }

        //load the steps from text in admi textbox
        private void admiBut_Click(object sender, RoutedEventArgs e)
        {
            if (!IsLoading)
            {
                IsUpdateFromText = !IsUpdateFromText;

                if (IsUpdateFromText)
                    this.Resources["TextLoadButColor"] = new SolidColorBrush(Colors.Red);
                else
                    this.Resources["TextLoadButColor"] = FindResource("LightColor");
            }
            else
                AnnounceText("請關閉自動讀取。");
        }

        //for dragging
        private void Cumaster_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /*if(e.ChangedButton==MouseButton.Left)
                this.DragMove();*/
        }

        void SetOffset(int n)
        {
            if (GlobalStepList != null)
            {
                stepOffset += (stepOffset + n <= 0 && stepOffset + n + GlobalStepList.Count >= 0) ? n : 0;
                game.Update(GlobalStepList, previewButt, stepOffset);

                cmd.Text = "";
                if (stepOffset == 0)
                    AnnounceText("");
                else
                    AnnounceText((GlobalStepList.Count+stepOffset) + "步 (前" + (-stepOffset) + ")");
            }
        }
    }
}

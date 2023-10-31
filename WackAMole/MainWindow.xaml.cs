using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WackAMole.Plane;
using WackAMole.Game;
using WackAMole.Logging;

namespace WackAMole;

[ValueConversion(typeof(object), typeof(int))]
public class ObjectPresenceToBorderWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((string)value)?.Length > 0 ? 2 : 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public bool IsGazeController => (string)cmbController.SelectedItem == ControllerType.Gaze.ToString();
    public bool IsGoNoGo => chkGoNoGo.IsChecked ?? false;
    public bool IsLowPassFilterEnabled => chkLowPassFilter.IsChecked ?? false;
    public bool UseGazeQuality => chkUseGazeQualityMeasurement.IsChecked ?? false;

    public event PropertyChangedEventHandler? PropertyChanged;

    const string GAME_OPTIONS_FILENAME = "wack_a_mole_options.json";
    const string SE_CLIENT_OPTIONS_FILENAME = "se_client_options.json";

#if USE_TCP
    readonly SEClient.Tcp.Client _tcpClient = new();
#else
    readonly SEClient.Cmd.DataSource _dataSource = new ();
    readonly SEClient.Cmd.Parser _parser;
#endif

    readonly Game.Game _game;
    readonly GameRenderer _gameRenderer;
    readonly MouseController _mouseController;
    readonly GazeController _gazeController;
    readonly PlaneCollection _planes;
    readonly PlaneRenderer _planeRenderer;

    readonly object _allContent;

    CancellationTokenSource? _gameTestCancellation;

#if USE_TCP
    string _currentIntersectionName = "";
#endif

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;

        var options = GameOptions.Load(GAME_OPTIONS_FILENAME);
        stpGame.Tag = options.ScreenName;

        _allContent = Content;

        SEClient.Options.Load(SE_CLIENT_OPTIONS_FILENAME);

#if USE_TCP
        _tcpClient.Disconnected += DataSource_Closed;
        _tcpClient.Sample += TcpClient_Sample;
#else
        _dataSource.Data += DataSource_Data;
        _dataSource.Closed += DataSource_Closed;

        _parser = new ();
        _parser.PlaneEnter += Parser_PlaneEnter;
        _parser.PlaneExit += Parser_PlaneExit;
        _parser.Sample += Parser_Sample;
#endif

        ScreenLogger.Initialize(txbOutput, wrpScreenLogger);

        _gameRenderer = new GameRenderer(grdGame, lblScore);
        _game = new Game.Game(_gameRenderer);

        var gazeCorrector = new GazeCorrector();
        _game.MoleVisibilityChanged += gazeCorrector.MoleVisibilityChangeHandler;

        _gazeController = new GazeController(_game, options.ScreenName, gazeCorrector);
        _mouseController = new MouseController(_game, grdGame, gazeCorrector)
        {
            IsEnabled = false
        };

        _planes = new (
            _gazeController,
            new Mirror("Left"),
            new Mirror("Right")
        );

        _planeRenderer = new (
            stpLeftMirror,
            stpRightMirror,
            stpGame,
            stpWindshield
        );

        KeyDown += MainWindow_KeyDown;
        Closing += MainWindow_Closing;

        var settings = Properties.Settings.Default;
        txbHost.Text = settings.Host;
        txbPort.Text = settings.Port;

        BindUIControls();

        options.Changed += Options_Changed;
        Options_Changed(options, GameOptions.Option.Controller);
    }

    private void BindUIControls()
    {
        var gameOptions = GameOptions.Instance;
        var seClientOptions = SEClient.Options.Instance;

        Utils.UIHelper.InitComboBox(cmbController, gameOptions.Controller, (value) =>
        {
            gameOptions.Controller = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGazeController)));
        });
        Utils.UIHelper.InitComboBox(cmbSource, seClientOptions.IntersectionSource, (value) =>
        {
            seClientOptions.IntersectionSource = value;
        });
        Utils.UIHelper.InitCheckBox(chkSourceFiltered, seClientOptions.IntersectionSourceFiltered, (value) =>
        {
            seClientOptions.IntersectionSourceFiltered = value;
        });
        Utils.UIHelper.InitCheckBox(chkGoNoGo, gameOptions.GoNoGo, (value) =>
        {
            gameOptions.GoNoGo = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGoNoGo)));
        });
        Utils.UIHelper.InitTextBox(txbDwellTime, gameOptions.DwellTime, (value) =>
        {
            gameOptions.DwellTime = value;
        });
        Utils.UIHelper.InitCheckBox(chkLowPassFilter, gameOptions.LowPassFilterEnabled, (value) =>
        {
            gameOptions.LowPassFilterEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLowPassFilterEnabled)));
        });
        Utils.UIHelper.InitTextBox(txbLowPassFilterGain, gameOptions.LowPassFilterGain, (value) =>
        {
            gameOptions.LowPassFilterGain = value;
        });
        Utils.UIHelper.InitTextBox(txbLowPassFilterResetDelay, gameOptions.LowPassFilterResetDelay, (value) =>
        {
            gameOptions.LowPassFilterResetDelay = value;
        });
        Utils.UIHelper.InitTextBox(txbFocusedCellExpansion, gameOptions.FocusedCellExpansion, (value) =>
        {
            gameOptions.FocusedCellExpansion = value;
        });
        Utils.UIHelper.InitTextBox(txbMoleTimerInterval, gameOptions.MoleTimerInterval, (value) =>
        {
            gameOptions.MoleTimerInterval = value;
        });
        Utils.UIHelper.InitTextBox(txbMoleEventRate, gameOptions.MoleEventRate, (value) =>
        {
            gameOptions.MoleEventRate = value;
        });
        Utils.UIHelper.InitTextBox(txbLPFWeightDamping, gameOptions.LowPassFilterWeightDamping, (value) =>
        {
            gameOptions.LowPassFilterWeightDamping = value;
        });
        Utils.UIHelper.InitTextBox(txbFocusLatency, gameOptions.FocusLatency, (value) =>
        {
            gameOptions.FocusLatency = value;
        });
        Utils.UIHelper.InitTextBox(txbNoGoProbability, gameOptions.NoGoProbability, (value) =>
        {
            gameOptions.NoGoProbability = value;
        });
        Utils.UIHelper.InitTextBox(txbShotDuration, gameOptions.ShotDuration, (value) =>
        {
            gameOptions.ShotDuration = value;
        });
        Utils.UIHelper.InitCheckBox(chkUseGazeQualityMeasurement, seClientOptions.UseGazeQualityMeasurement, (value) =>
        {
            seClientOptions.UseGazeQualityMeasurement = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseGazeQuality)));
        });
        Utils.UIHelper.InitTextBox(txbGazeQualityThreshold, seClientOptions.GazeQualityThreshold, (value) =>
        {
            seClientOptions.GazeQualityThreshold = value;
        });
        Utils.UIHelper.InitCheckBox(chkUseSmartGazeCorrection, gameOptions.UseSmartGazeCorrection, (value) =>
        {
            gameOptions.UseSmartGazeCorrection = value;
        });
        Utils.UIHelper.InitCheckBox(chkShowGazeCursor, gameOptions.ShowGazeCursor, (value) =>
        {
            gameOptions.ShowGazeCursor = value;
        });
    }

    private static void SaveLoggedData()
    {
        var logger = FlowLogger.Instance;
        if (logger.HasRecords)
        {
            logger.IsEnabled = false;
            logger.SaveTo($"wack-a-mole_{DateTime.Now:u}.txt".ToPath());
        }
    }

    // Handlers

    private void Options_Changed(object? sender, GameOptions.Option e)
    {
        if (sender != null && e == GameOptions.Option.Controller)
        {
            var options = (GameOptions)sender;
            _gazeController.IsEnabled = options.Controller == ControllerType.Gaze;
            _mouseController.IsEnabled = options.Controller == ControllerType.Mouse;
            _game.ClearFocus();
        }
    }

    private void DataSource_Closed(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            btnStartStop.Content = "Start";
            btnStartStop.IsEnabled = true;
            stpSettings.IsEnabled = true;

            SaveLoggedData();
        });
    }

#if USE_TCP
    private void TcpClient_Sample(object? sender, SEClient.Tcp.Data.Sample sample)
    {
        Dispatcher.Invoke(() =>
        {
            var seClientOptions = SEClient.Options.Instance;
            var intersectionSource = (seClientOptions.IntersectionSource, seClientOptions.IntersectionSourceFiltered) switch
            {
                (SEClient.IntersectionSource.Gaze, false) => sample.ClosestWorldIntersection,
                (SEClient.IntersectionSource.Gaze, true) => sample.FilteredClosestWorldIntersection,
                (SEClient.IntersectionSource.AI, false) => sample.EstimatedClosestWorldIntersection,
                (SEClient.IntersectionSource.AI, true) => sample.FilteredEstimatedClosestWorldIntersection,
                _ => throw new Exception($"This intersection source is not implemented")
            };

            if (intersectionSource is SEClient.Tcp.WorldIntersection intersection)
            {
                var intersectionName = intersection.ObjectName.AsString;
                if (_currentIntersectionName != intersectionName)
                {
                    _currentIntersectionName = intersectionName;

                    lblPlane.Content = intersectionName;
                    _planeRenderer.Enter(intersectionName);
                    _planes.Notify(Plane.Plane.Event.Enter, intersectionName);
                }

                _planes.Feed(new System.Collections.Generic.List<Intersection>()
                    { new Intersection() {
                        PlaneName = intersection.ObjectName.AsString,
                        Gaze = intersection.WorldPoint,
                        Point = new SEClient.Tcp.Point2D() {
                            X = intersection.ObjectPoint.X,
                            Y = intersection.ObjectPoint.Y,
                        }
                    } 
                });
            }
            else if (!string.IsNullOrEmpty(_currentIntersectionName))
            {
                lblPlane.Content = "";
                _planeRenderer.Exit(_currentIntersectionName);
                _planes.Notify(Plane.Plane.Event.Exit, _currentIntersectionName);

                _currentIntersectionName = "";
            }

            lblFrameID.Content = sample.TimeStamp ?? 0;
        });
    }
#else
    private void DataSource_Data(object? sender, string e)
    {
        _parser.Feed(e);
    }

    private void Parser_PlaneEnter(object? _, SEClient.Cmd.Intersection e)
    {
        Dispatcher.Invoke(() =>
        {
            lblPlane.Content = e.PlaneName;
            _planeRenderer.Enter(e.PlaneName);
            _planes.Notify(Plane.Plane.Event.Enter, e.PlaneName);
        });
    }

    private void Parser_PlaneExit(object? _, string e)
    {
        Dispatcher.Invoke(() =>
        {
            lblPlane.Content = "";
            _planeRenderer.Exit(e);
            _planes.Notify(Plane.Plane.Event.Exit, e);
        });
    }

    private void Parser_Sample(object? _, SEClient.Cmd.Sample e)
    {
        Dispatcher.Invoke(() =>
        {
            //lblPlane.Content = string.Join(", ", e.Intersections.Select(intersection => intersection.PlaneName));
            lblFrameID.Content = e.ID;

            _planes.Feed(e.Intersections);
        });
    }
#endif

    // UI handlers

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        var settings = Properties.Settings.Default;
        settings.Host = txbHost.Text;
        settings.Port = txbPort.Text;
        settings.Save();

        GameOptions.Save(GAME_OPTIONS_FILENAME);
#if !USE_TCP
        SEClient.Cmd.Options.Save(SE_CLIENT_OPTIONS_FILENAME);
#endif
    }

    private async Task RunTest(Func<Task> action)
    {
        stpSettings.IsEnabled = false;
        btnStartStop.IsEnabled = false;

        var task = action();
        await task;

        stpSettings.IsEnabled = true;
        btnStartStop.IsEnabled = true;
    }

    private async void MainWindow_KeyDown(object _, KeyEventArgs e)
    {
        if (e.Key == Key.F5)    // Test DataSource
        {
#if !USE_TCP
            Tests.Setup.IsDebugging = true;
            lblDebug.Visibility = Tests.Setup.IsDebugging ? Visibility.Visible : Visibility.Collapsed;
            StartStop_Click(null, new RoutedEventArgs());
#endif
        }
        else if (e.Key == Key.F6)   // Test Parser
        {
#if !USE_TCP
            await RunTest(async () =>
            {
                _parser.Reset();
                _game.Start();
                await Tests.Parser.Run(_parser, Keyboard.IsKeyDown(Key.LeftShift));
                _game.Stop();
            });
#endif
        }
        else if (e.Key == Key.F7)   // Test game with a mouse/touch
        {
            await RunTest(async () =>
            {
                if (_gameTestCancellation == null)
                {
                    _game.Start();
                    _gameTestCancellation = new CancellationTokenSource();

                    try { await Task.Delay(-1, _gameTestCancellation.Token); }
                    catch (Exception) { }
                    finally { _game.Stop(); }
                }
                else
                {
                    _gameTestCancellation.Cancel();
                    _gameTestCancellation = null;
                }
            });
        }
        else if (e.Key == Key.F8)   // Test GameController
        {
            await RunTest(async () =>
            {
#if !USE_TCP
                _parser.Reset();
#endif
                _game.Start();
                await Tests.GazeController.Run(_gazeController);
                _game.Stop();
            });
        }
        else if (e.Key == Key.F9)   // Test LowPassFilter
        {
            await RunTest(async () =>
            {
#if !USE_TCP
                _parser.Reset();
#endif
                _game.Start();
                await Tests.LowPassFilter.Run(_gazeController);
                _game.Stop();
            });
        }
    }

    private async void StartStop_Click(object? _, RoutedEventArgs e)
    {
#if USE_TCP
        if (_tcpClient.IsConnected)
#else
        if (_dataSource.IsRunning)
#endif
        {
            btnStartStop.IsEnabled = false;
            btnStartStop.Content = "Closing...";
#if USE_TCP
            await _tcpClient.Stop();
#else
            await _dataSource.Stop();
#endif
            _game.Stop();
        }
        else
        {
            txbOutput.Text = "";
            lblPlane.Content = "";
            lblFrameID.Content = "";
            stpSettings.IsEnabled = false;
            btnStartStop.Content = "Interrupt";

            _planeRenderer.Reset();
#if USE_TCP
            _tcpClient?.Connect(txbHost.Text, int.Parse(txbPort.Text), Tests.Setup.IsDebugging);
#else
            _parser.Reset();
            _dataSource.Start(txbHost.Text, txbPort.Text, Tests.Setup.IsDebugging);
#endif
            _game.Start();
        }
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowStyle == WindowStyle.None)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            Content = _allContent;
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            Content = grdGame;
        }
    }
}

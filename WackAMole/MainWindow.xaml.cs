using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WackAMole.Comm;
using WackAMole.Plane;
using WackAMole.Game;
using WackAMole.Logging;

namespace WackAMole
{
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

        public event PropertyChangedEventHandler PropertyChanged;

        const string OPTIONS_FILENAME = "options.json";

        readonly DataSource _dataSource = new ();
        readonly Parser _parser;

        readonly Game.Game _game;
        readonly GameRenderer _gameRenderer;
        readonly MouseController _mouseController;
        readonly GazeController _gazeController;
        readonly PlaneCollection _planes;
        readonly PlaneRenderer _planeRenderer;

        readonly object _allContent;

        CancellationTokenSource _gameTestCancellation;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            var options = Options.Load(OPTIONS_FILENAME);

            _allContent = Content;

            _dataSource.Data += DataSource_Data;
            _dataSource.Closed += DataSource_Closed;

            _parser = new ();
            _parser.PlaneEnter += Parser_PlaneEnter;
            _parser.PlaneExit += Parser_PlaneExit;
            _parser.Sample += Parser_Sample;

            ScreenLogger.Initialize(txbOutput, wrpScreenLogger);

            _gameRenderer = new GameRenderer(grdGame, lblScore);
            _game = new Game.Game(_gameRenderer);
            _gazeController = new GazeController(_game, options.ScreenName);
            _mouseController = new MouseController(_game, grdGame)
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
            Options_Changed(options, Options.Option.Controller);
        }

        private void BindUIControls()
        {
            var options = Options.Instance;

            Utils.UIHelper.InitComboBox(cmbController, options.Controller, (value) =>
            {
                options.Controller = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGazeController)));
            });
            Utils.UIHelper.InitComboBox(cmbSource, options.IntersectionSource, (value) =>
            {
                options.IntersectionSource = value;
            });
            Utils.UIHelper.InitCheckBox(chkSourceFiltered, options.IntersectionSourceFiltered, (value) =>
            {
                options.IntersectionSourceFiltered = value;
            });
            Utils.UIHelper.InitCheckBox(chkGoNoGo, options.GoNoGo, (value) =>
            {
                options.GoNoGo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsGoNoGo)));
            });
            Utils.UIHelper.InitTextBox(txbDwellTime, options.DwellTime, (value) =>
            {
                options.DwellTime = value;
            });
            Utils.UIHelper.InitCheckBox(chkLowPassFilter, options.LowPassFilterEnabled, (value) =>
            {
                options.LowPassFilterEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLowPassFilterEnabled)));
            });
            Utils.UIHelper.InitTextBox(txbLowPassFilterGain, options.LowPassFilterGain, (value) =>
            {
                options.LowPassFilterGain = value;
            });
            Utils.UIHelper.InitTextBox(txbLowPassFilterResetDelay, options.LowPassFilterResetDelay, (value) =>
            {
                options.LowPassFilterResetDelay = value;
            });
            Utils.UIHelper.InitTextBox(txbFocusedCellExpansion, options.FocusedCellExpansion, (value) =>
            {
                options.FocusedCellExpansion = value;
            });
            Utils.UIHelper.InitTextBox(txbMoleTimerInterval, options.MoleTimerInterval, (value) =>
            {
                options.MoleTimerInterval = value;
            });
            Utils.UIHelper.InitTextBox(txbMoleEventRate, options.MoleEventRate, (value) =>
            {
                options.MoleEventRate = value;
            });
            Utils.UIHelper.InitTextBox(txbLPFWeightDamping, options.LowPassFilterWeightDamping, (value) =>
            {
                options.LowPassFilterWeightDamping = value;
            });
            Utils.UIHelper.InitTextBox(txbFocusLatency, options.FocusLatency, (value) =>
            {
                options.FocusLatency = value;
            });
            Utils.UIHelper.InitTextBox(txbNoGoProbability, options.NoGoProbability, (value) =>
            {
                options.NoGoProbability = value;
            });
            Utils.UIHelper.InitTextBox(txbShotDuration, options.ShotDuration, (value) =>
            {
                options.ShotDuration = value;
            });
            Utils.UIHelper.InitCheckBox(chkUseGazeQualityMeasurement, options.UseGazeQualityMeasurement, (value) =>
            {
                options.UseGazeQualityMeasurement = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseGazeQuality)));
            });
            Utils.UIHelper.InitTextBox(txbGazeQualityThreshold, options.GazeQualityThreshold, (value) =>
            {
                options.GazeQualityThreshold = value;
            });
        }

        private void SaveLoggedData()
        {
            var logger = FlowLogger.Instance;
            if (logger.HasRecords)
            {
                logger.IsEnabled = false;
                logger.SaveTo($"wack-a-mole_{DateTime.Now:u}.txt".ToPath());
            }
        }

        // Handlers

        private void Options_Changed(object sender, Options.Option e)
        {
            if (e == Options.Option.Controller)
            {
                var options = (Options)sender;
                _gazeController.IsEnabled = options.Controller == ControllerType.Gaze;
                _mouseController.IsEnabled = options.Controller == ControllerType.Mouse;
                _game.ClearFocus();
            }
        }

        private void DataSource_Closed(object _, EventArgs e)
        {
            btnStartStop.Content = "Start";
            btnStartStop.IsEnabled = true;
            stpSettings.IsEnabled = true;

            SaveLoggedData();
        }

        private void DataSource_Data(object _, string e)
        {
            _parser.Feed(e);
        }

        private void Parser_PlaneEnter(object _, Intersection e)
        {
            Dispatcher.Invoke(() =>
            {
                lblPlane.Content = e.PlaneName;
                _planeRenderer.Enter(e.PlaneName);
                _planes.Notify(Plane.Plane.Event.Enter, e.PlaneName);
            });
        }

        private void Parser_PlaneExit(object _, string e)
        {
            Dispatcher.Invoke(() =>
            {
                lblPlane.Content = "";
                _planeRenderer.Exit(e);
                _planes.Notify(Plane.Plane.Event.Exit, e);
            });
        }

        private void Parser_Sample(object _, Sample e)
        {
            Dispatcher.Invoke(() =>
            {
                //lblPlane.Content = string.Join(", ", e.Intersections.Select(intersection => intersection.PlaneName));
                lblFrameID.Content = e.ID;

                _planes.Feed(ref e);
            });
        }

        // UI handlers

        private void MainWindow_Closing(object _, CancelEventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.Host = txbHost.Text;
            settings.Port = txbPort.Text;
            settings.Save();

            Options.Save(OPTIONS_FILENAME);
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
                Tests.Setup.IsDebugging = true;
                lblDebug.Visibility = Tests.Setup.IsDebugging ? Visibility.Visible : Visibility.Collapsed;
                StartStop_Click(null, null);
            }
            else if (e.Key == Key.F6)   // Test Parser
            {
                await RunTest(async () =>
                {
                    _parser.Reset();
                    _game.Start();
                    await Tests.Parser.Run(_parser, Keyboard.IsKeyDown(Key.LeftShift));
                    _game.Stop();
                });
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
                    _parser.Reset();
                    _game.Start();
                    await Tests.GazeController.Run(_gazeController);
                    _game.Stop();
                });
            }
            else if (e.Key == Key.F9)   // Test LowPassFilter
            {
                await RunTest(async () =>
                {
                    _parser.Reset();
                    _game.Start();
                    await Tests.LowPassFilter.Run(_gazeController);
                    _game.Stop();
                });
            }
            else if (e.Key == Key.F11)
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

        private async void StartStop_Click(object _, RoutedEventArgs e)
        {
            if (_dataSource.IsRunning)
            {
                btnStartStop.IsEnabled = false;
                btnStartStop.Content = "Closing...";

                await _dataSource.Stop();

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
                _parser.Reset();
                _dataSource.Start(txbHost.Text, txbPort.Text, Tests.Setup.IsDebugging);
                _game.Start();
            }
        }
    }
}

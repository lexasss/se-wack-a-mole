using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using SEReader.Comm;
using SEReader.Experiment;
using SEReader.Logging;

namespace SEReader
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

    public partial class MainWindow : Window
    {
        readonly DataSource _dataSource = new DataSource();
        readonly Parser _parser = new Parser();

        readonly Game.Game _game;
        readonly Game.GameRenderer _gameRenderer;
        readonly Observer _gameController;
        readonly Observer _leftMirror = new Mirror("LeftScreen");

        public MainWindow()
        {
            InitializeComponent();

            _dataSource.Data += DataSource_Data;
            _dataSource.Closed += DataSource_Closed;

            _parser.PlaneEnter += Parser_PlaneEnter;
            _parser.PlaneExit += Parser_PlaneExit;
            _parser.Sample += Parser_Frame;

            _game = new Game.Game(6, 4);
            _gameController = new Game.GameController("Windshield", 1.0, 1.0, _game);
            _gameRenderer = new Game.GameRenderer(_game, grdGame, lblScore);

            KeyDown += MainWindow_KeyDown;
            Closing += MainWindow_Closing;

            var settings = Properties.Settings.Default;
            txbHost.Text = settings.Host;
            txbPort.Text = settings.Port;
        }

        private void SaveLoggedData()
        {
            var logger = FlowLogger.Instance;
            if (logger.HasRecords)
            {
                logger.IsEnabled = false;
                logger.SaveTo($"sereader_{DateTime.Now:u}.txt".ToPath());
            }
        }

        // Handlers

        private void DataSource_Closed(object sender, EventArgs e)
        {
            btnStartStop.Content = "Start";
            btnStartStop.IsEnabled = true;
            stpSettings.IsEnabled = true;

            SaveLoggedData();
        }

        private void DataSource_Data(object sender, string e)
        {
            if (Tests.Setup.IsDebugging)
            {
                txbOutput.Text += "\n" + e;
            }

            _parser.Feed(e);

            txbOutput.ScrollToEnd();
        }

        private void Parser_Frame(object sender, Sample e)
        {
            lblPlane.Content = string.Join( ", ", e.Intersections.Select(intersection => intersection.PlaneName));
            lblFrameID.Content = e.ID;
            //txbOutput.Text += $"\nFrame: {e.TimeStamp}";

            _leftMirror.Feed(ref e);
            _gameController.Feed(ref e);
        }

        private void Parser_PlaneExit(object sender, string e)
        {
            txbOutput.Text += $"\nExited: {e}";
            lblPlane.Content = "";

            if (_gameController.PlaneName == e)
            {
                _gameController.Notify(Observer.Event.PlaneExit);
            }
        }

        private void Parser_PlaneEnter(object sender, Intersection e)
        {
            txbOutput.Text += $"\nEntered: {e.PlaneName}";
            lblPlane.Content = e.PlaneName;

            if (_gameController.PlaneName == e.PlaneName)
            {
                _gameController.Notify(Observer.Event.PlaneEnter);
            }
        }

        // UI handlers

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var settings = Properties.Settings.Default;
            settings.Host = txbHost.Text;
            settings.Port = txbPort.Text;
            settings.Save();
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (!stpSettings.IsEnabled)
            {
                return;
            }

            if (e.Key == Key.F5)    // Test DataSource
            {
                Tests.Setup.IsDebugging = true;
                lblDebug.Visibility = Tests.Setup.IsDebugging ? Visibility.Visible : Visibility.Collapsed;
                StartStop_Click(null, null);
            }
            else if (e.Key == Key.F6)   // Test Parser
            {
                stpSettings.IsEnabled = false;
                btnStartStop.IsEnabled = false;

                _game.Start();
                await Tests.Parser.Run(_parser);
                _game.Stop();

                stpSettings.IsEnabled = true;
                btnStartStop.IsEnabled = true;
            }
            else if (e.Key == Key.F7)   // Test game with a mouse/touch
            {
                if (_game.IsRunning)
                    _game.Stop();
                else
                    _game.Start();
            }
            else if (e.Key == Key.F8)
            {
                stpSettings.IsEnabled = false;
                btnStartStop.IsEnabled = false;

                _game.Start();
                await Tests.GameController.Run(_gameController as Game.GameController);
                _game.Stop();

                stpSettings.IsEnabled = true;
                btnStartStop.IsEnabled = true;
            }
        }

        private async void StartStop_Click(object sender, RoutedEventArgs e)
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

                _dataSource.Start(txbHost.Text, txbPort.Text, Tests.Setup.IsDebugging);
                _game.Start();
            }
        }
    }
}

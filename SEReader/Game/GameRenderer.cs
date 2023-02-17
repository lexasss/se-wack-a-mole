using SEReader.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SEReader.Game
{
    public class GameRenderer
    {
        public enum Target
        {
            Mole,
            Shot,
            Focus
        }

        public class CellClickedEventArgs : EventArgs
        {
            public int X { get; }
            public int Y { get; }
            public CellClickedEventArgs(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public GameRenderer(Grid grid, Label score)
        {
            _score = score;

            _dispatcher = Dispatcher.CurrentDispatcher;

            for (int y = 0; y < _options.CellY; ++y)
                grid.RowDefinitions.Add(new RowDefinition());
            for (int x = 0; x < _options.CellX; ++x)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            var imagePath = @"pack://application:,,,/SEReader;component/Assets/images/";
            var holeBitmap = new BitmapImage(new Uri($"{imagePath}hole.png", UriKind.Absolute));
            var aimingBitmap = new BitmapImage(new Uri($"{imagePath}shot.png", UriKind.Absolute));
            var focusBitmap = new BitmapImage(new Uri($"{imagePath}focus.png", UriKind.Absolute));
            var mole1Bitmap = new BitmapImage(new Uri($"{imagePath}mole1.png", UriKind.Absolute));
            var mole2Bitmap = new BitmapImage(new Uri($"{imagePath}mole2.png", UriKind.Absolute));

            for (int y = 0; y < _options.CellY; ++y)
            {
                for (int x = 0; x < _options.CellX; ++x)
                {
                    Image img = new()
                    {
                        Source = holeBitmap,
                        Tag = $"{x},{y}",       // used to decode coords when clicked
                    };

                    Grid.SetRow(img, y);
                    Grid.SetColumn(img, x);

                    grid.Children.Add(img);
                }
            }

            _mole1 = new Image
            {
                Source = mole1Bitmap,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            grid.Children.Add(_mole1);

            _mole2 = new Image
            {
                Source = mole2Bitmap,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            grid.Children.Add(_mole2);

            _shot = new Image
            {
                Source = aimingBitmap,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            grid.Children.Add(_shot);

            _focus = new Image
            {
                Source = focusBitmap,
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false,
            };
            grid.Children.Add(_focus);

            _mole = _mole1;
        }

        public void SetMoleType(MoleType mole)
        {
            _mole = mole switch
            {
                MoleType.Go => _mole1,
                MoleType.NoGo => _mole2,
                _ => throw new Exception($"No such mole '{mole}' to render")
            }; ;
        }

        public void Show(Target target, int x, int y)
        {
            var img = target switch
            {
                Target.Shot => _shot,
                Target.Mole => _mole,
                Target.Focus => _focus,
                _ => throw new Exception($"Unknown target '{target}' to show/hide")
            };

            _dispatcher.Invoke(() =>
            {
                Show(img, x, y);
            });
        }

        public void Hide(Target target)
        {
            var img = target switch
            {
                Target.Shot => _shot,
                Target.Mole => _mole,
                Target.Focus => _focus,
                _ => throw new Exception($"Unknown target '{target}' to show/hide")
            };

            _dispatcher.Invoke(() =>
            {
                Hide(img);
            });
        }

        public void SetScore(int score)
        {
            _dispatcher.Invoke(() =>
            {
                _score.Content = score;
            });
        }

        // Internal

        readonly Label _score;
        readonly Image _mole1;
        readonly Image _mole2;
        readonly Image _shot;
        readonly Image _focus;
        readonly Dispatcher _dispatcher;
        readonly Options _options = Options.Instance;

        Image _mole;

        private void Show(Image image, int x, int y)
        {
            Grid.SetColumn(image, x);
            Grid.SetRow(image, y);
            image.Visibility = Visibility.Visible;
        }

        private void Hide(Image image)
        {
            image.Visibility = Visibility.Collapsed;
        }
    }
}

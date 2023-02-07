using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SEReader.Game
{
    internal class GameRenderer
    {
        public GameRenderer(Game game, Grid grid, Label score)
        {
            _game = game;
            //_grid = grid;
            _score = score;

            _dispatcher = Dispatcher.CurrentDispatcher;

            game.MoleChanged += Game_MoleChanged;
            game.MoleVisibilityChanged += Game_MoleVisibilityChanged;
            game.ShotVisibilityChanged += Game_ShotVisibilityChanged;
            game.FocusVisibilityChanged += Game_FocusVisibilityChanged;
            game.ScoreChanged += Game_ScoreChanged;

            for (int y = 0; y < game.CellCountY; ++y)
                grid.RowDefinitions.Add(new RowDefinition());
            for (int x = 0; x < game.CellCountX; ++x)
                grid.ColumnDefinitions.Add(new ColumnDefinition());

            var imagePath = @"pack://application:,,,/SEReader;component/Assets/images/";
            var holeBitmap = new BitmapImage(new Uri($"{imagePath}hole.png", UriKind.Absolute));
            var aimingBitmap = new BitmapImage(new Uri($"{imagePath}shot.png", UriKind.Absolute));
            var focusBitmap = new BitmapImage(new Uri($"{imagePath}focus.png", UriKind.Absolute));
            var mole1Bitmap = new BitmapImage(new Uri($"{imagePath}mole1.png", UriKind.Absolute));
            var mole2Bitmap = new BitmapImage(new Uri($"{imagePath}mole2.png", UriKind.Absolute));

            for (int y = 0; y < game.CellCountY; ++y)
            {
                for (int x = 0; x < game.CellCountX; ++x)
                {
                    Image img = new Image
                    {
                        Source = holeBitmap,
                        Tag = $"{x},{y}",
                    };
                    img.TouchDown += Cell_Click;
                    img.MouseDown += Cell_Click;

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
            /*
            var st = new ScaleTransform(2, 1);
            _mole.LayoutTransform = st;

            DoubleAnimation scaleAnimation = new DoubleAnimation();
            scaleAnimation.From = 1.0;
            scaleAnimation.To = 2.0;
            scaleAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(500));
            scaleAnimation.AutoReverse = true;

            _storyboard = new Storyboard();
            _storyboard.Children.Add(scaleAnimation);
            Storyboard.SetTarget(scaleAnimation, _mole.LayoutTransform);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
            */
        }

        // Internal

        readonly Game _game;
        //readonly Grid _grid;
        readonly Label _score;
        readonly Image _mole1;
        readonly Image _mole2;
        readonly Image _shot;
        readonly Image _focus;
        readonly Dispatcher _dispatcher;
        //readonly Storyboard _storyboard;

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

        private void Game_MoleVisibilityChanged(object sender, Game.TargetVisibilityChangedArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Visibility == Game.TargetVisibility.Visible)
                {
                    Show(_mole, e.CellX, e.CellY);
                    //_storyboard.Begin(_grid);
                }
                else
                {
                    Hide(_mole);
                }
            });
        }

        private void Game_MoleChanged(object sender, Game.Mole e)
        {
            _mole = e switch
            {
                Game.Mole.Go => _mole1,
                Game.Mole.NoGo => _mole2,
                _ => throw new Exception($"No such mole '{e}' to render")
            }; ;
        }

        private void Game_ShotVisibilityChanged(object sender, Game.TargetVisibilityChangedArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Visibility == Game.TargetVisibility.Visible)
                {
                    Show(_shot, e.CellX, e.CellY);
                }
                else
                {
                    Hide(_shot);
                }
            });
        }

        private void Game_FocusVisibilityChanged(object sender, Game.TargetVisibilityChangedArgs e)
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Visibility == Game.TargetVisibility.Visible)
                {
                    Show(_focus, e.CellX, e.CellY);
                }
                else
                {
                    Hide(_focus);
                }
            });
        }

        private void Game_ScoreChanged(object sender, int e)
        {
            _dispatcher.Invoke(() =>
            {
                _score.Content = e;
            });
        }

        private void Cell_Click(object sender, System.Windows.Input.InputEventArgs e)
        {
            Image img = sender as Image;
            var cellCoords = (img.Tag as string).Split(',').Select(v => int.Parse(v)).ToArray();
            _game.FocusAndShoot(cellCoords[0], cellCoords[1]);
        }
    }
}

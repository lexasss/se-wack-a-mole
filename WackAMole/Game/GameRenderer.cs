using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WackAMole.Game;

/// <summary>
/// Controls the game assets shown on the screen. The renderer instance will be used by a <see cref="Game"/ instance>
/// </summary>
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

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="grid">Game grid. The grid will be filled with "cells"</param>
    /// <param name="score">Label to display the score</param>
    public GameRenderer(Grid grid, Label score)
    {
        _score = score;

        _dispatcher = Dispatcher.CurrentDispatcher;

        for (int y = 0; y < _options.CellY; ++y)
            grid.RowDefinitions.Add(new RowDefinition());
        for (int x = 0; x < _options.CellX; ++x)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        var imagePath = @"pack://application:,,,/WackAMole;component/Assets/images/";
        var holeBitmap = new BitmapImage(new Uri($"{imagePath}hole.png", UriKind.Absolute));
        var aimingBitmap = new BitmapImage(new Uri($"{imagePath}shot.png", UriKind.Absolute));
        var focusBitmap = new BitmapImage(new Uri($"{imagePath}focus.png", UriKind.Absolute));
        var mole1Bitmap = new BitmapImage(new Uri($"{imagePath}mole1.png", UriKind.Absolute));
        var mole2Bitmap = new BitmapImage(new Uri($"{imagePath}mole2.png", UriKind.Absolute));

        for (int y = 0; y < _options.CellY; ++y)
        {
            for (int x = 0; x < _options.CellX; ++x)
            {
                Image img = new ()
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

    /// <summary>
    /// Sets the mole type to appear next
    /// </summary>
    /// <param name="mole">Mole type</param>
    /// <exception cref="Exception">Throws if the mole type is unknown</exception>
    public void SetMoleType(MoleType mole)
    {
        _mole = mole switch
        {
            MoleType.Go => _mole1,
            MoleType.NoGo => _mole2,
            _ => throw new Exception($"No such mole '{mole}' to render")
        }; ;
    }

    /// <summary>
    /// Shows an asset in the specified grid location
    /// </summary>
    /// <param name="target">Accet to show</param>
    /// <param name="x">Location X</param>
    /// <param name="y">Location Y</param>
    /// <exception cref="Exception">Throws if the asset is unknown</exception>
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

    /// <summary>
    /// Hide the asset
    /// </summary>
    /// <param name="target">Asset to hide</param>
    /// <exception cref="Exception">Throws if the asset is unknown</exception>
    public void Hide(Target target)
    {
        var img = target switch
        {
            Target.Shot => _shot,
            Target.Mole => _mole,
            Target.Focus => _focus,
            _ => throw new Exception($"Unknown target '{target}' to show/hide")
        };

        try {
            _dispatcher.Invoke(() =>
            {
                Hide(img);
            });
        } catch (TaskCanceledException) { }
    }

    /// <summary>
    /// Displayes the score
    /// </summary>
    /// <param name="score">Score to display</param>
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
    readonly GameOptions _options = GameOptions.Instance;

    Image _mole;

    private static void Show(Image image, int x, int y)
    {
        Grid.SetColumn(image, x);
        Grid.SetRow(image, y);
        image.Visibility = Visibility.Visible;
    }

    private static void Hide(Image image)
    {
        image.Visibility = Visibility.Collapsed;
    }
}

using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WackAMole.Game;

/// <summary>
/// Game controller using mouse/touch
/// </summary>
internal class MouseController
{
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="game">Game instance</param>
    /// <param name="panel">Panel with cells represented as instances of <see cref="Image"/></param>
    public MouseController(Game game, Panel panel, GazeCorrector gazeCorrector)
    {
        _game = game;
        _gazeCorrector = gazeCorrector;

        foreach (FrameworkElement el in panel.Children)
        {
            if (el is Image)
            {
                el.TouchDown += Cell_Click;
                el.MouseDown += Cell_Click;
                el.MouseMove += Cell_MouseMove;
            }
        }
    }

    // Internal

    readonly Game _game;
    readonly GazeCorrector _gazeCorrector;


    private void Cell_Click(object? sender, System.Windows.Input.InputEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (sender is not FrameworkElement el || el.Tag == null)
        {
            return;
        }

        var cellCoords = (el.Tag as string)?.Split(',').Select(int.Parse).ToArray();
        if (cellCoords != null)
        {
            var x = cellCoords[0];
            var y = cellCoords[1];
            _game.Shoot(x, y);
        }
    }

    private void Cell_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        if (sender is not FrameworkElement el || el.Tag == null)
        {
            _game.ClearFocus();
            return;
        }

        // Simulate gaze correction
        var parent = (FrameworkElement)el.Parent;
        var pt = e.GetPosition(parent);
        var point = _gazeCorrector.Feed(new SEClient.Tcp.Point2D() { X = pt.X, Y = pt.Y }, parent.ActualWidth, parent.ActualHeight);
        _game.SetGaze(point.X, point.Y);
        // -- end

        var cellCoords = (el.Tag as string)?.Split(',').Select(int.Parse).ToArray();
        if (cellCoords != null)
        {
            var x = cellCoords[0];
            var y = cellCoords[1];
            _game.Focus(x, y);
        }
    }
}

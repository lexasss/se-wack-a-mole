using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SEReader.Game
{
    /// <summary>
    /// Game controller using mouse/touch
    /// </summary>
    public class MouseController
    {
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="panel">Panel with cells represented as instances of <see cref="Image"/></param>
        public MouseController(Game game, Panel panel)
        {
            _game = game;

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


        private void Cell_Click(object sender, System.Windows.Input.InputEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            var el = sender as FrameworkElement;
            if (el == null || el.Tag == null)
            {
                return;
            }

            var cellCoords = (el.Tag as string).Split(',').Select(v => int.Parse(v)).ToArray();
            var x = cellCoords[0];
            var y = cellCoords[1];
            _game.Shoot(x, y);
        }

        private void Cell_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            var el = sender as FrameworkElement;
            if (el == null || el.Tag == null)
            {
                _game.ClearFocus();
                return;
            }

            var cellCoords = (el.Tag as string).Split(',').Select(v => int.Parse(v)).ToArray();
            var x = cellCoords[0];
            var y = cellCoords[1];
            _game.Focus(x, y);
        }
    }
}

using SEReader.Comm;

namespace SEReader.Game
{
    public class GameController : Experiment.Observer
    {
        public GameController(string screenName, double screenWidth, double screenHeight, Game game) : base(screenName)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _game = game;
        }

        // Internal

        readonly double _screenWidth;
        readonly double _screenHeight;
        readonly Game _game;

        protected override void HandleIntersection(Intersection intersection)
        {
            // TODO: handle it properly here,

            int indexX = (int)(intersection.Point.X / _screenWidth * _game.CellCountX);
            int indexY = (int)(intersection.Point.Y / _screenHeight * _game.CellCountY);

            _game.Focus(indexX, indexY);
        }

        protected override void HandleEvent(Event evt)
        {
            if (evt == Event.PlaneExit)
            {
                _game.Clear();
            }
        }
    }
}

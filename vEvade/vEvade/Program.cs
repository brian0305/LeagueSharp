namespace vEvade
{
    #region

    using LeagueSharp.Common;

    using vEvade.Core;

    #endregion

    public class Program
    {
        #region Methods

        private static void Main()
        {
            CustomEvents.Game.OnGameLoad += Evade.OnGameLoad;
        }

        #endregion
    }
}
namespace vEvade
{
    #region

    using LeagueSharp.Common;

    using vEvade.Core;
    using vEvade.Helpers;

    #endregion

    public class Program
    {
        #region Methods

        private static void Main()
        {
            Configs.Debug = false;
            CustomEvents.Game.OnGameLoad += Evade.OnGameLoad;
        }

        #endregion
    }
}
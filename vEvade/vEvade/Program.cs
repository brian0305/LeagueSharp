namespace vEvade
{
    #region

    using LeagueSharp.Common;

    using vEvade.Core;

    #endregion

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Evade.OnGameLoad;
        }

        #endregion
    }
}
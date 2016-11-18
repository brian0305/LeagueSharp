namespace vEvade.PathFinding
{
    #region

    using System.Collections.Generic;

    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Core;
    using vEvade.Helpers;

    #endregion

    public static class PathFollow
    {
        #region Static Fields

        private static List<Vector2> paths = new List<Vector2>();

        #endregion

        #region Public Properties

        public static bool IsFollowing => paths.Count > 0;

        #endregion

        #region Public Methods and Operators

        public static void KeepFollowPath()
        {
            if (paths.Count == 0)
            {
                return;
            }

            while (paths.Count > 0 && Evade.PlayerPosition.Distance(paths[0]) < 80)
            {
                paths.RemoveAt(0);
            }

            if (paths.Count > 0)
            {
                paths[0].Move();
            }
        }

        public static void Start(List<Vector2> path)
        {
            paths = path;
        }

        public static void Stop()
        {
            paths = new List<Vector2>();
        }

        #endregion
    }
}
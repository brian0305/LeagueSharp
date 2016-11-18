namespace vEvade.Managers
{
    #region

    using System;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Helpers;

    #endregion

    public static class HiuManager
    {
        #region Public Methods and Operators

        public static Vector2 GetLastHiuOrientation()
        {
            var objs = ObjManager.ObjCache.Values.Where(i => i.Name == "Hiu").OrderByDescending(i => i.Time);

            return objs.Count() >= 2
                       ? (objs.ElementAt(1).Obj.Position - objs.First().Obj.Position).To2D().Normalized()
                       : Vector2.Zero;
        }

        public static void OnCreate(GameObject sender, EventArgs args)
        {
            var minion = sender as Obj_AI_Minion;

            if (minion == null || !minion.IsValid || (!Configs.Debug && !minion.IsEnemy)
                || !minion.CharData.BaseSkinName.Contains("TestCube")
                || ObjManager.ObjCache.ContainsKey(minion.NetworkId))
            {
                return;
            }

            ObjManager.ObjCache.Add(minion.NetworkId, new ObjManagerInfo(minion, "Hiu"));
            Utility.DelayAction.Add(250, () => ObjManager.ObjCache.Remove(minion.NetworkId));
        }

        #endregion
    }
}
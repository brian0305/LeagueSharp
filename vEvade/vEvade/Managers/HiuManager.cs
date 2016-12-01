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

        public static Vector2 GetHiuDirection(int time, Vector2 pos)
        {
            var hius =
                ObjManager.ObjCache.Values.Where(
                    i =>
                    i.Name == "Hiu" && time - i.Time >= 0 && time - i.Time < 20 && pos.Distance(i.Obj.Position) < 750)
                    .OrderByDescending(i => i.Time);

            return hius.Count() >= 2
                       ? (hius.ElementAt(1).Obj.Position - hius.First().Obj.Position).To2D().Normalized()
                       : Vector2.Zero;
        }

        public static void OnCreate(GameObject sender, EventArgs args)
        {
            var hiu = sender as Obj_AI_Minion;

            if (hiu == null || !hiu.IsValid || (!Configs.Debug && !hiu.IsEnemy)
                || !hiu.CharData.BaseSkinName.Contains("TestCube") || ObjManager.ObjCache.ContainsKey(hiu.NetworkId))
            {
                return;
            }

            ObjManager.ObjCache.Add(hiu.NetworkId, new ObjManagerInfo(hiu, "Hiu"));
            Utility.DelayAction.Add(250, () => ObjManager.ObjCache.Remove(hiu.NetworkId));
        }

        #endregion
    }
}
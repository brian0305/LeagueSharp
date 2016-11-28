namespace vEvade.Core
{
    #region

    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.EvadeSpells;
    using vEvade.Helpers;
    using vEvade.Managers;

    #endregion

    public static class Evader
    {
        #region Public Methods and Operators

        public static List<Vector2> GetEvadePoints(
            int speed = -1,
            int delay = 0,
            bool isBlink = false,
            bool onlyGood = false)
        {
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var goods = new List<Vector2>();
            var bads = new List<Vector2>();
            var myPos = Evade.PlayerPosition;
            var polygons = new List<Geometry.Polygon>();
            var closestPath = false;

            foreach (var spell in Evade.DetectedSpells.Values.Where(i => i.Enable))
            {
                if (spell.Data.TakeClosestPath && spell.IsDanger(myPos))
                {
                    closestPath = true;
                }

                polygons.Add(spell.EvadePolygon);
            }

            var dangerPolygons = Geometry.ClipPolygons(polygons).ToPolygons();

            foreach (var poly in dangerPolygons)
            {
                for (var i = 0; i <= poly.Points.Count - 1; i++)
                {
                    var start = poly.Points[i];
                    var end = poly.Points[i == poly.Points.Count - 1 ? 0 : i + 1];
                    var segment = myPos.ProjectOn(start, end).SegmentPoint;
                    var dist = segment.Distance(myPos);

                    if (dist >= 600)
                    {
                        continue;
                    }

                    var count = dist < 200 && end.Distance(start) > 90 ? Configs.EvadePointsCount : 0;
                    var step = Configs.EvadePointsStep * (end - start).Normalized();

                    for (var j = -count; j <= count; j++)
                    {
                        var pos = segment + j * step;
                        var paths = ObjectManager.Player.GetPath(pos.To3D()).Select(a => a.To2D()).ToList();

                        if (!isBlink)
                        {
                            if (Evade.IsSafePath(paths, Configs.EvadingFirstTime, speed, delay).IsSafe)
                            {
                                goods.Add(pos);
                            }

                            if (Evade.IsSafePath(paths, Configs.EvadingSecondTime, speed, delay).IsSafe && j == 0)
                            {
                                bads.Add(pos);
                            }
                        }
                        else
                        {
                            if (Evade.IsSafeToBlink(paths[paths.Count - 1], Configs.EvadingFirstTime, delay))
                            {
                                goods.Add(pos);
                            }

                            if (Evade.IsSafeToBlink(paths[paths.Count - 1], Configs.EvadingSecondTime, delay))
                            {
                                bads.Add(pos);
                            }
                        }
                    }
                }
            }

            if (closestPath)
            {
                if (goods.Count > 0)
                {
                    goods = new List<Vector2> { goods.OrderBy(i => myPos.Distance(i)).First() };
                }

                if (bads.Count > 0)
                {
                    bads = new List<Vector2> { bads.OrderBy(i => myPos.Distance(i)).First() };
                }
            }

            return goods.Count > 0 ? goods : (onlyGood ? new List<Vector2>() : bads);
        }

        public static List<Obj_AI_Base> GetEvadeTargets(
            SpellValidTargets[] validTargets,
            int speed,
            int delay,
            float range,
            bool isBlink = false,
            bool onlyGood = false,
            bool dontCheckForSafety = false)
        {
            var bads = new List<Obj_AI_Base>();
            var goods = new List<Obj_AI_Base>();
            var alls = new List<Obj_AI_Base>();
            var myPos = Evade.PlayerPosition.To3D();

            foreach (var type in validTargets)
            {
                switch (type)
                {
                    case SpellValidTargets.AllyChampions:
                        alls.AddRange(HeroManager.Allies.Where(i => !i.IsMe && i.IsValidTarget(range, false, myPos)));
                        break;
                    case SpellValidTargets.AllyMinions:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i => i.IsValidTarget(range, false, myPos) && i.IsAlly && (i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.AllyWards:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, false, myPos) && i.IsAlly && i.IsWard()));
                        break;
                    case SpellValidTargets.EnemyChampions:
                        alls.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(range, true, myPos)));
                        break;
                    case SpellValidTargets.EnemyMinions:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(
                                    i =>
                                    i.IsValidTarget(range, true, myPos) && (i.IsJungle() || i.IsMinion() || i.IsPet())));
                        break;
                    case SpellValidTargets.EnemyWards:
                        alls.AddRange(
                            ObjectManager.Get<Obj_AI_Minion>()
                                .Where(i => i.IsValidTarget(range, true, myPos) && i.IsWard()));
                        break;
                }
            }

            foreach (var target in alls)
            {
                var pos = target.ServerPosition.To2D();

                if (!dontCheckForSafety && !Evade.IsSafePoint(pos).IsSafe)
                {
                    continue;
                }

                if (!isBlink)
                {
                    var paths = new List<Vector2> { myPos.To2D(), pos };

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(paths, Configs.EvadingFirstTime, speed, delay).IsSafe)
                    {
                        goods.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafePath(paths, Configs.EvadingSecondTime, speed, delay).IsSafe)
                    {
                        bads.Add(target);
                    }
                }
                else
                {
                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(pos, Configs.EvadingFirstTime, delay))
                    {
                        goods.Add(target);
                    }

                    if (Utils.GameTimeTickCount - Evade.LastWardJumpTick < 250
                        || Evade.IsSafeToBlink(pos, Configs.EvadingSecondTime, delay))
                    {
                        bads.Add(target);
                    }
                }
            }

            return goods.Count > 0 ? goods : (onlyGood ? new List<Obj_AI_Base>() : bads);
        }

        #endregion
    }
}
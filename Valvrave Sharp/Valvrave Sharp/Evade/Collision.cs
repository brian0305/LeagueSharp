namespace Valvrave_Sharp.Evade
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Polygons;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    #endregion

    internal static class Collision
    {
        #region Static Fields

        private static MissileClient yasuoWallLeft, yasuoWallRight;

        private static RectanglePoly yasuoWallPoly;

        private static int yasuoWallTime;

        #endregion

        #region Methods

        internal static Vector2 GetCollisionPoint(Skillshot skillshot)
        {
            var collisions = new List<DetectedCollision>();
            var from = skillshot.GetMissilePosition(0);
            skillshot.ForceDisabled = false;
            if (skillshot.SpellData.CollisionObjects.HasFlag(CollisionableObjects.Minions))
            {
                var minions = new List<Obj_AI_Minion>();
                minions.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(1200, true, from.ToVector3())));
                minions.AddRange(
                    GameObjects.Minions.Where(
                        i =>
                        i.IsValidTarget(1200, false, from.ToVector3())
                        && (skillshot.Unit.Team == Program.Player.Team
                                ? i.Team != Program.Player.Team
                                : i.Team == Program.Player.Team) && (i.IsMinion() || i.IsPet())));
                collisions.AddRange(
                    from minion in minions
                    let pred =
                        FastPrediction(
                            @from,
                            minion,
                            Math.Max(0, skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick)),
                            skillshot.SpellData.MissileSpeed)
                    let pos = pred.PredictedPos
                    let w =
                        skillshot.SpellData.RawRadius + (!pred.IsMoving ? minion.BoundingRadius - 15 : 0)
                        - pos.Distance(@from, skillshot.End, true)
                    where w > 0
                    select
                        new DetectedCollision
                            {
                                Position =
                                    pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint + skillshot.Direction * 30,
                                Distance = pos.Distance(@from)
                            });
            }
            if (skillshot.SpellData.CollisionObjects.HasFlag(CollisionableObjects.Heroes))
            {
                collisions.AddRange(
                    from hero in GameObjects.AllyHeroes.Where(i => i.IsValidTarget(1200, false) && !i.IsMe)
                    let pred =
                        FastPrediction(
                            @from,
                            hero,
                            Math.Max(0, skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick)),
                            skillshot.SpellData.MissileSpeed)
                    let pos = pred.PredictedPos
                    let w = skillshot.SpellData.RawRadius + 30 - pos.Distance(@from, skillshot.End, true)
                    where w > 0
                    select
                        new DetectedCollision
                            {
                                Position =
                                    pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint + skillshot.Direction * 30,
                                Distance = pos.Distance(@from)
                            });
            }
            if (skillshot.SpellData.CollisionObjects.HasFlag(CollisionableObjects.YasuoWall))
            {
                if (yasuoWallLeft != null && yasuoWallRight != null)
                {
                    yasuoWallPoly = new RectanglePoly(yasuoWallLeft.Position, yasuoWallRight.Position, 75);
                    var intersections = new List<Vector2>();
                    for (var i = 0; i < yasuoWallPoly.Points.Count; i++)
                    {
                        var inter =
                            yasuoWallPoly.Points[i].Intersection(
                                yasuoWallPoly.Points[i != yasuoWallPoly.Points.Count - 1 ? i + 1 : 0],
                                from,
                                skillshot.End);
                        if (inter.Intersects)
                        {
                            intersections.Add(inter.Point);
                        }
                    }
                    if (intersections.Count > 0)
                    {
                        var intersection = intersections.OrderBy(item => item.Distance(from)).ToList()[0];
                        var collisionT = Variables.TickCount
                                         + Math.Max(
                                             0,
                                             skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick))
                                         + 100
                                         + 1000
                                         * (Math.Abs(skillshot.SpellData.MissileSpeed - int.MaxValue) > 0
                                                ? intersection.Distance(from) / skillshot.SpellData.MissileSpeed
                                                : 0);
                        if (collisionT - yasuoWallTime < 4000)
                        {
                            if (skillshot.SpellData.Type != SkillShotType.SkillshotMissileLine)
                            {
                                skillshot.ForceDisabled = true;
                            }
                            return intersection;
                        }
                    }
                }
            }
            return collisions.Count > 0 ? collisions.OrderBy(i => i.Distance).First().Position : new Vector2();
        }

        internal static void Init()
        {
            GameObject.OnCreate += (sender, args) =>
                {
                    var missile = sender as MissileClient;
                    var spellCaster = missile?.SpellCaster as Obj_AI_Hero;

                    if (spellCaster == null || spellCaster.ChampionName != "Yasuo"
                        || spellCaster.Team != GameObjects.Player.Team)
                    {
                        return;
                    }

                    switch (missile.SData.Name)
                    {
                        case "YasuoWMovingWallMisL":
                            yasuoWallLeft = missile;
                            break;
                        case "YasuoWMovingWallMisR":
                            yasuoWallRight = missile;
                            break;
                        case "YasuoWMovingWallMisVis":
                            yasuoWallTime = Variables.TickCount;
                            break;
                    }
                };
            GameObject.OnDelete += (sender, args) =>
                {
                    var missile = sender as MissileClient;

                    if (missile == null)
                    {
                        return;
                    }

                    if (missile.Compare(yasuoWallLeft))
                    {
                        yasuoWallLeft = null;
                    }
                    else if (missile.Compare(yasuoWallRight))
                    {
                        yasuoWallRight = null;
                    }
                };
        }

        private static FastPredResult FastPrediction(Vector2 from, Obj_AI_Base unit, int delay, int speed)
        {
            var tDelay = delay / 1000f + (Math.Abs(speed - int.MaxValue) > 0 ? unit.Distance(from) / speed : 0);
            var d = tDelay * unit.MoveSpeed;
            var path = unit.GetWaypoints();
            if (path.PathLength() > d)
            {
                return new FastPredResult { IsMoving = true, PredictedPos = path.CutPath((int)d)[0] };
            }
            return new FastPredResult { IsMoving = false, PredictedPos = path[path.Count - 1] };
        }

        #endregion

        private class DetectedCollision
        {
            #region Fields

            internal float Distance;

            internal Vector2 Position;

            #endregion
        }

        private class FastPredResult
        {
            #region Fields

            internal bool IsMoving;

            internal Vector2 PredictedPos;

            #endregion
        }
    }
}
namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    public enum CollisionObjectTypes
    {
        Minion,

        Champion,

        YasuoWall
    }

    internal class FastPredResult
    {
        #region Fields

        public Vector2 CurrentPos;

        public bool IsMoving;

        public Vector2 PredictedPos;

        #endregion
    }

    internal class DetectedCollision
    {
        #region Fields

        public float Diff;

        public float Distance;

        public Vector2 Position;

        public CollisionObjectTypes Type;

        public Obj_AI_Base Unit;

        #endregion
    }

    internal static class Collision
    {
        #region Static Fields

        private static Vector2 wallCastedPos;

        private static int wallCastT;

        #endregion

        #region Public Methods and Operators

        public static FastPredResult FastPrediction(Vector2 from, Obj_AI_Base unit, int delay, int speed)
        {
            var tDelay = delay / 1000f + (unit.Distance(from) / speed);
            var d = tDelay * unit.MoveSpeed;
            var path = unit.GetWaypoints();
            if (path.PathLength() > d)
            {
                return new FastPredResult
                           {
                               IsMoving = true, CurrentPos = unit.ServerPosition.ToVector2(),
                               PredictedPos = path.CutPath((int)d)[0]
                           };
            }
            return new FastPredResult
                       { IsMoving = false, CurrentPos = path[path.Count - 1], PredictedPos = path[path.Count - 1] };
        }

        public static Vector2 GetCollisionPoint(Skillshot skillshot)
        {
            var collisions = new List<DetectedCollision>();
            var from = skillshot.GetMissilePosition(0);
            skillshot.ForceDisabled = false;
            foreach (var cObject in skillshot.SpellData.CollisionObjects)
            {
                switch (cObject)
                {
                    case CollisionObjectTypes.Minion:
                        var minions = new List<Obj_AI_Minion>();
                        minions.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(1200, true, from.ToVector3())));
                        minions.AddRange(
                            GameObjects.Minions.Where(
                                i =>
                                i.IsValidTarget(1200, false, @from.ToVector3())
                                && (skillshot.Unit.Team == ObjectManager.Player.Team
                                        ? i.Team != ObjectManager.Player.Team
                                        : i.Team == ObjectManager.Player.Team) && i.IsMinion()));
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
                                skillshot.SpellData.RawRadius + (!pred.IsMoving ? (minion.BoundingRadius - 15) : 0)
                                - pos.Distance(@from, skillshot.End, true)
                            where w > 0
                            select
                                new DetectedCollision
                                    {
                                        Position =
                                            pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint
                                            + skillshot.Direction * 30,
                                        Unit = minion,
                                        Type = CollisionObjectTypes.Minion, Distance = pos.Distance(@from), Diff = w
                                    });
                        break;
                    case CollisionObjectTypes.Champion:
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
                                            pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint
                                            + skillshot.Direction * 30,
                                        Unit = hero, Type = CollisionObjectTypes.Minion,
                                        Distance = pos.Distance(@from), Diff = w
                                    });
                        break;
                    case CollisionObjectTypes.YasuoWall:
                        if (
                            !GameObjects.AllyHeroes.Any(
                                i => i.IsValidTarget(float.MaxValue, false) && i.ChampionName == "Yasuo"))
                        {
                            break;
                        }
                        GameObject wall = null;
                        foreach (var gameObject in
                            GameObjects.AllGameObjects.Where(
                                i => i.IsValid && Regex.IsMatch(i.Name, "_w_windwall.\\.troy", RegexOptions.IgnoreCase))
                            )
                        {
                            wall = gameObject;
                        }
                        if (wall == null)
                        {
                            break;
                        }
                        var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                        var wallWidth = (300 + 50 * Convert.ToInt32(level));
                        var wallDirection = (wall.Position.ToVector2() - wallCastedPos).Normalized().Perpendicular();
                        var wallStart = wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                        var wallPolygon = new Geometry.Rectangle(wallStart, wallEnd, 75).ToPolygon();
                        var intersections = new List<Vector2>();
                        for (var i = 0; i < wallPolygon.Points.Count; i++)
                        {
                            var inter =
                                wallPolygon.Points[i].Intersection(
                                    wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0],
                                    from,
                                    skillshot.End);
                            if (inter.Intersects)
                            {
                                intersections.Add(inter.Point);
                            }
                        }
                        if (intersections.Count > 0)
                        {
                            var intersection = intersections.OrderBy(item => item.Distance(@from)).ToList()[0];
                            var collisionT = Variables.TickCount
                                             + Math.Max(
                                                 0,
                                                 skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick))
                                             + 100
                                             + (1000 * intersection.Distance(from)) / skillshot.SpellData.MissileSpeed;
                            if (collisionT - wallCastT < 4000)
                            {
                                if (skillshot.SpellData.Type != SkillShotType.SkillshotMissileLine)
                                {
                                    skillshot.ForceDisabled = true;
                                }
                                return intersection;
                            }
                        }
                        break;
                }
            }
            return collisions.Count > 0 ? collisions.OrderBy(i => i.Distance).ToList()[0].Position : Vector2.Zero;
        }

        public static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsValid || sender.Team != ObjectManager.Player.Team
                        || args.SData.Name != "YasuoWMovingWall")
                    {
                        return;
                    }
                    wallCastT = Variables.TickCount;
                    wallCastedPos = sender.ServerPosition.ToVector2();
                };
        }

        #endregion
    }
}
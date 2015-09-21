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

    using Rectangle = LeagueSharp.SDK.Core.Math.Polygons.Rectangle;

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

        private static Vector2 yasuoWallCastedPos;

        private static int yasuoWallCastT;

        #endregion

        #region Methods

        internal static Vector2 GetCollisionPoint(this Skillshot skillshot)
        {
            var collisions = new List<DetectedCollision>();
            var from = skillshot.GetMissilePosition(0);
            skillshot.ForceDisabled = false;
            foreach (var cObject in skillshot.SpellData.CollisionObjects)
            {
                switch (cObject)
                {
                    case CollisionObjectTypes.Minion:
                        collisions.AddRange(
                            from minion in
                                GameObjects.Minions.Where(
                                    i =>
                                    i.IsValidTarget(1200, false, @from.ToVector3())
                                    && (skillshot.Unit.Team == ObjectManager.Player.Team
                                            ? i.Team != ObjectManager.Player.Team
                                            : i.Team == ObjectManager.Player.Team) && i.IsMinion())
                            let pred =
                                @from.FastPrediction(
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
                                            pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint
                                            + skillshot.Direction * 30,
                                        Unit = minion, Type = cObject,
                                        Distance = pos.Distance(@from), Diff = w
                                    });
                        if (skillshot.Unit.Team != ObjectManager.Player.Team)
                        {
                            collisions.AddRange(
                                from minion in
                                    GameObjects.Jungle.Where(i => i.IsValidTarget(1200, true, @from.ToVector3()))
                                let pred =
                                    @from.FastPrediction(
                                        minion,
                                        Math.Max(
                                            0,
                                            skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick)),
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
                                                pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint
                                                + skillshot.Direction * 30,
                                            Unit = minion, Type = cObject,
                                            Distance = pos.Distance(@from), Diff = w
                                        });
                        }
                        break;
                    case CollisionObjectTypes.Champion:
                        collisions.AddRange(
                            from hero in GameObjects.AllyHeroes.Where(i => i.IsValidTarget(1200, false) && !i.IsMe)
                            let pred =
                                @from.FastPrediction(
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
                                        Unit = hero, Type = cObject,
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
                        var wall =
                            GameObjects.AllGameObjects.FirstOrDefault(
                                i => i.IsValid && Regex.IsMatch(i.Name, "_w_windwall.\\.troy", RegexOptions.IgnoreCase));
                        if (wall == null)
                        {
                            break;
                        }
                        var wallWidth = 300 + 50 * Convert.ToInt32(wall.Name.Substring(wall.Name.Length - 6, 1));
                        var wallDirection =
                            (wall.Position.ToVector2() - yasuoWallCastedPos).Normalized().Perpendicular();
                        var wallStart = wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                        var wallPolygon = new Rectangle(wallStart, wallEnd, 75);
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
                            var intersection = intersections.MinOrDefault(i => i.Distance(@from));
                            if (Variables.TickCount
                                + Math.Max(0, skillshot.SpellData.Delay - (Variables.TickCount - skillshot.StartTick))
                                + 100 + (1000 * intersection.Distance(@from)) / skillshot.SpellData.MissileSpeed
                                - yasuoWallCastT < 4000)
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
            return collisions.Count > 0 ? collisions.MinOrDefault(i => i.Distance).Position : new Vector2();
        }

        internal static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsValid() || sender.Team != ObjectManager.Player.Team
                        || args.SData.Name != "YasuoWMovingWall")
                    {
                        return;
                    }
                    yasuoWallCastT = Variables.TickCount;
                    yasuoWallCastedPos = sender.ServerPosition.ToVector2();
                };
        }

        private static FastPredResult FastPrediction(this Vector2 from, Obj_AI_Base unit, int delay, int speed)
        {
            var d = (delay / 1000f + unit.Distance(from) / speed) * unit.MoveSpeed;
            var path = unit.GetWaypoints();
            return path.PathLength() > d
                       ? new FastPredResult
                             {
                                 IsMoving = true, CurrentPos = unit.ServerPosition.ToVector2(),
                                 PredictedPos = path.CutPath((int)d)[0]
                             }
                       : new FastPredResult
                             {
                                 IsMoving = false, CurrentPos = path[path.Count - 1], PredictedPos = path[path.Count - 1]
                             };
        }

        #endregion
    }
}
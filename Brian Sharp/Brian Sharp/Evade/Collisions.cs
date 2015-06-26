using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace BrianSharp.Evade
{
    public enum CollisionObjectTypes
    {
        Minion,
        Champions,
        YasuoWall
    }

    internal class FastPredResult
    {
        public Vector2 CurrentPos;
        public bool IsMoving;
        public Vector2 PredictedPos;
    }

    internal class DetectedCollision
    {
        public float Diff;
        public float Distance;
        public Vector2 Position;
        public CollisionObjectTypes Type;
        public Obj_AI_Base Unit;
    }

    internal static class Collisions
    {
        private static int _wallCastT;
        private static Vector2 _wallCastedPos;

        public static void Init()
        {
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsValid || sender.Team != ObjectManager.Player.Team || args.SData.Name != "YasuoWMovingWall")
            {
                return;
            }
            _wallCastT = Utils.GameTimeTickCount;
            _wallCastedPos = sender.ServerPosition.To2D();
        }

        private static FastPredResult FastPrediction(Vector2 from, Obj_AI_Base unit, int delay, int speed)
        {
            var tDelay = delay / 1000f + (from.Distance(unit) / speed);
            var d = tDelay * unit.MoveSpeed;
            var path = unit.GetWaypoints();
            return path.PathLength() > d
                ? new FastPredResult
                {
                    IsMoving = true,
                    CurrentPos = unit.ServerPosition.To2D(),
                    PredictedPos = path.CutPath((int) d)[0]
                }
                : (path.Count == 0
                    ? new FastPredResult
                    {
                        IsMoving = false,
                        CurrentPos = unit.ServerPosition.To2D(),
                        PredictedPos = unit.ServerPosition.To2D()
                    }
                    : new FastPredResult
                    {
                        IsMoving = false,
                        CurrentPos = path[path.Count - 1],
                        PredictedPos = path[path.Count - 1]
                    });
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
                        collisions.AddRange(
                            from minion in
                                MinionManager.GetMinions(
                                    @from.To3D(), 1200, MinionTypes.All,
                                    skillshot.Unit.Team == ObjectManager.Player.Team
                                        ? MinionTeam.NotAlly
                                        : MinionTeam.NotAllyForEnemy)
                            let pred =
                                FastPrediction(
                                    @from, minion,
                                    Math.Max(
                                        0, skillshot.SpellData.Delay - (Utils.GameTimeTickCount - skillshot.StartTick)),
                                    skillshot.SpellData.MissileSpeed)
                            let pos = pred.PredictedPos
                            let w =
                                skillshot.SpellData.RawRadius + (!pred.IsMoving ? minion.BoundingRadius - 15 : 0) -
                                pos.Distance(@from, skillshot.End, true)
                            where w > 0
                            select
                                new DetectedCollision
                                {
                                    Position =
                                        pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint +
                                        skillshot.Direction * 30,
                                    Unit = minion,
                                    Type = CollisionObjectTypes.Minion,
                                    Distance = pos.Distance(@from),
                                    Diff = w
                                });
                        break;
                    case CollisionObjectTypes.Champions:
                        collisions.AddRange(
                            from hero in
                                ObjectManager.Get<Obj_AI_Hero>()
                                    .Where(
                                        i =>
                                            i.IsValidTarget(1200, false) && i.Team == ObjectManager.Player.Team &&
                                            !i.IsMe)
                            let pred =
                                FastPrediction(
                                    @from, hero,
                                    Math.Max(
                                        0, skillshot.SpellData.Delay - (Utils.GameTimeTickCount - skillshot.StartTick)),
                                    skillshot.SpellData.MissileSpeed)
                            let pos = pred.PredictedPos
                            let w = skillshot.SpellData.RawRadius + 30 - pos.Distance(@from, skillshot.End, true)
                            where w > 0
                            select
                                new DetectedCollision
                                {
                                    Position =
                                        pos.ProjectOn(skillshot.End, skillshot.Start).LinePoint +
                                        skillshot.Direction * 30,
                                    Unit = hero,
                                    Type = CollisionObjectTypes.Minion,
                                    Distance = pos.Distance(@from),
                                    Diff = w
                                });
                        break;
                    case CollisionObjectTypes.YasuoWall:
                        if (
                            !ObjectManager.Get<Obj_AI_Hero>()
                                .Any(
                                    i =>
                                        i.IsValidTarget(float.MaxValue, false) && i.Team == ObjectManager.Player.Team &&
                                        i.ChampionName == "Yasuo"))
                        {
                            break;
                        }
                        var wall =
                            ObjectManager.Get<GameObject>()
                                .FirstOrDefault(
                                    i =>
                                        i.IsValid &&
                                        Regex.IsMatch(i.Name, "_w_windwall.\\.troy", RegexOptions.IgnoreCase));
                        if (wall == null)
                        {
                            break;
                        }
                        var level = wall.Name.Substring(wall.Name.Length - 6, 1);
                        var wallWidth = 300 + 50 * Convert.ToInt32(level);
                        var wallDirection = (wall.Position.To2D() - _wallCastedPos).Normalized().Perpendicular();
                        var subWallWidth = wallWidth / 2;
                        var wallStart = wall.Position.To2D() + subWallWidth * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                        var wallPolygon = new Geometry.Polygon.Rectangle(wallStart, wallEnd, 75);
                        var intersections = new List<Vector2>();
                        for (var i = 0; i < wallPolygon.Points.Count; i++)
                        {
                            var inter =
                                wallPolygon.Points[i].Intersection(
                                    wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0], from,
                                    skillshot.End);
                            if (inter.Intersects)
                            {
                                intersections.Add(inter.Point);
                            }
                        }
                        if (intersections.Count > 0)
                        {
                            var intersection = intersections.OrderBy(i => i.Distance(@from)).ToList()[0];
                            var collisionT = Utils.GameTimeTickCount +
                                             Math.Max(
                                                 0,
                                                 skillshot.SpellData.Delay -
                                                 (Utils.GameTimeTickCount - skillshot.StartTick)) + 100 +
                                             (1000 * intersection.Distance(from)) / skillshot.SpellData.MissileSpeed;
                            if (collisionT - _wallCastT < 4000)
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
            return collisions.Count > 0 ? collisions.OrderBy(i => i.Distance).ToList()[0].Position : new Vector2();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace BrianSharp.Evade
{
    public enum SkillShotType
    {
        SkillshotCircle,
        SkillshotLine,
        SkillshotMissileLine,
        SkillshotCone,
        SkillshotMissileCone,
        SkillshotRing
    }

    public enum DetectionType
    {
        RecvPacket,
        ProcessSpell
    }

    public struct SafePathResult
    {
        public FoundIntersection Intersection;
        public bool IsSafe;

        public SafePathResult(bool isSafe, FoundIntersection intersection)
        {
            IsSafe = isSafe;
            Intersection = intersection;
        }
    }

    public struct FoundIntersection
    {
        public Vector2 ComingFrom;
        public float Distance;
        public Vector2 Point;
        public int Time;
        public bool Valid;

        public FoundIntersection(float distance, int time, Vector2 point, Vector2 comingFrom)
        {
            Distance = distance;
            ComingFrom = comingFrom;
            Valid = point.IsValid();
            Point = point + Configs.GridSize * (ComingFrom - point).Normalized();
            Time = time;
        }
    }


    public class Skillshot
    {
        private bool _cachedValue;
        private int _cachedValueTick;
        private Vector2 _collisionEnd;
        private int _helperTick;
        private int _lastCollisionCalc;
        public Geometry.Polygon.Circle Circle;
        public DetectionType DetectionType;
        public Vector2 Direction;
        public Vector2 End;
        public bool ForceDisabled;
        public Geometry.Polygon Polygon;
        public Geometry.Polygon.Rectangle Rectangle;
        public Geometry.Polygon.Ring Ring;
        public Geometry.Polygon.Sector Sector;
        public SpellData SpellData;
        public Vector2 Start;
        public int StartTick;

        public Skillshot(DetectionType detectionType,
            SpellData spellData,
            int startT,
            Vector2 start,
            Vector2 end,
            Obj_AI_Base unit)
        {
            DetectionType = detectionType;
            SpellData = spellData;
            StartTick = startT;
            Start = start;
            End = end;
            Direction = (end - start).Normalized();
            Unit = unit;
            switch (spellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    Circle = new Geometry.Polygon.Circle(CollisionEnd, spellData.Radius, 22);
                    break;
                case SkillShotType.SkillshotLine:
                case SkillShotType.SkillshotMissileLine:
                    Rectangle = new Geometry.Polygon.Rectangle(Start, CollisionEnd, spellData.Radius);
                    break;
                case SkillShotType.SkillshotCone:
                    Sector = new Geometry.Polygon.Sector(
                        start, CollisionEnd - start, spellData.Radius * (float) Math.PI / 180, spellData.Range, 22);
                    break;
                case SkillShotType.SkillshotRing:
                    Ring = new Geometry.Polygon.Ring(CollisionEnd, spellData.Radius, spellData.RingRadius, 22);
                    break;
            }
            UpdatePolygon();
        }

        public Vector2 Perpendicular
        {
            get { return Direction.Perpendicular(); }
        }

        private Vector2 CollisionEnd
        {
            get
            {
                return _collisionEnd.IsValid()
                    ? _collisionEnd
                    : (IsGlobal
                        ? GlobalGetMissilePosition(0) +
                          Direction * SpellData.MissileSpeed *
                          (0.5f + SpellData.Radius * 2 / ObjectManager.Player.MoveSpeed)
                        : End);
            }
        }

        private bool IsGlobal
        {
            get { return SpellData.RawRange == 20000; }
        }

        public Obj_AI_Base Unit { get; set; }

        public bool IsActive()
        {
            return SpellData.MissileAccel != 0
                ? Utils.GameTimeTickCount <= StartTick + 5000
                : Utils.GameTimeTickCount <=
                  StartTick + SpellData.Delay + SpellData.ExtraDuration +
                  1000 * (Start.Distance(End) / SpellData.MissileSpeed);
        }

        public bool Evade()
        {
            if (ForceDisabled)
            {
                return false;
            }
            if (Utils.GameTimeTickCount - _cachedValueTick < 100)
            {
                return _cachedValue;
            }
            _cachedValue = Helper.GetValue<bool>("SS_" + SpellData.MenuItemName, "Enabled");
            _cachedValueTick = Utils.GameTimeTickCount;
            return _cachedValue;
        }

        public void OnUpdate()
        {
            if (SpellData.CollisionObjects.Count() > 0 && SpellData.CollisionObjects != null &&
                Utils.GameTimeTickCount - _lastCollisionCalc > 50)
            {
                _lastCollisionCalc = Utils.GameTimeTickCount;
                _collisionEnd = Collisions.GetCollisionPoint(this);
            }
            if (SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                Rectangle = new Geometry.Polygon.Rectangle(GetMissilePosition(0), CollisionEnd, SpellData.Radius);
                UpdatePolygon();
            }
            if (SpellData.MissileFollowsUnit)
            {
                if (Unit.IsVisible)
                {
                    End = Unit.ServerPosition.To2D();
                    Direction = (End - Start).Normalized();
                    UpdatePolygon();
                }
            }
            if (SpellData.SpellName == "SionR")
            {
                if (_helperTick == 0)
                {
                    _helperTick = StartTick;
                }
                SpellData.MissileSpeed = (int) Unit.MoveSpeed;
                if (Unit.IsValidTarget(float.MaxValue, false))
                {
                    if (!Unit.HasBuff("SionR") && Utils.GameTimeTickCount - _helperTick > 600)
                    {
                        StartTick = 0;
                    }
                    else
                    {
                        StartTick = Utils.GameTimeTickCount - SpellData.Delay;
                        Start = Unit.ServerPosition.To2D();
                        End = Unit.ServerPosition.To2D() + 1000 * Unit.Direction.To2D().Perpendicular();
                        Direction = (End - Start).Normalized();
                        UpdatePolygon();
                    }
                }
                else
                {
                    StartTick = 0;
                }
            }
        }

        private void UpdatePolygon()
        {
            switch (SpellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    Circle.UpdatePolygon();
                    Polygon = Circle;
                    break;
                case SkillShotType.SkillshotLine:
                case SkillShotType.SkillshotMissileLine:
                    Rectangle.UpdatePolygon();
                    Polygon = Rectangle;
                    break;
                case SkillShotType.SkillshotCone:
                    Sector.UpdatePolygon();
                    Polygon = Sector;
                    break;
                case SkillShotType.SkillshotRing:
                    Ring.UpdatePolygon();
                    Polygon = Ring;
                    break;
            }
        }

        private Vector2 GlobalGetMissilePosition(int time)
        {
            var t = Math.Max(0, Utils.GameTimeTickCount + time - StartTick - SpellData.Delay);
            var sub = t * SpellData.MissileSpeed / 1000;
            t = (int) Math.Max(0, Math.Min(End.Distance(Start), sub));
            return Start + Direction * t;
        }

        public Vector2 GetMissilePosition(int time)
        {
            var t = Math.Max(0, Utils.GameTimeTickCount + time - StartTick - SpellData.Delay);
            int x;
            if (SpellData.MissileAccel == 0)
            {
                x = t * SpellData.MissileSpeed / 1000;
            }
            else
            {
                var t1 = (SpellData.MissileAccel > 0
                    ? SpellData.MissileMaxSpeed
                    : SpellData.MissileMinSpeed - SpellData.MissileSpeed) * 1000f / SpellData.MissileAccel;
                if (t <= t1)
                {
                    x =
                        (int)
                            (t * SpellData.MissileSpeed / 1000d + 0.5d * SpellData.MissileAccel * Math.Pow(t / 1000d, 2));
                }
                else
                {
                    x =
                        (int)
                            (t1 * SpellData.MissileSpeed / 1000d +
                             0.5d * SpellData.MissileAccel * Math.Pow(t1 / 1000d, 2) +
                             (t - t1) / 1000d *
                             (SpellData.MissileAccel < 0 ? SpellData.MissileMaxSpeed : SpellData.MissileMinSpeed));
                }
            }
            t = (int) Math.Max(0, Math.Min(CollisionEnd.Distance(Start), x));
            return Start + Direction * t;
        }

        public SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var distance = 0f;
            timeOffset += Game.Ping / 2;
            speed = (speed == -1) ? (int) ObjectManager.Player.MoveSpeed : speed;
            var allIntersections = new List<FoundIntersection>();
            for (var i = 0; i <= path.Count - 2; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var segmentIntersections = new List<FoundIntersection>();
                for (var j = 0; j <= Polygon.Points.Count - 1; j++)
                {
                    var sideStart = Polygon.Points[j];
                    var sideEnd = Polygon.Points[j == (Polygon.Points.Count - 1) ? 0 : j + 1];
                    var intersection = from.Intersection(to, sideStart, sideEnd);
                    if (intersection.Intersects)
                    {
                        segmentIntersections.Add(
                            new FoundIntersection(
                                distance + intersection.Point.Distance(from),
                                (int) ((distance + intersection.Point.Distance(from)) * 1000 / speed),
                                intersection.Point, from));
                    }
                }
                allIntersections.AddRange(segmentIntersections.OrderBy(o => o.Distance));
                distance += from.Distance(to);
            }
            if (SpellData.Type == SkillShotType.SkillshotMissileLine ||
                SpellData.Type == SkillShotType.SkillshotMissileCone)
            {
                if (IsSafe(ObjectManager.Player.ServerPosition.To2D()))
                {
                    if (allIntersections.Count == 0)
                    {
                        return new SafePathResult(true, new FoundIntersection());
                    }
                    for (var i = 0; i <= allIntersections.Count - 1; i = i + 2)
                    {
                        var enterIntersection = allIntersections[i];
                        var enterIntersectionProjection = enterIntersection.Point.ProjectOn(Start, End).SegmentPoint;
                        if (i == allIntersections.Count - 1)
                        {
                            return
                                new SafePathResult(
                                    (End.Distance(GetMissilePosition(enterIntersection.Time - timeOffset)) + 50 <=
                                     End.Distance(enterIntersectionProjection)) &&
                                    ObjectManager.Player.MoveSpeed < SpellData.MissileSpeed, allIntersections[0]);
                        }
                        var exitIntersection = allIntersections[i + 1];
                        var exitIntersectionProjection = exitIntersection.Point.ProjectOn(Start, End).SegmentPoint;
                        var missilePosOnEnter = GetMissilePosition(enterIntersection.Time - timeOffset);
                        var missilePosOnExit = GetMissilePosition(exitIntersection.Time + timeOffset);
                        if (missilePosOnEnter.Distance(End) + 50 > enterIntersectionProjection.Distance(End) &&
                            missilePosOnExit.Distance(End) <= exitIntersectionProjection.Distance(End))
                        {
                            return new SafePathResult(false, allIntersections[0]);
                        }
                    }
                    return new SafePathResult(true, allIntersections[0]);
                }
                if (allIntersections.Count == 0)
                {
                    return new SafePathResult(false, new FoundIntersection());
                }
                if (allIntersections.Count > 0)
                {
                    var exitIntersection = allIntersections[0];
                    var exitIntersectionProjection = exitIntersection.Point.ProjectOn(Start, End).SegmentPoint;
                    var missilePosOnExit = GetMissilePosition(exitIntersection.Time + timeOffset);
                    if (missilePosOnExit.Distance(End) <= exitIntersectionProjection.Distance(End))
                    {
                        return new SafePathResult(false, allIntersections[0]);
                    }
                }
            }
            if (IsSafe(ObjectManager.Player.ServerPosition.To2D()))
            {
                if (allIntersections.Count == 0)
                {
                    return new SafePathResult(true, new FoundIntersection());
                }
                if (SpellData.DontCross)
                {
                    return new SafePathResult(false, allIntersections[0]);
                }
            }
            else if (allIntersections.Count == 0)
            {
                return new SafePathResult(false, new FoundIntersection());
            }
            var timeToExplode = (SpellData.DontAddExtraDuration ? 0 : SpellData.ExtraDuration) + SpellData.Delay +
                                (int) (1000 * Start.Distance(End) / SpellData.MissileSpeed) -
                                (Utils.GameTimeTickCount - StartTick);
            var myPositionWhenExplodes = path.PositionAfter(timeToExplode, speed, delay);
            return !IsSafe(myPositionWhenExplodes)
                ? new SafePathResult(false, allIntersections[0])
                : new SafePathResult(IsSafe(path.PositionAfter(timeToExplode, speed, timeOffset)), allIntersections[0]);
        }

        public bool IsSafe(Vector2 point)
        {
            return Polygon.IsOutside(point);
        }

        public bool IsAboutToHit(int time, Obj_AI_Base unit)
        {
            if (SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                var missilePos = GetMissilePosition(0);
                var missilePosAfterT = GetMissilePosition(time);
                return unit.ServerPosition.To2D().Distance(missilePos, missilePosAfterT, true) < SpellData.Radius;
            }
            return !IsSafe(unit.ServerPosition.To2D()) &&
                   SpellData.ExtraDuration + SpellData.Delay +
                   (int) ((1000 * Start.Distance(End)) / SpellData.MissileSpeed) - (Utils.GameTimeTickCount - StartTick) <=
                   time;
        }
    }
}
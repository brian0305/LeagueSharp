namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Math.Polygons;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Rectangle = LeagueSharp.SDK.Core.Math.Polygons.Rectangle;

    public enum SkillShotType
    {
        SkillshotCircle,

        SkillshotLine,

        SkillshotMissileLine,

        SkillshotCone,

        SkillshotMissileCone,

        SkillshotRing,

        SkillshotArc
    }

    public enum DetectionType
    {
        RecvPacket,

        ProcessSpell
    }

    public struct SafePathResult
    {
        #region Fields

        public FoundIntersection Intersection;

        public bool IsSafe;

        #endregion

        #region Constructors and Destructors

        public SafePathResult(bool isSafe, FoundIntersection intersection)
        {
            this.IsSafe = isSafe;
            this.Intersection = intersection;
        }

        #endregion
    }

    public struct FoundIntersection
    {
        #region Fields

        public Vector2 ComingFrom;

        public float Distance;

        public Vector2 Point;

        public int Time;

        public bool Valid;

        #endregion

        #region Constructors and Destructors

        public FoundIntersection(float distance, int time, Vector2 point, Vector2 comingFrom)
        {
            this.Distance = distance;
            this.ComingFrom = comingFrom;
            this.Valid = point.IsValid();
            this.Point = point + Config.GridSize * (this.ComingFrom - point).Normalized();
            this.Time = time;
        }

        #endregion
    }

    public class Arc : Polygon
    {
        #region Fields

        private readonly float distance;

        private readonly Vector2 end;

        private readonly int quality;

        private readonly int radius;

        private Vector2 start;

        #endregion

        #region Constructors and Destructors

        public Arc(Vector2 start, Vector2 end, int radius, int quality = 20)
        {
            this.start = start;
            this.end = end;
            this.radius = radius;
            this.distance = this.start.Distance(this.end);
            this.quality = quality;
        }

        #endregion

        #region Public Methods and Operators

        public void UpdatePolygon(int offset = 0)
        {
            this.Points.Clear();
            offset += this.radius;
            var innerRadius = -0.1562f * this.distance + 687.31f;
            var outerRadius = 0.35256f * this.distance + 133f;
            outerRadius = outerRadius / (float)Math.Cos(2 * Math.PI / this.quality);
            var innerCenter = this.start.CircleCircleIntersection(this.end, innerRadius, innerRadius)[0];
            var outerCenter = this.start.CircleCircleIntersection(this.end, outerRadius, outerRadius)[0];
            var direction = (this.end - outerCenter).Normalized();
            var step = -(float)(direction.AngleBetween((this.start - outerCenter).Normalized()) * Math.PI / 180)
                       / this.quality;
            for (var i = 0; i < this.quality; i++)
            {
                this.Points.Add(outerCenter + (outerRadius + offset + 15f) * direction.Rotated(step * i));
            }
            direction = (this.start - innerCenter).Normalized();
            step = (float)(direction.AngleBetween((this.end - innerCenter).Normalized()) * Math.PI / 180) / this.quality;
            for (var i = 0; i < this.quality; i++)
            {
                this.Points.Add(innerCenter + Math.Max(0, innerRadius - offset - 100) * direction.Rotated(step * i));
            }
        }

        #endregion
    }

    public class Skillshot
    {
        #region Fields

        public Arc Arc;

        public Circle Circle;

        public DetectionType DetectionType;

        public Vector2 Direction;

        public Vector2 End;

        public Polygon EvadePolygon;

        public bool ForceDisabled;

        public Polygon Polygon;

        public Rectangle Rectangle;

        public Ring Ring;

        public Sector Sector;

        public SpellData SpellData;

        public Vector2 Start;

        public int StartTick;

        private bool cachedValue;

        private int cachedValueTick;

        private Vector2 collisionEnd;

        private int helperTick;

        private int lastCollisionCalc;

        #endregion

        #region Constructors and Destructors

        public Skillshot(
            DetectionType detectionType,
            SpellData spellData,
            int startT,
            Vector2 start,
            Vector2 end,
            Obj_AI_Base unit)
        {
            this.DetectionType = detectionType;
            this.SpellData = spellData;
            this.StartTick = startT;
            this.Start = start;
            this.End = end;
            this.Direction = (end - start).Normalized();
            this.Unit = unit;
            switch (spellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    this.Circle = new Circle(this.CollisionEnd, spellData.Radius, 22);
                    break;
                case SkillShotType.SkillshotLine:
                case SkillShotType.SkillshotMissileLine:
                    this.Rectangle = new Rectangle(this.Start, this.CollisionEnd, spellData.Radius);
                    break;
                case SkillShotType.SkillshotCone:
                    this.Sector = new Sector(
                        start,
                        this.CollisionEnd - start,
                        spellData.Radius * (float)Math.PI / 180,
                        spellData.Range,
                        22);
                    break;
                case SkillShotType.SkillshotRing:
                    this.Ring = new Ring(this.CollisionEnd, spellData.Radius, spellData.RingRadius, 22);
                    break;
                case SkillShotType.SkillshotArc:
                    this.Arc = new Arc(
                        start,
                        end,
                        Config.SkillShotsExtraRadius + (int)ObjectManager.Player.BoundingRadius,
                        22);
                    break;
            }
            this.UpdatePolygon();
        }

        #endregion

        #region Public Properties

        public int DangerLevel
        {
            get
            {
                return
                    Program.MainMenu["Evade"][this.SpellData.ChampionName.ToLowerInvariant()][this.SpellData.SpellName][
                        "DangerLevel"];
            }
        }

        public bool Enabled
        {
            get
            {
                if (this.ForceDisabled)
                {
                    return false;
                }
                if (Variables.TickCount - this.cachedValueTick < 100)
                {
                    return this.cachedValue;
                }
                if (
                    !Program.MainMenu["Evade"][this.SpellData.ChampionName.ToLowerInvariant()][this.SpellData.SpellName]
                         ["IsDangerous"] && Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active)
                {
                    this.cachedValue = false;
                    this.cachedValueTick = Variables.TickCount;
                    return this.cachedValue;
                }
                this.cachedValue =
                    Program.MainMenu["Evade"][this.SpellData.ChampionName.ToLowerInvariant()][this.SpellData.SpellName][
                        "Enabled"];
                this.cachedValueTick = Variables.TickCount;
                return this.cachedValue;
            }
        }

        public bool IsActive
        {
            get
            {
                return this.SpellData.MissileAccel != 0
                           ? Variables.TickCount <= this.StartTick + 5000
                           : Variables.TickCount
                             <= this.StartTick + this.SpellData.Delay + this.SpellData.ExtraDuration
                             + 1000 * (this.Start.Distance(this.End) / this.SpellData.MissileSpeed);
            }
        }

        public Obj_AI_Base Unit { get; set; }

        #endregion

        #region Properties

        private Vector2 CollisionEnd
        {
            get
            {
                return this.collisionEnd.IsValid()
                           ? this.collisionEnd
                           : (this.SpellData.RawRange == 20000
                                  ? this.GetGlobalMissilePosition(0)
                                    + this.Direction * this.SpellData.MissileSpeed
                                    * (0.5f + this.SpellData.Radius * 2 / ObjectManager.Player.MoveSpeed)
                                  : this.End);
            }
        }

        #endregion

        #region Public Methods and Operators

        public Vector2 GetMissilePosition(int time)
        {
            var t = Math.Max(0, Variables.TickCount + time - this.StartTick - this.SpellData.Delay);
            int x;
            if (this.SpellData.MissileAccel == 0)
            {
                x = t * this.SpellData.MissileSpeed / 1000;
            }
            else
            {
                var t1 = (this.SpellData.MissileAccel > 0
                              ? this.SpellData.MissileMaxSpeed
                              : this.SpellData.MissileMinSpeed - this.SpellData.MissileSpeed) * 1000f
                         / this.SpellData.MissileAccel;
                x = t <= t1
                        ? (int)
                          (t * this.SpellData.MissileSpeed / 1000d
                           + 0.5d * this.SpellData.MissileAccel * Math.Pow(t / 1000d, 2))
                        : (int)
                          (t1 * this.SpellData.MissileSpeed / 1000d
                           + 0.5d * this.SpellData.MissileAccel * Math.Pow(t1 / 1000d, 2)
                           + (t - t1) / 1000d
                           * (this.SpellData.MissileAccel < 0
                                  ? this.SpellData.MissileMaxSpeed
                                  : this.SpellData.MissileMinSpeed));
            }
            return this.Start + this.Direction * (int)Math.Max(0, Math.Min(this.CollisionEnd.Distance(this.Start), x));
        }

        public bool IsAboutToHit(int time, Obj_AI_Base unit)
        {
            if (this.SpellData.Type != SkillShotType.SkillshotMissileLine)
            {
                return !this.IsSafePoint(unit.ServerPosition.ToVector2())
                       && this.SpellData.ExtraDuration + this.SpellData.Delay
                       + (int)(1000 * this.Start.Distance(this.End) / this.SpellData.MissileSpeed)
                       - (Variables.TickCount - this.StartTick) <= time;
            }
            var project = unit.ServerPosition.ToVector2()
                .ProjectOn(this.GetMissilePosition(0), this.GetMissilePosition(time));
            return project.IsOnSegment && unit.Distance(project.SegmentPoint) < this.SpellData.Radius;
        }

        public SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var distance = 0f;
            timeOffset += Game.Ping / 2;
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var allIntersections = new List<FoundIntersection>();
            for (var i = 0; i <= path.Count - 2; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var segmentIntersections = new List<FoundIntersection>();
                for (var j = 0; j <= this.Polygon.Points.Count - 1; j++)
                {
                    var sideStart = this.Polygon.Points[j];
                    var sideEnd = this.Polygon.Points[j == this.Polygon.Points.Count - 1 ? 0 : j + 1];
                    var intersection = from.Intersection(to, sideStart, sideEnd);
                    if (intersection.Intersects)
                    {
                        segmentIntersections.Add(
                            new FoundIntersection(
                                distance + intersection.Point.Distance(from),
                                (int)((distance + intersection.Point.Distance(from)) * 1000 / speed),
                                intersection.Point,
                                from));
                    }
                }
                allIntersections.AddRange(segmentIntersections.OrderBy(o => o.Distance));
                distance += from.Distance(to);
            }
            if (this.SpellData.Type == SkillShotType.SkillshotMissileLine
                || this.SpellData.Type == SkillShotType.SkillshotMissileCone
                || this.SpellData.Type == SkillShotType.SkillshotArc)
            {
                if (this.IsSafePoint(Evade.PlayerPosition))
                {
                    if (allIntersections.Count == 0)
                    {
                        return new SafePathResult(true, new FoundIntersection());
                    }
                    if (this.SpellData.DontCross)
                    {
                        return new SafePathResult(false, allIntersections[0]);
                    }
                    for (var i = 0; i <= allIntersections.Count - 1; i = i + 2)
                    {
                        var enterIntersection = allIntersections[i];
                        var enterIntersectionProjection =
                            enterIntersection.Point.ProjectOn(this.Start, this.End).SegmentPoint;
                        if (i == allIntersections.Count - 1)
                        {
                            return
                                new SafePathResult(
                                    (this.End.Distance(this.GetMissilePosition(enterIntersection.Time - timeOffset))
                                     + 50 <= this.End.Distance(enterIntersectionProjection))
                                    && ObjectManager.Player.MoveSpeed < this.SpellData.MissileSpeed,
                                    allIntersections[0]);
                        }
                        var exitIntersection = allIntersections[i + 1];
                        var exitIntersectionProjection =
                            exitIntersection.Point.ProjectOn(this.Start, this.End).SegmentPoint;
                        if (this.GetMissilePosition(enterIntersection.Time - timeOffset).Distance(this.End) + 50
                            > enterIntersectionProjection.Distance(this.End)
                            && this.GetMissilePosition(exitIntersection.Time + timeOffset).Distance(this.End)
                            <= exitIntersectionProjection.Distance(this.End))
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
                    var exitIntersectionProjection = exitIntersection.Point.ProjectOn(this.Start, this.End).SegmentPoint;
                    if (this.GetMissilePosition(exitIntersection.Time + timeOffset).Distance(this.End)
                        <= exitIntersectionProjection.Distance(this.End))
                    {
                        return new SafePathResult(false, allIntersections[0]);
                    }
                }
            }
            if (allIntersections.Count == 0)
            {
                return new SafePathResult(false, new FoundIntersection());
            }
            if (this.IsSafePoint(Evade.PlayerPosition) && this.SpellData.DontCross)
            {
                return new SafePathResult(false, allIntersections[0]);
            }
            var timeToExplode = (this.SpellData.DontAddExtraDuration ? 0 : this.SpellData.ExtraDuration)
                                + this.SpellData.Delay
                                + (int)(1000 * this.Start.Distance(this.End) / this.SpellData.MissileSpeed)
                                - (Variables.TickCount - this.StartTick);
            return !this.IsSafePoint(path.PositionAfter(timeToExplode, speed, delay))
                       ? new SafePathResult(false, allIntersections[0])
                       : new SafePathResult(
                             this.IsSafePoint(path.PositionAfter(timeToExplode, speed, timeOffset)),
                             allIntersections[0]);
        }

        public bool IsSafePoint(Vector2 point)
        {
            return this.Polygon.IsOutside(point);
        }

        public bool IsSafeToBlink(Vector2 point, int timeOffset, int delay = 0)
        {
            timeOffset /= 2;
            if (this.IsSafePoint(Evade.PlayerPosition))
            {
                return true;
            }
            if (this.SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                var missilePositionAfterBlink = this.GetMissilePosition(delay + timeOffset);
                var myPositionProjection = Evade.PlayerPosition.ProjectOn(this.Start, this.End);
                return missilePositionAfterBlink.Distance(this.End)
                       >= myPositionProjection.SegmentPoint.Distance(this.End);
            }
            var timeToExplode = this.SpellData.ExtraDuration + this.SpellData.Delay
                                + (int)(1000 * this.Start.Distance(this.End) / this.SpellData.MissileSpeed)
                                - (Variables.TickCount - this.StartTick);
            return timeToExplode > timeOffset + delay;
        }

        public void OnUpdate()
        {
            if (this.SpellData.CollisionObjects.Length > 0 && this.SpellData.CollisionObjects != null
                && Variables.TickCount - this.lastCollisionCalc > 50)
            {
                this.lastCollisionCalc = Variables.TickCount;
                this.collisionEnd = this.GetCollisionPoint();
            }
            if (this.SpellData.Type == SkillShotType.SkillshotMissileLine)
            {
                this.Rectangle = new Rectangle(this.GetMissilePosition(0), this.CollisionEnd, this.SpellData.Radius);
                this.UpdatePolygon();
            }
            if (this.SpellData.MissileFollowsUnit && this.Unit.IsVisible)
            {
                this.End = this.Unit.ServerPosition.ToVector2();
                this.Direction = (this.End - this.Start).Normalized();
                this.UpdatePolygon();
            }
            if (this.SpellData.SpellName == "SionR")
            {
                if (this.helperTick == 0)
                {
                    this.helperTick = this.StartTick;
                }
                this.SpellData.MissileSpeed = (int)this.Unit.MoveSpeed;
                if (this.Unit.IsValidTarget(float.MaxValue, false))
                {
                    if (!this.Unit.HasBuff("SionR") && Variables.TickCount - this.helperTick > 600)
                    {
                        this.StartTick = 0;
                    }
                    else
                    {
                        this.StartTick = Variables.TickCount - this.SpellData.Delay;
                        this.Start = this.Unit.ServerPosition.ToVector2();
                        this.End = this.Unit.ServerPosition.ToVector2()
                                   + 1000 * this.Unit.Direction.ToVector2().Perpendicular();
                        this.Direction = (this.End - this.Start).Normalized();
                        this.UpdatePolygon();
                    }
                }
                else
                {
                    this.StartTick = 0;
                }
            }
            if (this.SpellData.FollowCaster)
            {
                this.Circle.Center = this.Unit.ServerPosition.ToVector2();
                this.UpdatePolygon();
            }
        }

        #endregion

        #region Methods

        private Vector2 GetGlobalMissilePosition(int time)
        {
            return this.Start
                   + this.Direction
                   * (int)
                     Math.Max(
                         0,
                         Math.Min(
                             this.End.Distance(this.Start),
                             (float)Math.Max(0, Variables.TickCount + time - this.StartTick - this.SpellData.Delay)
                             * this.SpellData.MissileSpeed / 1000));
        }

        private void UpdatePolygon()
        {
            switch (this.SpellData.Type)
            {
                case SkillShotType.SkillshotCircle:
                    this.Circle.UpdatePolygon();
                    this.Polygon = this.Circle;
                    this.Circle.UpdatePolygon(Config.ExtraEvadeDistance);
                    this.EvadePolygon = this.Circle;
                    break;
                case SkillShotType.SkillshotLine:
                case SkillShotType.SkillshotMissileLine:
                    this.Rectangle.UpdatePolygon();
                    this.Polygon = this.Rectangle;
                    this.Rectangle.UpdatePolygon(Config.ExtraEvadeDistance);
                    this.EvadePolygon = this.Rectangle;
                    break;
                case SkillShotType.SkillshotCone:
                    this.Sector.UpdatePolygon();
                    this.Polygon = this.Sector;
                    this.Sector.UpdatePolygon(Config.ExtraEvadeDistance);
                    this.EvadePolygon = this.Sector;
                    break;
                case SkillShotType.SkillshotRing:
                    this.Ring.UpdatePolygon();
                    this.Polygon = this.Ring;
                    this.Ring.UpdatePolygon(Config.ExtraEvadeDistance);
                    this.EvadePolygon = this.Ring;
                    break;
                case SkillShotType.SkillshotArc:
                    this.Arc.UpdatePolygon();
                    this.Polygon = this.Arc;
                    this.Arc.UpdatePolygon(Config.ExtraEvadeDistance);
                    this.EvadePolygon = this.Arc;
                    break;
            }
        }

        #endregion
    }
}
namespace vEvade.Spells
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Core;
    using vEvade.Helpers;

    using Color = System.Drawing.Color;

    #endregion

    public struct SafePath
    {
        #region Fields

        public Intersects Intersect;

        public bool IsSafe;

        #endregion

        #region Constructors and Destructors

        public SafePath(bool isSafe, Intersects intersect)
        {
            this.IsSafe = isSafe;
            this.Intersect = intersect;
        }

        #endregion
    }

    public struct SafePoint
    {
        #region Fields

        public bool IsSafe;

        public List<SpellInstance> Spells;

        #endregion

        #region Constructors and Destructors

        public SafePoint(List<SpellInstance> spells)
        {
            this.Spells = spells;
            this.IsSafe = this.Spells.Count == 0;
        }

        #endregion
    }

    public struct Intersects
    {
        #region Fields

        public float Distance;

        public Vector2 Point;

        public int Time;

        public bool Valid;

        #endregion

        #region Constructors and Destructors

        public Intersects(float distance, int time, Vector2 point, Vector2 comingFrom)
        {
            this.Distance = distance;
            this.Valid = point.IsValid();
            this.Point = point + Configs.GridSize * (comingFrom - point).Normalized();
            this.Time = time;
        }

        #endregion
    }

    public class SpellInstance
    {
        #region Fields

        public Polygons.Arc Arc;

        public Polygons.Circle Circle;

        public Polygons.Cone Cone;

        public SpellData Data;

        public Vector2 Direction;

        public Geometry.Polygon DrawPolygon;

        public Vector2 End;

        public int EndTick;

        public Geometry.Polygon EvadePolygon;

        public bool ForceDisabled;

        public bool IsFromFoW;

        public Polygons.Line Line;

        public GameObject MissileObject = null;

        public Geometry.Polygon PathFindingInnerPolygon;

        public Geometry.Polygon PathFindingOuterPolygon;

        public Geometry.Polygon Polygon;

        public Vector2 PredEnd;

        public Polygons.Ring Ring;

        public int SpellId;

        public Vector2 Start;

        public int StartTick;

        public GameObject ToggleObject = null;

        public SpellType Type;

        public GameObject TrapObject = null;

        public Obj_AI_Base Unit;

        private bool cachedValue;

        private int cachedValueTick;

        private int lastCalcColTick;

        private int radius;

        #endregion

        #region Constructors and Destructors

        public SpellInstance(
            SpellData data,
            int startT,
            int endT,
            Vector2 start,
            Vector2 end,
            Obj_AI_Base unit,
            SpellType type)
        {
            this.Data = data;
            this.StartTick = startT;
            this.EndTick = this.StartTick + endT;
            this.Start = start;
            this.End = end;
            this.Direction = (end - start).Normalized();
            this.Unit = unit;
            this.Type = type;
            this.Radius = this.GetRadius;

            switch (this.Type)
            {
                case SpellType.Line:
                case SpellType.MissileLine:
                    this.Line = new Polygons.Line(this.Start, this.End, this.Radius);
                    break;
                case SpellType.Cone:
                case SpellType.MissileCone:
                    this.Cone = new Polygons.Cone(this.Start, this.Direction, this.Radius, data.Range);
                    break;
                case SpellType.Circle:
                    this.Circle = new Polygons.Circle(this.End, this.Radius);
                    break;
                case SpellType.Ring:
                    this.Ring = new Polygons.Ring(this.End, data.RadiusEx, this.Radius);
                    break;
                case SpellType.Arc:
                    this.Arc = new Polygons.Arc(this.Start, this.End, this.Radius);
                    break;
            }

            this.UpdatePolygon();
        }

        #endregion

        #region Public Properties

        public bool Enable
        {
            get
            {
                if (this.ForceDisabled)
                {
                    return false;
                }

                if (Utils.GameTimeTickCount - this.cachedValueTick <= 100)
                {
                    return this.cachedValue;
                }

                this.cachedValueTick = Utils.GameTimeTickCount;

                if (Configs.DodgeDangerous && !this.GetValue<bool>("IsDangerous"))
                {
                    this.cachedValue = false;

                    return this.cachedValue;
                }

                this.cachedValue = this.GetValue<bool>("Enabled");

                if (this.cachedValue)
                {
                    switch (this.Type)
                    {
                        case SpellType.Line:
                        case SpellType.MissileLine:
                            this.cachedValue = Configs.DodgeLine;
                            break;
                        case SpellType.Cone:
                        case SpellType.MissileCone:
                            this.cachedValue = Configs.DodgeCone;
                            break;
                        case SpellType.Circle:
                            this.cachedValue = !string.IsNullOrEmpty(this.Data.TrapName)
                                                   ? Configs.DodgeTrap
                                                   : Configs.DodgeCircle;
                            break;
                    }

                    if (Configs.CheckHp)
                    {
                        this.cachedValue = ObjectManager.Player.HealthPercent <= this.GetValue<Slider>("IgnoreHp").Value;
                    }

                    if (this.IsFromFoW && Configs.DodgeFoW == 1)
                    {
                        this.cachedValue = false;
                    }
                }

                return this.cachedValue;
            }
        }

        public int Radius
        {
            get
            {
                return this.radius + (!Configs.Debug ? Configs.ExtraSpellRadius : 0);
            }
            set
            {
                this.radius = value;
            }
        }

        #endregion

        #region Properties

        private int GetRadius
            =>
                this.Type == SpellType.Circle && (this.Data.HasStartExplosion || this.Data.HasEndExplosion)
                    ? this.Data.RadiusEx + (int)ObjectManager.Player.BoundingRadius
                    : this.Data.Radius;

        private bool IsGlobal => this.Data.RawRange == 25000;

        private Vector2 PredictEnd
        {
            get
            {
                if (this.PredEnd.IsValid())
                {
                    return this.PredEnd;
                }

                if (this.IsGlobal)
                {
                    return this.GetGlobalMissilePosition(0)
                           + this.Direction * this.Data.MissileSpeed
                           * (0.5f + this.Radius * 2 / ObjectManager.Player.MoveSpeed);
                }

                return this.End;
            }
        }

        #endregion

        #region Public Methods and Operators

        public void Draw(Color color)
        {
            if (!this.GetValue<bool>("Draw"))
            {
                return;
            }

            this.DrawPolygon.Draw(color);

            if (Configs.Debug && (this.Type == SpellType.Circle || this.Type == SpellType.Ring) && this.Data.Range > 0)
            {
                Render.Circle.DrawCircle(this.Start.To3D(), this.Data.Range, Color.White);
            }

            if (this.Type == SpellType.MissileLine)
            {
                var pos = Configs.Debug && this.MissileObject != null && this.MissileObject.IsValid
                          && this.MissileObject.IsVisible
                              ? this.MissileObject.Position.To2D()
                              : this.GetMissilePosition(0);
                Util.DrawLine(
                    pos + this.Radius * this.Direction.Perpendicular(),
                    pos - this.Radius * this.Direction.Perpendicular(),
                    Color.LimeGreen);
            }
        }

        public Vector2 GetGlobalMissilePosition(int time)
        {
            var t = Math.Max(0, Utils.GameTimeTickCount + time - this.StartTick - this.Data.Delay);
            t = (int)Math.Max(0, Math.Min(this.End.Distance(this.Start), t * this.Data.MissileSpeed / 1000f));

            return this.Start + this.Direction * t;
        }

        public Vector2 GetMissilePosition(int time)
        {
            if (this.Data.MissileSpeed == 0)
            {
                return this.Start;
            }

            var t = Math.Max(0, Utils.GameTimeTickCount + time - this.StartTick - this.Data.Delay);
            int x;

            if (this.Data.MissileAccel == 0)
            {
                x = t * this.Data.MissileSpeed / 1000;
            }
            else
            {
                var t1 = (this.Data.MissileAccel > 0
                              ? this.Data.MissileMaxSpeed
                              : this.Data.MissileMinSpeed - this.Data.MissileSpeed) * 1000f / this.Data.MissileAccel;
                x =
                    (int)
                    (t <= t1
                         ? t * this.Data.MissileSpeed / 1000d + 0.5d * this.Data.MissileAccel * Math.Pow(t / 1000d, 2)
                         : t1 * this.Data.MissileSpeed / 1000d + 0.5d * this.Data.MissileAccel * Math.Pow(t1 / 1000d, 2)
                           + (t - t1) / 1000d
                           * (this.Data.MissileAccel < 0 ? this.Data.MissileMaxSpeed : this.Data.MissileMinSpeed));
            }

            t = (int)Math.Max(0, Math.Min(this.PredictEnd.Distance(this.Start), x));

            return this.Start + this.Direction * t;
        }

        public T GetValue<T>(string name)
        {
            return Configs.Menu.Item("S_" + this.Data.MenuName + "_" + name).GetValue<T>();
        }

        public bool IsAboutToHit(int time, Obj_AI_Base unit)
        {
            if (this.IsSafePoint(unit.ServerPosition.To2D()))
            {
                return false;
            }

            if (this.Type == SpellType.MissileLine)
            {
                return unit.ServerPosition.To2D()
                           .Distance(this.GetMissilePosition(0), this.GetMissilePosition(time), true) < this.Radius;
            }

            return this.Data.ExtraDuration + (this.EndTick - this.StartTick)
                   - (Utils.GameTimeTickCount - this.StartTick) <= time;
        }

        public SafePath IsSafePath(List<Vector2> paths, int time, int speed, int delay)
        {
            var dist = 0f;
            time += Game.Ping / 2;
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var intersects = new List<Intersects>();

            for (var i = 0; i <= paths.Count - 2; i++)
            {
                var from = paths[i];
                var to = paths[i + 1];
                var segments = new List<Intersects>();

                /*if (this.Type == SpellType.Circle)
                {
                    foreach (var inter in this.Circle.Center.GetIntersectPointsLineCircle(this.Circle.Radius, from, to))
                    {
                        var d = inter.Distance(from);
                        segments.Add(new Intersects(d, (int)(d / speed * 1000), inter, from));
                    }
                }
                else*/
                {
                    for (var j = 0; j <= this.Polygon.Points.Count - 1; j++)
                    {
                        var inter = from.Intersection(
                            to,
                            this.Polygon.Points[j],
                            this.Polygon.Points[j == this.Polygon.Points.Count - 1 ? 0 : j + 1]);

                        if (!inter.Intersects)
                        {
                            continue;
                        }

                        var d = dist + inter.Point.Distance(from);
                        segments.Add(new Intersects(d, (int)(d / speed * 1000), inter.Point, from));
                    }

                    dist += from.Distance(to);
                }

                intersects.AddRange(segments.OrderBy(a => a.Distance));
            }

            if (this.Type == SpellType.MissileLine || this.Type == SpellType.MissileCone || this.Type == SpellType.Arc)
            {
                if (this.IsSafePoint(Evade.PlayerPosition))
                {
                    if (intersects.Count == 0)
                    {
                        return new SafePath(true, new Intersects());
                    }

                    if (this.Data.DontCross)
                    {
                        return new SafePath(false, intersects[0]);
                    }

                    for (var i = 0; i <= intersects.Count - 1; i = i + 2)
                    {
                        var enterInter = intersects[i];
                        var enterInterSegment = enterInter.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                        if (i == intersects.Count - 1)
                        {
                            return
                                new SafePath(
                                    this.End.Distance(this.GetMissilePosition(enterInter.Time - time)) + 50
                                    <= this.End.Distance(enterInterSegment)
                                    && ObjectManager.Player.MoveSpeed < this.Data.MissileSpeed,
                                    intersects[0]);
                        }

                        var exitInter = intersects[i + 1];
                        var exitInterSegment = exitInter.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                        if (this.GetMissilePosition(enterInter.Time - time).Distance(this.End) + 50
                            > enterInterSegment.Distance(this.End)
                            && this.GetMissilePosition(exitInter.Time + time).Distance(this.End)
                            <= exitInterSegment.Distance(this.End))
                        {
                            return new SafePath(false, intersects[0]);
                        }
                    }

                    return new SafePath(true, intersects[0]);
                }

                if (intersects.Count == 0)
                {
                    return new SafePath(false, new Intersects());
                }

                var exit = intersects[0];
                var exitSegment = exit.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                if (this.GetMissilePosition(exit.Time + time).Distance(this.End) <= exitSegment.Distance(this.End))
                {
                    return new SafePath(false, intersects[0]);
                }
            }

            if (intersects.Count == 0)
            {
                return new SafePath(this.IsSafePoint(Evade.PlayerPosition), new Intersects());
            }

            if (this.Data.DontCross && this.IsSafePoint(Evade.PlayerPosition))
            {
                return new SafePath(false, intersects[0]);
            }

            var endT = (this.Data.DontAddExtraDuration ? 0 : this.Data.ExtraDuration) + (this.EndTick - this.StartTick)
                       - (Utils.GameTimeTickCount - this.StartTick);

            return !this.IsSafePoint(paths.PositionAfter(endT, speed, delay))
                       ? new SafePath(false, intersects[0])
                       : new SafePath(this.IsSafePoint(paths.PositionAfter(endT, speed, time)), intersects[0]);
        }

        public bool IsSafePoint(Vector2 pos)
        {
            return this.Polygon.IsOutside(pos);
        }

        public bool IsSafeToBlink(Vector2 point, int time, int delay)
        {
            time /= 2;

            if (this.IsSafePoint(point))
            {
                return true;
            }

            if (this.Type == SpellType.MissileLine)
            {
                return point.Distance(this.GetMissilePosition(0), this.GetMissilePosition(delay + time), true)
                       < this.Radius;
            }

            return this.Data.ExtraDuration + (this.EndTick - this.StartTick)
                   - (Utils.GameTimeTickCount - this.StartTick) > time + delay;
        }

        public void OnUpdate()
        {
            if (this.Data.CollisionObjects != null && this.Data.CollisionObjects.Length > 0)
            {
                if (Utils.GameTimeTickCount - this.lastCalcColTick > 50 && Configs.CheckCollision)
                {
                    this.lastCalcColTick = Utils.GameTimeTickCount;
                    this.PredEnd = Collisions.GetCollision(this);
                }
            }
            else if (this.PredEnd.IsValid())
            {
                this.PredEnd = Vector2.Zero;
            }

            if (this.Type == SpellType.Line || this.Type == SpellType.MissileLine)
            {
                this.Line = new Polygons.Line(this.GetMissilePosition(0), this.PredictEnd, this.Radius);
                this.UpdatePolygon();
            }

            if (this.Type == SpellType.Circle && string.IsNullOrEmpty(this.Data.TrapName))
            {
                this.Circle = new Polygons.Circle(this.PredictEnd, this.Radius);
                this.UpdatePolygon();
            }

            if (!this.Unit.IsVisible)
            {
                return;
            }

            if (this.Data.MissileToUnit)
            {
                this.End = this.Unit.ServerPosition.To2D();
                this.Direction = (this.End - this.Start).Normalized();

                if (this.Type == SpellType.Ring)
                {
                    this.Ring.Center = this.End;
                    this.UpdatePolygon();
                }
            }

            if (this.Data.MissileFromUnit)
            {
                this.Start = this.Unit.ServerPosition.To2D();
                this.End = this.Start + this.Direction * this.Data.Range;
            }

            if (this.Data.MenuName == "GalioR" && !this.Unit.HasBuff("GalioIdolOfDurand")
                && Utils.GameTimeTickCount - this.StartTick > this.Data.Delay + 300)
            {
                this.EndTick = 0;
            }

            if (this.Data.MenuName == "SionR")
            {
                if (this.Unit.HasBuff("SionR"))
                {
                    this.Start = this.Unit.ServerPosition.To2D();
                    this.End = this.Start + this.Unit.Direction.To2D().Perpendicular() * this.Data.Range;
                    this.Direction = (this.End - this.Start).Normalized();
                    this.Data.MissileSpeed = (int)this.Unit.MoveSpeed;
                    this.StartTick = Utils.GameTimeTickCount;
                    this.EndTick = this.StartTick + (int)(this.Start.Distance(this.End) / this.Data.MissileSpeed * 1000);
                }
                else
                {
                    this.EndTick = Utils.GameTimeTickCount - this.StartTick > 500 ? 0 : Utils.GameTimeTickCount + 100;
                }
            }

            if (this.Data.MenuName == "MonkeyKingR")
            {
                if (this.Unit.HasBuff("MonkeyKingSpinToWin"))
                {
                    this.StartTick = Utils.GameTimeTickCount;
                    this.EndTick = this.StartTick + 10;
                }
                else
                {
                    this.EndTick = Utils.GameTimeTickCount - this.StartTick > 500 ? 0 : Utils.GameTimeTickCount + 100;
                }
            }
        }

        public void UpdatePolygon()
        {
            switch (this.Type)
            {
                case SpellType.Line:
                case SpellType.MissileLine:
                    this.Polygon = this.Line.ToPolygon();
                    this.DrawPolygon = this.Line.ToPolygon(
                        0,
                        this.Radius - (!this.Data.AddHitbox ? 0 : ObjectManager.Player.BoundingRadius));
                    this.EvadePolygon = this.Line.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingOuterPolygon = this.Line.ToPolygon(Configs.PathFindingOuterDistance);
                    this.PathFindingInnerPolygon = this.Line.ToPolygon(Configs.PathFindingInnerDistance);
                    break;
                case SpellType.Cone:
                case SpellType.MissileCone:
                    this.Polygon = this.Cone.ToPolygon();
                    this.DrawPolygon = this.Polygon;
                    this.EvadePolygon = this.Cone.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingOuterPolygon = this.Cone.ToPolygon(Configs.PathFindingOuterDistance);
                    this.PathFindingInnerPolygon = this.Cone.ToPolygon(Configs.PathFindingInnerDistance);
                    break;
                case SpellType.Circle:
                    this.Polygon = this.Circle.ToPolygon();
                    if (this.Data.HasStartExplosion || this.Data.HasEndExplosion)
                    {
                        this.DrawPolygon = this.Circle.ToPolygon(0, this.Radius - ObjectManager.Player.BoundingRadius);
                    }
                    else
                    {
                        this.DrawPolygon = this.Circle.ToPolygon(
                            0,
                            this.Radius - (!this.Data.AddHitbox ? 0 : ObjectManager.Player.BoundingRadius));
                    }
                    this.EvadePolygon = this.Circle.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingOuterPolygon = this.Circle.ToPolygon(Configs.PathFindingOuterDistance);
                    this.PathFindingInnerPolygon = this.Circle.ToPolygon(Configs.PathFindingInnerDistance);
                    break;
                case SpellType.Ring:
                    this.Polygon = this.Ring.ToPolygon();
                    this.DrawPolygon = this.Polygon;
                    this.EvadePolygon = this.Ring.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingOuterPolygon = this.Ring.ToPolygon(Configs.PathFindingOuterDistance);
                    this.PathFindingInnerPolygon = this.Ring.ToPolygon(Configs.PathFindingInnerDistance);
                    break;
                case SpellType.Arc:
                    this.Polygon = this.Arc.ToPolygon();
                    this.DrawPolygon = this.Polygon;
                    this.EvadePolygon = this.Arc.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingOuterPolygon = this.Arc.ToPolygon(Configs.PathFindingOuterDistance);
                    this.PathFindingInnerPolygon = this.Arc.ToPolygon(Configs.PathFindingInnerDistance);
                    break;
            }
        }

        #endregion
    }
}
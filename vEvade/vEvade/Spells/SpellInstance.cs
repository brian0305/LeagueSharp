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

        public FindIntersect Intersect;

        public bool IsSafe;

        #endregion

        #region Constructors and Destructors

        public SafePath(bool isSafe, FindIntersect intersect)
        {
            this.IsSafe = isSafe;
            this.Intersect = intersect;
        }

        #endregion
    }

    public struct FindIntersect
    {
        #region Fields

        public float Distance;

        public Vector2 Point;

        public int Time;

        public bool Valid;

        #endregion

        #region Constructors and Destructors

        public FindIntersect(float distance, int time, Vector2 point, Vector2 comingFrom)
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

        public Geometry.Polygon DrawingPolygon;

        public Vector2 End;

        public int EndTick;

        public Geometry.Polygon EvadePolygon;

        public bool ForceDisabled;

        public GameObject MissileObject = null;

        public Geometry.Polygon PathFindingInnerPolygon;

        public Geometry.Polygon PathFindingPolygon;

        public Geometry.Polygon Polygon;

        public int Radius;

        public Polygons.Line Rectangle;

        public Polygons.Ring Ring;

        public int SpellId;

        public Vector2 Start;

        public int StartTick;

        public GameObject ToggleObject = null;

        public SpellType Type;

        public Obj_AI_Base Unit;

        private bool cachedValue;

        private int cachedValueTick;

        private Vector2 collisionEnd;

        private int lastCollisionCalc;

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
                case SpellType.Circle:
                    this.Circle = new Polygons.Circle(this.CollisionEnd, this.Radius);
                    break;
                case SpellType.Line:
                    this.Rectangle = new Polygons.Line(this.Start, this.CollisionEnd, this.Radius);
                    break;
                case SpellType.MissileLine:
                    this.Rectangle = new Polygons.Line(this.Start, this.CollisionEnd, this.Radius);
                    break;
                case SpellType.Cone:
                    this.Cone = new Polygons.Cone(
                        start,
                        (this.CollisionEnd - start).Normalized(),
                        this.Radius,
                        data.Range);
                    break;
                case SpellType.Ring:
                    this.Ring = new Polygons.Ring(this.CollisionEnd, this.Radius, data.RadiusEx);
                    break;
                case SpellType.Arc:
                    this.Arc = new Polygons.Arc(
                        start,
                        end,
                        Configs.SpellExtraRadius + (int)ObjectManager.Player.BoundingRadius);
                    break;
            }

            this.UpdatePolygon();
        }

        #endregion

        #region Public Properties

        public Vector2 CollisionEnd
        {
            get
            {
                if (this.collisionEnd.IsValid())
                {
                    return this.collisionEnd;
                }

                if (this.IsGlobal)
                {
                    return this.GlobalGetMissilePosition(0)
                           + this.Direction * this.Data.MissileSpeed
                           * (0.5f + this.Radius * 2 / ObjectManager.Player.MoveSpeed);
                }

                return this.End;
            }
        }

        public bool Enable
        {
            get
            {
                if (this.ForceDisabled)
                {
                    return false;
                }

                if (Utils.GameTimeTickCount - this.cachedValueTick < 100)
                {
                    return this.cachedValue;
                }

                if (!this.GetValue<bool>("IsDangerous")
                    && Configs.Menu.Item("DodgeDangerous").GetValue<KeyBind>().Active)
                {
                    this.cachedValue = false;
                    this.cachedValueTick = Utils.GameTimeTickCount;

                    return this.cachedValue;
                }

                this.cachedValue = this.GetValue<bool>("Enabled");
                this.cachedValueTick = Utils.GameTimeTickCount;

                switch (this.Type)
                {
                    case SpellType.Line:
                    case SpellType.MissileLine:
                        if (!Configs.Menu.Item("DodgeLine").GetValue<bool>())
                        {
                            this.cachedValue = false;
                        }
                        break;
                    case SpellType.Circle:
                        if (!Configs.Menu.Item("DodgeCircle").GetValue<bool>())
                        {
                            this.cachedValue = false;
                        }
                        break;
                    case SpellType.Cone:
                    case SpellType.MissileCone:
                        if (!Configs.Menu.Item("DodgeCone").GetValue<bool>())
                        {
                            this.cachedValue = false;
                        }
                        break;
                }

                if (Configs.Menu.Item("CheckHp").GetValue<bool>()
                    && ObjectManager.Player.HealthPercent >= this.GetValue<Slider>("IgnoreHp").Value)
                {
                    this.cachedValue = false;
                }

                return this.cachedValue;
            }
        }

        #endregion

        #region Properties

        private int GetRadius
            =>
                this.Type == SpellType.Circle
                && (this.Data.HasStartExplosion || this.Data.HasEndExplosion || this.Data.UseEndPosition)
                    ? this.Data.RadiusEx
                    : this.Data.Radius;

        private bool IsGlobal => this.Data.RawRange == 25000;

        #endregion

        #region Public Methods and Operators

        public void Draw(Color color, Color missileColor)
        {
            if (!this.GetValue<bool>("Draw"))
            {
                return;
            }

            this.DrawingPolygon.Draw(color);

            if (this.Data.Type == SpellType.MissileLine && this.MissileObject != null && this.MissileObject.IsVisible)
            {
                var position = this.MissileObject.Position.To2D();
                Util.DrawLine(
                    (position + this.Radius * this.Direction.Perpendicular()).To3D(),
                    (position - this.Radius * this.Direction.Perpendicular()).To3D(),
                    missileColor);
            }
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

            t = (int)Math.Max(0, Math.Min(this.CollisionEnd.Distance(this.Start), x));

            return this.Start + this.Direction * t;
        }

        public T GetValue<T>(string name)
        {
            return Configs.Menu.Item("S_" + this.Data.MenuName + "_" + name).GetValue<T>();
        }

        public Vector2 GlobalGetMissilePosition(int time)
        {
            var t = Math.Max(0, Utils.GameTimeTickCount + time - this.StartTick - this.Data.Delay);
            t = (int)Math.Max(0, Math.Min(this.End.Distance(this.Start), t * this.Data.MissileSpeed / 1000f));

            return this.Start + this.Direction * t;
        }

        public bool IsAboutToHit(int time, Obj_AI_Base unit)
        {
            if (this.Type == SpellType.MissileLine
                || (this.Type == SpellType.Circle && this.Data.SpellName.EndsWith("_EndExp")))
            {
                var missilePos = this.GetMissilePosition(0);
                var missilePosAfterT = this.GetMissilePosition(time);

                return unit.ServerPosition.To2D().Distance(missilePos, missilePosAfterT, true) < this.Radius;
            }

            if (!this.IsSafe(unit.ServerPosition.To2D()))
            {
                var timeToExplode = this.Data.ExtraDuration + (this.EndTick - this.StartTick)
                                    - (Utils.GameTimeTickCount - this.StartTick);

                if (timeToExplode <= time)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsDanger(Vector2 point)
        {
            return !this.IsSafe(point);
        }

        public bool IsSafe(Vector2 point)
        {
            return this.Polygon.IsOutside(point);
        }

        public SafePath IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var dist = 0f;
            timeOffset += Game.Ping / 2;
            speed = speed == -1 ? (int)ObjectManager.Player.MoveSpeed : speed;
            var allIntersects = new List<FindIntersect>();

            for (var i = 0; i <= path.Count - 2; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var segmentIntersects = new List<FindIntersect>();

                for (var j = 0; j <= this.Polygon.Points.Count - 1; j++)
                {
                    var sideStart = this.Polygon.Points[j];
                    var sideEnd = this.Polygon.Points[j == this.Polygon.Points.Count - 1 ? 0 : j + 1];
                    var intersect = from.Intersection(to, sideStart, sideEnd);

                    if (intersect.Intersects)
                    {
                        segmentIntersects.Add(
                            new FindIntersect(
                                dist + intersect.Point.Distance(from),
                                (int)((dist + intersect.Point.Distance(from)) * 1000 / speed),
                                intersect.Point,
                                from));
                    }
                }

                allIntersects.AddRange(segmentIntersects.OrderBy(a => a.Distance));
                dist += from.Distance(to);
            }

            if (this.Type == SpellType.MissileLine || this.Type == SpellType.MissileCone || this.Type == SpellType.Arc)
            {
                if (this.IsSafe(Evade.PlayerPosition))
                {
                    if (allIntersects.Count == 0)
                    {
                        return new SafePath(true, new FindIntersect());
                    }

                    if (this.Data.DontCross)
                    {
                        return new SafePath(false, allIntersects[0]);
                    }

                    for (var i = 0; i <= allIntersects.Count - 1; i = i + 2)
                    {
                        var enterIntersect = allIntersects[i];
                        var enterIntersectPoint = enterIntersect.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                        if (i == allIntersects.Count - 1)
                        {
                            return
                                new SafePath(
                                    (this.End.Distance(this.GetMissilePosition(enterIntersect.Time - timeOffset)) + 50
                                     <= this.End.Distance(enterIntersectPoint))
                                    && ObjectManager.Player.MoveSpeed < this.Data.MissileSpeed,
                                    allIntersects[0]);
                        }

                        var exitIntersect = allIntersects[i + 1];
                        var exitIntersectPoint = exitIntersect.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                        if (this.GetMissilePosition(enterIntersect.Time - timeOffset).Distance(this.End) + 50
                            > enterIntersectPoint.Distance(this.End)
                            && this.GetMissilePosition(exitIntersect.Time + timeOffset).Distance(this.End)
                            <= exitIntersectPoint.Distance(this.End))
                        {
                            return new SafePath(false, allIntersects[0]);
                        }
                    }

                    return new SafePath(true, allIntersects[0]);
                }

                if (allIntersects.Count == 0)
                {
                    return new SafePath(false, new FindIntersect());
                }

                if (allIntersects.Count > 0)
                {
                    var exitIntersect = allIntersects[0];
                    var exitIntersectPoint = exitIntersect.Point.ProjectOn(this.Start, this.End).SegmentPoint;

                    if (this.GetMissilePosition(exitIntersect.Time + timeOffset).Distance(this.End)
                        <= exitIntersectPoint.Distance(this.End))
                    {
                        return new SafePath(false, allIntersects[0]);
                    }
                }
            }

            if (this.IsSafe(Evade.PlayerPosition))
            {
                if (allIntersects.Count == 0)
                {
                    return new SafePath(true, new FindIntersect());
                }

                if (this.Data.DontCross)
                {
                    return new SafePath(false, allIntersects[0]);
                }
            }
            else if (allIntersects.Count == 0)
            {
                return new SafePath(false, new FindIntersect());
            }

            var timeToExplode = (this.Data.DontAddExtraDuration ? 0 : this.Data.ExtraDuration)
                                + (this.EndTick - this.StartTick) - (Utils.GameTimeTickCount - this.StartTick);

            return !this.IsSafe(path.PositionAfter(timeToExplode, speed, delay))
                       ? new SafePath(false, allIntersects[0])
                       : new SafePath(
                             this.IsSafe(path.PositionAfter(timeToExplode, speed, timeOffset)),
                             allIntersects[0]);
        }

        public bool IsSafeToBlink(Vector2 point, int timeOffset, int delay = 0)
        {
            timeOffset /= 2;

            if (this.IsSafe(Evade.PlayerPosition))
            {
                return true;
            }

            if (this.Type == SpellType.MissileLine)
            {
                var missilePositionAfterBlink = this.GetMissilePosition(delay + timeOffset);
                var myPositionProjection = Evade.PlayerPosition.ProjectOn(this.Start, this.End);

                return missilePositionAfterBlink.Distance(this.End)
                       >= myPositionProjection.SegmentPoint.Distance(this.End);
            }

            var timeToExplode = this.Data.ExtraDuration + (this.EndTick - this.StartTick)
                                - (Utils.GameTimeTickCount - this.StartTick);

            return timeToExplode > timeOffset + delay;
        }

        public void OnUpdate()
        {
            if (this.Data.CollisionObjects != null && this.Data.CollisionObjects.Length > 0
                && Utils.GameTimeTickCount - this.lastCollisionCalc > 50
                && Configs.Menu.Item("CheckCollision").GetValue<bool>())
            {
                this.lastCollisionCalc = Utils.GameTimeTickCount;
                this.collisionEnd = Collisions.GetCollision(this);
            }

            if (this.Type == SpellType.Line || this.Type == SpellType.MissileLine)
            {
                this.Rectangle = new Polygons.Line(this.GetMissilePosition(0), this.CollisionEnd, this.Radius);
                this.UpdatePolygon();
            }

            if (this.Type == SpellType.Circle && this.Data.SpellName.EndsWith("_EndExp"))
            {
                this.Circle = new Polygons.Circle(this.GetMissilePosition(0), this.Radius);
                this.UpdatePolygon();
            }

            if (!this.Unit.IsVisible)
            {
                return;
            }

            if (this.Data.MissileToUnit)
            {
                this.End = this.Unit.ServerPosition.To2D();

                if (this.Type == SpellType.Circle)
                {
                    this.Start = this.End;
                }

                if (this.Type == SpellType.Ring)
                {
                    this.Ring.Center = this.End;
                }

                this.Direction = (this.End - this.Start).Normalized();
                this.UpdatePolygon();
            }

            if (this.Data.MissileFromUnit)
            {
                this.Start = this.Unit.ServerPosition.To2D();
                this.End = this.Start + this.Direction * this.Data.Range;
                this.Rectangle = new Polygons.Line(this.Start, this.CollisionEnd, this.Radius);
                this.UpdatePolygon();
            }

            if (this.Data.MenuName == "SionR" && this.Unit.HasBuff("SionR"))
            {
                this.Start = this.Unit.ServerPosition.To2D();
                this.End = this.Start + this.Unit.Direction.To2D().Perpendicular() * this.Data.Range;
                this.Direction = (this.End - this.Start).Normalized();
                this.Data.MissileSpeed = (int)this.Unit.MoveSpeed;
                this.StartTick = Utils.GameTimeTickCount;
                this.EndTick = this.StartTick + (int)(this.Start.Distance(this.End) / this.Data.MissileSpeed * 1000);
                this.UpdatePolygon();
            }

            if (this.Data.MenuName == "MonkeyKingR")
            {
                if (this.Unit.HasBuff("MonkeyKingSpinToWin"))
                {
                    this.EndTick = Utils.GameTimeTickCount + 10;
                    this.Start = this.End = this.Circle.Center = this.Unit.ServerPosition.To2D();
                    this.Direction = (this.End - this.Start).Normalized();
                    this.UpdatePolygon();
                }
                else
                {
                    this.EndTick = Utils.GameTimeTickCount - this.StartTick > 500 ? 0 : Utils.GameTimeTickCount + 10;
                }
            }
        }

        public void UpdatePolygon()
        {
            switch (this.Type)
            {
                case SpellType.Circle:
                    this.Polygon = this.Circle.ToPolygon();
                    this.EvadePolygon = this.Circle.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Circle.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Circle.ToPolygon(Configs.PathFindingDistance2);
                    this.DrawingPolygon = this.Circle.ToPolygon(
                        0,
                        this.Radius - (!this.Data.AddHitbox ? 0 : ObjectManager.Player.BoundingRadius));
                    break;
                case SpellType.Line:
                    this.Polygon = this.Rectangle.ToPolygon();
                    this.DrawingPolygon = this.Rectangle.ToPolygon(
                        0,
                        this.Radius - (!this.Data.AddHitbox ? 0 : ObjectManager.Player.BoundingRadius));
                    this.EvadePolygon = this.Rectangle.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Rectangle.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Rectangle.ToPolygon(Configs.PathFindingDistance2);
                    break;
                case SpellType.MissileLine:
                    this.Polygon = this.Rectangle.ToPolygon();
                    this.DrawingPolygon = this.Rectangle.ToPolygon(
                        0,
                        this.Radius - (!this.Data.AddHitbox ? 0 : ObjectManager.Player.BoundingRadius));
                    this.EvadePolygon = this.Rectangle.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Rectangle.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Rectangle.ToPolygon(Configs.PathFindingDistance2);
                    break;
                case SpellType.Cone:
                    this.Polygon = this.Cone.ToPolygon();
                    this.DrawingPolygon = this.Polygon;
                    this.EvadePolygon = this.Cone.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Cone.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Cone.ToPolygon(Configs.PathFindingDistance2);
                    break;
                case SpellType.Ring:
                    this.Polygon = this.Ring.ToPolygon();
                    this.DrawingPolygon = this.Polygon;
                    this.EvadePolygon = this.Ring.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Ring.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Ring.ToPolygon(Configs.PathFindingDistance2);
                    break;
                case SpellType.Arc:
                    this.Polygon = this.Arc.ToPolygon();
                    this.DrawingPolygon = this.Polygon;
                    this.EvadePolygon = this.Arc.ToPolygon(Configs.ExtraEvadeDistance);
                    this.PathFindingPolygon = this.Arc.ToPolygon(Configs.PathFindingDistance);
                    this.PathFindingInnerPolygon = this.Arc.ToPolygon(Configs.PathFindingDistance2);
                    break;
            }
        }

        #endregion
    }
}
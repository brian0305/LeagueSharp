namespace Valvrave_Sharp.Core
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    #endregion

    internal static class Prediction
    {
        #region Public Methods and Operators

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed });
        }

        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return input.GetPrediction(true, true);
        }

        #endregion

        #region Methods

        private static double GetAngle(this Vector2 from, Vector2 to, Vector2 wayPoint)
        {
            if (to == wayPoint)
            {
                return 60;
            }
            var a = Math.Pow(wayPoint.X - from.X, 2) + Math.Pow(wayPoint.Y - from.Y, 2);
            var b = Math.Pow(from.X - to.X, 2) + Math.Pow(from.Y - to.Y, 2);
            var c = Math.Pow(wayPoint.X - to.X, 2) + Math.Pow(wayPoint.Y - to.Y, 2);
            return Math.Cos((a + b - c) / (2 * Math.Sqrt(a) * Math.Sqrt(b))) * 180 / Math.PI;
        }

        private static PredictionOutput GetDashingPrediction(this PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            if (!dashData.IsBlink)
            {
                var endP = dashData.EndPos;
                var dashPred = input.GetPositionOnPath(
                    new List<Vector2> { input.Unit.ServerPosition.ToVector2(), endP },
                    dashData.Speed);
                if (dashPred.Hitchance >= HitChance.High
                    && dashPred.UnitPosition.ToVector2().Distance(input.Unit.Position.ToVector2(), endP, true) < 200)
                {
                    dashPred.CastPosition = dashPred.UnitPosition;
                    dashPred.Hitchance = HitChance.Dashing;
                    return dashPred;
                }
                if (dashData.Path.PathLength() > 200)
                {
                    var timeToPoint = input.Delay / 2f + input.From.Distance(endP) / input.Speed - 0.25f;
                    if (timeToPoint
                        <= input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        result.Hitchance = HitChance.Dashing;
                    }
                }
                dashPred.CastPosition = endP.ToVector3();
                dashPred.UnitPosition = dashPred.CastPosition;
            }
            return result;
        }

        private static HitChance GetHitChance(this PredictionInput input)
        {
            var hero = input.Unit as Obj_AI_Hero;
            if (hero == null || !hero.IsValid || input.Radius <= 1f)
            {
                return HitChance.VeryHigh;
            }
            var isTrack = UnitTracker.CanTrack(hero);
            if (hero.IsCastingInterruptableSpell(true) || hero.IsRecalling()
                || (isTrack && UnitTracker.GetLastStop(hero) < 0.1 && hero.IsRooted))
            {
                return HitChance.VeryHigh;
            }
            /*if (hero.Path.Length > 0 != hero.IsMoving)
            {
                return HitChance.Medium;
            }*/
            var wayPoint = input.Unit.GetWaypoints().Last();
            var distUnitToWay = hero.Distance(wayPoint);
            var distUnitToFrom = hero.Distance(input.From);
            var distFromToWay = input.From.Distance(wayPoint);
            var delay = input.Delay
                        + (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon ? 0 : distUnitToFrom / input.Speed);
            var moveArea = hero.MoveSpeed * delay;
            var fixRange = moveArea * 0.4f;
            var minPath = 900 + moveArea;
            var moveAngle = 31d;
            if (input.Radius > 70)
            {
                moveAngle++;
            }
            else if (input.Radius <= 60)
            {
                moveAngle--;
            }
            if (input.Delay < 0.3)
            {
                moveAngle++;
            }
            if (GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1d)
            {
                fixRange = moveArea * 0.3f;
                minPath = 700 + moveArea;
                moveAngle += 1.5;
            }
            if (input.Type == SkillshotType.SkillshotCircle)
            {
                fixRange -= input.Radius / 2;
            }
            if (distFromToWay <= distUnitToFrom)
            {
                /*if (distUnitToFrom > input.Range - fixRange)
                {
                    return HitChance.Medium;
                }*/
            }
            else if (distUnitToWay > 350)
            {
                moveAngle += 1.5;
            }
            if (isTrack)
            {
                /*if (UnitTracker.IsSpamClick(hero))
                {
                    return distUnitToFrom < input.Range - fixRange ? HitChance.VeryHigh : HitChance.Medium;
                }*/
                if (UnitTracker.IsSpamPos(hero))
                {
                    return HitChance.VeryHigh;
                }
            }
            if (!hero.IsMoving)
            {
                if (hero.IsWindingUp)
                {
                    return isTrack && (UnitTracker.GetLastAttack(hero) < 0.1 || UnitTracker.GetLastStop(hero) < 0.1)
                           && delay < 0.6
                               ? HitChance.VeryHigh
                               : HitChance.High;
                }
                return isTrack && UnitTracker.GetLastStop(hero) < 0.5 ? HitChance.High : HitChance.VeryHigh;
            }
            if (distUnitToFrom < 250 || hero.MoveSpeed < 250 || distFromToWay < 250)
            {
                return HitChance.VeryHigh;
            }
            if (distUnitToWay > minPath)
            {
                return HitChance.VeryHigh;
            }
            if (hero.HealthPercent < 20 || Program.Player.HealthPercent < 20)
            {
                return HitChance.VeryHigh;
            }
            if (input.From.ToVector2().GetAngle(hero.ServerPosition.ToVector2(), wayPoint) < moveAngle
                && distUnitToWay > 260)
            {
                return HitChance.VeryHigh;
            }
            if (input.Type == SkillshotType.SkillshotCircle
                && GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1d && distUnitToWay > fixRange)
            {
                return HitChance.VeryHigh;
            }
            return HitChance.High;
        }

        private static PredictionOutput GetImmobilePrediction(this PredictionInput input, double remainingImmobileT)
        {
            var result = new PredictionOutput
                             {
                                 Input = input, CastPosition = input.Unit.ServerPosition,
                                 UnitPosition = input.Unit.Position, Hitchance = HitChance.Immobile
                             };
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;
            if (timeToReachTargetPosition > remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                result.UnitPosition = input.Unit.ServerPosition;
                result.Hitchance = HitChance.High;
            }
            return result;
        }

        private static PredictionOutput GetPositionOnPath(
            this PredictionInput input,
            List<Vector2> path,
            float speed = -1)
        {
            if (path.Count <= 1)
            {
                return new PredictionOutput
                           {
                               Input = input, CastPosition = input.Unit.ServerPosition,
                               UnitPosition = input.Unit.ServerPosition, Hitchance = HitChance.VeryHigh
                           };
            }
            speed = Math.Abs(speed - -1) < float.Epsilon ? input.Unit.MoveSpeed : speed;
            var pLength = path.PathLength();
            if (pLength >= input.Delay * speed - input.RealRadius
                && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var tDistance = input.Delay * speed - input.RealRadius;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.Distance(b);
                    if (d >= tDistance)
                    {
                        var direction = (b - a).Normalized();
                        var cp = a + direction * tDistance;
                        var p = a
                                + direction
                                * (i == path.Count - 2
                                       ? Math.Min(tDistance + input.RealRadius, d)
                                       : tDistance + input.RealRadius);
                        return new PredictionOutput
                                   {
                                       Input = input, CastPosition = cp.ToVector3(), UnitPosition = p.ToVector3(),
                                       Hitchance =
                                           GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1d
                                               ? HitChance.VeryHigh
                                               : HitChance.High
                                   };
                    }
                    tDistance -= d;
                }
            }
            if (pLength >= input.Delay * speed - input.RealRadius
                && Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = input.Delay * speed - input.RealRadius;
                if ((input.Type == SkillshotType.SkillshotLine || input.Type == SkillshotType.SkillshotCone)
                    && input.Unit.DistanceSquared(input.From) < 200 * 200)
                {
                    d -= input.RealRadius;
                }
                path = path.CutPath(d);
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.Distance(b) / speed;
                    var direction = (b - a).Normalized();
                    a = a - speed * tT * direction;
                    var sol = a.VectorMovementCollision(b, speed, input.From.ToVector2(), input.Speed, tT);
                    var t = (float)sol[0];
                    var pos = (Vector2)sol[1];
                    if (pos.IsValid() && t >= tT && t <= tT + tB)
                    {
                        if (pos.DistanceSquared(b) < 20)
                        {
                            break;
                        }
                        var p = pos + input.RealRadius * direction;
                        /*if (input.Type == SkillshotType.SkillshotLine)
                        {
                            var alpha = (input.From - p).AngleBetween(a - b);
                            if (alpha > 30 && alpha < 180 - 30)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / p.Distance(input.From));
                                var cp1 = input.From + (p - input.From).Rotated(beta);
                                var cp2 = input.From + (p - input.From).Rotated(-beta);
                                pos = cp1.DistanceSquared(pos) < cp2.DistanceSquared(pos) ? cp1 : cp2;
                            }
                        }*/
                        return new PredictionOutput
                                   {
                                       Input = input, CastPosition = pos.ToVector3(), UnitPosition = p.ToVector3(),
                                       Hitchance =
                                           GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1d
                                               ? HitChance.VeryHigh
                                               : HitChance.High
                                   };
                    }
                    tT += tB;
                }
            }
            var position = path.Last().ToVector3();
            return new PredictionOutput
                       { Input = input, CastPosition = position, UnitPosition = position, Hitchance = HitChance.Medium };
        }

        private static PredictionOutput GetPrediction(this PredictionInput input, bool ft, bool checkCollision)
        {
            if (!input.Unit.IsValidTarget(float.MaxValue, false))
            {
                return new PredictionOutput();
            }
            if (ft)
            {
                input.Delay += Game.Ping / 2000f + 0.06f;
                if (input.AoE)
                {
                    return Cluster.GetAoEPrediction(input);
                }
            }
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon
                && input.Unit.DistanceSquared(input.RangeCheckFrom) > Math.Pow(input.Range * 1.5, 2))
            {
                return new PredictionOutput { Input = input, Hitchance = HitChance.OutOfRange };
            }
            PredictionOutput result = null;
            if (input.Unit.IsDashing())
            {
                result = input.GetDashingPrediction();
            }
            else
            {
                var remainingImmobileT = input.Unit.IsImmobileUntil();
                if (remainingImmobileT >= 0d)
                {
                    result = input.GetImmobilePrediction(remainingImmobileT);
                }
            }
            if (result == null)
            {
                result = input.GetStandardPrediction();
            }
            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.Hitchance >= HitChance.High
                    && input.RangeCheckFrom.DistanceSquared(input.Unit.Position)
                    > Math.Pow(input.Range + input.RealRadius * 3 / 4, 2))
                {
                    result.Hitchance = HitChance.Medium;
                }
                if (input.RangeCheckFrom.DistanceSquared(result.UnitPosition)
                    > Math.Pow(input.Range + (input.Type == SkillshotType.SkillshotCircle ? input.RealRadius : 0), 2))
                {
                    result.Hitchance = HitChance.OutOfRange;
                }
                if (input.RangeCheckFrom.DistanceSquared(result.CastPosition) > Math.Pow(input.Range, 2))
                {
                    if (result.Hitchance != HitChance.OutOfRange)
                    {
                        result.CastPosition = input.RangeCheckFrom.Extend(result.UnitPosition, input.Range);
                    }
                    else
                    {
                        result.Hitchance = HitChance.OutOfRange;
                    }
                }
            }
            if (result.Hitchance == HitChance.High)
            {
                result.Hitchance = input.GetHitChance();
            }
            if (checkCollision && input.Collision && result.Hitchance > HitChance.Impossible)
            {
                var originalUnit = input.Unit;
                result.CollisionObjects = Collisions.GetCollision(result.UnitPosition, input);
                result.CollisionObjects.RemoveAll(i => i.Compare(originalUnit));
                if (result.CollisionObjects.Count > 0)
                {
                    result.Hitchance = HitChance.Collision;
                }
            }
            return result;
        }

        private static PredictionOutput GetStandardPrediction(this PredictionInput input)
        {
            var speed = input.Unit.MoveSpeed;
            if (input.Unit.DistanceSquared(input.From) < 200 * 200)
            {
                speed /= 1.5f;
            }
            /*var hero = input.Unit as Obj_AI_Hero;
            if (hero != null && UnitTracker.CanTrack(hero) && UnitTracker.IsSpamClick(hero))
            {
                return input.GetPositionOnPath(UnitTracker.GetWaypoints(hero), speed);
            }*/
            return input.GetPositionOnPath(input.Unit.GetWaypoints(), speed);
        }

        private static double IsImmobileUntil(this Obj_AI_Base unit)
        {
            return
                unit.Buffs.Where(
                    i =>
                    i.IsValid
                    && (i.Type == BuffType.Charm || i.Type == BuffType.Knockup || i.Type == BuffType.Stun
                        || i.Type == BuffType.Suppression || i.Type == BuffType.Snare))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime)) - Game.Time;
        }

        #endregion

        internal static class Collisions
        {
            #region Static Fields

            private static Vector2 yasuoWallCastPos;

            private static int yasuoWallCastT;

            #endregion

            #region Constructors and Destructors

            static Collisions()
            {
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsValid() || sender.Team == Program.Player.Team
                            || args.SData.Name != "YasuoWMovingWall")
                        {
                            return;
                        }
                        yasuoWallCastT = Variables.TickCount;
                        yasuoWallCastPos = sender.ServerPosition.ToVector2();
                    };
            }

            #endregion

            #region Methods

            internal static List<Obj_AI_Base> GetCollision(Vector3 pos, PredictionInput input)
            {
                return GetCollision(pos.ToVector2(), input);
            }

            internal static List<Obj_AI_Base> GetCollision(Vector2 pos, PredictionInput input)
            {
                var result = new List<Obj_AI_Base>();
                foreach (var colType in input.CollisionObjects)
                {
                    switch (colType)
                    {
                        case CollisionableObjects.Minions:
                            GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet())
                                .Concat(GameObjects.Jungle)
                                .Where(
                                    i =>
                                    i.IsValidTarget(
                                        Math.Min(input.Range + input.Radius + 100, 2000),
                                        true,
                                        input.RangeCheckFrom))
                                .ForEach(
                                    i =>
                                        {
                                            input.Unit = i;
                                            var posPred = input.GetPrediction(false, false).UnitPosition.ToVector2();
                                            if (posPred.Distance(input.From) < input.Radius)
                                            {
                                                result.Add(i);
                                            }
                                            else if (posPred.DistanceSquared(input.From.ToVector2(), pos, true)
                                                     <= Math.Pow(input.RealRadius + 20, 2))
                                            {
                                                result.Add(i);
                                            }
                                        });
                            break;
                        case CollisionableObjects.Heroes:
                            GameObjects.EnemyHeroes.Where(
                                i =>
                                i.IsValidTarget(
                                    Math.Min(input.Range + input.Radius + 100, 2000),
                                    true,
                                    input.RangeCheckFrom)).ForEach(
                                        i =>
                                            {
                                                input.Unit = i;
                                                var posPred = input.GetPrediction(false, false).UnitPosition.ToVector2();
                                                if (posPred.DistanceSquared(input.From.ToVector2(), pos, true)
                                                    <= Math.Pow(input.RealRadius + 50, 2))
                                                {
                                                    result.Add(i);
                                                }
                                            });
                            break;
                        case CollisionableObjects.Walls:
                            var step = pos.Distance(input.From) / 20;
                            for (var i = 0; i < 20; i++)
                            {
                                if (input.From.ToVector2().Extend(pos, step * i).IsWall())
                                {
                                    result.Add(Program.Player);
                                }
                            }
                            break;
                        case CollisionableObjects.YasuoWall:
                            if (Variables.TickCount - yasuoWallCastT <= 4000)
                            {
                                var wall =
                                    GameObjects.AllGameObjects.FirstOrDefault(
                                        i =>
                                        i.IsValid
                                        && Regex.IsMatch(i.Name, "_w_windwall_enemy_0.\\.troy", RegexOptions.IgnoreCase));
                                if (wall != null)
                                {
                                    var wallWidth = 300
                                                    + 50 * Convert.ToInt32(wall.Name.Substring(wall.Name.Length - 6, 1));
                                    var wallDirection =
                                        (wall.Position.ToVector2() - yasuoWallCastPos).Normalized().Perpendicular();
                                    var wallStart = wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                                    var wallEnd = wallStart - wallWidth * wallDirection;
                                    var wallInter = wallStart.Intersection(wallEnd, pos, input.From.ToVector2());
                                    if (wallInter.Intersects)
                                    {
                                        var t = Variables.TickCount
                                                + (wallInter.Point.Distance(input.From) / input.Speed + input.Delay)
                                                * 1000;
                                        if (t < yasuoWallCastT + 4000)
                                        {
                                            result.Add(Program.Player);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
                return result.Distinct().ToList();
            }

            #endregion
        }

        private static class Cluster
        {
            #region Methods

            internal static PredictionOutput GetAoEPrediction(PredictionInput input)
            {
                switch (input.Type)
                {
                    case SkillshotType.SkillshotCircle:
                        return Circle.GetCirclePrediction(input);
                    case SkillshotType.SkillshotCone:
                        return Cone.GetConePrediction(input);
                    case SkillshotType.SkillshotLine:
                        return Line.GetLinePrediction(input);
                }
                return new PredictionOutput();
            }

            private static List<PossibleTarget> GetPossibleTargets(PredictionInput input)
            {
                var result = new List<PossibleTarget>();
                var originalUnit = input.Unit;
                GameObjects.EnemyHeroes.Where(
                    i =>
                    !i.Compare(originalUnit)
                    && i.IsValidTarget(input.Range + 200 + input.RealRadius, true, input.RangeCheckFrom)).ForEach(
                        i =>
                            {
                                input.Unit = i;
                                var pred = input.GetPrediction(false, false);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    result.Add(
                                        new PossibleTarget { Position = pred.UnitPosition.ToVector2(), Unit = i });
                                }
                            });
                return result;
            }

            #endregion

            private static class Circle
            {
                #region Methods

                internal static PredictionOutput GetCirclePrediction(PredictionInput input)
                {
                    var mainTargetPrediction = input.GetPrediction(false, true);
                    var posibleTargets = new List<PossibleTarget>
                                             {
                                                 new PossibleTarget
                                                     {
                                                         Position = mainTargetPrediction.UnitPosition.ToVector2(),
                                                         Unit = input.Unit
                                                     }
                                             };
                    if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                    {
                        posibleTargets.AddRange(GetPossibleTargets(input));
                    }
                    while (posibleTargets.Count > 1)
                    {
                        var mecCircle = ConvexHull.GetMec(posibleTargets.Select(i => i.Position).ToList());
                        if (mecCircle.Radius <= input.RealRadius - 10
                            && mecCircle.Center.DistanceSquared(input.RangeCheckFrom) < input.Range * input.Range)
                        {
                            return new PredictionOutput
                                       {
                                           Input = input, CastPosition = mecCircle.Center.ToVector3(),
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           Hitchance = mainTargetPrediction.Hitchance, AoeHitCount = posibleTargets.Count,
                                           AoeTargetsHit = posibleTargets.Select(i => (Obj_AI_Hero)i.Unit).ToList()
                                       };
                        }
                        var maxdist = -1f;
                        var maxdistindex = 1;
                        for (var i = 1; i < posibleTargets.Count; i++)
                        {
                            var distance = posibleTargets[i].Position.DistanceSquared(posibleTargets[0].Position);
                            if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                            {
                                maxdistindex = i;
                                maxdist = distance;
                            }
                        }
                        posibleTargets.RemoveAt(maxdistindex);
                    }
                    return mainTargetPrediction;
                }

                #endregion
            }

            private static class Cone
            {
                #region Methods

                internal static PredictionOutput GetConePrediction(PredictionInput input)
                {
                    var mainTargetPrediction = input.GetPrediction(false, true);
                    var posibleTargets = new List<PossibleTarget>
                                             {
                                                 new PossibleTarget
                                                     {
                                                         Position = mainTargetPrediction.UnitPosition.ToVector2(),
                                                         Unit = input.Unit
                                                     }
                                             };
                    if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                    {
                        posibleTargets.AddRange(GetPossibleTargets(input));
                    }
                    if (posibleTargets.Count > 1)
                    {
                        var candidates = new List<Vector2>();
                        posibleTargets.ForEach(i => i.Position -= input.From.ToVector2());
                        for (var i = 0; i < posibleTargets.Count; i++)
                        {
                            for (var j = 0; j < posibleTargets.Count; j++)
                            {
                                if (i == j)
                                {
                                    continue;
                                }
                                var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
                                if (!candidates.Contains(p))
                                {
                                    candidates.Add(p);
                                }
                            }
                        }
                        var bestCandidateHits = -1;
                        var bestCandidate = new Vector2();
                        var positionsList = posibleTargets.Select(i => i.Position).ToList();
                        candidates.ForEach(
                            i =>
                                {
                                    var hits = GetHits(i, input.Range, input.Radius, positionsList);
                                    if (hits > bestCandidateHits)
                                    {
                                        bestCandidate = i;
                                        bestCandidateHits = hits;
                                    }
                                });
                        if (bestCandidateHits > 1 && input.From.DistanceSquared(bestCandidate) > 50 * 50)
                        {
                            return new PredictionOutput
                                       {
                                           Input = input, CastPosition = bestCandidate.ToVector3(),
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           Hitchance = mainTargetPrediction.Hitchance, AoeHitCount = bestCandidateHits
                                       };
                        }
                    }
                    return mainTargetPrediction;
                }

                private static int GetHits(Vector2 end, double range, float angle, List<Vector2> points)
                {
                    return (from point in points
                            let edge1 = end.Rotated(-angle / 2)
                            let edge2 = edge1.Rotated(angle)
                            where
                                point.DistanceSquared(new Vector2()) < range * range && edge1.CrossProduct(point) > 0
                                && edge2.CrossProduct(point) > 0
                            select point).Count();
                }

                #endregion
            }

            private static class Line
            {
                #region Methods

                internal static PredictionOutput GetLinePrediction(PredictionInput input)
                {
                    var mainTargetPrediction = input.GetPrediction(false, true);
                    var posibleTargets = new List<PossibleTarget>
                                             {
                                                 new PossibleTarget
                                                     {
                                                         Position = mainTargetPrediction.UnitPosition.ToVector2(),
                                                         Unit = input.Unit
                                                     }
                                             };
                    if (mainTargetPrediction.Hitchance >= HitChance.Medium)
                    {
                        posibleTargets.AddRange(GetPossibleTargets(input));
                    }
                    if (posibleTargets.Count > 1)
                    {
                        var candidates = new List<Vector2>();
                        posibleTargets.ForEach(
                            i =>
                            candidates.AddRange(
                                GetCandidates(input.From.ToVector2(), i.Position, input.Radius, input.Range)));
                        var bestCandidateHits = -1;
                        var bestCandidate = new Vector2();
                        var bestCandidateHitPoints = new List<Vector2>();
                        var positionsList = posibleTargets.Select(i => i.Position).ToList();
                        foreach (var candidate in
                            candidates.Where(
                                i =>
                                GetHits(
                                    input.From,
                                    i,
                                    input.Radius + input.Unit.BoundingRadius / 3 - 10,
                                    new List<Vector2> { posibleTargets[0].Position }).Count == 1))
                        {
                            var hits = GetHits(input.From, candidate, input.Radius, positionsList);
                            var hitsCount = hits.Count;
                            if (hitsCount >= bestCandidateHits)
                            {
                                bestCandidateHits = hitsCount;
                                bestCandidate = candidate;
                                bestCandidateHitPoints = hits;
                            }
                        }
                        if (bestCandidateHits > 1)
                        {
                            var maxDistance = -1f;
                            Vector2 p1 = new Vector2(), p2 = new Vector2();
                            for (var i = 0; i < bestCandidateHitPoints.Count; i++)
                            {
                                for (var j = 0; j < bestCandidateHitPoints.Count; j++)
                                {
                                    var proj1 = positionsList[i].ProjectOn(input.From.ToVector2(), bestCandidate);
                                    var proj2 = positionsList[j].ProjectOn(input.From.ToVector2(), bestCandidate);
                                    var dist = bestCandidateHitPoints[i].DistanceSquared(proj1.LinePoint)
                                               + bestCandidateHitPoints[j].DistanceSquared(proj2.LinePoint);
                                    if (dist >= maxDistance
                                        && (proj1.LinePoint - positionsList[i]).AngleBetween(
                                            proj2.LinePoint - positionsList[j]) > 90)
                                    {
                                        maxDistance = dist;
                                        p1 = positionsList[i];
                                        p2 = positionsList[j];
                                    }
                                }
                            }
                            return new PredictionOutput
                                       {
                                           Input = input, CastPosition = ((p1 + p2) * 0.5f).ToVector3(),
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           Hitchance = mainTargetPrediction.Hitchance, AoeHitCount = bestCandidateHits
                                       };
                        }
                    }
                    return mainTargetPrediction;
                }

                private static Vector2[] GetCandidates(Vector2 from, Vector2 to, float radius, float range)
                {
                    var middlePoint = (from + to) / 2;
                    var intersections = @from.CircleCircleIntersection(middlePoint, radius, from.Distance(middlePoint));
                    if (intersections.Length <= 1)
                    {
                        return new Vector2[] { };
                    }
                    return new[]
                               {
                                   @from + range * (to - intersections[0]).Normalized(),
                                   @from + range * (to - intersections[1]).Normalized()
                               };
                }

                private static List<Vector2> GetHits(Vector3 start, Vector2 end, double radius, List<Vector2> points)
                {
                    return
                        points.Where(i => i.DistanceSquared(start.ToVector2(), end, true) <= radius * radius).ToList();
                }

                #endregion
            }

            private class PossibleTarget
            {
                #region Public Properties

                public Vector2 Position { get; set; }

                public Obj_AI_Base Unit { get; set; }

                #endregion
            }
        }

        private static class UnitTracker
        {
            #region Static Fields

            private static readonly Dictionary<int, TrackerInfo> StoredList = new Dictionary<int, TrackerInfo>();

            #endregion

            #region Constructors and Destructors

            static UnitTracker()
            {
                foreach (var hero in GameObjects.Heroes.Where(i => !i.IsMe && !StoredList.ContainsKey(i.NetworkId)))
                {
                    var info = new TrackerInfo();
                    info.AttackTick = info.StopTick = TickCount;
                    StoredList.Add(hero.NetworkId, info);
                }

                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (sender.IsMe || !(sender is Obj_AI_Hero) || !StoredList.ContainsKey(sender.NetworkId)
                            || !AutoAttack.IsAutoAttack(args.SData.Name))
                        {
                            return;
                        }
                        StoredList[sender.NetworkId].AttackTick = TickCount;
                    };
                Obj_AI_Base.OnNewPath += (sender, args) =>
                    {
                        if (sender.IsMe || !(sender is Obj_AI_Hero) || !StoredList.ContainsKey(sender.NetworkId))
                        {
                            return;
                        }
                        if (args.Path.Length == 1 && !sender.IsMoving)
                        {
                            StoredList[sender.NetworkId].StopTick = TickCount;
                        }
                        else
                        {
                            StoredList[sender.NetworkId].Paths.Add(
                                new PathInfo { Position = args.Path.Last().ToVector2(), Time = Game.Time });
                        }
                        if (StoredList[sender.NetworkId].Paths.Count > 3)
                        {
                            StoredList[sender.NetworkId].Paths.RemoveAt(0);
                        }
                    };
            }

            #endregion

            #region Properties

            private static int TickCount => Environment.TickCount & int.MaxValue;

            #endregion

            #region Methods

            internal static bool CanTrack(Obj_AI_Hero unit)
            {
                return StoredList.ContainsKey(unit.NetworkId);
            }

            internal static double GetLastAttack(Obj_AI_Hero unit)
            {
                return (TickCount - StoredList[unit.NetworkId].AttackTick) / 1000d;
            }

            internal static double GetLastStop(Obj_AI_Hero unit)
            {
                return (TickCount - StoredList[unit.NetworkId].StopTick) / 1000d;
            }

            internal static List<Vector2> GetWaypoints(Obj_AI_Hero unit)
            {
                return new List<Vector2> { unit.ServerPosition.ToVector2() };
            }

            internal static bool IsSpamClick(Obj_AI_Hero unit)
            {
                var info = StoredList[unit.NetworkId];
                if (info.Paths.Count >= 3 && info.Paths[2].Time - info.Paths[0].Time < 0.4
                    && Game.Time - info.Paths[2].Time < 0.1)
                {
                    var posUnit = unit.Position;
                    return info.Paths[2].Position.Distance(posUnit) < 300
                           && info.Paths[1].Position.Distance(posUnit) < 300
                           && info.Paths[0].Position.Distance(posUnit) < 300
                           && info.Paths[1].Position.Distance(info.Paths[2].Position)
                           > info.Paths[2].Position.Distance(posUnit)
                           && info.Paths[0].Position.Distance(info.Paths[1].Position)
                           > info.Paths[2].Position.Distance(posUnit);
                }
                return false;
            }

            internal static bool IsSpamPos(Obj_AI_Hero unit)
            {
                var info = StoredList[unit.NetworkId];
                return info.Paths.Count >= 3 && info.Paths[2].Time - info.Paths[1].Time < 0.2
                       && info.Paths[2].Time + 0.1 < Game.Time
                       && info.Paths[1].Position.Distance(info.Paths[2].Position) < 100;
            }

            #endregion

            private class PathInfo
            {
                #region Properties

                internal Vector2 Position { get; set; }

                internal float Time { get; set; }

                #endregion
            }

            private class TrackerInfo
            {
                #region Fields

                internal readonly List<PathInfo> Paths = new List<PathInfo>();

                #endregion

                #region Properties

                internal int AttackTick { get; set; }

                internal int StopTick { get; set; }

                #endregion
            }
        }

        internal class PredictionInput
        {
            #region Fields

            private Vector3 @from;

            private Vector3 rangeCheckFrom;

            #endregion

            #region Public Properties

            public bool AoE { get; set; } = false;

            public bool Collision { get; set; } = false;

            public CollisionableObjects[] CollisionObjects { get; set; } = {
                                                                               CollisionableObjects.Minions,
                                                                               CollisionableObjects.YasuoWall
                                                                           };

            public float Delay { get; set; }

            public Vector3 From
            {
                get
                {
                    return this.@from.ToVector2().IsValid() ? this.@from : Program.Player.ServerPosition;
                }
                set
                {
                    this.@from = value;
                }
            }

            public float Radius { get; set; } = 1f;

            public float Range { get; set; } = float.MaxValue;

            public Vector3 RangeCheckFrom
            {
                get
                {
                    return this.rangeCheckFrom.ToVector2().IsValid() ? this.rangeCheckFrom : this.From;
                }
                set
                {
                    this.rangeCheckFrom = value;
                }
            }

            public float Speed { get; set; } = float.MaxValue;

            public SkillshotType Type { get; set; } = SkillshotType.SkillshotLine;

            public Obj_AI_Base Unit { get; set; } = Program.Player;

            #endregion

            #region Properties

            internal float RealRadius => this.Radius + this.Unit.BoundingRadius;

            #endregion
        }

        internal class PredictionOutput
        {
            #region Fields

            private Vector3 castPosition;

            private Vector3 unitPosition;

            #endregion

            #region Public Properties

            public List<Obj_AI_Hero> AoeTargetsHit { get; set; } = new List<Obj_AI_Hero>();

            public int AoeTargetsHitCount => Math.Max(this.AoeHitCount, this.AoeTargetsHit.Count);

            public Vector3 CastPosition
            {
                get
                {
                    return this.castPosition.ToVector2().IsValid()
                               ? this.castPosition.SetZ()
                               : this.Input.Unit.ServerPosition;
                }
                set
                {
                    this.castPosition = value;
                }
            }

            public List<Obj_AI_Base> CollisionObjects { get; set; } = new List<Obj_AI_Base>();

            public HitChance Hitchance { get; set; } = HitChance.Impossible;

            public Vector3 UnitPosition
            {
                get
                {
                    return this.unitPosition.ToVector2().IsValid()
                               ? this.unitPosition.SetZ()
                               : this.Input.Unit.ServerPosition;
                }
                set
                {
                    this.unitPosition = value;
                }
            }

            #endregion

            #region Properties

            internal int AoeHitCount { get; set; }

            internal PredictionInput Input { get; set; }

            #endregion
        }
    }
}
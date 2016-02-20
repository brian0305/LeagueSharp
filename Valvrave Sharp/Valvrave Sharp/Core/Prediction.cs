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
                    var timeToPoint = input.Delay / 2 + input.From.Distance(endP) / input.Speed - 0.25;
                    if (timeToPoint
                        <= input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                    {
                        return new PredictionOutput
                                   {
                                       CastPosition = endP.ToVector3(), UnitPosition = endP.ToVector3(),
                                       Hitchance = HitChance.Dashing
                                   };
                    }
                }
                result.CastPosition = result.UnitPosition = endP.ToVector3();
            }
            return result;
        }

        private static PredictionOutput GetImmobilePrediction(this PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;
            return timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed
                       ? new PredictionOutput
                             {
                                 CastPosition = input.Unit.ServerPosition, UnitPosition = input.Unit.Position,
                                 Hitchance = HitChance.Immobile
                             }
                       : new PredictionOutput
                             {
                                 Input = input, CastPosition = input.Unit.ServerPosition,
                                 UnitPosition = input.Unit.ServerPosition, Hitchance = HitChance.High
                             };
        }

        private static PredictionOutput GetPositionOnPath(
            this PredictionInput input,
            List<Vector2> path,
            float speed = -1)
        {
            speed = Math.Abs(speed - -1) < float.Epsilon ? input.Unit.MoveSpeed : speed;
            if (path.Count <= 1)
            {
                return new PredictionOutput
                           {
                               Input = input, UnitPosition = input.Unit.ServerPosition,
                               CastPosition = input.Unit.ServerPosition, Hitchance = HitChance.VeryHigh
                           };
            }
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
                                           GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1
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
                    d = input.Delay * speed;
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
                        /*if (input.Type == SkillshotType.SkillshotLine) //This
                        {
                            var alpha = (input.From.ToVector2() - p).AngleBetween(a - b);
                            if (alpha > 30 && alpha < 180 - 30)
                            {
                                var beta = (float)Math.Asin(input.RealRadius / p.Distance(input.From));
                                var cp1 = input.From.ToVector2() + (p - input.From.ToVector2()).Rotated(beta);
                                var cp2 = input.From.ToVector2() + (p - input.From.ToVector2()).Rotated(-beta);
                                pos = cp1.DistanceSquared(pos) < cp2.DistanceSquared(pos) ? cp1 : cp2;
                            }
                        }*/
                        return new PredictionOutput
                                   {
                                       Input = input, CastPosition = pos.ToVector3(), UnitPosition = p.ToVector3(),
                                       Hitchance =
                                           GamePath.PathTracker.GetCurrentPath(input.Unit).Time < 0.1
                                               ? HitChance.VeryHigh
                                               : HitChance.High
                                   };
                    }
                    tT += tB;
                }
            }
            var position = path.Last();
            return new PredictionOutput
                       {
                           Input = input, CastPosition = position.ToVector3(), UnitPosition = position.ToVector3(),
                           Hitchance = HitChance.Medium
                       };
        }

        private static PredictionOutput GetPrediction(this PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = null;
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
                return new PredictionOutput { Input = input };
            }
            if (input.Unit.IsDashing())
            {
                result = input.GetDashingPrediction();
            }
            else
            {
                var remainingImmobileT = input.Unit.IsImmobileUntil();
                if (remainingImmobileT >= 0)
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
                        result.CastPosition = input.RangeCheckFrom
                                              + input.Range * (result.UnitPosition - input.RangeCheckFrom).Normalized();
                    }
                    else
                    {
                        result.Hitchance = HitChance.OutOfRange;
                    }
                }
            }
            if ((result.Hitchance == HitChance.High || result.Hitchance == HitChance.VeryHigh)
                && input.Unit.Type == GameObjectType.obj_AI_Minion)
            {
                result.Hitchance = HitChance.VeryHigh;
            }
            if (checkCollision && input.Collision && result.Hitchance > HitChance.Impossible)
            {
                var positions = new List<Vector3> { result.UnitPosition /*, result.CastPosition, input.Unit.Position*/ };
                var originalUnit = input.Unit;
                result.CollisionObjects = Collisions.GetCollision(positions, input);
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

            internal static List<Obj_AI_Base> GetCollision(List<Vector3> positions, PredictionInput input)
            {
                var result = new List<Obj_AI_Base>();
                foreach (var position in positions)
                {
                    if (input.CollisionObjects.HasFlag(CollisionableObjects.Minions))
                    {
                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet())
                            .Concat(GameObjects.Jungle)
                            .Where(
                                i =>
                                i.IsValidTarget(
                                    Math.Min(input.Range + input.Radius + 100, 2000),
                                    true,
                                    input.RangeCheckFrom) && !result.Any(a => a.Compare(i)))
                            .ForEach(
                                i =>
                                    {
                                        input.Unit = i;
                                        if (IsDead(input, i))
                                        {
                                            return;
                                        }
                                        if (i.Distance(input.From) < input.Radius || i.Distance(position) < input.Radius)
                                        {
                                            result.Add(i);
                                        }
                                        else
                                        {
                                            var pos = i.ServerPosition;
                                            var bonusRadius = 15;
                                            if (i.IsMoving)
                                            {
                                                pos = input.GetPrediction(false, false).UnitPosition;
                                                bonusRadius = 50;
                                            }
                                            if (pos.ToVector2()
                                                    .DistanceSquared(input.From.ToVector2(), position.ToVector2(), true)
                                                <= Math.Pow(input.Radius + bonusRadius + i.BoundingRadius, 2))
                                            {
                                                result.Add(i);
                                            }
                                        }
                                    });
                    }
                    if (input.CollisionObjects.HasFlag(CollisionableObjects.Heroes))
                    {
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            i.IsValidTarget(
                                Math.Min(input.Range + input.Radius + 100, 2000),
                                true,
                                input.RangeCheckFrom) && !result.Any(a => a.Compare(i))).ForEach(
                                    i =>
                                        {
                                            input.Unit = i;
                                            if (
                                                input.GetPrediction(false, false)
                                                    .UnitPosition.ToVector2()
                                                    .DistanceSquared(input.From.ToVector2(), position.ToVector2(), true)
                                                <= Math.Pow(input.Radius + 50 + i.BoundingRadius, 2))
                                            {
                                                result.Add(i);
                                            }
                                        });
                    }
                    if (input.CollisionObjects.HasFlag(CollisionableObjects.Walls) && !result.Any(i => i.IsMe))
                    {
                        var step = position.Distance(input.From) / 20;
                        for (var i = 0; i < 20; i++)
                        {
                            if (input.From.ToVector2().Extend(position, step * i).IsWall())
                            {
                                result.Add(Program.Player);
                                break;
                            }
                        }
                    }
                    if (input.CollisionObjects.HasFlag(CollisionableObjects.YasuoWall) && !result.Any(i => i.IsMe)
                        && Variables.TickCount - yasuoWallCastT <= 4000)
                    {
                        var wall =
                            GameObjects.AllGameObjects.FirstOrDefault(
                                i =>
                                i.IsValid
                                && Regex.IsMatch(i.Name, "_w_windwall_enemy_0.\\.troy", RegexOptions.IgnoreCase));
                        if (wall == null)
                        {
                            continue;
                        }
                        var wallWidth = 300 + 50 * Convert.ToInt32(wall.Name.Substring(wall.Name.Length - 6, 1));
                        var wallDirection = (wall.Position.ToVector2() - yasuoWallCastPos).Normalized().Perpendicular();
                        var wallStart = wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                        var wallEnd = wallStart - wallWidth * wallDirection;
                        var wallIntersect = wallStart.Intersection(
                            wallEnd,
                            position.ToVector2(),
                            input.From.ToVector2());
                        if (wallIntersect.Intersects)
                        {
                            var t = Variables.TickCount
                                    + (wallIntersect.Point.Distance(input.From) / input.Speed + input.Delay) * 1000;
                            if (t < yasuoWallCastT + 4000)
                            {
                                result.Add(Program.Player);
                            }
                        }
                    }
                }
                return result.Distinct().ToList();
            }

            private static bool IsDead(PredictionInput input, Obj_AI_Base minion)
            {
                var delay = (minion.Distance(input.From) / input.Speed) + input.Delay;
                if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
                {
                    delay = input.Delay;
                }
                return Health.GetPrediction(minion, (int)(delay * 1000), 0, HealthPredictionType.Simulated) <= 0;
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
                                var prediction = input.GetPrediction(false, false);
                                if (prediction.Hitchance >= HitChance.High)
                                {
                                    result.Add(
                                        new PossibleTarget { Position = prediction.UnitPosition.ToVector2(), Unit = i });
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
                                           AoeTargetsHit = posibleTargets.Select(i => (Obj_AI_Hero)i.Unit).ToList(),
                                           CastPosition = mecCircle.Center.ToVector3(),
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           Hitchance = mainTargetPrediction.Hitchance, Input = input,
                                           AoeHitCount = posibleTargets.Count
                                       };
                        }
                        float maxdist = -1;
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
                                if (i != j)
                                {
                                    var p = (posibleTargets[i].Position + posibleTargets[j].Position) * 0.5f;
                                    if (!candidates.Contains(p))
                                    {
                                        candidates.Add(p);
                                    }
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
                                           Hitchance = mainTargetPrediction.Hitchance, AoeHitCount = bestCandidateHits,
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           CastPosition = bestCandidate.ToVector3(), Input = input
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
                                && point.CrossProduct(edge2) > 0
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
                                    input.From.ToVector2(),
                                    i,
                                    input.Radius + input.Unit.BoundingRadius / 3 - 10,
                                    new List<Vector2> { posibleTargets[0].Position }).Count == 1))
                        {
                            var hits = GetHits(input.From.ToVector2(), candidate, input.Radius, positionsList);
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
                            float maxDistance = -1;
                            Vector2 p1 = new Vector2(), p2 = new Vector2();
                            for (var i = 0; i < bestCandidateHitPoints.Count; i++)
                            {
                                for (var j = 0; j < bestCandidateHitPoints.Count; j++)
                                {
                                    var startP = input.From.ToVector2();
                                    var endP = bestCandidate;
                                    var proj1 = positionsList[i].ProjectOn(startP, endP);
                                    var proj2 = positionsList[j].ProjectOn(startP, endP);
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
                                           Hitchance = mainTargetPrediction.Hitchance, AoeHitCount = bestCandidateHits,
                                           UnitPosition = mainTargetPrediction.UnitPosition,
                                           CastPosition = ((p1 + p2) * 0.5f).ToVector3(), Input = input
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
                    var c1 = intersections[0];
                    var c2 = intersections[1];
                    c1 = @from + range * (to - c1).Normalized();
                    c2 = @from + range * (to - c2).Normalized();
                    return new[] { c1, c2 };
                }

                private static List<Vector2> GetHits(Vector2 start, Vector2 end, double radius, List<Vector2> points)
                {
                    return points.Where(i => i.DistanceSquared(start, end, true) <= radius * radius).ToList();
                }

                #endregion
            }

            private class PossibleTarget
            {
                #region Properties

                internal Vector2 Position { get; set; }

                internal Obj_AI_Base Unit { get; set; }

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

            public bool AoE { get; set; }

            public bool Collision { get; set; }

            public CollisionableObjects CollisionObjects { get; set; } = CollisionableObjects.Minions
                                                                         | CollisionableObjects.YasuoWall;

            public float Delay { get; set; }

            public Vector3 From
            {
                get
                {
                    return this.@from.IsValid() ? this.@from : Program.Player.ServerPosition;
                }
                set
                {
                    this.@from = value;
                }
            }

            public float Radius { get; set; } = 1;

            public float Range { get; set; } = float.MaxValue;

            public Vector3 RangeCheckFrom
            {
                get
                {
                    return this.rangeCheckFrom.IsValid()
                               ? this.rangeCheckFrom
                               : (this.From.IsValid() ? this.From : Program.Player.ServerPosition);
                }
                set
                {
                    this.rangeCheckFrom = value;
                }
            }

            public float Speed { get; set; } = float.MaxValue;

            public SkillshotType Type { get; set; } = SkillshotType.SkillshotLine;

            public Obj_AI_Base Unit { get; set; } = Program.Player;

            public bool UseBoundingRadius { get; set; } = true;

            #endregion

            #region Properties

            internal float RealRadius => this.Radius + (this.UseBoundingRadius ? this.Unit.BoundingRadius : 0);

            #endregion
        }

        internal class PredictionOutput
        {
            #region Fields

            private Vector3 castPosition;

            private Vector3 unitPosition;

            #endregion

            #region Public Properties

            public int AoeHitCount { get; set; }

            public List<Obj_AI_Hero> AoeTargetsHit { get; set; } = new List<Obj_AI_Hero>();

            public int AoeTargetsHitCount => Math.Max(this.AoeHitCount, this.AoeTargetsHit.Count);

            public Vector3 CastPosition
            {
                get
                {
                    return this.castPosition.IsValid() ? this.castPosition : this.Input.Unit.ServerPosition;
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
                    return this.unitPosition.IsValid() ? this.unitPosition : this.Input.Unit.ServerPosition;
                }
                set
                {
                    this.unitPosition = value;
                }
            }

            #endregion

            #region Properties

            internal PredictionInput Input { get; set; }

            #endregion
        }
    }
}
namespace Valvrave_Sharp.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Math;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

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

        private static double GetAngle(this Vector3 from, Obj_AI_Hero target)
        {
            var posTarget = target.ServerPosition;
            var lastWaypoint = target.GetWaypoints().Last();
            if (posTarget.ToVector2() == lastWaypoint)
            {
                return 60;
            }
            var ab = Math.Pow(lastWaypoint.X - from.X, 2) + Math.Pow(lastWaypoint.Y - from.Y, 2);
            var bc = Math.Pow(from.X - posTarget.X, 2) + Math.Pow(from.Y - posTarget.Y, 2);
            var ac = Math.Pow(lastWaypoint.X - posTarget.X, 2) + Math.Pow(lastWaypoint.Y - posTarget.Y, 2);
            return Math.Cos((ab + bc - ac) / (2 * Math.Sqrt(ab) * Math.Sqrt(bc))) * 180 / Math.PI;
        }

        private static PredictionOutput GetDashingPrediction(this PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            if (!dashData.IsBlink)
            {
                var endP = dashData.Path.Last();
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
                    var timeToPoint = input.Delay / 2 + input.From.ToVector2().Distance(endP) / input.Speed - 0.25;
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
                result.CastPosition = dashData.Path.Last().ToVector3();
                result.UnitPosition = result.CastPosition;
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
            speed = Math.Abs(speed - (-1)) < float.Epsilon ? input.Unit.MoveSpeed : speed;
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
                                       Hitchance = HitChance.High
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
                        /*if (input.Type == SkillshotType.SkillshotLine)
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
                                       Hitchance = HitChance.High
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
                if (input.RangeCheckFrom.DistanceSquared(result.CastPosition) > Math.Pow(input.Range, 2)
                    && result.Hitchance != HitChance.OutOfRange)
                {
                    result.CastPosition = input.RangeCheckFrom
                                          + input.Range
                                          * (result.UnitPosition - input.RangeCheckFrom).ToVector2()
                                                .Normalized()
                                                .ToVector3();
                }
            }
            if (result.Hitchance == HitChance.High || result.Hitchance == HitChance.VeryHigh)
            {
                result = input.WayPointAnalysis(result);
            }
            if (checkCollision && input.Collision && result.Hitchance > HitChance.Impossible)
            {
                var originalUnit = input.Unit;
                result.CollisionObjects = Collisions.GetCollision(new List<Vector3> { result.CastPosition }, input);
                result.CollisionObjects.RemoveAll(i => i.NetworkId == originalUnit.NetworkId);
                result.Hitchance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.Hitchance;
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
            var heroUnit = input.Unit as Obj_AI_Hero;
            if (heroUnit != null && UnitTracker.CanCalcWaypoints(heroUnit))
            {
                return input.GetPositionOnPath(UnitTracker.CalcWaypoints(heroUnit), speed);
            }
            return input.GetPositionOnPath(input.Unit.GetWaypoints(), speed);
        }

        private static double IsImmobileUntil(this Obj_AI_Base unit)
        {
            var result =
                unit.Buffs.Where(
                    i =>
                    i.IsValid
                    && (i.Type == BuffType.Charm || i.Type == BuffType.Knockup || i.Type == BuffType.Stun
                        || i.Type == BuffType.Suppression || i.Type == BuffType.Snare || i.Type == BuffType.Fear
                        || i.Type == BuffType.Taunt || i.Type == BuffType.Knockback))
                    .Aggregate(0d, (current, buff) => Math.Max(current, buff.EndTime));
            return result - Game.Time;
        }

        private static PredictionOutput WayPointAnalysis(this PredictionInput input, PredictionOutput result)
        {
            var heroUnit = input.Unit as Obj_AI_Hero;
            if (heroUnit == null || input.Radius.Equals(1))
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (UnitTracker.GetLastSpecialSpellTime(heroUnit) > 0 || heroUnit.IsRecalling())
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            result.Hitchance = HitChance.Medium;
            var lastWaypoint = heroUnit.GetWaypoints().Last().ToVector3();
            var distUnitToWaypoint = heroUnit.Distance(lastWaypoint);
            var distFromToWaypoint = input.From.Distance(lastWaypoint);
            var distUnitToFrom = heroUnit.Distance(input.From);
            var speedDelay = distUnitToFrom / input.Speed;
            if (Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                speedDelay = 0;
            }
            var totalDelay = speedDelay + input.Delay;
            var moveArea = heroUnit.MoveSpeed * totalDelay;
            var fixRange = moveArea * 0.5f;
            var moveAngle = 30 + (input.Radius / 17) - (totalDelay * 2);
            var backToFront = moveArea * 1.5f;
            var minPath = 900f;
            if (moveAngle < 31)
            {
                moveAngle = 31;
            }
            if (UnitTracker.GetLastNewPathTime(heroUnit) < 0.1)
            {
                minPath = 600 + backToFront;
                result.Hitchance = HitChance.High;
            }
            if (input.Type == SkillshotType.SkillshotCircle)
            {
                fixRange -= input.Radius / 2;
            }
            if (UnitTracker.CanCalcWaypoints(heroUnit))
            {
                result.Hitchance = distUnitToFrom < input.Range - fixRange ? HitChance.VeryHigh : HitChance.High;
                return result;
            }
            if (UnitTracker.GetLastVisableTime(heroUnit) < 0.08)
            {
                result.Hitchance = HitChance.Medium;
                return result;
            }
            if (distUnitToFrom < 300 || distFromToWaypoint < 200)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (distUnitToWaypoint > minPath)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (distFromToWaypoint > distUnitToFrom + fixRange && input.From.GetAngle(heroUnit) < moveAngle)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (distFromToWaypoint <= heroUnit.Distance(input.From) && distUnitToFrom > input.Range - fixRange)
            {
                result.Hitchance = HitChance.Medium;
                return result;
            }
            if (UnitTracker.GetLastAttackTime(heroUnit) < 0.1)
            {
                if (input.Type == SkillshotType.SkillshotLine && totalDelay < 0.6 + (input.Radius * 0.001))
                {
                    result.Hitchance = HitChance.VeryHigh;
                }
                else if (input.Type == SkillshotType.SkillshotCircle && totalDelay < 0.7 + (input.Radius * 0.001))
                {
                    result.Hitchance = HitChance.VeryHigh;
                }
                else
                {
                    result.Hitchance = HitChance.High;
                }
                return result;
            }
            if (heroUnit.IsWindingUp)
            {
                result.Hitchance = HitChance.High;
                return result;
            }
            if (heroUnit.Path.Length == 0 && !heroUnit.IsMoving)
            {
                if (distUnitToFrom > input.Range - fixRange)
                {
                    result.Hitchance = HitChance.Medium;
                }
                else if (UnitTracker.GetLastStopMoveTime(heroUnit) < 0.8)
                {
                    result.Hitchance = HitChance.High;
                }
                else
                {
                    result.Hitchance = HitChance.VeryHigh;
                }
                return result;
            }
            if (input.Type == SkillshotType.SkillshotLine && heroUnit.Path.Length > 0 && heroUnit.IsMoving
                && UnitTracker.GetLastNewPathTime(heroUnit) < 0.1 && input.From.GetAngle(heroUnit) < moveAngle
                && distUnitToWaypoint > moveArea * 0.6)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            if (input.Type == SkillshotType.SkillshotCircle && UnitTracker.GetLastNewPathTime(heroUnit) < 0.1
                && distUnitToWaypoint > fixRange && distUnitToFrom < input.Range - fixRange)
            {
                result.Hitchance = HitChance.VeryHigh;
                return result;
            }
            return result;
        }

        #endregion

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
                foreach (var enemy in
                    GameObjects.EnemyHeroes.Where(
                        i =>
                        i.NetworkId != originalUnit.NetworkId
                        && i.IsValidTarget(input.Range + 200 + input.RealRadius, true, input.RangeCheckFrom)))
                {
                    input.Unit = enemy;
                    var prediction = input.GetPrediction(false, false);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        result.Add(new PossibleTarget { Position = prediction.UnitPosition.ToVector2(), Unit = enemy });
                    }
                }
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
                            && mecCircle.Center.DistanceSquared(input.RangeCheckFrom.ToVector2())
                            < input.Range * input.Range)
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
                        foreach (var target in posibleTargets)
                        {
                            target.Position = target.Position - input.From.ToVector2();
                        }
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
                        foreach (var candidate in candidates)
                        {
                            var hits = GetHits(candidate, input.Range, input.Radius, positionsList);
                            if (hits > bestCandidateHits)
                            {
                                bestCandidate = candidate;
                                bestCandidateHits = hits;
                            }
                        }
                        if (bestCandidateHits > 1 && input.From.ToVector2().DistanceSquared(bestCandidate) > 50 * 50)
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
                        foreach (var targetCandidates in
                            posibleTargets.Select(
                                i => GetCandidates(input.From.ToVector2(), i.Position, input.Radius, input.Range)))
                        {
                            candidates.AddRange(targetCandidates);
                        }
                        var bestCandidateHits = -1;
                        var bestCandidate = new Vector2();
                        var bestCandidateHitPoints = new List<Vector2>();
                        var positionsList = posibleTargets.Select(i => i.Position).ToList();
                        foreach (var candidate in candidates)
                        {
                            if (
                                GetHits(
                                    input.From.ToVector2(),
                                    candidate,
                                    input.Radius + input.Unit.BoundingRadius / 3 - 10,
                                    new List<Vector2> { posibleTargets[0].Position }).Count == 1)
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

        private static class Collisions
        {
            #region Static Fields

            private static int wallCastT;

            private static Vector2 yasuoWallCastedPos;

            #endregion

            #region Constructors and Destructors

            static Collisions()
            {
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsValid() || sender.Team == GameObjects.Player.Team
                            || args.SData.Name != "YasuoWMovingWall")
                        {
                            return;
                        }
                        wallCastT = Variables.TickCount;
                        yasuoWallCastedPos = sender.ServerPosition.ToVector2();
                    };
            }

            #endregion

            #region Methods

            internal static List<Obj_AI_Base> GetCollision(List<Vector3> positions, PredictionInput input)
            {
                var result = new List<Obj_AI_Base>();
                foreach (var position in positions)
                {
                    foreach (var objType in input.CollisionObjects)
                    {
                        switch (objType)
                        {
                            case CollisionableObjects.Minions:
                                var minions = new List<Obj_AI_Minion>();
                                minions.AddRange(
                                    GameObjects.EnemyMinions.Where(
                                        i =>
                                        i.IsValidTarget(
                                            Math.Min(input.Range + input.Radius + 100, 2000),
                                            true,
                                            input.RangeCheckFrom) && i.IsMinion()));
                                minions.AddRange(
                                    GameObjects.Jungle.Where(
                                        i =>
                                        i.IsValidTarget(
                                            Math.Min(input.Range + input.Radius + 100, 2000),
                                            true,
                                            input.RangeCheckFrom)));
                                foreach (var minion in
                                    minions)
                                {
                                    input.Unit = minion;
                                    if (minion.Distance(input.From) < input.Radius)
                                    {
                                        result.Add(minion);
                                    }
                                    else
                                    {
                                        var minionPos = minion.ServerPosition;
                                        var bonusRadius = 15;
                                        if (minion.IsMoving)
                                        {
                                            minionPos = input.GetPrediction(false, false).UnitPosition;
                                            bonusRadius *= 2;
                                        }
                                        if (minionPos.ToVector2()
                                                .DistanceSquared(input.From.ToVector2(), position.ToVector2(), true)
                                            <= Math.Pow(input.Radius + bonusRadius + minion.BoundingRadius, 2))
                                        {
                                            result.Add(minion);
                                        }
                                    }
                                }
                                break;
                            case CollisionableObjects.Heroes:
                                foreach (var hero in
                                    GameObjects.EnemyHeroes.Where(
                                        i =>
                                        i.IsValidTarget(
                                            Math.Min(input.Range + input.Radius + 100, 2000),
                                            true,
                                            input.RangeCheckFrom)))
                                {
                                    input.Unit = hero;
                                    if (
                                        input.GetPrediction(false, false)
                                            .UnitPosition.ToVector2()
                                            .DistanceSquared(input.From.ToVector2(), position.ToVector2(), true)
                                        <= Math.Pow(input.Radius + 50 + hero.BoundingRadius, 2))
                                    {
                                        result.Add(hero);
                                    }
                                }
                                break;
                            case CollisionableObjects.Walls:
                                var step = position.Distance(input.From) / 20;
                                for (var i = 0; i < 20; i++)
                                {
                                    var p = input.From.ToVector2().Extend(position.ToVector2(), step * i);
                                    if (p.IsWall())
                                    {
                                        result.Add(GameObjects.Player);
                                    }
                                }
                                break;
                            case CollisionableObjects.YasuoWall:
                                if (Variables.TickCount - wallCastT > 4000)
                                {
                                    break;
                                }
                                var wall =
                                    GameObjects.AllGameObjects.FirstOrDefault(
                                        i =>
                                        i.IsValid
                                        && Regex.IsMatch(i.Name, "_w_windwall_enemy_0.\\.troy", RegexOptions.IgnoreCase));
                                if (wall == null)
                                {
                                    break;
                                }
                                var wallWidth = 300 + 50 * Convert.ToInt32(wall.Name.Substring(wall.Name.Length - 6, 1));
                                var wallDirection =
                                    (wall.Position.ToVector2() - yasuoWallCastedPos).Normalized().Perpendicular();
                                var wallStart = wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                                var wallEnd = wallStart - wallWidth * wallDirection;
                                var wallIntersect = wallStart.Intersection(
                                    wallEnd,
                                    position.ToVector2(),
                                    input.From.ToVector2());
                                if (wallIntersect.Intersects)
                                {
                                    var t = Variables.TickCount
                                            + (wallIntersect.Point.Distance(input.From) / input.Speed + input.Delay)
                                            * 1000;
                                    if (t < wallCastT + 4000)
                                    {
                                        result.Add(GameObjects.Player);
                                    }
                                }
                                break;
                        }
                    }
                }
                return result.Distinct().ToList();
            }

            #endregion
        }

        private static class UnitTracker
        {
            #region Static Fields

            private static readonly List<SpellInfo> Spells = new List<SpellInfo>();

            private static readonly List<TrackerInfo> StoredList = new List<TrackerInfo>();

            #endregion

            #region Constructors and Destructors

            static UnitTracker()
            {
                Spells.Add(new SpellInfo { Name = "caitlynpiltoverpeacemaker", Duration = 0.5 }); //Caitlyn Q
                Spells.Add(new SpellInfo { Name = "drain", Duration = 1 }); //Fiddle W
                Spells.Add(new SpellInfo { Name = "crowstorm", Duration = 1 }); //Fiddle R
                Spells.Add(new SpellInfo { Name = "galioidolofdurand", Duration = 1 }); //Galio R
                Spells.Add(new SpellInfo { Name = "reapthewhirlwind", Duration = 1 }); //Janna R
                Spells.Add(new SpellInfo { Name = "katarinar", Duration = 1 }); //Kata R
                Spells.Add(new SpellInfo { Name = "lucianq", Duration = 0.5 }); //Lucian Q
                Spells.Add(new SpellInfo { Name = "alzaharnethergrasp", Duration = 1 }); //Malza R
                Spells.Add(new SpellInfo { Name = "meditate", Duration = 1 }); //Yi W
                Spells.Add(new SpellInfo { Name = "missfortunebullettime", Duration = 1 }); //MF R
                Spells.Add(new SpellInfo { Name = "consume", Duration = 0.5 }); //Nunu Q
                Spells.Add(new SpellInfo { Name = "absolutezero", Duration = 1 }); //Nunu R
                Spells.Add(new SpellInfo { Name = "shenstandunited", Duration = 1 }); //Shen R
                Spells.Add(new SpellInfo { Name = "velkozr", Duration = 0.5 }); //Velkoz R
                Spells.Add(new SpellInfo { Name = "infiniteduress", Duration = 1 }); //WW R
                Spells.Add(new SpellInfo { Name = "staticfield", Duration = 0.5 }); //Blitz R
                Spells.Add(new SpellInfo { Name = "cassiopeiapetrifyinggaze", Duration = 0.5 }); //Cass R
                Spells.Add(new SpellInfo { Name = "ezrealtrueshotbarrage", Duration = 1 }); //Ez R
                Spells.Add(new SpellInfo { Name = "luxmalicecannon", Duration = 1 }); //Lux R
                Spells.Add(new SpellInfo { Name = "jinxw", Duration = 0.6 }); //Jinx W
                Spells.Add(new SpellInfo { Name = "jinxr", Duration = 0.6 }); //Jinx R
                Spells.Add(new SpellInfo { Name = "threshq", Duration = 0.75 }); //Thresh Q
                Spells.Add(new SpellInfo { Name = "threshe", Duration = 0.4 }); //Thresh E
                Spells.Add(new SpellInfo { Name = "threshrpenta", Duration = 0.75 }); //Thresh R
                foreach (var hero in GameObjects.Heroes)
                {
                    StoredList.Add(
                        new TrackerInfo
                            {
                                NetworkId = hero.NetworkId, AttackTick = Variables.TickCount,
                                NewPathTick = Variables.TickCount, StopMoveTick = Variables.TickCount,
                                LastInviTick = Variables.TickCount, EndSpecialSpellTick = Variables.TickCount
                            });
                }
                Game.OnUpdate += args =>
                    {
                        foreach (var hero in GameObjects.Heroes.Where(i => i.IsValid()))
                        {
                            if (hero.IsVisible)
                            {
                                if (hero.Path.Length > 0)
                                {
                                    StoredList.First(i => i.NetworkId == hero.NetworkId).StopMoveTick =
                                        Variables.TickCount;
                                }
                            }
                            else
                            {
                                StoredList.First(i => i.NetworkId == hero.NetworkId).LastInviTick = Variables.TickCount;
                            }
                        }
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        var caster = sender as Obj_AI_Hero;
                        if (caster == null)
                        {
                            return;
                        }
                        if (AutoAttack.IsAutoAttack(args.SData.Name))
                        {
                            StoredList.First(i => i.NetworkId == caster.NetworkId).AttackTick = Variables.TickCount;
                        }
                        else
                        {
                            var specialSpell = Spells.FirstOrDefault(i => i.Name.Equals(args.SData.Name.ToLower()));
                            if (specialSpell != null)
                            {
                                StoredList.First(i => i.NetworkId == caster.NetworkId).EndSpecialSpellTick =
                                    Variables.TickCount + (int)(specialSpell.Duration * 1000);
                            }
                        }
                    };
                Obj_AI_Base.OnNewPath += (sender, args) =>
                    {
                        var unit = sender as Obj_AI_Hero;
                        if (unit == null)
                        {
                            return;
                        }
                        var info = StoredList.First(i => i.NetworkId == unit.NetworkId);
                        info.NewPathTick = Variables.TickCount;
                        if (args.Path.Last() != unit.ServerPosition)
                        {
                            info.Path.Add(new PathInfo { Position = args.Path.Last().ToVector2(), Time = Game.Time });
                        }
                        if (info.Path.Count > 3)
                        {
                            info.Path.Remove(info.Path.First());
                        }
                    };
            }

            #endregion

            #region Methods

            internal static List<Vector2> CalcWaypoints(Obj_AI_Hero unit)
            {
                var info = StoredList.First(x => x.NetworkId == unit.NetworkId);
                return new List<Vector2>
                           {
                               new Vector2(
                                   (info.Path[0].Position.X + info.Path[1].Position.X + info.Path[2].Position.X) / 3,
                                   (info.Path[0].Position.Y + info.Path[1].Position.Y + info.Path[2].Position.Y) / 3)
                           };
            }

            internal static bool CanCalcWaypoints(Obj_AI_Hero unit)
            {
                var info = StoredList.First(i => i.NetworkId == unit.NetworkId);
                return info.Path.Count >= 3 && info.Path[2].Time - info.Path[0].Time < 0.35
                       && info.Path[2].Time + 0.1 < Game.Time && info.Path[2].Time + 0.2 > Game.Time
                       && info.Path[1].Position.Distance(info.Path[2].Position) > unit.Distance(info.Path[2].Position);
            }

            internal static double GetLastAttackTime(Obj_AI_Hero unit)
            {
                return (Variables.TickCount - StoredList.First(i => i.NetworkId == unit.NetworkId).AttackTick) / 1000d;
            }

            internal static double GetLastNewPathTime(Obj_AI_Hero unit)
            {
                return (Variables.TickCount - StoredList.First(i => i.NetworkId == unit.NetworkId).NewPathTick) / 1000d;
            }

            internal static double GetLastSpecialSpellTime(Obj_AI_Hero unit)
            {
                return (StoredList.First(i => i.NetworkId == unit.NetworkId).EndSpecialSpellTick - Variables.TickCount)
                       / 1000d;
            }

            internal static double GetLastStopMoveTime(Obj_AI_Hero unit)
            {
                return (Variables.TickCount - StoredList.First(i => i.NetworkId == unit.NetworkId).StopMoveTick) / 1000d;
            }

            internal static double GetLastVisableTime(Obj_AI_Hero unit)
            {
                return (Variables.TickCount - StoredList.First(i => i.NetworkId == unit.NetworkId).LastInviTick) / 1000d;
            }

            #endregion

            private class PathInfo
            {
                #region Properties

                internal Vector2 Position { get; set; }

                internal float Time { get; set; }

                #endregion
            }

            private class SpellInfo
            {
                #region Properties

                internal double Duration { get; set; }

                internal string Name { get; set; }

                #endregion
            }

            private class TrackerInfo
            {
                #region Fields

                internal readonly List<PathInfo> Path = new List<PathInfo>();

                #endregion

                #region Properties

                internal int AttackTick { get; set; }

                internal int EndSpecialSpellTick { get; set; }

                internal int LastInviTick { get; set; }

                internal int NetworkId { get; set; }

                internal int NewPathTick { get; set; }

                internal int StopMoveTick { get; set; }

                #endregion
            }
        }

        internal class PredictionInput
        {
            #region Fields

            public CollisionableObjects[] CollisionObjects =
                {
                    CollisionableObjects.Minions,
                    CollisionableObjects.YasuoWall
                };

            private Vector3 @from;

            private Vector3 rangeCheckFrom;

            #endregion

            #region Public Properties

            public bool AoE { get; set; }

            public bool Collision { get; set; }

            public float Delay { get; set; }

            public Vector3 From
            {
                get
                {
                    return this.@from.IsValid() ? this.@from : GameObjects.Player.ServerPosition;
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
                               : (this.From.IsValid() ? this.From : GameObjects.Player.ServerPosition);
                }
                set
                {
                    this.rangeCheckFrom = value;
                }
            }

            public float Speed { get; set; } = float.MaxValue;

            public SkillshotType Type { get; set; } = SkillshotType.SkillshotLine;

            public Obj_AI_Base Unit { get; set; } = GameObjects.Player;

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

            public List<Obj_AI_Hero> AoeTargetsHit { get; set; } = new List<Obj_AI_Hero>();

            public int AoeTargetsHitCount => Math.Max(this.AoeHitCount, this.AoeTargetsHit.Count);

            public Vector3 CastPosition
            {
                get
                {
                    return this.castPosition.IsValid() ? this.castPosition.SetZ() : this.Input.Unit.ServerPosition;
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
                    return this.unitPosition.IsValid() ? this.unitPosition.SetZ() : this.Input.Unit.ServerPosition;
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
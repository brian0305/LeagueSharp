﻿namespace Valvrave_Sharp.Evade
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDKEx;
    using LeagueSharp.SDKEx.UI;
    using LeagueSharp.SDKEx.Utils;

    using SharpDX;

    using Color = System.Drawing.Color;

    #endregion

    internal class Evade
    {
        #region Static Fields

        internal static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        internal static int LastWardJumpAttempt = 0;

        #endregion

        #region Delegates

        internal delegate void EvadingH(Obj_AI_Base sender);

        internal delegate void TryEvadingH(List<Skillshot> skillshot, Vector2 to);

        #endregion

        #region Events

        internal static event EvadingH Evading;

        internal static event TryEvadingH TryEvading;

        #endregion

        #region Properties

        internal static Vector2 PlayerPosition => Program.Player.ServerPosition.ToVector2();

        #endregion

        #region Methods

        internal static void Init()
        {
            EvadeSpellDatabase.Init();
            SpellDatabase.Init();
            Config.CreateMenu(Program.MainMenu);
            Collision.Init();
            SkillshotDetector.Init();
            Game.OnUpdate += args =>
                {
                    DetectedSkillshots.RemoveAll(i => !i.IsActive);
                    DetectedSkillshots.ForEach(i => i.OnUpdate());
                    if (!Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active)
                    {
                        return;
                    }
                    if (Program.Player.IsDead)
                    {
                        return;
                    }
                    if (Program.Player.IsCastingInterruptableSpell(true))
                    {
                        return;
                    }
                    if (Program.Player.HasBuffOfType(BuffType.SpellShield)
                        || Program.Player.HasBuffOfType(BuffType.SpellImmunity))
                    {
                        return;
                    }
                    Evading?.Invoke(Program.Player);
                    var currentPath = Program.Player.GetWaypoints();
                    var safePoint = IsSafePoints(PlayerPosition);
                    var safePath = IsSafePath(currentPath, 100);
                    if (!safePath.IsSafe && !safePoint.IsSafe)
                    {
                        TryEvading?.Invoke(safePoint.SkillshotList, Game.CursorPos.ToVector2());
                    }
                };
            SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
            SkillshotDetector.OnDeleteSkillshot += (skillshot, missile) =>
                {
                    if (skillshot.SpellData.SpellName == "VelkozQ")
                    {
                        var spellData = SpellDatabase.GetByName("VelkozQSplit");
                        var direction = skillshot.Direction.Perpendicular();
                        if (DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") == 0)
                        {
                            for (var i = -1; i <= 1; i = i + 2)
                            {
                                DetectedSkillshots.Add(
                                    new Skillshot(
                                        DetectionType.RecvPacket,
                                        spellData,
                                        Variables.TickCount,
                                        missile.Position.ToVector2(),
                                        missile.Position.ToVector2() + i * direction * spellData.Range,
                                        skillshot.Unit));
                            }
                        }
                    }
                };
            Drawing.OnDraw += args =>
                {
                    if (Program.Player.IsDead)
                    {
                        return;
                    }
                    if (Program.MainMenu["Evade"]["Draw"]["Status"])
                    {
                        var active = Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active;
                        var text =
                            $"Evade Skillshot: {(active ? (Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active ? "Dangerous" : "On") : "Off")}";
                        var pos = Drawing.WorldToScreen(Program.Player.Position);
                        Drawing.DrawText(
                            pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                            pos.Y + 60,
                            active
                                ? (Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active
                                       ? Color.Yellow
                                       : Color.White)
                                : Color.Gray,
                            text);
                    }
                    if (Program.MainMenu["Evade"]["Draw"]["Skillshot"])
                    {
                        DetectedSkillshots.ForEach(
                            i =>
                            i.Draw(
                                i.Enable && Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active
                                    ? Color.White
                                    : Color.Red,
                                Color.LimeGreen));
                    }
                };
        }

        internal static bool IsAboutToHit(Obj_AI_Base unit, int time)
        {
            return SkillshotAboutToHit(unit, time).Count > 0;
        }

        internal static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersections = new List<FoundIntersection>();
            var intersection = new FoundIntersection();
            foreach (var sResult in
                from skillshot in DetectedSkillshots
                where skillshot.Enable
                select skillshot.IsSafePath(path, timeOffset, speed, delay))
            {
                isSafe = isSafe && sResult.IsSafe;
                if (sResult.Intersection.Valid)
                {
                    intersections.Add(sResult.Intersection);
                }
            }
            if (isSafe)
            {
                return new SafePathResult(true, intersection);
            }
            var intersetion = intersections.MinOrDefault(i => i.Distance);
            return new SafePathResult(false, intersetion.Valid ? intersetion : intersection);
        }

        internal static bool IsSafePoint(Vector2 point)
        {
            return IsSafePoints(point).IsSafe;
        }

        internal static bool IsSafeToBlink(Vector2 point, int timeOffset, int delay)
        {
            return DetectedSkillshots.Where(i => i.Enable).All(i => i.IsSafeToBlink(point, timeOffset, delay));
        }

        internal static List<Skillshot> SkillshotAboutToHit(Obj_AI_Base unit, int time, bool onlyWindWall = false)
        {
            time += 150;
            return
                DetectedSkillshots.Where(
                    i =>
                    i.Enable && i.IsAboutToHit(time, unit)
                    && (!onlyWindWall || i.SpellData.CollisionObjects.HasFlag(CollisionableObjects.YasuoWall)))
                    .OrderBy(i => i.DangerLevel)
                    .ToList();
        }

        private static IsSafeResult IsSafePoints(Vector2 point)
        {
            var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
            DetectedSkillshots.Where(i => i.Enable && !i.IsSafe(point)).ForEach(i => result.SkillshotList.Add(i));
            result.IsSafe = result.SkillshotList.Count == 0;
            return result;
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            if (!skillshot.Unit.IsVisible)
            {
                var preventDodgeFoW = Program.MainMenu["Evade"]["DisableFoW"];
                if (!preventDodgeFoW && !skillshot.SpellData.DisableFowDetection
                    && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    preventDodgeFoW =
                        Program.MainMenu["Evade"][skillshot.SpellData.ChampionName.ToLowerInvariant()][
                            skillshot.SpellData.SpellName]["DisableFoW"];
                }
                if (preventDodgeFoW)
                {
                    return;
                }
            }
            if (skillshot.Unit.Team == Program.Player.Team)
            {
                return;
            }
            if (skillshot.Start.Distance(PlayerPosition)
                > (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }
            var alreadyAdded =
                DetectedSkillshots.Where(
                    i => i.SpellData.SpellName == skillshot.SpellData.SpellName && i.Unit.Compare(skillshot.Unit))
                    .Any(
                        i =>
                        i.Direction.AngleBetween(skillshot.Direction) < 5
                        && (i.Start.Distance(skillshot.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0));
            if (alreadyAdded && !skillshot.SpellData.DontCheckForDuplicates)
            {
                return;
            }
            if (skillshot.DetectionType == DetectionType.ProcessSpell)
            {
                if (skillshot.SpellData.MultipleNumber != -1)
                {
                    var originalDirection = skillshot.Direction;
                    for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                         i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                         i++)
                    {
                        var end = skillshot.Start
                                  + skillshot.SpellData.Range
                                  * originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                        DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.Start,
                                end,
                                skillshot.Unit));
                    }
                    return;
                }
                if (skillshot.SpellData.SpellName == "UFSlash")
                {
                    skillshot.SpellData.MissileSpeed = 1600 + (int)skillshot.Unit.MoveSpeed;
                }
                if (skillshot.SpellData.SpellName == "SionR")
                {
                    skillshot.SpellData.MissileSpeed = (int)skillshot.Unit.MoveSpeed;
                }
                if (skillshot.SpellData.Invert)
                {
                    var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                    var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start,
                            end,
                            skillshot.Unit));
                    return;
                }
                if (skillshot.SpellData.Centered)
                {
                    var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                    var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            start,
                            end,
                            skillshot.Unit));
                    return;
                }
                if (skillshot.SpellData.SpellName == "TaricE")
                {
                    var source = skillshot.Unit as Obj_AI_Hero;
                    if (source != null && source.ChampionName == "Taric")
                    {
                        var target =
                            GameObjects.Heroes.FirstOrDefault(
                                h => h.Team == source.Team && h.IsVisible && h.HasBuff("taricwleashactive"));
                        if (target != null)
                        {
                            var start = target.ServerPosition.ToVector2();
                            var direction = (skillshot.OriginalEnd - start).Normalized();
                            var end = start + direction * skillshot.SpellData.Range;
                            DetectedSkillshots.Add(
                                new Skillshot(
                                    skillshot.DetectionType,
                                    skillshot.SpellData,
                                    skillshot.StartTick,
                                    start,
                                    end,
                                    target) { OriginalEnd = skillshot.OriginalEnd });
                        }
                    }
                }
                if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                {
                    const int Angle = 60;
                    var edge1 =
                        (skillshot.End - skillshot.Unit.ServerPosition.ToVector2()).Rotated(
                            -Angle / 2f * (float)Math.PI / 180);
                    var edge2 = edge1.Rotated(Angle * (float)Math.PI / 180);
                    var positions = new List<Vector2>();
                    var explodingQ = DetectedSkillshots.FirstOrDefault(s => s.SpellData.SpellName == "SyndraQ");
                    if (explodingQ != null)
                    {
                        positions.Add(explodingQ.End);
                    }
                    positions.AddRange(
                        GameObjects.EnemyMinions.Where(i => !i.IsDead && i.Name == "Seed")
                            .Select(minion => minion.ServerPosition.ToVector2()));
                    foreach (var pos in positions.Where(i => skillshot.Unit.Distance(i) < 800))
                    {
                        var v = pos - skillshot.Unit.ServerPosition.ToVector2();
                        if (edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0)
                        {
                            var start = pos;
                            var end = skillshot.Unit.ServerPosition.ToVector2()
                                .Extend(pos, skillshot.Unit.Distance(pos) > 200 ? 1300 : 1000);
                            DetectedSkillshots.Add(
                                new Skillshot(
                                    skillshot.DetectionType,
                                    skillshot.SpellData,
                                    skillshot.StartTick + (int)(150 + skillshot.Unit.Distance(pos) / 2.5),
                                    start,
                                    end,
                                    skillshot.Unit));
                        }
                    }
                    return;
                }
                if (skillshot.SpellData.SpellName == "MalzaharQ")
                {
                    var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                    var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            start,
                            end,
                            skillshot.Unit));
                    return;
                }
                if (skillshot.SpellData.SpellName == "ZyraQ")
                {
                    var start = skillshot.End - skillshot.Direction.Perpendicular() * 450;
                    var end = skillshot.End + skillshot.Direction.Perpendicular() * 450;
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            start,
                            end,
                            skillshot.Unit));
                    return;
                }
                if (skillshot.SpellData.SpellName == "DianaArc")
                {
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            SpellDatabase.GetByName("DianaArcArc"),
                            skillshot.StartTick,
                            skillshot.Start,
                            skillshot.End,
                            skillshot.Unit));
                }
                if (skillshot.SpellData.SpellName == "ZiggsQ")
                {
                    var d1 = skillshot.Start.Distance(skillshot.End);
                    var d2 = d1 * 0.4f;
                    var d3 = d2 * 0.69f;
                    var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                    var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");
                    var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                    var bounce2Pos = bounce1Pos + skillshot.Direction * d3;
                    bounce1SpellData.Delay =
                        (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                    bounce2SpellData.Delay =
                        (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            bounce1SpellData,
                            skillshot.StartTick,
                            skillshot.End,
                            bounce1Pos,
                            skillshot.Unit));
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            bounce2SpellData,
                            skillshot.StartTick,
                            bounce1Pos,
                            bounce2Pos,
                            skillshot.Unit));
                }
                if (skillshot.SpellData.SpellName == "ZiggsR")
                {
                    skillshot.SpellData.Delay =
                        (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                }
                if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                {
                    var endPos = new Vector2();
                    foreach (var s in
                        DetectedSkillshots.Where(i => i.SpellData.Slot == SpellSlot.E))
                    {
                        if (!s.Unit.Compare(skillshot.Unit))
                        {
                            continue;
                        }
                        var extendedE = new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start,
                            skillshot.End + skillshot.Direction * 100,
                            skillshot.Unit);
                        if (!extendedE.IsSafe(s.End))
                        {
                            endPos = s.End;
                        }
                        break;
                    }
                    foreach (var m in
                        ObjectManager.Get<Obj_AI_Minion>().Where(i => i.CharData.BaseSkinName == "jarvanivstandard"))
                    {
                        if (m.Team != skillshot.Unit.Team)
                        {
                            continue;
                        }
                        var extendedE = new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start,
                            skillshot.End + skillshot.Direction * 100,
                            skillshot.Unit);
                        if (!extendedE.IsSafe(m.Position.ToVector2()))
                        {
                            endPos = m.Position.ToVector2();
                        }
                        break;
                    }
                    if (endPos.IsValid())
                    {
                        skillshot = new Skillshot(
                            DetectionType.ProcessSpell,
                            SpellDatabase.GetByName("JarvanIVEQ"),
                            Variables.TickCount,
                            skillshot.Start,
                            endPos,
                            skillshot.Unit);
                        skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                        skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                    }
                }
            }
            if (skillshot.SpellData.SpellName == "OriannasQ")
            {
                DetectedSkillshots.Add(
                    new Skillshot(
                        skillshot.DetectionType,
                        SpellDatabase.GetByName("OriannaQend"),
                        skillshot.StartTick,
                        skillshot.Start,
                        skillshot.End,
                        skillshot.Unit));
            }
            if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
            {
                return;
            }
            DetectedSkillshots.Add(skillshot);
        }

        #endregion

        private struct IsSafeResult
        {
            #region Fields

            internal bool IsSafe;

            internal List<Skillshot> SkillshotList;

            #endregion
        }
    }
}
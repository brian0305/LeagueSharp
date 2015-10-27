namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class Evade
    {
        #region Static Fields

        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        public static int LastWardJumpAttempt = 0;

        #endregion

        #region Delegates

        public delegate void EvadingH();

        public delegate void TryEvadingH(List<Skillshot> skillshot, Vector2 to);

        #endregion

        #region Public Events

        public static event EvadingH Evading;

        public static event TryEvadingH TryEvading;

        #endregion

        #region Public Properties

        public static Vector2 PlayerPosition => ObjectManager.Player.ServerPosition.ToVector2();

        #endregion

        #region Public Methods and Operators

        public static void Init()
        {
            Config.CreateMenu(Program.MainMenu);
            Collision.Init();
            Game.OnUpdate += args =>
                {
                    DetectedSkillshots.RemoveAll(i => !i.IsActive);
                    foreach (var skillshot in DetectedSkillshots)
                    {
                        skillshot.OnUpdate();
                    }
                    if (!Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active)
                    {
                        return;
                    }
                    if (ObjectManager.Player.IsDead)
                    {
                        return;
                    }
                    if (InterruptableSpell.IsCastingInterruptableSpell(ObjectManager.Player, true))
                    {
                        return;
                    }
                    if (ObjectManager.Player.HasBuffOfType(BuffType.SpellShield)
                        || ObjectManager.Player.HasBuffOfType(BuffType.SpellImmunity))
                    {
                        return;
                    }
                    Evading?.Invoke();
                    var currentPath = ObjectManager.Player.GetWaypoints();
                    var safePoint = IsSafePoint(PlayerPosition);
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
                                var skillshotToAdd = new Skillshot(
                                    DetectionType.ProcessSpell,
                                    spellData,
                                    Variables.TickCount,
                                    missile.Position.ToVector2(),
                                    missile.Position.ToVector2() + i * direction * spellData.Range,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                    }
                };
            Drawing.OnDraw += args =>
                {
                    if (Program.MainMenu["Evade"]["Draw"]["Status"])
                    {
                        var active = Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active;
                        var text =
                            $"Evade Skillshot: {(active ? (Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active ? "Dangerous" : "On") : "Off")}";
                        var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                        Drawing.DrawText(
                            pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                            pos.Y + 40,
                            active
                                ? (Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active
                                       ? Color.Yellow
                                       : Color.White)
                                : Color.Gray,
                            text);
                    }
                    if (Program.MainMenu["Evade"]["Draw"]["Skillshot"])
                    {
                        foreach (var skillshot in DetectedSkillshots)
                        {
                            skillshot.Draw(
                                skillshot.Enable && Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active
                                    ? Color.White
                                    : Color.Red,
                                Color.LimeGreen);
                        }
                    }
                };
        }

        public static bool IsAboutToHit(Obj_AI_Base unit, int time)
        {
            return SkillshotAboutToHit(unit, time).Count > 0;
        }

        public static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
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

        public static IsSafeResult IsSafePoint(Vector2 point)
        {
            var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
            foreach (var skillshot in DetectedSkillshots.Where(i => i.Enable && i.IsDanger(point)))
            {
                result.SkillshotList.Add(skillshot);
            }
            result.IsSafe = result.SkillshotList.Count == 0;
            return result;
        }

        public static bool IsSafeToBlink(Vector2 point, int timeOffset, int delay)
        {
            return DetectedSkillshots.Where(i => i.Enable).All(i => i.IsSafeToBlink(point, timeOffset, delay));
        }

        public static List<Skillshot> SkillshotAboutToHit(Obj_AI_Base unit, int time, bool onlyWindWall = false)
        {
            time += 150;
            return
                DetectedSkillshots.Where(
                    i =>
                    i.Enable && i.IsAboutToHit(time, unit)
                    && (!onlyWindWall || i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)))
                    .OrderBy(i => i.DangerLevel)
                    .ToList();
        }

        #endregion

        #region Methods

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            var alreadyAdded = false;
            foreach (var item in DetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName
                    && item.Unit.NetworkId == skillshot.Unit.NetworkId
                    && skillshot.Direction.AngleBetween(item.Direction) < 5
                    && (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0))
                {
                    alreadyAdded = true;
                }
            }
            if (skillshot.Unit.Team == ObjectManager.Player.Team)
            {
                return;
            }
            if (skillshot.Start.Distance(PlayerPosition)
                > (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }
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
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start,
                            end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
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
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType,
                        skillshot.SpellData,
                        skillshot.StartTick,
                        skillshot.Start,
                        end,
                        skillshot.Unit);
                    DetectedSkillshots.Add(skillshotToAdd);
                    return;
                }
                if (skillshot.SpellData.Centered)
                {
                    var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                    var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType,
                        skillshot.SpellData,
                        skillshot.StartTick,
                        start,
                        end,
                        skillshot.Unit);
                    DetectedSkillshots.Add(skillshotToAdd);
                    return;
                }
                if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                {
                    const int Angle = 60;
                    var edge1 =
                        (skillshot.End - skillshot.Unit.ServerPosition.ToVector2()).Rotated(
                            -Angle / 2f * (float)Math.PI / 180);
                    var edge2 = edge1.Rotated(Angle * (float)Math.PI / 180);
                    foreach (var skillshotToAdd in
                        from minion in
                            GameObjects.EnemyMinions.Where(i => i.Name == "Seed" && i.Distance(skillshot.Unit) < 800)
                        let v = minion.ServerPosition.ToVector2() - skillshot.Unit.ServerPosition.ToVector2()
                        where edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0
                        let start = minion.ServerPosition.ToVector2()
                        let end =
                            skillshot.Unit.ServerPosition.ToVector2()
                            .Extend(
                                minion.ServerPosition.ToVector2(),
                                skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000)
                        select
                            new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            start,
                            end,
                            skillshot.Unit))
                    {
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                    return;
                }
                if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                {
                    var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                    var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType,
                        skillshot.SpellData,
                        skillshot.StartTick,
                        start,
                        end,
                        skillshot.Unit);
                    DetectedSkillshots.Add(skillshotToAdd);
                    return;
                }
                if (skillshot.SpellData.SpellName == "DianaArc")
                {
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType,
                        SpellDatabase.GetByName("DianaArcArc"),
                        skillshot.StartTick,
                        skillshot.Start,
                        skillshot.End,
                        skillshot.Unit);

                    DetectedSkillshots.Add(skillshotToAdd);
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
                    var bounce1 = new Skillshot(
                        skillshot.DetectionType,
                        bounce1SpellData,
                        skillshot.StartTick,
                        skillshot.End,
                        bounce1Pos,
                        skillshot.Unit);
                    var bounce2 = new Skillshot(
                        skillshot.DetectionType,
                        bounce2SpellData,
                        skillshot.StartTick,
                        bounce1Pos,
                        bounce2Pos,
                        skillshot.Unit);
                    DetectedSkillshots.Add(bounce1);
                    DetectedSkillshots.Add(bounce2);
                }
                if (skillshot.SpellData.SpellName == "ZiggsR")
                {
                    skillshot.SpellData.Delay =
                        (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                }
                if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                {
                    var endPos = Vector2.Zero;
                    foreach (var s in DetectedSkillshots)
                    {
                        if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                        {
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
                    }
                    foreach (var m in ObjectManager.Get<Obj_AI_Minion>())
                    {
                        if (m.CharData.BaseSkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team)
                        {
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
                var skillshotToAdd = new Skillshot(
                    skillshot.DetectionType,
                    SpellDatabase.GetByName("OriannaQend"),
                    skillshot.StartTick,
                    skillshot.Start,
                    skillshot.End,
                    skillshot.Unit);
                DetectedSkillshots.Add(skillshotToAdd);
            }
            if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
            {
                return;
            }
            DetectedSkillshots.Add(skillshot);
        }

        #endregion

        public struct IsSafeResult
        {
            #region Fields

            public bool IsSafe;

            public List<Skillshot> SkillshotList;

            #endregion
        }
    }
}
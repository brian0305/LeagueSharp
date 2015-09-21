namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal static class Evade
    {
        #region Static Fields

        public static List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        #endregion

        #region Properties

        internal static Vector2 PlayerPosition
        {
            get
            {
                return ObjectManager.Player.ServerPosition.ToVector2();
            }
        }

        #endregion

        #region Public Methods and Operators

        public static void Init()
        {
            Program.MainMenu.CreateMenu();
            Collision.Init();
            Game.OnUpdate += args =>
                {
                    DetectedSkillshots.RemoveAll(i => !i.IsActive);
                    foreach (var skillshot in DetectedSkillshots)
                    {
                        skillshot.OnUpdate();
                    }
                };
            Drawing.OnDraw += args =>
                {
                    if (ObjectManager.Player.IsDead || !Program.MainMenu["Evade"]["DrawStatus"])
                    {
                        return;
                    }
                    var active = Program.MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active;
                    var text = string.Format(
                        "Evade Skillshot: {0}",
                        active
                            ? (Program.MainMenu["Evade"]["OnlyDangerous"].GetValue<MenuKeyBind>().Active
                                   ? "Dangerous"
                                   : "On")
                            : "Off");
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
                };
            SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
            SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
        }

        #endregion

        #region Methods

        private static void OnDeleteMissile(Skillshot skillshot, MissileClient missile)
        {
            if (skillshot.SpellData.SpellName != "VelkozQ"
                || DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") != 0)
            {
                return;
            }
            var spellData = SpellDatabase.GetByName("VelkozQSplit");
            for (var i = -1; i <= 1; i = i + 2)
            {
                DetectedSkillshots.Add(
                    new Skillshot(
                        DetectionType.ProcessSpell,
                        spellData,
                        Variables.TickCount,
                        missile.Position.ToVector2(),
                        missile.Position.ToVector2() + i * skillshot.Direction.Perpendicular() * spellData.Range,
                        skillshot.Unit));
            }
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            var alreadyAdded =
                DetectedSkillshots.Any(
                    i =>
                    i.SpellData.SpellName == skillshot.SpellData.SpellName
                    && i.Unit.NetworkId == skillshot.Unit.NetworkId && skillshot.Direction.AngleBetween(i.Direction) < 5
                    && (skillshot.Start.Distance(i.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0));
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
                        DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.Start,
                                skillshot.Start
                                + skillshot.SpellData.Range
                                * originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i),
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
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start,
                            skillshot.Start
                            + -(skillshot.End - skillshot.Start).Normalized() * skillshot.Start.Distance(skillshot.End),
                            skillshot.Unit));
                    return;
                }
                if (skillshot.SpellData.Centered)
                {
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.Start - skillshot.Direction * skillshot.SpellData.Range,
                            skillshot.Start + skillshot.Direction * skillshot.SpellData.Range,
                            skillshot.Unit));
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
                        from orb in
                            GameObjects.EnemyMinions.Where(i => i.Name == "Seed" && i.Distance(skillshot.Unit) < 800)
                        let v = (orb.ServerPosition - skillshot.Unit.ServerPosition).ToVector2()
                        where edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0
                        select
                            new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            orb.ServerPosition.ToVector2(),
                            skillshot.Unit.ServerPosition.Extend(
                                orb.ServerPosition,
                                skillshot.Unit.Distance(orb) > 200 ? 1300 : 1000).ToVector2(),
                            skillshot.Unit))
                    {
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                    return;
                }
                if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                {
                    DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            skillshot.SpellData,
                            skillshot.StartTick,
                            skillshot.End - skillshot.Direction.Perpendicular() * 400,
                            skillshot.End + skillshot.Direction.Perpendicular() * 400,
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
                        if (s.Unit.NetworkId != skillshot.Unit.NetworkId)
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
                        if (!extendedE.IsSafePoint(s.End))
                        {
                            endPos = s.End;
                        }
                        break;
                    }
                    foreach (var m in
                        GameObjects.Minions.Where(i => i.CharData.BaseSkinName == "jarvanivstandard"))
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
                        if (!extendedE.IsSafePoint(m.Position.ToVector2()))
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
                            endPos + 200 * (endPos - skillshot.Start).Normalized(),
                            skillshot.Unit);
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
            if ((skillshot.SpellData.DisableFowDetection
                 || Program.MainMenu["Evade"][skillshot.SpellData.ChampionName.ToLowerInvariant()][
                     skillshot.SpellData.SpellName]["DisableFoW"])
                && skillshot.DetectionType == DetectionType.RecvPacket)
            {
                return;
            }
            DetectedSkillshots.Add(skillshot);
        }

        #endregion
    }
}
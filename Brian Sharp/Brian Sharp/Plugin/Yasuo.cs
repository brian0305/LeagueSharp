namespace BrianSharp.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using BrianSharp.Common;
    using BrianSharp.Evade;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;
    using Orbwalk = BrianSharp.Common.Orbwalker;

    internal class Yasuo : Helper
    {
        #region Constants

        private const int QRange = 550, Q2Range = 1150, QCirWidth = 275, QCirWidthMin = 250, RWidth = 400;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 500);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 1300);
            Q.SetSkillshot(GetQDelay, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(GetQ2Delay, 90, 1500, false, SkillshotType.SkillshotLine);
            E.SetTargetted(0.05f, 1000);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QStack", "-> Stack Q While Gap (E Gap Must On)", false);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EDmg", "-> Q3 Circle (Q Must On)");
                    AddBool(comboMenu, "EGap", "-> Gap Closer");
                    AddSlider(comboMenu, "EGapRange", "-> If Enemy Not In", 300, 1, 475);
                    AddBool(comboMenu, "EGapTower", "-> Under Tower", false);
                    AddBool(comboMenu, "R", "Use R");
                    AddBool(comboMenu, "RDelay", "-> Delay");
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle, true);
                    AddBool(harassMenu, "AutoQ3", "-> Use Q3", false);
                    AddBool(harassMenu, "AutoQTower", "-> Under Tower", false);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "Q3", "-> Use Q3");
                    AddBool(harassMenu, "QTower", "-> Under Tower");
                    AddBool(harassMenu, "QLastHit", "-> Last Hit (Q1/Q2)");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "Q3", "-> Use Q3");
                    AddBool(clearMenu, "E", "Use E");
                    AddBool(clearMenu, "ETower", "-> Under Tower", false);
                    AddBool(clearMenu, "Item", "Use Tiamat/Hydra Item");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    AddBool(lastHitMenu, "Q3", "-> Use Q3", false);
                    AddBool(lastHitMenu, "E", "Use E");
                    AddBool(lastHitMenu, "ETower", "-> Under Tower", false);
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "E", "Use E");
                    AddBool(fleeMenu, "EStackQ", "-> Stack Q While Dashing");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    if (HeroManager.Enemies.Any())
                    {
                        EvadeSkillshot.Init(miscMenu);
                        EvadeTarget.Init(miscMenu);
                    }
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "Q", "Use Q3");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                interruptMenu,
                                spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    AddKeybind(miscMenu, "StackQ", "Auto Stack Q", "Z", KeyBindType.Toggle);
                    AddBool(miscMenu, "StackQDraw", "-> Draw Text");
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        #endregion

        #region Properties

        private static float GetQ2Delay
        {
            get
            {
                return 0.5f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static float GetQDelay
        {
            get
            {
                return 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static bool HaveQ3
        {
            get
            {
                return Player.HasBuff("YasuoQ3W");
            }
        }

        private static Obj_AI_Hero QCirTarget
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos.To3D();
                var target = TargetSelector.GetTarget(QCirWidth, TargetSelector.DamageType.Physical, true, null, pos);
                return target != null && Player.Distance(pos) < 150 ? target : null;
            }
        }

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active || Player.IsDashing()
                || (HaveQ3 && !GetValue<bool>("Harass", "AutoQ3"))
                || (UnderTower(Player.ServerPosition) && !GetValue<bool>("Harass", "AutoQTower")))
            {
                return;
            }
            var target = TargetSelector.GetTarget(!HaveQ3 ? QRange : Q2Range, TargetSelector.DamageType.Physical);
            if (target == null)
            {
                return;
            }
            (!HaveQ3 ? Q : Q2).Cast(target, PacketCast, true);
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        private static bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback);
        }

        private static bool CastQCir(Obj_AI_Base target)
        {
            return target.IsValidTarget(QCirWidthMin - target.BoundingRadius) && Q.Cast(Game.CursorPos, PacketCast);
        }

        private static void Clear()
        {
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var minionObj =
                    GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(i => CanCastE(i) && (!UnderTower(PosAfterE(i)) || GetValue<bool>("Clear", "ETower")))
                        .ToList();
                if (minionObj.Any())
                {
                    var obj = minionObj.FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                    if (obj == null && GetValue<bool>("Clear", "Q") && Q.IsReady(50)
                        && (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
                    {
                        obj = (from i in minionObj
                               let sub = GetMinions(PosAfterE(i), QCirWidth, MinionTypes.All, MinionTeam.NotAlly)
                               where
                                   i.Team == GameObjectTeam.Neutral
                                   || (i.Distance(PosAfterE(i)) < QCirWidthMin && CanKill(i, GetEDmg(i) + GetQDmg(i)))
                                   || sub.Any(a => CanKill(a, GetQDmg(a))) || sub.Count > 1
                               select i).MaxOrDefault(
                                   i => GetMinions(PosAfterE(i), QCirWidth, MinionTypes.All, MinionTeam.NotAlly).Count);
                    }
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && (!HaveQ3 || GetValue<bool>("Clear", "Q3")))
            {
                if (Player.IsDashing())
                {
                    var minionObj = GetMinions(
                        Player.GetDashInfo().EndPos.To3D(),
                        QCirWidth,
                        MinionTypes.All,
                        MinionTeam.NotAlly);
                    if ((minionObj.Any(i => CanKill(i, GetQDmg(i)) || i.Team == GameObjectTeam.Neutral)
                         || minionObj.Count > 1) && Player.Distance(Player.GetDashInfo().EndPos) < 150
                        && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                    {
                        return;
                    }
                }
                else
                {
                    var minionObj = GetMinions(
                        !HaveQ3 ? QRange : Q2Range,
                        MinionTypes.All,
                        MinionTeam.NotAlly,
                        MinionOrderTypes.MaxHealth);
                    if (minionObj.Any())
                    {
                        if (!HaveQ3)
                        {
                            var obj = minionObj.FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                            if (obj != null && Q.Cast(obj, PacketCast).IsCasted())
                            {
                                return;
                            }
                        }
                        var qMinHit = Q.MinHitChance;
                        Q.MinHitChance = HitChance.Medium;
                        var pos = (!HaveQ3 ? Q : Q2).GetLineFarmLocation(minionObj.Cast<Obj_AI_Base>().ToList());
                        Q.MinHitChance = qMinHit;
                        if (pos.MinionsHit > 0 && (!HaveQ3 ? Q : Q2).Cast(pos.Position, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>("Clear", "Item") && (Hydra.IsReady() || Tiamat.IsReady()))
            {
                var minionObj = GetMinions(
                    (Hydra.IsReady() ? Hydra : Tiamat).Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                if (minionObj.Count > 2
                    || minionObj.Any(
                        i => i.MaxHealth >= 1200 && i.Distance(Player) < (Hydra.IsReady() ? Hydra : Tiamat).Range - 80))
                {
                    if (Tiamat.IsReady())
                    {
                        Tiamat.Cast();
                    }
                    if (Hydra.IsReady())
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.IsReady())
                {
                    var obj = (from enemy in HeroManager.Enemies.Where(i => R.IsInRange(i) && CanCastR(i))
                               let sub = enemy.GetEnemiesInRange(RWidth).Where(CanCastR).ToList()
                               where
                                   (sub.Count > 1 && R.IsKillable(enemy))
                                   || sub.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value)
                                   || sub.Count >= GetValue<Slider>(mode, "RCountA").Value
                               orderby sub.Count descending
                               select enemy).ToList();
                    if (obj.Any())
                    {
                        var target = !GetValue<bool>(mode, "RDelay")
                                         ? obj.FirstOrDefault()
                                         : obj.Where(i => TimeLeftR(i) * 1000 < 150 + Game.Ping * 2)
                                               .MinOrDefault(TimeLeftR);
                        if (target != null && R.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                }
                if (GetValue<bool>(mode, "E") && E.IsReady())
                {
                    if (GetValue<bool>(mode, "EDmg") && GetValue<bool>(mode, "Q") && HaveQ3 && Q.IsReady(50))
                    {
                        var target = TargetSelector.GetTarget(QRange, TargetSelector.DamageType.Physical);
                        if (target != null)
                        {
                            var obj = GetNearObj(target, true);
                            if (obj != null && E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                    if (GetValue<bool>(mode, "EGap"))
                    {
                        var target = TargetSelector.GetTarget(QRange, TargetSelector.DamageType.Physical)
                                     ?? TargetSelector.GetTarget(Q2Range, TargetSelector.DamageType.Physical);
                        if (target != null)
                        {
                            var obj = GetNearObj(target);
                            if (obj != null
                                && (obj.NetworkId != target.NetworkId
                                        ? Player.Distance(target) > GetValue<Slider>(mode, "EGapRange").Value
                                        : !Orbwalk.InAutoAttackRange(target))
                                && (!UnderTower(PosAfterE(obj)) || GetValue<bool>(mode, "EGapTower"))
                                && E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                if (mode == "Combo"
                    || ((!HaveQ3 || GetValue<bool>(mode, "Q3"))
                        && (!UnderTower(Player.ServerPosition) || GetValue<bool>(mode, "QTower"))))
                {
                    if (Player.IsDashing())
                    {
                        if (QCirTarget != null && CastQCir(QCirTarget))
                        {
                            return;
                        }
                        if (!HaveQ3 && mode == "Combo" && GetValue<bool>(mode, "QStack") && GetValue<bool>(mode, "E")
                            && GetValue<bool>(mode, "EGap") && Q.GetTarget(200) == null)
                        {
                            var minionObj = GetMinions(
                                Player.GetDashInfo().EndPos.To3D(),
                                QCirWidth,
                                MinionTypes.All,
                                MinionTeam.NotAlly);
                            if (minionObj.Any() && Player.Distance(Player.GetDashInfo().EndPos) < 150
                                && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        var target = TargetSelector.GetTarget(
                            !HaveQ3 ? QRange : Q2Range,
                            TargetSelector.DamageType.Physical);
                        if (target != null)
                        {
                            if (!HaveQ3)
                            {
                                if (Q.Cast(target, PacketCast, true).IsCasted())
                                {
                                    return;
                                }
                            }
                            else
                            {
                                var hit = -1;
                                var predPos = new Vector3();
                                foreach (var hero in HeroManager.Enemies.Where(i => i.IsValidTarget(Q2Range)))
                                {
                                    var pred = Q2.GetPrediction(hero, true);
                                    if (pred.Hitchance >= Q2.MinHitChance && pred.AoeTargetsHitCount > hit)
                                    {
                                        hit = pred.AoeTargetsHitCount;
                                        predPos = pred.CastPosition;
                                    }
                                }
                                if (predPos.IsValid())
                                {
                                    if (Q2.Cast(predPos, PacketCast))
                                    {
                                        return;
                                    }
                                }
                                else if (Q2.Cast(target, PacketCast, true).IsCasted())
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                if (mode == "Harass" && GetValue<bool>(mode, "QLastHit") && Q.GetTarget(100) == null && !HaveQ3
                    && !Player.IsDashing())
                {
                    var obj =
                        GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                            .FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                    if (obj != null)
                    {
                        Q.Cast(obj, PacketCast, true);
                    }
                }
            }
        }

        private static void Flee()
        {
            if (!GetValue<bool>("Flee", "E"))
            {
                return;
            }
            if (GetValue<bool>("Flee", "EStackQ") && Q.IsReady() && !HaveQ3 && Player.IsDashing())
            {
                if (QCirTarget != null && CastQCir(QCirTarget))
                {
                    return;
                }
                var minionObj = GetMinions(
                    Player.GetDashInfo().EndPos.To3D(),
                    QCirWidth,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                if (minionObj.Any() && Player.Distance(Player.GetDashInfo().EndPos) < 150
                    && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                {
                    return;
                }
            }
            var obj = GetNearObj();
            if (obj == null || !E.IsReady())
            {
                return;
            }
            E.CastOnUnit(obj, PacketCast);
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalcDamage(
                target,
                Damage.DamageType.Magical,
                (50 + 20 * E.Level) * (1 + Math.Max(0, Player.GetBuffCount("YasuoDashScalar") * 0.25))
                + 0.6 * Player.FlatMagicDamageMod);
        }

        private static Obj_AI_Base GetNearObj(Obj_AI_Base target = null, bool inQCir = false)
        {
            var pos = target != null
                          ? Prediction.GetPrediction(target, E.Delay, 0, E.Speed).UnitPosition
                          : Game.CursorPos;
            var obj = new List<Obj_AI_Base>();
            obj.AddRange(GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly));
            obj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)));
            return
                obj.Where(
                    i =>
                    CanCastE(i) && pos.Distance(PosAfterE(i)) < (inQCir ? QCirWidthMin : Player.Distance(pos))
                    && EvadeSkillshot.IsSafePoint(PosAfterE(i).To2D()).IsSafe)
                    .MinOrDefault(i => pos.Distance(PosAfterE(i)));
        }

        private static double GetQDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            var k = 1d;
            var reduction = 0d;
            var dmg = Player.TotalAttackDamage * (Player.Crit >= 0.85f ? (Items.HasItem(3031) ? 1.875 : 1.5) : 1)
                      + dmgItem;
            if (Items.HasItem(3153))
            {
                var dmgBotrk = Math.Max(0.08 * target.Health, 10);
                if (target.IsValid<Obj_AI_Minion>())
                {
                    dmgBotrk = Math.Min(dmgBotrk, 60);
                }
                dmg += dmgBotrk;
            }
            if (target.IsValid<Obj_AI_Hero>())
            {
                var hero = (Obj_AI_Hero)target;
                if (Items.HasItem(3047, hero))
                {
                    k *= 0.9d;
                }
                if (hero.ChampionName == "Fizz")
                {
                    reduction += hero.Level > 15
                                     ? 14
                                     : (hero.Level > 12
                                            ? 12
                                            : (hero.Level > 9 ? 10 : (hero.Level > 6 ? 8 : (hero.Level > 3 ? 6 : 4))));
                }
                var mastery = hero.Masteries.FirstOrDefault(m => m.Page == MasteryPage.Defense && m.Id == 65);
                if (mastery != null && mastery.Points > 0)
                {
                    reduction += 1 * mastery.Points;
                }
            }
            return Player.CalcDamage(target, Damage.DamageType.Physical, 20 * Q.Level + (dmg - reduction) * k)
                   + (Player.GetBuffCount("ItemStatikShankCharge") == 100
                          ? Player.CalcDamage(
                              target,
                              Damage.DamageType.Magical,
                              100 * (Player.Crit >= 0.85f ? (Items.HasItem(3031) ? 2.25 : 1.8) : 1))
                          : 0);
        }

        private static void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    var target = QCirTarget;
                    if (target != null && CanKill(target, GetQDmg(target)) && CastQCir(target))
                    {
                        return;
                    }
                }
                else
                {
                    var target = TargetSelector.GetTarget(
                        !HaveQ3 ? QRange : Q2Range,
                        TargetSelector.DamageType.Physical);
                    if (target != null && CanKill(target, GetQDmg(target))
                        && (!HaveQ3 ? Q : Q2).Cast(target, PacketCast).IsCasted())
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastE(i)));
                if (target != null
                    && (CanKill(target, GetEDmg(target))
                        || (GetValue<bool>("KillSteal", "Q") && Q.IsReady(50)
                            && CanKill(target, GetEDmg(target) + GetQDmg(target)))) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastR(i)));
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void LastHit()
        {
            if (GetValue<bool>("LastHit", "Q") && Q.IsReady() && !Player.IsDashing()
                && (!HaveQ3 || GetValue<bool>("LastHit", "Q3")))
            {
                var obj =
                    GetMinions(
                        !HaveQ3 ? QRange : Q2Range,
                        MinionTypes.All,
                        MinionTeam.NotAlly,
                        MinionOrderTypes.MaxHealth).FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                if (obj != null && (!HaveQ3 ? Q : Q2).Cast(obj, PacketCast, true).IsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>("LastHit", "E") && E.IsReady())
            {
                var obj =
                    GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(
                            i =>
                            CanCastE(i)
                            && (!Orbwalk.InAutoAttackRange(i) || i.Health > Player.GetAutoAttackDamage(i, true))
                            && (!UnderTower(PosAfterE(i)) || GetValue<bool>("LastHit", "ETower")))
                        .FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<KeyBind>("Misc", "StackQ").Active && GetValue<bool>("Misc", "StackQDraw"))
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, "Auto Stack Q");
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position,
                    Player.IsDashing() ? QCirWidth : (!HaveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q")
                || !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !HaveQ3)
            {
                return;
            }
            if (E.IsReady() && Q.IsReady(50))
            {
                if (E.IsInRange(unit) && CanCastE(unit) && unit.Distance(PosAfterE(unit)) < QCirWidthMin
                    && E.CastOnUnit(unit, PacketCast))
                {
                    return;
                }
                if (E.IsInRange(unit, E.Range + QCirWidthMin))
                {
                    var obj = GetNearObj(unit, true);
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (!Q.IsReady())
            {
                return;
            }
            if (Player.IsDashing())
            {
                var pos = Player.GetDashInfo().EndPos;
                if (Player.Distance(pos) < 150 && unit.Distance(pos) < QCirWidth)
                {
                    CastQCir(unit);
                }
            }
            else
            {
                Q2.Cast(unit, PacketCast);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!Equals(Q.Delay, GetQDelay))
            {
                Q.Delay = GetQDelay;
            }
            if (!Equals(Q2.Delay, GetQ2Delay))
            {
                Q2.Delay = GetQ2Delay;
            }
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            AutoQ();
            KillSteal();
            StackQ();
        }

        private static Vector3 PosAfterE(Obj_AI_Base target)
        {
            return Player.ServerPosition.Extend(
                target.ServerPosition,
                Player.Distance(target) < 410 ? E.Range : Player.Distance(target) + 65);
        }

        private static void StackQ()
        {
            if (!GetValue<KeyBind>("Misc", "StackQ").Active || !Q.IsReady() || Player.IsDashing() || HaveQ3)
            {
                return;
            }
            var target = Q.GetTarget();
            if (target != null && (!UnderTower(Player.ServerPosition) || !UnderTower(target.ServerPosition)))
            {
                Q.Cast(target, PacketCast, true);
            }
            else
            {
                var minionObj = GetMinions(QRange, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (!minionObj.Any())
                {
                    return;
                }
                var obj = minionObj.FirstOrDefault(i => CanKill(i, GetQDmg(i)))
                          ?? minionObj.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
                }
            }
        }

        private static float TimeLeftR(Obj_AI_Hero target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockback || i.Type == BuffType.Knockup);
            return buff != null ? buff.EndTime - Game.Time : -1;
        }

        private static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + Player.BoundingRadius);
        }

        #endregion

        protected class EvadeSkillshot
        {
            #region Public Methods and Operators

            public static void Init(Menu menu)
            {
                var evadeMenu = new Menu("Evade Skillshot", "EvadeSkillshot");
                {
                    evadeMenu.AddItem(new MenuItem("Credit", "Credit: Evade#"));
                    var evadeSpells = new Menu("Spells", "Spells");
                    {
                        foreach (var spell in EvadeSpellDatabase.Spells)
                        {
                            var sub = new Menu(spell.Name + " (" + spell.Slot + ")", "ESSS_" + spell.Name);
                            {
                                if (spell.Name == "YasuoDashWrapper")
                                {
                                    AddBool(sub, "ETower", "Under Tower", false);
                                }
                                else if (spell.Name == "YasuoWMovingWall")
                                {
                                    AddSlider(sub, "WDelay", "Extra Delay", 100, 0, 150);
                                }
                                AddSlider(sub, "DangerLevel", "If Danger Level >=", spell.DangerLevel, 1, 5);
                                AddBool(sub, "Enabled", "Enabled", false);
                                evadeSpells.AddSubMenu(sub);
                            }
                        }
                        evadeMenu.AddSubMenu(evadeSpells);
                    }
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => SpellDatabase.Spells.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        evadeMenu.AddSubMenu(new Menu("-> " + hero.ChampionName, "EvadeSS_" + hero.ChampionName));
                    }
                    foreach (var spell in
                        SpellDatabase.Spells.Where(i => HeroManager.Enemies.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        var sub = new Menu(spell.SpellName + " (" + spell.Slot + ")", "ESS_" + spell.MenuItemName);
                        {
                            AddSlider(sub, "DangerLevel", "Danger Level", spell.DangerValue, 1, 5);
                            AddBool(sub, "Enabled", "Enabled", !spell.DisabledByDefault);
                            evadeMenu.SubMenu("EvadeSS_" + spell.ChampionName).AddSubMenu(sub);
                        }
                    }
                }
                menu.AddSubMenu(evadeMenu);
                Collisions.Init();
                Game.OnUpdate += OnUpdateEvade;
                SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
                SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
            }

            public static IsSafeResult IsSafePoint(Vector2 point)
            {
                var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
                foreach (var skillshot in
                    SkillshotDetector.DetectedSkillshots.Where(i => i.Evade && !i.IsSafePoint(point)))
                {
                    result.SkillshotList.Add(skillshot);
                }
                result.IsSafe = result.SkillshotList.Count == 0;
                return result;
            }

            #endregion

            #region Methods

            private static IEnumerable<Obj_AI_Base> GetEvadeTargets(
                EvadeSpellData spell,
                bool onlyGood = false,
                bool dontCheckForSafety = false)
            {
                var badTargets = new List<Obj_AI_Base>();
                var goodTargets = new List<Obj_AI_Base>();
                var allTargets = new List<Obj_AI_Base>();
                foreach (var targetType in spell.ValidTargets)
                {
                    switch (targetType)
                    {
                        case SpellTargets.AllyChampions:
                            allTargets.AddRange(
                                HeroManager.Allies.Where(i => i.IsValidTarget(spell.MaxRange, false) && !i.IsMe));
                            break;
                        case SpellTargets.AllyMinions:
                            allTargets.AddRange(GetMinions(spell.MaxRange, MinionTypes.All, MinionTeam.Ally));
                            break;
                        case SpellTargets.AllyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<Obj_AI_Minion>()
                                    .Where(
                                        i =>
                                        IsWard(i) && i.IsValidTarget(spell.MaxRange, false) && i.Team == Player.Team));
                            break;
                        case SpellTargets.EnemyChampions:
                            allTargets.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(spell.MaxRange)));
                            break;
                        case SpellTargets.EnemyMinions:
                            allTargets.AddRange(GetMinions(spell.MaxRange, MinionTypes.All, MinionTeam.NotAlly));
                            break;
                        case SpellTargets.EnemyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<Obj_AI_Minion>()
                                    .Where(i => IsWard(i) && i.IsValidTarget(spell.MaxRange)));
                            break;
                    }
                }
                foreach (var target in
                    allTargets.Where(i => dontCheckForSafety || IsSafePoint(i.ServerPosition.To2D()).IsSafe))
                {
                    if (spell.Name == "YasuoDashWrapper" && target.HasBuff("YasuoDashWrapper"))
                    {
                        continue;
                    }
                    var pathToTarget = new List<Vector2> { Player.ServerPosition.To2D(), target.ServerPosition.To2D() };
                    if (IsSafePath(pathToTarget, Configs.EvadingFirstTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }
                    if (IsSafePath(pathToTarget, Configs.EvadingSecondTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        badTargets.Add(target);
                    }
                }
                return goodTargets.Any() ? goodTargets : (onlyGood ? new List<Obj_AI_Base>() : badTargets);
            }

            private static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
            {
                var isSafe = false;
                var intersections = new List<FoundIntersection>();
                var intersection = new FoundIntersection();
                foreach (var sResult in
                    SkillshotDetector.DetectedSkillshots.Where(i => i.Evade)
                        .Select(i => i.IsSafePath(path, timeOffset, speed, delay)))
                {
                    isSafe = sResult.IsSafe;
                    if (sResult.Intersection.Valid)
                    {
                        intersections.Add(sResult.Intersection);
                    }
                }
                return isSafe
                           ? new SafePathResult(true, intersection)
                           : new SafePathResult(
                                 false,
                                 intersections.Count > 0 ? intersections.MinOrDefault(i => i.Distance) : intersection);
            }

            private static void OnDeleteMissile(Skillshot skillshot, MissileClient missile)
            {
                if (skillshot.SpellData.SpellName != "VelkozQ"
                    || SkillshotDetector.DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") != 0)
                {
                    return;
                }
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                for (var i = -1; i <= 1; i = i + 2)
                {
                    SkillshotDetector.DetectedSkillshots.Add(
                        new Skillshot(
                            DetectionType.ProcessSpell,
                            spellData,
                            Utils.GameTimeTickCount,
                            missile.Position.To2D(),
                            missile.Position.To2D() + i * skillshot.Perpendicular * spellData.Range,
                            skillshot.Unit));
                }
            }

            private static void OnDetectSkillshot(Skillshot skillshot)
            {
                var alreadyAdded =
                    SkillshotDetector.DetectedSkillshots.Any(
                        i =>
                        i.SpellData.SpellName == skillshot.SpellData.SpellName
                        && i.Unit.NetworkId == skillshot.Unit.NetworkId
                        && skillshot.Direction.AngleBetween(i.Direction) < 5
                        && (skillshot.Start.Distance(i.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0));
                if (skillshot.Unit.Team == Player.Team)
                {
                    return;
                }
                if (skillshot.Start.Distance(Player.ServerPosition.To2D())
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
                            SkillshotDetector.DetectedSkillshots.Add(
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
                        SkillshotDetector.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.Start,
                                skillshot.Start
                                + -(skillshot.End - skillshot.Start).Normalized()
                                * skillshot.Start.Distance(skillshot.End),
                                skillshot.Unit));
                        return;
                    }
                    if (skillshot.SpellData.Centered)
                    {
                        SkillshotDetector.DetectedSkillshots.Add(
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
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                -Angle / 2f * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(Angle * (float)Math.PI / 180);
                        foreach (var skillshotToAdd in from minion in ObjectManager.Get<Obj_AI_Minion>()
                                                       let v =
                                                           (minion.ServerPosition - skillshot.Unit.ServerPosition).To2D(
                                                               )
                                                       where
                                                           minion.Name == "Seed" && edge1.CrossProduct(v) > 0
                                                           && v.CrossProduct(edge2) > 0
                                                           && minion.Distance(skillshot.Unit) < 800
                                                           && minion.Team != Player.Team
                                                       let start = minion.ServerPosition.To2D()
                                                       let end =
                                                           skillshot.Unit.ServerPosition.Extend(
                                                               minion.ServerPosition,
                                                               skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000)
                                                           .To2D()
                                                       select
                                                           new Skillshot(
                                                           skillshot.DetectionType,
                                                           skillshot.SpellData,
                                                           skillshot.StartTick,
                                                           start,
                                                           end,
                                                           skillshot.Unit))
                        {
                            SkillshotDetector.DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        SkillshotDetector.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.End - skillshot.Perpendicular * 400,
                                skillshot.End + skillshot.Perpendicular * 400,
                                skillshot.Unit));
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "DianaArc")
                    {
                        SkillshotDetector.DetectedSkillshots.Add(
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
                        SkillshotDetector.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                bounce1SpellData,
                                skillshot.StartTick,
                                skillshot.End,
                                bounce1Pos,
                                skillshot.Unit));
                        SkillshotDetector.DetectedSkillshots.Add(
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
                        foreach (var s in SkillshotDetector.DetectedSkillshots)
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
                                if (!extendedE.IsSafePoint(s.End))
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
                                if (!extendedE.IsSafePoint(m.Position.To2D()))
                                {
                                    endPos = m.Position.To2D();
                                }
                                break;
                            }
                        }
                        if (endPos.IsValid())
                        {
                            skillshot = new Skillshot(
                                DetectionType.ProcessSpell,
                                SpellDatabase.GetByName("JarvanIVEQ"),
                                Utils.GameTimeTickCount,
                                skillshot.Start,
                                endPos + 200 * (endPos - skillshot.Start).Normalized(),
                                skillshot.Unit);
                        }
                    }
                }
                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    SkillshotDetector.DetectedSkillshots.Add(
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
                SkillshotDetector.DetectedSkillshots.Add(skillshot);
            }

            private static void OnUpdateEvade(EventArgs args)
            {
                SkillshotDetector.DetectedSkillshots.RemoveAll(i => !i.IsActive);
                foreach (var skillshot in SkillshotDetector.DetectedSkillshots)
                {
                    skillshot.OnUpdate();
                }
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                var safePoint = IsSafePoint(Player.ServerPosition.To2D());
                var safePath = IsSafePath(Player.GetWaypoints(), 100);
                if (!safePath.IsSafe && !safePoint.IsSafe)
                {
                    TryToEvade(safePoint.SkillshotList, Game.CursorPos.To2D());
                }
            }

            private static void TryToEvade(List<Skillshot> hitBy, Vector2 to)
            {
                var dangerLevel =
                    hitBy.Select(i => GetValue<Slider>("ESS_" + i.SpellData.MenuItemName, "DangerLevel").Value)
                        .Concat(new[] { 0 })
                        .Max();
                foreach (var evadeSpell in
                    EvadeSpellDatabase.Spells.Where(i => i.Enabled && i.DangerLevel <= dangerLevel && i.IsReady)
                        .OrderBy(i => i.DangerLevel))
                {
                    if (evadeSpell.EvadeType == EvadeTypes.Dash && evadeSpell.CastType == CastTypes.Target)
                    {
                        var targets =
                            GetEvadeTargets(evadeSpell)
                                .Where(
                                    i =>
                                    IsSafePoint(PosAfterE(i).To2D()).IsSafe
                                    && (!UnderTower(PosAfterE(i)) || GetValue<bool>("ESSS_" + evadeSpell.Name, "ETower")))
                                .ToList();
                        if (targets.Count > 0)
                        {
                            var closestTarget = targets.MinOrDefault(i => PosAfterE(i).To2D().Distance(to));
                            Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget);
                            return;
                        }
                    }
                    if (evadeSpell.EvadeType == EvadeTypes.WindWall
                        && hitBy.Where(
                            i =>
                            i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)
                            && i.IsAboutToHit(
                                150 + evadeSpell.Delay - GetValue<Slider>("ESSS_" + evadeSpell.Name, "WDelay").Value,
                                Player))
                               .OrderByDescending(
                                   i => GetValue<Slider>("ESS_" + i.SpellData.MenuItemName, "DangerLevel").Value)
                               .Any(
                                   i =>
                                   Player.Spellbook.CastSpell(
                                       evadeSpell.Slot,
                                       Player.ServerPosition.Extend(i.Start.To3D(), 100))))
                    {
                        return;
                    }
                }
            }

            #endregion

            internal struct IsSafeResult
            {
                #region Fields

                public bool IsSafe;

                public List<Skillshot> SkillshotList;

                #endregion
            }
        }

        protected class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            private static Vector2 wallCastedPos;

            #endregion

            #region Properties

            private static GameObject Wall
            {
                get
                {
                    return
                        ObjectManager.Get<GameObject>()
                            .FirstOrDefault(
                                i => i.IsValid && Regex.IsMatch(i.Name, "_w_windwall.\\.troy", RegexOptions.IgnoreCase));
                }
            }

            #endregion

            #region Public Methods and Operators

            public static void Init(Menu menu)
            {
                LoadSpellData();
                var evadeMenu = new Menu("Evade Target", "EvadeTarget");
                {
                    AddBool(evadeMenu, "W", "Use W");
                    AddBool(evadeMenu, "E", "Use E (To Dash Behind WindWall)");
                    AddBool(evadeMenu, "ETower", "-> Under Tower", false);
                    AddBool(evadeMenu, "BAttack", "Basic Attack");
                    AddSlider(evadeMenu, "BAttackHpU", "-> If Hp <", 35);
                    AddBool(evadeMenu, "CAttack", "Crit Attack");
                    AddSlider(evadeMenu, "CAttackHpU", "-> If Hp <", 40);
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => Spells.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        evadeMenu.AddSubMenu(new Menu("-> " + hero.ChampionName, "ET_" + hero.ChampionName));
                    }
                    foreach (
                        var spell in Spells.Where(i => HeroManager.Enemies.Any(a => a.ChampionName == i.ChampionName)))
                    {
                        AddBool(
                            evadeMenu.SubMenu("ET_" + spell.ChampionName),
                            spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            false);
                    }
                }
                menu.AddSubMenu(evadeMenu);
                Game.OnUpdate += OnUpdateTarget;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            }

            #endregion

            #region Methods

            private static bool GoThroughWall(Vector2 pos1, Vector2 pos2)
            {
                if (Wall == null)
                {
                    return false;
                }
                var wallWidth = 300 + 50 * Convert.ToInt32(Wall.Name.Substring(Wall.Name.Length - 6, 1));
                var wallDirection = (Wall.Position.To2D() - wallCastedPos).Normalized().Perpendicular();
                var wallStart = Wall.Position.To2D() + wallWidth / 2f * wallDirection;
                var wallEnd = wallStart - wallWidth * wallDirection;
                var wallPolygon = new Geometry.Polygon.Rectangle(wallStart, wallEnd, 75);
                var intersections = new List<Vector2>();
                for (var i = 0; i < wallPolygon.Points.Count; i++)
                {
                    var inter =
                        wallPolygon.Points[i].Intersection(
                            wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0],
                            pos1,
                            pos2);
                    if (inter.Intersects)
                    {
                        intersections.Add(inter.Point);
                    }
                }
                return intersections.Any();
            }

            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                        { ChampionName = "Ahri", SpellNames = new[] { "ahrifoxfiremissiletwo" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Akali", SpellNames = new[] { "akalimota" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Annie", SpellNames = new[] { "disintegrate" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Brand", SpellNames = new[] { "brandconflagrationmissile" }, Slot = SpellSlot.E
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Brand", SpellNames = new[] { "brandwildfire", "brandwildfiremissile" },
                            Slot = SpellSlot.R
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Caitlyn", SpellNames = new[] { "caitlynaceintheholemissile" },
                            Slot = SpellSlot.R
                        });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Cassiopeia", SpellNames = new[] { "cassiopeiatwinfang" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Ezreal", SpellNames = new[] { "ezrealarcaneshiftmissile" }, Slot = SpellSlot.E
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "FiddleSticks",
                            SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                            Slot = SpellSlot.E
                        });
                Spells.Add(
                    new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "parley" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Katarina", SpellNames = new[] { "katarinaq", "katarinaqmis" },
                            Slot = SpellSlot.Q
                        });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Kayle", SpellNames = new[] { "judicatorreckoning" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Leblanc", SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                            Slot = SpellSlot.Q
                        });
                Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "MissFortune",
                            SpellNames = new[] { "missfortunericochetshot", "missFortunershotextra" }, Slot = SpellSlot.Q
                        });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Nami", SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                            Slot = SpellSlot.W
                        });
                Spells.Add(
                    new SpellData { ChampionName = "Nunu", SpellNames = new[] { "iceblast" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Pantheon", SpellNames = new[] { "pantheonq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Ryze", SpellNames = new[] { "spellflux", "spellfluxmissile" },
                            Slot = SpellSlot.E
                        });
                Spells.Add(
                    new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Shen", SpellNames = new[] { "shenvorpalstar" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { ChampionName = "Taric", SpellNames = new[] { "dazzle" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Tristana", SpellNames = new[] { "detonatingshot" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Urgot", SpellNames = new[] { "urgotheatseekinghomemissile" },
                            Slot = SpellSlot.Q
                        });
                Spells.Add(
                    new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemn" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                        {
                            ChampionName = "Vladimir", SpellNames = new[] { "vladimirtidesofbloodnuke" },
                            Slot = SpellSlot.E
                        });
            }

            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                if (!sender.IsValid<MissileClient>())
                {
                    return;
                }
                var missile = (MissileClient)sender;
                if (!missile.SpellCaster.IsValid<Obj_AI_Hero>() || missile.SpellCaster.Team == Player.Team)
                {
                    return;
                }
                var unit = (Obj_AI_Hero)missile.SpellCaster;
                var spellData =
                    Spells.FirstOrDefault(
                        i =>
                        i.SpellNames.Contains(missile.SData.Name.ToLower())
                        && GetItem("ET_" + i.ChampionName, i.MissileName) != null
                        && GetValue<bool>("ET_" + i.ChampionName, i.MissileName));
                if (spellData == null && missile.SData.IsAutoAttack()
                    && (!missile.SData.Name.ToLower().Contains("crit")
                            ? GetValue<bool>("EvadeTarget", "BAttack")
                              && Player.HealthPercent < GetValue<Slider>("EvadeTarget", "BAttackHpU").Value
                            : GetValue<bool>("EvadeTarget", "CAttack")
                              && Player.HealthPercent < GetValue<Slider>("EvadeTarget", "CAttackHpU").Value))
                {
                    spellData = new SpellData
                                    { ChampionName = unit.ChampionName, SpellNames = new[] { missile.SData.Name } };
                }
                if (spellData == null || !missile.Target.IsMe)
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Start = unit.ServerPosition, Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                if (!sender.IsValid<MissileClient>())
                {
                    return;
                }
                var missile = (MissileClient)sender;
                if (missile.SpellCaster.IsValid<Obj_AI_Hero>() && missile.SpellCaster.Team != Player.Team)
                {
                    DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
                }
            }

            private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
            {
                if (!sender.IsValid || sender.Team != ObjectManager.Player.Team || args.SData.Name != "YasuoWMovingWall")
                {
                    return;
                }
                wallCastedPos = sender.ServerPosition.To2D();
            }

            private static void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady(300) && (Wall == null || !E.IsReady(200)))
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => Player.Distance(i.Obj.Position) < 700))
                {
                    if (E.IsReady() && GetValue<bool>("EvadeTarget", "E") && Wall != null
                        && Utils.TickCount - W.LastCastAttemptT > 1000
                        && !GoThroughWall(Player.ServerPosition.To2D(), target.Obj.Position.To2D())
                        && W.IsInRange(target.Obj, 250))
                    {
                        var obj = new List<Obj_AI_Base>();
                        obj.AddRange(GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly));
                        obj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)));
                        if (
                            obj.Where(
                                i =>
                                CanCastE(i) && EvadeSkillshot.IsSafePoint(i.ServerPosition.To2D()).IsSafe
                                && EvadeSkillshot.IsSafePoint(PosAfterE(i).To2D()).IsSafe
                                && (!UnderTower(PosAfterE(i)) || GetValue<bool>("EvadeTarget", "ETower"))
                                && GoThroughWall(Player.ServerPosition.To2D(), PosAfterE(i).To2D()))
                                .OrderBy(i => PosAfterE(i).Distance(Game.CursorPos))
                                .Any(i => E.CastOnUnit(i, PacketCast)))
                        {
                            return;
                        }
                    }
                    if (W.IsReady() && GetValue<bool>("EvadeTarget", "W") && W.IsInRange(target.Obj, 500)
                        && W.Cast(Player.ServerPosition.Extend(target.Start, 100), PacketCast))
                    {
                        return;
                    }
                }
            }

            #endregion

            private class SpellData
            {
                #region Fields

                public string ChampionName;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                #endregion

                #region Public Properties

                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.First();
                    }
                }

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                public Vector3 Start;

                #endregion
            }
        }
    }
}
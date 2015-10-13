﻿namespace Valvrave_Sharp.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class LeeSin : Program
    {
        #region Static Fields

        private static float lastWardT;

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 400);
            E2 = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R, 800);
            Q.SetSkillshot(0.25f, 55, 1800, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0, 1, 1500, false, SkillshotType.SkillshotLine);
            R2.SetSkillshot(0.25f, 100, 1500, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = R2.MinHitChance = HitChance.VeryHigh;

            Insec.Init();
            var orbwalkMenu = new Menu("Orbwalk", "Orbwalk");
            {
                orbwalkMenu.KeyBind("Star", "Star Combo", Keys.X);
                orbwalkMenu.Bool("Q", "Use Q");
                orbwalkMenu.Bool("QCol", "-> Smite Collision");
                orbwalkMenu.Bool("E", "Use E");
                orbwalkMenu.Separator("R Settings");
                orbwalkMenu.Bool("R", "Use R");
                orbwalkMenu.Bool("RKill", "If Kill Enemy Behind");
                orbwalkMenu.Slider("RCountA", "Or Hit Enemy Behind >=", 1, 1, 4);
                orbwalkMenu.Separator("Sub Settings");
                orbwalkMenu.Bool("Ignite", "Use Ignite");
                orbwalkMenu.Bool("Item", "Use Item");
                MainMenu.Add(orbwalkMenu);
            }
            var farmMenu = new Menu("Farm", "Farm");
            {
                farmMenu.Bool("Q", "Use Q1");
                MainMenu.Add(farmMenu);
            }
            var ksMenu = new Menu("KillSteal", "Kill Steal");
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
                ksMenu.Bool("R", "Use R");
                ksMenu.Separator("Extra R Settings");
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    ksMenu.Bool("RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName, false);
                }
                MainMenu.Add(ksMenu);
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
                MainMenu.Add(drawMenu);
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += args =>
                {
                    if (Player.IsDead)
                    {
                        return;
                    }
                    if (MainMenu["Draw"]["Q"] && Q.Level > 0)
                    {
                        Drawing.DrawCircle(
                            Player.Position,
                            (IsQOne ? Q : Q2).Range,
                            Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
                    }
                    if (MainMenu["Draw"]["W"] && W.Level > 0 && IsWOne)
                    {
                        Drawing.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
                    }
                    if (MainMenu["Draw"]["E"] && E.Level > 0)
                    {
                        Drawing.DrawCircle(
                            Player.Position,
                            (IsEOne ? E : E2).Range,
                            E.IsReady() ? Color.LimeGreen : Color.IndianRed);
                    }
                    if (MainMenu["Draw"]["R"] && R.Level > 0)
                    {
                        Drawing.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
                    }
                };
        }

        #endregion

        #region Properties

        private static bool CanR
        {
            get
            {
                return !R.IsReady() && Variables.TickCount - R.LastCastAttemptT > 300
                       && Variables.TickCount - R.LastCastAttemptT < 1500;
            }
        }

        private static Obj_AI_Base GetQObj
        {
            get
            {
                var obj = new List<Obj_AI_Base>();
                obj.AddRange(GameObjects.EnemyHeroes);
                obj.AddRange(GameObjects.Jungle);
                obj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion()));
                return obj.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
            }
        }

        private static int GetSmiteDmg
        {
            get
            {
                return
                    new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                        Player.Level - 1];
            }
        }

        private static bool IsEOne
        {
            get
            {
                return E.Instance.SData.Name.ToLower().Contains("one");
            }
        }

        private static bool IsQOne
        {
            get
            {
                return Q.Instance.SData.Name.ToLower().Contains("one");
            }
        }

        private static bool IsWOne
        {
            get
            {
                return W.Instance.SData.Name.ToLower().Contains("one");
            }
        }

        private static int Passive
        {
            get
            {
                return Player.GetBuffCount("BlindMonkFlurry");
            }
        }

        #endregion

        #region Methods

        private static bool CanE2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time <= 0.1f;
        }

        private static bool CanQ2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time <= 0.1f;
        }

        private static void Farm()
        {
            if (!MainMenu["Farm"]["Q"] || !Q.IsReady() || !IsQOne)
            {
                return;
            }
            var minion =
                GameObjects.EnemyMinions.Where(
                    i =>
                    i.IsValidTarget(Q.Range) && i.IsMinion()
                    && Q.GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i)
                    && (!i.InAutoAttackRange()
                            ? Q.GetHealthPrediction(i) > 0
                            : i.Health > Player.GetAutoAttackDamage(i, true)))
                    .OrderByDescending(i => i.MaxHealth)
                    .Select(i => Q.VPrediction(i))
                    .FirstOrDefault(i => i.Hitchance >= Q.MinHitChance);
            if (minion != null)
            {
                Q.Cast(minion.CastPosition);
            }
        }

        private static void Flee(Vector3 pos, bool isStar = false)
        {
            if (!W.IsReady() || !IsWOne || Variables.TickCount - W.LastCastAttemptT <= 500)
            {
                return;
            }
            var posJump = Player.ServerPosition.Extend(pos, Math.Min(W.Range, Player.Position.Distance(pos)));
            var objNear = new List<Obj_AI_Base>();
            objNear.AddRange(GameObjects.AllyHeroes.Where(i => !i.IsMe));
            objNear.AddRange(
                GameObjects.AllyMinions.Where(i => i.IsMinion() || i.CharData.BaseSkinName == "jarvanivstandard"));
            objNear.AddRange(GameObjects.AllyWards);
            var objJump =
                objNear.Where(i => i.IsValidTarget(W.Range, false) && i.Distance(posJump) <= (isStar ? R.Range : 250))
                    .MinOrDefault(i => i.Distance(posJump));
            if (objJump != null)
            {
                if (W.CastOnUnit(objJump))
                {
                    W.LastCastAttemptT = Variables.TickCount;
                }
            }
            else if (Game.Time - lastWardT >= 3)
            {
                var ward = Items.GetWardSlot();
                if (ward == null)
                {
                    return;
                }
                var posPlace = Player.ServerPosition.Extend(pos, Math.Min(590, Player.Position.Distance(pos)));
                if (Player.Spellbook.CastSpell(ward.SpellSlot, posPlace))
                {
                    lastWardT = Game.Time;
                }
            }
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Magical,
                new[] { 60, 95, 130, 165, 200 }[E.Level - 1] + Player.FlatPhysicalDamageMod);
        }

        private static double GetQ2Dmg(Obj_AI_Base target, double subHp = 0)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod
                      + 0.08 * (target.MaxHealth - (target.Health - subHp));
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                target is Obj_AI_Minion ? Math.Min(dmg, 400) : dmg) + subHp;
        }

        private static double GetQDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod);
        }

        private static double GetR2Dmg(Obj_AI_Base target, Obj_AI_Base firstTarget)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 200, 400, 600 }[R.Level - 1] + 2 * Player.FlatPhysicalDamageMod
                + new[] { 0.12, 0.15, 0.18 }[R.Level - 1] * (firstTarget.MaxHealth - firstTarget.Health));
        }

        private static double GetRDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 200, 400, 600 }[R.Level - 1] + 2 * Player.FlatPhysicalDamageMod);
        }

        private static bool HaveE(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkTempest");
        }

        private static bool HaveQ(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkSonicWave");
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                if (IsQOne)
                {
                    var targets =
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            i.IsValidTarget(Q.Range)
                            && (i.Health + i.PhysicalShield <= GetQDmg(i)
                                || (i.Health + i.PhysicalShield <= GetQ2Dmg(i, GetQDmg(i))
                                    && Player.Mana - Q.Instance.ManaCost >= 30))).ToList();
                    if (targets.Count > 0
                        && targets.Select(i => Q.VPrediction(i))
                               .Where(i => i.Hitchance >= Q.MinHitChance)
                               .Any(i => Q.Cast(i.CastPosition)))
                    {
                        return;
                    }
                }
                else
                {
                    var target =
                        GameObjects.EnemyHeroes.FirstOrDefault(
                            i => i.IsValidTarget(Q2.Range) && HaveQ(i) && i.Health + i.PhysicalShield <= GetQ2Dmg(i));
                    if (target != null && Q.Cast())
                    {
                        return;
                    }
                }
                if (MainMenu["KillSteal"]["E"] && E.IsReady() && IsEOne)
                {
                    var target = E.GetTarget();
                    if (target != null && target.Health + target.MagicalShield <= GetEDmg(target) && E.Cast())
                    {
                        return;
                    }
                }
                if (MainMenu["KillSteal"]["R"] && R.IsReady())
                {
                    var target =
                        TargetSelector.GetTarget(
                            GameObjects.EnemyHeroes.Where(
                                i =>
                                i.IsValidTarget(R.Range) && MainMenu["KillSteal"]["RCast" + i.ChampionName]
                                && i.Health + i.PhysicalShield <= GetRDmg(i)),
                            R.DamageType);
                    if (target != null)
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Orbwalk:
                    Orbwalk();
                    break;
                case OrbwalkerMode.LastHit:
                    Farm();
                    break;
            }
            if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
            {
                Orbwalker.MoveOrder(Game.CursorPos);
                Flee(Game.CursorPos);
            }
            if (MainMenu["Orbwalk"]["Star"].GetValue<MenuKeyBind>().Active)
            {
                Star();
            }
        }

        private static void Orbwalk()
        {
            if (MainMenu["Orbwalk"]["Q"] && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        var pred = Q.VPrediction(target);
                        if (pred.Hitchance == HitChance.Collision)
                        {
                            if (MainMenu["Orbwalk"]["QCol"] && Smite.IsReady()
                                && !pred.CollisionObjects.Any(i => i.IsMe))
                            {
                                var col = pred.CollisionObjects.Cast<Obj_AI_Minion>().ToList();
                                if (col.Count == 1
                                    && col.Any(i => i.Health <= GetSmiteDmg && Player.Distance(i) < SmiteRange)
                                    && Player.Spellbook.CastSpell(Smite, col.First()))
                                {
                                    DelayAction.Add(Game.Ping / 2, () => Q.Cast(pred.CastPosition));
                                    return;
                                }
                            }
                        }
                        else if (pred.Hitchance >= Q.MinHitChance && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
                    if (target != null)
                    {
                        if ((CanQ2(target) || (!target.InAutoAttackRange() && CanR)
                             || target.Health + target.PhysicalShield
                             <= GetQ2Dmg(target) + Player.GetAutoAttackDamage(target, true)
                             || Player.Distance(target) > target.GetRealAutoAttackRange() + 100 || Passive == -1)
                            && Q.Cast())
                        {
                            return;
                        }
                    }
                    else if (GetQObj != null)
                    {
                        var q2Target = Q2.GetTarget();
                        if (q2Target != null && GetQObj.Distance(q2Target) < Player.Distance(q2Target)
                            && GetQObj.Distance(q2Target) < E.Range && Q.Cast())
                        {
                            return;
                        }
                    }
                }
            }
            if (MainMenu["Orbwalk"]["E"] && E.IsReady())
            {
                if (IsEOne)
                {
                    if (E.GetTarget() != null && Player.Mana >= 70 && E.Cast())
                    {
                        return;
                    }
                }
                else if (
                    GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E2.Range) && HaveE(i))
                        .Any(i => CanE2(i) || Player.Distance(i) > i.GetRealAutoAttackRange() + 50 || Passive == -1)
                    && Player.Mana >= 50 && E.Cast())
                {
                    return;
                }
            }
            if (MainMenu["Orbwalk"]["R"] && R.IsReady())
            {
                if (MainMenu["Orbwalk"]["Q"] && Q.IsReady() && !IsQOne)
                {
                    var target =
                        GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(R.Range) && HaveQ(i))
                            .FirstOrDefault(
                                i =>
                                i.Health + i.PhysicalShield
                                <= GetQ2Dmg(i, GetRDmg(i)) + Player.GetAutoAttackDamage(i, true));
                    if (target != null && R.CastOnUnit(target))
                    {
                        return;
                    }
                }
                foreach (var hero in
                    GameObjects.EnemyHeroes.Where(
                        i => i.IsValidTarget(R.Range) && i.Health + i.PhysicalShield > GetRDmg(i)))
                {
                    R2.UpdateSourcePosition(hero.ServerPosition, hero.ServerPosition);
                    var heroBehind =
                        (from behind in
                             GameObjects.EnemyHeroes.Where(
                                 i => i.IsValidTarget(R2.Range, true, R2.From) && !i.Compare(hero))
                         let predPos = Prediction.GetPrediction(behind, R2.Delay, 1, R2.Speed).UnitPosition
                         where
                             R2.WillHit(
                                 predPos,
                                 hero.ServerPosition.Extend(Player.ServerPosition, -R2.Range),
                                 (int)hero.BoundingRadius)
                         select behind).ToList();
                    if (heroBehind.Count == 0)
                    {
                        break;
                    }
                    if (MainMenu["Orbwalk"]["RKill"] && heroBehind.Any(i => i.Health + i.PhysicalShield <= GetRDmg(i))
                        && R.CastOnUnit(hero))
                    {
                        return;
                    }
                    if (heroBehind.Count >= MainMenu["Orbwalk"]["RCountA"] && R.CastOnUnit(hero))
                    {
                        return;
                    }
                }
            }
            var subTarget = W.GetTarget();
            if (MainMenu["Orbwalk"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget == null)
            {
                return;
            }
            if (MainMenu["Orbwalk"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 30
                && Player.Distance(subTarget) <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Star()
        {
            var target = (IsQOne ? Q : Q2).GetTarget();
            Orbwalker.Orbwalk(target.InAutoAttackRange() ? target : null);
            if (target == null)
            {
                return;
            }
            if (Q.IsReady())
            {
                if (IsQOne)
                {
                    if (Q.Casting(target) == CastStates.SuccessfullyCasted)
                    {
                        return;
                    }
                }
                else if (HaveQ(target)
                         && (target.Health + target.PhysicalShield
                             <= GetQ2Dmg(target) + Player.GetAutoAttackDamage(target, true)
                             || (!target.InAutoAttackRange() && CanR)) && Q.Cast())
                {
                    return;
                }
            }
            if (E.IsReady() && IsEOne && E.IsInRange(target) && HaveQ(target) && CanR && Player.Mana >= 80 && E.Cast())
            {
                return;
            }
            if (!R.IsReady() || !Q.IsReady() || IsQOne || !HaveQ(target))
            {
                return;
            }
            if (R.IsInRange(target))
            {
                R.CastOnUnit(target);
            }
            else if (Player.Distance(target) <= W.Range + R.Range - 100 && Player.Mana >= 60)
            {
                Flee(Prediction.GetPrediction(target, W.Delay, W.Width, W.Speed).UnitPosition, true);
            }
        }

        private static void UseItem(Obj_AI_Hero target)
        {
            if (target != null && (target.HealthPercent < 40 || Player.HealthPercent < 50))
            {
                if (Bilgewater.IsReady)
                {
                    Bilgewater.Cast(target);
                }
                if (BotRuinedKing.IsReady)
                {
                    BotRuinedKing.Cast(target);
                }
            }
            if (Youmuu.IsReady && Player.CountEnemy(W.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Player.CountEnemy(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Player.CountEnemy(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady && Player.CountEnemy(Player.GetRealAutoAttackRange()) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion

        internal class Insec
        {
            #region Static Fields

            private static readonly int KickRange = (int)R2.Range;

            private static Vector3 insecPos = Vector3.Zero;

            private static Vector3 insecPosAdv = Vector3.Zero;

            private static Obj_AI_Hero insecTarget;

            private static int lastFlash;

            private static int lastWard;

            #endregion

            #region Properties

            private static float DistBehind
            {
                get
                {
                    return
                        Math.Min(
                            (Player.BoundingRadius + insecTarget.BoundingRadius + 80)
                            * (MainMenu["Insec"]["Dist"] + 100) / 100,
                            R.Range);
                }
            }

            private static bool IsReadyAdvanced
            {
                get
                {
                    return Flash.IsReady() && R.IsReady() && insecTarget != null && PosInsecTo.IsValid();
                }
            }

            private static bool IsReadyNormal
            {
                get
                {
                    return ((W.IsReady() && IsWOne && Items.GetWardSlot() != null)
                            || (MainMenu["Insec"]["Flash"] && Flash.IsReady()) || RecentInsec(4000)) && R.IsReady()
                           && insecTarget != null && PosInsecTo.IsValid();
                }
            }

            private static Vector3 PosAfterInsec
            {
                get
                {
                    return insecTarget.ServerPosition.Extend(PosInsecTo, KickRange);
                }
            }

            private static Vector3 PosInsecTo
            {
                get
                {
                    var pos = Vector3.Zero;
                    switch (MainMenu["Insec"]["Mode"].GetValue<MenuList>().Index)
                    {
                        case 0:
                            var hero =
                                GameObjects.AllyHeroes.Where(
                                    i =>
                                    i.IsValidTarget(KickRange + 500, false, insecTarget.ServerPosition) && !i.IsMe
                                    && i.Distance(insecTarget) > 500)
                                    .OrderBy(i => i.MaxHealth)
                                    .MinOrDefault(i => i.Distance(insecTarget));
                            var turret =
                                GameObjects.AllyTurrets.Where(
                                    i =>
                                    !i.IsDead && i.Distance(Player) < 3000 && i.Distance(insecTarget) - KickRange < 1150)
                                    .MinOrDefault(i => i.Distance(insecTarget));
                            if (turret != null)
                            {
                                pos = turret.ServerPosition;
                            }
                            if (!pos.IsValid() && hero != null)
                            {
                                pos = hero.ServerPosition
                                      + (insecTarget.ServerPosition - hero.ServerPosition).Normalized().Perpendicular()
                                      * (hero.AttackRange + hero.BoundingRadius + insecTarget.BoundingRadius) / 2;
                            }
                            if (!pos.IsValid())
                            {
                                pos = Player.ServerPosition;
                            }
                            break;
                        case 1:
                            pos = Game.CursorPos;
                            break;
                        case 2:
                            pos = Player.ServerPosition;
                            break;
                    }
                    return insecPos.IsValid() ? insecPos : pos;
                }
            }

            #endregion

            #region Methods

            internal static void Init()
            {
                var insecMenu = new Menu("Insec", "Insec");
                {
                    insecMenu.Separator("Settings");
                    insecMenu.Bool("Flash", "Use Flash", false);
                    insecMenu.Bool("PriorFlash", "Priorize Flash Over WardJump", false);
                    insecMenu.List("Mode", "Mode", new[] { "Tower/Hero", "Mouse Position", "Current Position" }, 2);
                    insecMenu.Slider("Dist", "Extra Distance Behind (%)", 20);
                    insecMenu.Bool("Line", "Draw Line");
                    insecMenu.Separator("Keybinds");
                    insecMenu.KeyBind("Normal", "Normal Insec", Keys.T);
                    insecMenu.KeyBind("Advanced", "Advanced Insec (R-Flash)", Keys.Z);
                }
                MainMenu.Add(insecMenu);

                Game.OnUpdate += args =>
                    {
                        if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
                        {
                            return;
                        }
                        insecTarget = Q2.GetTarget();
                        if (!R.IsReady())
                        {
                            insecPos = Vector3.Zero;
                        }
                        if (MainMenu["Insec"]["Normal"].GetValue<MenuKeyBind>().Active
                            || MainMenu["Insec"]["Advanced"].GetValue<MenuKeyBind>().Active)
                        {
                            Orbwalker.Orbwalk(insecTarget.InAutoAttackRange() ? insecTarget : null);
                            if (MainMenu["Insec"]["Normal"].GetValue<MenuKeyBind>().Active && IsReadyNormal)
                            {
                                Normal();
                            }
                            if (MainMenu["Insec"]["Advanced"].GetValue<MenuKeyBind>().Active && IsReadyAdvanced)
                            {
                                Advanced();
                            }
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || !MainMenu["Insec"]["Line"] || R.Level == 0 || !R.IsReady()
                            || insecTarget == null || !PosInsecTo.IsValid())
                        {
                            return;
                        }
                        Drawing.DrawCircle(insecTarget.Position, insecTarget.BoundingRadius * 1.5f, Color.BlueViolet);
                        Drawing.DrawLine(
                            Drawing.WorldToScreen(insecTarget.ServerPosition),
                            Drawing.WorldToScreen(PosAfterInsec),
                            2,
                            Color.BlueViolet);
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe)
                        {
                            return;
                        }
                        if (args.SData.Name == "summonerflash")
                        {
                            lastFlash = Variables.TickCount;
                        }
                        if (args.SData.Name == "ItemGhostWard" || args.SData.Name == "sightward"
                            || args.SData.Name.StartsWith("TrinketTotemLvl"))
                        {
                            lastWard = Variables.TickCount;
                        }
                        if (MainMenu["Insec"]["Advanced"].GetValue<MenuKeyBind>().Active
                            && args.SData.Name == "BlindMonkRKick" && Flash.IsReady() && insecPosAdv.IsValid())
                        {
                            DelayAction.Add(
                                R.Delay / 2 * 1000,
                                () =>
                                    {
                                        Player.Spellbook.CastSpell(Flash, insecPosAdv);
                                        insecPosAdv = Vector3.Zero;
                                    });
                        }
                    };
                GameObject.OnCreate += (sender, args) =>
                    {
                        if (!MainMenu["Insec"]["Normal"].GetValue<MenuKeyBind>().Active || !IsReadyNormal
                            || !W.IsReady() || !IsWOne)
                        {
                            return;
                        }
                        var ward = sender as Obj_AI_Minion;
                        if (ward == null || ward.IsEnemy
                            || (!ward.CharData.BaseSkinName.ToLower().Contains("ward")
                                && !ward.CharData.BaseSkinName.ToLower().Contains("trinket")) || !W.IsInRange(ward)
                            || Variables.TickCount - lastWard >= 1000)
                        {
                            return;
                        }
                        W.CastOnUnit(ward);
                    };
            }

            private static void Advanced()
            {
                CastQ(true);
                if (!R.IsInRange(insecTarget))
                {
                    return;
                }
                var posPred = Prediction.GetPrediction(insecTarget, 0.05f, 1, float.MaxValue).UnitPosition;
                var posBehind = posPred + (posPred - PosAfterInsec).Normalized() * DistBehind;
                if (Player.Distance(posBehind) >= FlashRange)
                {
                    return;
                }
                insecPos = PosAfterInsec;
                DelayAction.Add(5000, () => insecPos = Vector3.Zero);
                R.CastOnUnit(insecTarget);
                insecPosAdv = posBehind;
            }

            private static void CastQ(bool isAdvanced = false)
            {
                if (Q.IsReady())
                {
                    if (IsQOne)
                    {
                        var state = Q.Casting(insecTarget);
                        if (state == CastStates.SuccessfullyCasted)
                        {
                            return;
                        }
                        if (state == CastStates.OutOfRange || state == CastStates.Collision
                            || state == CastStates.LowHitChance)
                        {
                            var nearObj = new List<Obj_AI_Base>();
                            nearObj.AddRange(GameObjects.EnemyHeroes);
                            nearObj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion()));
                            nearObj.AddRange(GameObjects.Jungle);
                            nearObj =
                                nearObj.Where(
                                    i =>
                                    i.IsValidTarget(Q.Range) && i.Health + i.PhysicalShield > GetQDmg(i)
                                    && i.Distance(insecTarget) < (isAdvanced ? R.Range - 50 : 600 - DistBehind))
                                    .OrderBy(i => i.Distance(insecTarget))
                                    .ToList();
                            if (nearObj.Count == 0)
                            {
                                return;
                            }
                            var bestObj =
                                nearObj.Select(i => Q.VPrediction(i)).FirstOrDefault(i => i.Hitchance >= Q.MinHitChance);
                            if (bestObj != null)
                            {
                                Q.Cast(bestObj.CastPosition);
                            }
                        }
                    }
                    else if (GetQObj != null
                             && (isAdvanced
                                     ? Flash.IsReady()
                                     : (W.IsReady() && IsWOne && Items.GetWardSlot() != null && Player.Mana >= 80)
                                       || (MainMenu["Insec"]["Flash"] && Flash.IsReady()))
                             && Player.Distance(insecTarget) > (isAdvanced ? R.Range : 600 - DistBehind)
                             && GetQObj.Distance(insecTarget) < (isAdvanced ? R.Range - 50 : 600 - DistBehind))
                    {
                        Q.Cast();
                    }
                }
            }

            private static void GapClose(bool isFlash = false)
            {
                var posPred =
                    Prediction.GetPrediction(
                        insecTarget,
                        isFlash ? 0.05f : W.Delay,
                        1,
                        isFlash ? float.MaxValue : W.Speed).UnitPosition;
                var posBehind = posPred + (posPred - PosAfterInsec).Normalized() * DistBehind;
                if (posBehind.Distance(PosAfterInsec) <= insecTarget.Distance(PosAfterInsec)
                    || posBehind.Distance(posPred) < 80)
                {
                    return;
                }
                if (isFlash)
                {
                    if (Player.Distance(posBehind) >= FlashRange || Player.Distance(posBehind) <= FlashRange / 2f)
                    {
                        return;
                    }
                    insecPos = PosAfterInsec;
                    DelayAction.Add(5000, () => insecPos = Vector3.Zero);
                    Player.Spellbook.CastSpell(Flash, posBehind);
                }
                else if (Player.Distance(posBehind) < 600 && posBehind.Distance(posPred) < R.Range - 75)
                {
                    insecPos = PosAfterInsec;
                    DelayAction.Add(5000, () => insecPos = Vector3.Zero);
                    Player.Spellbook.CastSpell(
                        Items.GetWardSlot().SpellSlot,
                        Player.ServerPosition.Extend(posBehind, Math.Min(600, Player.Position.Distance(posBehind))));
                }
            }

            private static void Normal()
            {
                CastQ();
                if (Player.Distance(insecTarget) < 600 - DistBehind
                    && Player.Distance(PosAfterInsec) < insecTarget.Distance(PosAfterInsec) && !RecentInsec())
                {
                    if (MainMenu["Insec"]["PriorFlash"])
                    {
                        if (MainMenu["Insec"]["Flash"] && Flash.IsReady())
                        {
                            GapClose(true);
                        }
                        else if (Items.GetWardSlot() != null && W.IsReady() && IsWOne)
                        {
                            GapClose();
                        }
                    }
                    else
                    {
                        if (Items.GetWardSlot() != null && W.IsReady() && IsWOne)
                        {
                            GapClose();
                        }
                        else if (MainMenu["Insec"]["Flash"] && Flash.IsReady())
                        {
                            GapClose(true);
                        }
                    }
                }
                if (R.IsInRange(insecTarget) && Player.Distance(PosAfterInsec) > insecTarget.Distance(PosAfterInsec))
                {
                    var project = PosAfterInsec.ProjectOn(
                        Player.ServerPosition,
                        Player.ServerPosition.Extend(insecTarget.ServerPosition, KickRange));
                    if (project.LinePoint.Distance(PosAfterInsec) < KickRange * 0.5)
                    {
                        R.CastOnUnit(insecTarget);
                    }
                }
            }

            private static bool RecentInsec(int time = 1000)
            {
                return Variables.TickCount - lastWard < time
                       || (MainMenu["Insec"]["Flash"] && Variables.TickCount - lastFlash < time);
            }

            #endregion
        }
    }
}
namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.UI.Menu;

    #endregion

    internal class Vladimir : Program
    {
        #region Static Fields

        private static BuffInstance buffE;

        private static bool haveQ, haveW, haveE;

        private static int lastE;

        #endregion

        #region Constructors and Destructors

        public Vladimir()
        {
            Q = new Spell(SpellSlot.Q, 600).SetTargetted(0.25f, float.MaxValue);
            W = new Spell(SpellSlot.W, 350);
            E = new Spell(SpellSlot.E, 630).SetSkillshot(0, 40, 4000, false, SkillshotType.SkillshotLine);
            R = new Spell(SpellSlot.R, 700).SetSkillshot(
                0.001f,
                375,
                float.MaxValue,
                false,
                SkillshotType.SkillshotCircle);
            Q.DamageType = W.DamageType = E.DamageType = R.DamageType = DamageType.Magical;
            R.MinHitChance = HitChance.VeryHigh;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q: Always On");
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("E", "Use E");
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Slider("RHpU", "If Enemies Hp < (%) And Hit >= 2", 60);
                comboMenu.Slider("RCountA", "Or Count >=", 3, 1, 5);
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.Separator("Q: Always On");
                hybridMenu.Bool("QLastHit", "Last Hit (Stack < 2)");
                hybridMenu.Bool("E", "Use E", false);
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q");
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Variables.Orbwalker.OnAction += (sender, args) =>
                {
                    if (!Q.IsReady() || args.Type != OrbwalkingType.BeforeAttack)
                    {
                        return;
                    }
                    var mode = Variables.Orbwalker.GetActiveMode();
                    var hero = args.Target as Obj_AI_Hero;
                    if (hero == null || (mode != OrbwalkingMode.Combo && mode != OrbwalkingMode.Hybrid))
                    {
                        return;
                    }
                    args.Process = !Q.IsInRange(hero);
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "VladimirQFrenzy":
                            haveQ = true;
                            break;
                        case "VladimirSanguinePool":
                            haveW = true;
                            break;
                        case "VladimirE":
                            buffE = args.Buff;
                            break;
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "VladimirQFrenzy":
                            haveQ = false;
                            break;
                        case "VladimirSanguinePool":
                            haveW = false;
                            break;
                        case "VladimirE":
                            buffE = null;
                            haveE = false;
                            break;
                    }
                };
            GameObjectNotifier<MissileClient>.OnCreate += (sender, args) =>
                {
                    var spellCaster = args.SpellCaster as Obj_AI_Hero;
                    if (spellCaster == null || !spellCaster.IsMe || args.SData.Name != "VladimirEMissile" || !haveE)
                    {
                        return;
                    }
                    haveE = false;
                    buffE = null;
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.SData.Name != "VladimirE" || haveE)
                    {
                        return;
                    }
                    haveE = true;
                };
        }

        #endregion

        #region Properties

        private static bool CanE => E.IsReady() || haveE;

        private static bool EChargeMax => haveE && buffE != null && Game.Time - buffE.StartTime >= 1;

        private static List<Tuple<Obj_AI_Hero, Vector3>> GetETarget
            =>
                Variables.TargetSelector.GetTargets(E.Range + 20, E.DamageType)
                    .Select(i => new Tuple<Obj_AI_Hero, Vector3>(i, E.GetPredPosition(i)))
                    .Where(i => CanEHit(i.Item2))
                    .ToList();

        private static bool IsChargeE => Variables.TickCount - lastE <= 100 || haveE;

        private static bool IsEmpQ => Player.Mana >= (haveQ ? 0.25f : 1.85f);

        #endregion

        #region Methods

        private static bool CanEHit(Vector3 pos)
        {
            for (var i = 0; i < 360; i += 18)
            {
                if (E.WillHit(
                    pos,
                    E.From + E.Range * new Vector2(1, 0).ToVector3().Rotated((float)(Math.PI * i / 180.0))))
                {
                    return true;
                }
            }
            return false;
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"] && R.IsReady())
            {
                var targets = Variables.TargetSelector.GetTargets(R.Range + 50, R.DamageType);
                if (targets.Count > 0)
                {
                    var hit = 0;
                    var posCast = new Vector3();
                    foreach (var target in targets)
                    {
                        var pred = R.GetPrediction(target);
                        if (pred.Hitchance < R.MinHitChance)
                        {
                            continue;
                        }
                        var hits =
                            GameObjects.EnemyHeroes.Where(
                                i =>
                                !i.Compare(target) && i.IsValidTarget()
                                && R.WillHit(R.GetPredPosition(i), pred.CastPosition)).ToList();
                        hits.Add(target);
                        if (hits.Count <= hit)
                        {
                            continue;
                        }
                        if ((hits.Count > 1
                             && (hits.Any(i => i.Health + i.MagicalShield <= R.GetDamage(i))
                                 || hits.Sum(i => i.HealthPercent) / hits.Count < MainMenu["Combo"]["RHpU"]))
                            || hits.Count >= MainMenu["Combo"]["RCountA"] || Player.HealthPercent <= 30)
                        {
                            hit = hits.Count;
                            posCast = pred.CastPosition;
                        }
                    }
                    if (posCast.IsValid() && R.Cast(posCast))
                    {
                        return;
                    }
                }
            }
            if (MainMenu["Combo"]["E"] && CanE)
            {
                var canE = GetETarget.Count > 0;
                if (E.IsReady())
                {
                    if (canE && ECharge())
                    {
                        return;
                    }
                }
                else if (EChargeMax && (canE || Player.CountAllyHeroesInRange(900) == 0) && ERelease())
                {
                    return;
                }
            }
            if ((IsEmpQ || !IsChargeE) && Q.CastOnBestTarget().IsCasted())
            {
                return;
            }
            var subTarget = Q.GetTarget(100);
            if (subTarget != null && MainMenu["Combo"]["Ignite"] && Common.CanIgnite && subTarget.HealthPercent < 25
                && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static bool ECharge()
        {
            if (IsChargeE || !Player.Spellbook.CastSpell(E.Slot))
            {
                return false;
            }
            lastE = Variables.TickCount;
            return true;
        }

        private static bool ERelease()
        {
            return Player.Spellbook.UpdateChargedSpell(E.Slot, Player.Position, true, false);
        }

        private static float GetEDmg(Obj_AI_Base target)
        {
            float minDmg = E.GetDamage(target), maxDmg = E.GetDamage(target, DamageStage.Empowered);
            return buffE != null
                       ? (Game.Time - buffE.StartTime >= 1
                              ? maxDmg
                              : minDmg + (Game.Time - buffE.StartTime) * (maxDmg - minDmg))
                       : minDmg;
        }

        private static void Hybrid()
        {
            if (MainMenu["Hybrid"]["E"] && CanE)
            {
                var canE = GetETarget.Count > 0;
                if (E.IsReady())
                {
                    if (canE && ECharge())
                    {
                        return;
                    }
                }
                else if (EChargeMax && (canE || Player.CountAllyHeroesInRange(750) == 0) && ERelease())
                {
                    return;
                }
            }
            if ((IsEmpQ || !IsChargeE) && Q.CastOnBestTarget().IsCasted())
            {
                return;
            }
            if (MainMenu["Hybrid"]["QLastHit"] && Q.IsReady() && !IsEmpQ && (!MainMenu["Hybrid"]["E"] || !IsChargeE))
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                        .MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    Q.CastOnUnit(minion);
                }
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                var target =
                    Variables.TargetSelector.GetTargets(Q.Range, Q.DamageType)
                        .FirstOrDefault(
                            i =>
                            i.Health + i.MagicalShield
                            <= Q.GetDamage(i, !IsEmpQ ? DamageStage.Default : DamageStage.Empowered));
                if (target != null && Q.CastOnUnit(target))
                {
                    return;
                }
            }
            if (MainMenu["KillSteal"]["E"] && CanE
                && GetETarget.Any(i => i.Item1.Health + i.Item1.MagicalShield <= GetEDmg(i.Item1)))
            {
                if (E.IsReady())
                {
                    if (ECharge())
                    {
                        DelayAction.Add(10, () => ERelease());
                    }
                }
                else if (IsChargeE)
                {
                    ERelease();
                }
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            var minion =
                GameObjects.EnemyMinions.Where(
                    i =>
                    (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range)
                    && Q.CanLastHit(i, Q.GetDamage(i, !IsEmpQ ? DamageStage.Default : DamageStage.Empowered)))
                    .MaxOrDefault(i => i.MaxHealth);
            if (minion == null)
            {
                return;
            }
            Q.CastOnUnit(minion);
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            Variables.Orbwalker.SetAttackState(!(haveW || IsChargeE));
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        if (W.IsReady() && !haveW)
                        {
                            W.Cast();
                        }
                    }
                    break;
            }
        }

        #endregion
    }
}
namespace Valvrave_Sharp.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class Yasuo : Program
    {
        #region Constants

        private const int QCirWidth = 250, RWidth = 400;

        #endregion

        #region Static Fields

        private static bool isDashing;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 500);
            Q2 = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1300);
            Q.SetSkillshot(GetQ12Delay, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2.SetSkillshot(GetQ3Delay, 90, 1500, false, SkillshotType.SkillshotLine);
            E.SetTargetted(0.05f, GetESpeed);
            Q.DamageType = Q2.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

            var orbwalkMenu = new Menu("Orbwalk", "Orbwalk");
            {
                orbwalkMenu.Separator("Q: Always On");
                orbwalkMenu.Separator("Sub Settings");
                orbwalkMenu.Bool("Ignite", "Use Ignite");
                orbwalkMenu.Bool("Item", "Use Item");
                orbwalkMenu.Separator("E Gap Settings");
                orbwalkMenu.Bool("EGap", "Use E");
                orbwalkMenu.Bool("EMouse", "Follow Mouse", false);
                orbwalkMenu.Slider("ERange", "-> If Distance >", 300, 0, (int)E.Range);
                orbwalkMenu.Bool("ETower", "Under Tower");
                orbwalkMenu.Bool("EStackQ", "Stack Q While Gap", false);
                orbwalkMenu.Separator("R Settings");
                orbwalkMenu.Bool("R", "Use R");
                orbwalkMenu.Bool("RDelay", "Delay Casting");
                orbwalkMenu.Slider("RHpU", "If Enemy Hp < (%)", 60);
                orbwalkMenu.Slider("RCountA", "Or Enemy >=", 2, 1, 5);
                MainMenu.Add(orbwalkMenu);
            }
            var hybridMenu = new Menu("Hybrid", "Hybrid");
            {
                hybridMenu.Separator("Q: Always On");
                hybridMenu.Bool("Q3", "Also Q3");
                hybridMenu.Bool("QLastHit", "Last Hit (Q1/2)");
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Bool("AutoQ3", "Also Q3", false);
                MainMenu.Add(hybridMenu);
            }
            var lcMenu = new Menu("LaneClear", "Lane Clear");
            {
                lcMenu.Separator("Q: Always On");
                lcMenu.Bool("Q3", "Also Q3");
                lcMenu.Separator("E Settings");
                lcMenu.Bool("E", "Use E");
                lcMenu.Bool("ELastHit", "Last Hit Only", false);
                lcMenu.Bool("ETower", "Under Tower", false);
                MainMenu.Add(lcMenu);
            }
            var farmMenu = new Menu("Farm", "Farm");
            {
                farmMenu.Separator("Q Settings");
                farmMenu.Bool("Q", "Use Q");
                farmMenu.Bool("Q3", "Also Q3", false);
                farmMenu.Separator("E Settings");
                farmMenu.Bool("E", "Use E");
                farmMenu.Bool("ETower", "Under Tower", false);
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
            var fleeMenu = new Menu("Flee", "Flee");
            {
                fleeMenu.KeyBind("E", "Use E", Keys.C);
                fleeMenu.Bool("Q", "Stack Q While Dash");
                MainMenu.Add(fleeMenu);
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
                EvadeTarget.Init();
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                drawMenu.Bool("Q", "Q Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range");
                drawMenu.Bool("StackQ", "Auto Stack Q Status");
                MainMenu.Add(drawMenu);
            }
            MainMenu.KeyBind("StackQ", "Auto Stack Q", Keys.Z, KeyBindType.Toggle);

            Game.OnUpdate += OnUpdateEvade;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Spellbook.OnStopCast += (sender, args) =>
                {
                    if (sender.Owner.IsMe && args.DestroyMissile && args.StopAnimation)
                    {
                        isDashing = false;
                    }
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe || !isDashing)
                    {
                        return;
                    }
                    if (args.Buff.Type == BuffType.Knockback || args.Buff.Type == BuffType.Knockup
                        || args.Buff.Type == BuffType.Charm || args.Buff.Type == BuffType.Fear
                        || args.Buff.Type == BuffType.Flee || args.Buff.Type == BuffType.Taunt
                        || args.Buff.Type == BuffType.Snare || args.Buff.Type == BuffType.Stun
                        || args.Buff.Type == BuffType.Suppression)
                    {
                        isDashing = false;
                    }
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.SData.Name != "YasuoDashWrapper")
                    {
                        return;
                    }
                    isDashing = true;
                    DelayAction.Add(
                        470000 / E.Speed,
                        () =>
                            {
                                if (isDashing)
                                {
                                    isDashing = false;
                                }
                            });
                };
        }

        #endregion

        #region Properties

        private static float GetESpeed
        {
            get
            {
                return 700 + Player.MoveSpeed;
            }
        }

        private static float GetQ12Delay
        {
            get
            {
                return 0.4f * GetQDelay;
            }
        }

        private static float GetQ3Delay
        {
            get
            {
                return 0.5f * GetQDelay;
            }
        }

        private static List<Obj_AI_Base> GetQCirObj
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos.ToVector3();
                var obj = new List<Obj_AI_Base>();
                obj.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(QCirWidth, true, pos)));
                obj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget(QCirWidth, true, pos) && i.IsMinion()));
                obj.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(QCirWidth, true, pos)));
                return obj;
            }
        }

        private static Obj_AI_Hero GetQCirTarget
        {
            get
            {
                return TargetSelector.GetTarget(
                    QCirWidth,
                    DamageType.Physical,
                    null,
                    Player.GetDashInfo().EndPos.ToVector3());
            }
        }

        private static float GetQDelay
        {
            get
            {
                return (float)(1 - Math.Min(Math.Round(Player.AttackSpeedMod - 1, 2) * 0.06, 0.66));
            }
        }

        private static List<Obj_AI_Hero> GetRTarget
        {
            get
            {
                return GameObjects.EnemyHeroes.Where(i => R.IsInRange(i) && CanCastR(i)).ToList();
            }
        }

        private static bool HaveQ3
        {
            get
            {
                return Player.HasBuff("YasuoQ3W");
            }
        }

        private static bool HaveStatik
        {
            get
            {
                return Player.GetBuffCount("ItemStatikShankCharge") == 100;
            }
        }

        private static bool IsDashing
        {
            get
            {
                return isDashing;
            }
        }

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (IsDashing || !Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
                || (HaveQ3 && !MainMenu["Hybrid"]["AutoQ3"]))
            {
                return;
            }
            if (!HaveQ3)
            {
                Q.CastingBestTarget(Q.Width, true);
            }
            else
            {
                CastQ3();
            }
        }

        private static bool CanCastDelayR(Obj_AI_Hero target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockback || i.Type == BuffType.Knockup);
            return buff != null && buff.EndTime - Game.Time <= (buff.EndTime - buff.StartTime) / 3;
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return target.IsValidTarget(E.Range) && !target.HasBuff("YasuoDashWrapper");
        }

        private static bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup);
        }

        private static bool CastQ3(Obj_AI_Hero target = null)
        {
            var spellQ = new Spell(SpellSlot.Q, Q2.Range);
            spellQ.SetSkillshot(Q2.Delay, Q2.Width, Q2.Speed, true, Q2.Type);
            if (target != null)
            {
                var pred = spellQ.VPrediction(target, true, new[] { CollisionableObjects.YasuoWall });
                if (pred.Hitchance >= Q2.MinHitChance && Q.Cast(pred.CastPosition))
                {
                    return true;
                }
            }
            else
            {
                int[] hit = { -1 };
                var predPos = new Vector3();
                foreach (var pred in
                    GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(Q2.Range))
                        .Select(i => spellQ.VPrediction(i, true, new[] { CollisionableObjects.YasuoWall }))
                        .Where(i => i.Hitchance >= Q2.MinHitChance && i.AoeTargetsHitCount > hit[0]))
                {
                    hit[0] = pred.AoeTargetsHitCount;
                    predPos = pred.CastPosition;
                }
                if (predPos.IsValid() && Q.Cast(predPos))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool CastQCir(Obj_AI_Base target)
        {
            return target.Distance(Player.GetDashInfo().EndPos) < QCirWidth - target.BoundingRadius / 2
                   && Q.Cast(target.ServerPosition);
        }

        private static void Farm()
        {
            if (!IsDashing && MainMenu["Farm"]["Q"] && Q.IsReady() && (!HaveQ3 || MainMenu["Farm"]["Q3"]))
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - Q.Width) && i.IsMinion()
                        && (!HaveQ3 ? Q : Q2).GetHealthPrediction(i) > 0
                        && (!HaveQ3 ? Q : Q2).GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i))
                        .MaxOrDefault(i => i.MaxHealth);
                if (minion != null && (!HaveQ3 ? Q : Q2).Casting(minion, true) == CastStates.SuccessfullyCasted)
                {
                    return;
                }
            }
            if (MainMenu["Farm"]["E"] && E.IsReady())
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        CanCastE(i) && i.IsMinion() && Evader.IsSafePoint(PosAfterE(i)).IsSafe
                        && (!UnderTower(PosAfterE(i)) || MainMenu["Farm"]["ETower"]) && E.GetHealthPrediction(i) > 0
                        && E.GetHealthPrediction(i) + i.MagicalShield <= GetEDmg(i)).MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    E.CastOnUnit(minion);
                }
            }
        }

        private static void Flee()
        {
            if (IsDashing)
            {
                if (HaveQ3 || !MainMenu["Flee"]["Q"] || !Q.IsReady())
                {
                    return;
                }
                var obj = GetQCirObj.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    CastQCir(obj);
                }
            }
            else if (E.IsReady())
            {
                var obj = GetNearObj();
                if (obj != null)
                {
                    E.CastOnUnit(obj);
                }
            }
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Magical,
                (50 + 20 * E.Level) * (1 + Math.Max(0, Player.GetBuffCount("YasuoDashScalar") * 0.25))
                + 0.6 * Player.FlatMagicDamageMod);
        }

        private static Obj_AI_Base GetNearObj(Obj_AI_Base target = null, bool inQCir = false, bool underTower = true)
        {
            var pos = target != null
                          ? Prediction.GetPrediction(target, E.Delay, 1, E.Speed).UnitPosition
                          : Game.CursorPos;
            var obj = new List<Obj_AI_Base>();
            obj.AddRange(GameObjects.EnemyHeroes.Where(CanCastE));
            obj.AddRange(GameObjects.EnemyMinions.Where(i => CanCastE(i) && i.IsMinion()));
            obj.AddRange(GameObjects.Jungle.Where(CanCastE));
            return
                obj.Where(
                    i =>
                    (!UnderTower(PosAfterE(i)) || underTower)
                    && PosAfterE(i).Distance(pos) < (inQCir ? QCirWidth : Player.Distance(pos))
                    && Evader.IsSafePoint(PosAfterE(i)).IsSafe).MinOrDefault(i => PosAfterE(i).Distance(pos));
        }

        private static double GetQDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Items.HasItem(3057) && (Items.CanUseItem(3057) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Items.HasItem(3078) && (Items.CanUseItem(3078) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            var k = 1d;
            var reduction = 0d;
            var dmgBonus = dmgItem
                           + Player.TotalAttackDamage * (Player.Crit >= 0.85f ? (Items.HasItem(3031) ? 1.875 : 1.5) : 1);
            if (Items.HasItem(3153))
            {
                var dmgBotrk = Math.Max(0.08 * target.Health, 10);
                var minion = target as Obj_AI_Minion;
                if (minion != null)
                {
                    dmgBotrk = Math.Min(dmgBotrk, 60);
                }
                dmgBonus += dmgBotrk;
            }
            var hero = target as Obj_AI_Hero;
            if (hero != null)
            {
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
                var mastery = hero.Masteries.FirstOrDefault(i => i.Page == MasteryPage.Defense && i.Id == 68);
                if (mastery != null && mastery.Points > 0)
                {
                    reduction += 1 * mastery.Points;
                }
            }
            return Player.CalculateDamage(target, DamageType.Physical, 20 * Q.Level + (dmgBonus - reduction) * k)
                   + (HaveStatik
                          ? Player.CalculateDamage(
                              target,
                              DamageType.Magical,
                              100 * (Player.Crit >= 0.85f ? (Items.HasItem(3031) ? 2.25 : 1.8) : 1))
                          : 0);
        }

        private static double GetRDmg(Obj_AI_Hero target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                new[] { 200, 300, 400 }[R.Level - 1] + 1.5f * Player.FlatPhysicalDamageMod);
        }

        private static void Hybrid()
        {
            if (IsDashing || !Q.IsReady())
            {
                return;
            }
            if (!HaveQ3)
            {
                var state = Q.CastingBestTarget(Q.Width, true);
                if (state == CastStates.SuccessfullyCasted)
                {
                    return;
                }
                if (state == CastStates.InvalidTarget && MainMenu["Hybrid"]["QLastHit"] && Q.GetTarget(100) == null)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i =>
                            i.IsValidTarget(Q.Range - Q.Width) && i.IsMinion() && Q.GetHealthPrediction(i) > 0
                            && Q.GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i)).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        Q.Casting(minion, true);
                    }
                }
            }
            else if (MainMenu["Hybrid"]["Q3"])
            {
                CastQ3();
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                if (IsDashing)
                {
                    var target = GetQCirTarget;
                    if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target) && CastQCir(target))
                    {
                        return;
                    }
                }
                else
                {
                    var target = (!HaveQ3 ? Q : Q2).GetTarget(!HaveQ3 ? Q.Width : 0);
                    if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target))
                    {
                        if (!HaveQ3)
                        {
                            if (Q.Casting(target, true) == CastStates.SuccessfullyCasted)
                            {
                                return;
                            }
                        }
                        else if (CastQ3(target))
                        {
                            return;
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady() && !IsDashing)
            {
                var target =
                    TargetSelector.GetTarget(
                        GameObjects.EnemyHeroes.Where(i => CanCastE(i) && i.Health + i.MagicalShield <= GetEDmg(i)),
                        E.DamageType);
                if (target != null)
                {
                    if (E.CastOnUnit(target))
                    {
                        return;
                    }
                }
                else if (MainMenu["KillSteal"]["Q"] && Q.IsReady(20))
                {
                    target =
                        TargetSelector.GetTarget(
                            GameObjects.EnemyHeroes.Where(
                                i =>
                                CanCastE(i) && i.Distance(PosAfterE(i)) < QCirWidth
                                && i.Health - GetEDmg(i) + i.PhysicalShield <= GetQDmg(i)),
                            Q.DamageType);
                    if (target != null && E.CastOnUnit(target))
                    {
                        return;
                    }
                }
            }
            if (MainMenu["KillSteal"]["R"] && R.IsReady())
            {
                var target =
                    TargetSelector.GetTarget(
                        GetRTarget.Where(
                            i =>
                            MainMenu["KillSteal"]["RCast" + i.ChampionName] && i.Health + i.PhysicalShield <= GetRDmg(i)),
                        R.DamageType);
                if (target != null)
                {
                    R.CastOnUnit(target);
                }
            }
        }

        private static void LaneClear()
        {
            if (MainMenu["LaneClear"]["E"] && E.IsReady() && !IsDashing)
            {
                var minion = new List<Obj_AI_Minion>();
                minion.AddRange(GameObjects.EnemyMinions.Where(i => CanCastE(i) && i.IsMinion()));
                minion.AddRange(GameObjects.Jungle.Where(CanCastE));
                minion =
                    minion.Where(i => !UnderTower(PosAfterE(i)) || MainMenu["LaneClear"]["ETower"])
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minion.Count > 0)
                {
                    var obj =
                        minion.FirstOrDefault(
                            i =>
                            E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) + i.MagicalShield <= GetEDmg(i));
                    if (!MainMenu["LaneClear"]["ELastHit"] && obj == null && Q.IsReady(20)
                        && (!HaveQ3 || MainMenu["LaneClear"]["Q3"]))
                    {
                        var sub = new List<Obj_AI_Minion>();
                        foreach (var mob in minion)
                        {
                            if (((E.GetHealthPrediction(mob) > 0
                                  && E.GetHealthPrediction(mob) - GetEDmg(mob) + mob.PhysicalShield <= GetQDmg(mob))
                                 || mob.Team == GameObjectTeam.Neutral) && mob.Distance(PosAfterE(mob)) < QCirWidth)
                            {
                                sub.Add(mob);
                            }
                            var nearMinion = new List<Obj_AI_Minion>();
                            nearMinion.AddRange(
                                GameObjects.EnemyMinions.Where(
                                    i => i.IsValidTarget(QCirWidth, true, PosAfterE(mob).ToVector3()) && i.IsMinion()));
                            nearMinion.AddRange(
                                GameObjects.Jungle.Where(
                                    i => i.IsValidTarget(QCirWidth, true, PosAfterE(mob).ToVector3())));
                            if (nearMinion.Count > 2
                                || nearMinion.Any(
                                    i =>
                                    E.GetHealthPrediction(mob) > 0
                                    && E.GetHealthPrediction(mob) + i.PhysicalShield <= GetQDmg(mob)))
                            {
                                sub.Add(mob);
                            }
                        }
                        obj = sub.FirstOrDefault();
                    }
                    if (obj != null && E.CastOnUnit(obj))
                    {
                        return;
                    }
                }
            }
            if (Q.IsReady() && (!HaveQ3 || MainMenu["LaneClear"]["Q3"]))
            {
                if (IsDashing)
                {
                    var minion = GetQCirObj.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                    if (
                        minion.Any(
                            i =>
                            (Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i))
                            || i.Team == GameObjectTeam.Neutral) || minion.Count > 2)
                    {
                        CastQCir(minion.MinOrDefault(i => i.Distance(Player)));
                    }
                }
                else
                {
                    var minion = new List<Obj_AI_Minion>();
                    minion.AddRange(
                        GameObjects.EnemyMinions.Where(
                            i => i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - Q.Width) && i.IsMinion()));
                    minion.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - Q.Width)));
                    minion = minion.OrderByDescending(i => i.MaxHealth).ToList();
                    if (minion.Count == 0)
                    {
                        return;
                    }
                    if (!HaveQ3)
                    {
                        var obj =
                            minion.FirstOrDefault(
                                i =>
                                Q.GetHealthPrediction(i) > 0
                                && Q.GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i));
                        if (obj != null && Q.Casting(obj, true) == CastStates.SuccessfullyCasted)
                        {
                            return;
                        }
                    }
                    var pos =
                        (!HaveQ3 ? Q : Q2).GetLineFarmLocation(
                            minion.Select(i => (!HaveQ3 ? Q : Q2).VPrediction(i).UnitPosition.ToVector2()).ToList());
                    if (pos.MinionsHit > 0)
                    {
                        Q.Cast(pos.Position);
                    }
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (MainMenu["Draw"]["Q"] && Q.Level > 0)
            {
                Drawing.DrawCircle(
                    Player.Position,
                    IsDashing ? QCirWidth : (!HaveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.Level > 0)
            {
                Drawing.DrawCircle(
                    Player.Position,
                    R.Range,
                    R.IsReady() && GetRTarget.Count > 0 ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["StackQ"] && Q.Level > 0)
            {
                var text = string.Format(
                    "Auto Stack Q: {0}",
                    MainMenu["StackQ"].GetValue<MenuKeyBind>().Active
                        ? (HaveQ3 ? "Full" : (Q.IsReady() ? "Ready" : "Not Ready"))
                        : "Off");
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(
                    pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                    pos.Y + 20,
                    MainMenu["StackQ"].GetValue<MenuKeyBind>().Active && Q.IsReady() && !HaveQ3
                        ? Color.White
                        : Color.Gray,
                    text);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!Equals(Q.Delay, GetQ12Delay))
            {
                Q.Delay = GetQ12Delay;
            }
            if (!Equals(Q2.Delay, GetQ3Delay))
            {
                Q2.Delay = GetQ3Delay;
            }
            if (!Equals(E.Speed, GetESpeed))
            {
                E.Speed = GetESpeed;
            }
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
                case OrbwalkerMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkerMode.LastHit:
                    Farm();
                    break;
            }
            if (Orbwalker.ActiveMode != OrbwalkerMode.Orbwalk && Orbwalker.ActiveMode != OrbwalkerMode.Hybrid)
            {
                AutoQ();
            }
            StackQ();
            if (MainMenu["Flee"]["E"].GetValue<MenuKeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                Flee();
            }
        }

        private static void OnUpdateEvade(EventArgs args)
        {
            if (Player.IsDead || !MainMenu["Evade"]["Enabled"].GetValue<MenuKeyBind>().Active)
            {
                return;
            }
            if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
            {
                return;
            }
            var windWall = EvadeSpellDatabase.Spells.FirstOrDefault(
                i => i.Enabled && i.IsReady && i.Slot == SpellSlot.W);
            if (windWall != null)
            {
                var skillshot =
                    Evade.DetectedSkillshots.Where(
                        i =>
                        i.Enabled && windWall.DangerLevel <= i.DangerLevel
                        && i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)
                        && i.IsAboutToHit(
                            150 + windWall.Delay - MainMenu["Evade"]["Spells"][windWall.Name]["WDelay"],
                            Player)).MaxOrDefault(i => i.DangerLevel);
                if (skillshot != null)
                {
                    Player.Spellbook.CastSpell(windWall.Slot, Player.ServerPosition.Extend(skillshot.Start, 100));
                }
            }
            var safePoint = Evader.IsSafePoint(Player.ServerPosition.ToVector2());
            var safePath = Evader.IsSafePath(Player.GetWaypoints(), 100);
            if (!safePath.IsSafe && !safePoint.IsSafe)
            {
                TryToEvade(safePoint.SkillshotList, Game.CursorPos.ToVector2());
            }
        }

        private static void Orbwalk()
        {
            if (MainMenu["Orbwalk"]["R"] && R.IsReady() && GetRTarget.Count > 0)
            {
                var hero = (from enemy in GetRTarget
                            let nearEnemy =
                                GameObjects.EnemyHeroes.Where(i => i.Distance(enemy) < RWidth && CanCastR(i)).ToList()
                            where
                                (nearEnemy.Count > 1 && enemy.Health + enemy.PhysicalShield <= GetRDmg(enemy))
                                || (nearEnemy.Count > 0
                                    && nearEnemy.Sum(i => i.HealthPercent) / nearEnemy.Count
                                    <= MainMenu["Orbwalk"]["RHpU"]) || nearEnemy.Count >= MainMenu["Orbwalk"]["RCountA"]
                            orderby nearEnemy.Count descending
                            select enemy).ToList();
                if (hero.Count > 0)
                {
                    var target = !MainMenu["Orbwalk"]["RDelay"]
                                     ? hero.FirstOrDefault()
                                     : hero.FirstOrDefault(CanCastDelayR);
                    if (target != null && R.CastOnUnit(target))
                    {
                        return;
                    }
                }
            }
            if (MainMenu["Orbwalk"]["EGap"] && E.IsReady() && !IsDashing)
            {
                var target = Q.GetTarget(Q.Width);
                if (target != null && HaveQ3 && Q.IsReady(20))
                {
                    var nearObj = GetNearObj(target, true);
                    if (nearObj != null
                        && (PosAfterE(nearObj).CountEnemy(QCirWidth) > 1 || Player.CountEnemy(Q2.Range) < 3)
                        && E.CastOnUnit(nearObj))
                    {
                        return;
                    }
                }
                target = target ?? Q2.GetTarget();
                if (target != null)
                {
                    if (!E.IsInRange(target))
                    {
                        var nearObj = GetNearObj(target, true)
                                      ?? GetNearObj(target, false, MainMenu["Orbwalk"]["ETower"]);
                        if (nearObj != null && E.CastOnUnit(nearObj))
                        {
                            return;
                        }
                    }
                    if (MainMenu["Orbwalk"]["EMouse"] && Player.Distance(target) >= MainMenu["Orbwalk"]["ERange"])
                    {
                        var nearObj = GetNearObj(null, false, MainMenu["Orbwalk"]["ETower"]);
                        if (nearObj != null && (!target.Compare(nearObj) || !target.InAutoAttackRange())
                            && E.CastOnUnit(nearObj))
                        {
                            return;
                        }
                    }
                }
            }
            if (Q.IsReady())
            {
                if (IsDashing)
                {
                    var target = GetQCirTarget;
                    if (target != null && CastQCir(target))
                    {
                        return;
                    }
                    if (!HaveQ3 && MainMenu["Orbwalk"]["EGap"] && MainMenu["Orbwalk"]["EStackQ"]
                        && Q.GetTarget(50) == null)
                    {
                        var obj = GetQCirObj.MinOrDefault(i => i.Distance(Player));
                        if (obj != null && CastQCir(obj))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    if (!HaveQ3)
                    {
                        if (Q.CastingBestTarget(Q.Width, true) == CastStates.SuccessfullyCasted)
                        {
                            return;
                        }
                    }
                    else if (CastQ3())
                    {
                        return;
                    }
                }
            }
            var subTarget = Q.GetTarget(Q.Width) ?? Q2.GetTarget();
            if (MainMenu["Orbwalk"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget == null)
            {
                return;
            }
            if (MainMenu["Orbwalk"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 30
                && Player.Distance(subTarget) <= 600)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static Vector2 PosAfterE(Obj_AI_Base target)
        {
            var pos = Prediction.GetPrediction(target, E.Delay, 1, E.Speed).UnitPosition;
            return
                Player.ServerPosition.Extend(
                    pos,
                    Player.Distance(pos) < 410 ? E.Range : Player.Distance(pos) + target.BoundingRadius).ToVector2();
        }

        private static void StackQ()
        {
            if (IsDashing || HaveQ3 || !Q.IsReady() || !MainMenu["StackQ"].GetValue<MenuKeyBind>().Active)
            {
                return;
            }
            var state = Q.CastingBestTarget(Q.Width, true);
            switch (state)
            {
                case CastStates.SuccessfullyCasted:
                    return;
                case CastStates.InvalidTarget:
                    var minion = new List<Obj_AI_Minion>();
                    minion.AddRange(
                        GameObjects.EnemyMinions.Where(i => i.IsValidTarget(Q.Range - Q.Width) && i.IsMinion()));
                    minion.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(Q.Range - Q.Width)));
                    minion = minion.OrderByDescending(i => i.MaxHealth).ToList();
                    if (minion.Count == 0)
                    {
                        return;
                    }
                    var obj =
                        minion.FirstOrDefault(
                            i =>
                            Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) + i.PhysicalShield <= GetQDmg(i));
                    if (obj != null && Q.Casting(obj, true) == CastStates.SuccessfullyCasted)
                    {
                        return;
                    }
                    var pos =
                        Q.GetLineFarmLocation(minion.Select(i => Q.VPrediction(i).UnitPosition.ToVector2()).ToList());
                    if (pos.MinionsHit > 0)
                    {
                        Q.Cast(pos.Position);
                    }
                    break;
            }
        }

        private static void TryToEvade(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            var dashE =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enabled && i.DangerLevel <= dangerLevel && i.IsReady && i.Slot == SpellSlot.E);
            if (dashE == null)
            {
                return;
            }
            var target =
                Evader.GetEvadeTargets(
                    dashE.ValidTargets,
                    (int)E.Speed,
                    dashE.Delay,
                    dashE.MaxRange,
                    false,
                    false,
                    false,
                    dashE.CheckBuffName)
                    .Select(
                        obj =>
                        new
                            {
                                obj, point = Player.ServerPosition.Extend(obj.ServerPosition, dashE.MaxRange).ToVector2()
                            })
                    .Where(
                        i =>
                        Evader.IsSafePoint(i.point).IsSafe
                        && (!UnderTower(i.point) || MainMenu["Evade"]["Spells"][dashE.Name]["ETower"]))
                    .OrderBy(i => to.Distance(i.point))
                    .Select(i => i.obj)
                    .FirstOrDefault();
            if (target != null)
            {
                Player.Spellbook.CastSpell(dashE.Slot, target);
            }
        }

        private static bool UnderTower(Vector2 pos)
        {
            return GameObjects.EnemyTurrets.Any(i => !i.IsDead && i.Distance(pos) <= 900 + Player.BoundingRadius);
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
            if (Youmuu.IsReady && Player.CountEnemy(Q.Range + E.Range) > 0)
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
            if (Titanic.IsReady && Player.CountEnemy(Titanic.Range) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion

        internal class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            #endregion

            #region Methods

            internal static void Init()
            {
                LoadSpellData();
                var evadeMenu = new Menu("EvadeTarget", "Evade Target");
                {
                    evadeMenu.Bool("W", "Use W");
                    var aaMenu = new Menu("AA", "Auto Attack");
                    {
                        aaMenu.Bool("B", "Basic Attack");
                        aaMenu.Slider("BHpU", "-> If Hp < (%)", 35);
                        aaMenu.Bool("C", "Crit Attack");
                        aaMenu.Slider("CHpU", "-> If Hp < (%)", 40);
                        evadeMenu.Add(aaMenu);
                    }
                    foreach (var hero in
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            Spells.Any(
                                a =>
                                string.Equals(
                                    a.ChampionName,
                                    i.ChampionName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        evadeMenu.Add(new Menu(hero.ChampionName.ToLowerInvariant(), "-> " + hero.ChampionName));
                    }
                    foreach (var spell in
                        Spells.Where(
                            i =>
                            GameObjects.EnemyHeroes.Any(
                                a =>
                                string.Equals(
                                    a.ChampionName,
                                    i.ChampionName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        ((Menu)evadeMenu[spell.ChampionName.ToLowerInvariant()]).Bool(
                            spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            false);
                    }
                }
                MainMenu.Add(evadeMenu);
                Game.OnUpdate += OnUpdateTarget;
                GameObject.OnCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
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
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as Obj_AI_Hero;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team || !missile.Target.IsMe)
                {
                    return;
                }
                var spellData =
                    Spells.FirstOrDefault(
                        i =>
                        i.SpellNames.Contains(missile.SData.Name.ToLower())
                        && MainMenu["EvadeTarget"][i.ChampionName.ToLowerInvariant()][i.MissileName]);
                if (spellData == null && AutoAttack.IsAutoAttack(missile.SData.Name)
                    && (!missile.SData.Name.ToLower().Contains("crit")
                            ? MainMenu["EvadeTarget"]["AA"]["B"]
                              && Player.HealthPercent < MainMenu["EvadeTarget"]["AA"]["BHpU"]
                            : MainMenu["EvadeTarget"]["AA"]["C"]
                              && Player.HealthPercent < MainMenu["EvadeTarget"]["AA"]["CHpU"]))
                {
                    spellData = new SpellData
                                    { ChampionName = caster.ChampionName, SpellNames = new[] { missile.SData.Name } };
                }
                if (spellData == null)
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Start = caster.ServerPosition, Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                var missile = sender as MissileClient;
                if (missile == null || !missile.IsValid)
                {
                    return;
                }
                var caster = missile.SpellCaster as Obj_AI_Hero;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team)
                {
                    return;
                }
                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
            }

            private static void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return;
                }
                if (!MainMenu["EvadeTarget"]["W"] || !W.IsReady())
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => W.IsInRange(i.Obj)).OrderBy(i => i.Obj.Distance(Player)))
                {
                    if (W.Cast(Player.ServerPosition.Extend(target.Start, 100)))
                    {
                        break;
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
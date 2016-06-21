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
    using LeagueSharp.SDK.TSModes;
    using LeagueSharp.SDK.UI;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.UI.Menu;
    using Skillshot = Valvrave_Sharp.Evade.Skillshot;

    #endregion

    internal class Yasuo : Program
    {
        #region Constants

        private const float QDelay = 0.38f, Q2Delay = 0.35f, QDelays = 0.19f, Q2Delays = 0.29f;

        private const int RWidth = 400;

        #endregion

        #region Static Fields

        private static int cDash;

        private static bool haveQ3;

        private static bool isDash;

        private static int lastE;

        private static Vector2 posDash;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 505).SetSkillshot(QDelay, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            Q2 = new Spell(Q.Slot, 1100).SetSkillshot(Q2Delay, 90, 1200, true, Q.Type);
            Q3 = new Spell(Q.Slot, 250).SetTargetted(0.005f, float.MaxValue);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475).SetTargetted(0, 1040);
            E2 = new Spell(E.Slot, E.Range).SetTargetted(Q3.Delay, E.Speed);
            R = new Spell(SpellSlot.R, 1200);
            Q.DamageType = Q2.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;
            E.CastCondition += () => !posDash.IsValid();

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q: Always On");
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
                comboMenu.Separator("E Gap Settings");
                comboMenu.Bool("EGap", "Use E");
                comboMenu.List("EMode", "Follow Mode", new[] { "Enemy", "Mouse" });
                comboMenu.Bool("ETower", "Under Tower", false);
                comboMenu.Bool("EStackQ", "Stack Q While Gap", false);
                comboMenu.Separator("R Settings");
                comboMenu.KeyBind("R", "Use R", Keys.X, KeyBindType.Toggle);
                comboMenu.Bool("RDelay", "Delay Cast");
                comboMenu.Slider("RHpU", "If Enemies Hp < (%)", 60);
                comboMenu.Slider("RCountA", "Or Count >=", 2, 1, 5);
            }
            var hybridMenu = MainMenu.Add(new Menu("Hybrid", "Hybrid"));
            {
                hybridMenu.Separator("Q: Always On");
                hybridMenu.Bool("Q3", "Also Q3");
                hybridMenu.Bool("QLastHit", "Last Hit (Q1/2)");
                hybridMenu.Separator("Auto Q Settings");
                hybridMenu.KeyBind("AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                hybridMenu.Bool("AutoQ3", "Also Q3", false);
            }
            var lcMenu = MainMenu.Add(new Menu("LaneClear", "Lane Clear"));
            {
                lcMenu.Separator("Q Settings");
                lcMenu.Bool("Q", "Use Q");
                lcMenu.Bool("Q3", "Also Q3", false);
                lcMenu.Separator("E Settings");
                lcMenu.Bool("E", "Use E");
                lcMenu.Bool("ELastHit", "Last Hit Only", false);
                lcMenu.Bool("ETower", "Under Tower", false);
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Separator("Q Settings");
                lhMenu.Bool("Q", "Use Q");
                lhMenu.Bool("Q3", "Also Q3", false);
                lhMenu.Separator("E Settings");
                lhMenu.Bool("E", "Use E");
                lhMenu.Bool("ETower", "Under Tower", false);
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("E", "Use E");
                ksMenu.Bool("R", "Use R");
                if (GameObjects.EnemyHeroes.Any())
                {
                    ksMenu.Separator("Extra R Settings");
                    GameObjects.EnemyHeroes.ForEach(
                        i => ksMenu.Bool("RCast" + i.ChampionName, "Cast On " + i.ChampionName, false));
                }
            }
            var fleeMenu = MainMenu.Add(new Menu("Flee", "Flee"));
            {
                fleeMenu.KeyBind("E", "Use E", Keys.C);
                fleeMenu.Bool("Q", "Stack Q While Dash");
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
                EvadeTarget.Init();
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
                drawMenu.Bool("UseR", "R In Combo Status");
                drawMenu.Bool("StackQ", "Auto Stack Q Status");
            }
            MainMenu.KeyBind("StackQ", "Auto Stack Q", Keys.Z, KeyBindType.Toggle);

            Evade.Evading += Evading;
            Evade.TryEvading += TryEvading;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += args =>
                {
                    if (Player.IsDead)
                    {
                        if (isDash)
                        {
                            isDash = false;
                            posDash = new Vector2();
                        }
                        return;
                    }
                    if (isDash && !Player.IsDashing())
                    {
                        isDash = false;
                        DelayAction.Add(50, () => posDash = new Vector2());
                    }
                    Q.Delay = GetQDelay(false);
                    Q2.Delay = GetQDelay(true);
                    E.Speed = E2.Speed = 1040 + (Player.MoveSpeed - 345);
                };
            Variables.Orbwalker.OnAction += (sender, args) =>
                {
                    if (!Q.IsReady() || haveQ3 || args.Type != OrbwalkingType.AfterAttack
                        || Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.LaneClear || args.Target is Obj_AI_Hero
                        || args.Target is Obj_AI_Minion)
                    {
                        return;
                    }
                    if (Q.GetTarget(50) != null || Common.ListMinions().Count(i => i.IsValidTarget(Q.Range + 50)) > 0)
                    {
                        return;
                    }
                    if ((Items.HasItem((int)ItemId.Sheen) && Items.CanUseItem((int)ItemId.Sheen))
                        || (Items.HasItem((int)ItemId.Trinity_Force) && Items.CanUseItem((int)ItemId.Trinity_Force)))
                    {
                        Q.Cast(Game.CursorPos);
                    }
                };
            Events.OnDash += (sender, args) =>
                {
                    if (!args.Unit.IsMe)
                    {
                        return;
                    }
                    isDash = true;
                    posDash = args.EndPos;
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    switch (args.Buff.DisplayName)
                    {
                        case "YasuoQ3W":
                            haveQ3 = true;
                            break;
                        case "YasuoDashScalar":
                            cDash = 1;
                            break;
                        case "yasuoeqcombosoundmiss":
                        case "YasuoEQComboSoundHit":
                            DelayAction.Add(
                                100,
                                () =>
                                    {
                                        Variables.Orbwalker.ResetSwingTimer();
                                        Player.IssueOrder(
                                            GameObjectOrder.AttackTo,
                                            Player.ServerPosition.Extend(Game.CursorPos, Player.BoundingRadius));
                                    });
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
                        case "YasuoQ3W":
                            haveQ3 = false;
                            break;
                        case "YasuoDashScalar":
                            cDash = 0;
                            break;
                    }
                };
            Obj_AI_Base.OnBuffUpdateCount += (sender, args) =>
                {
                    if (!sender.IsMe || args.Buff.DisplayName != "YasuoDashScalar")
                    {
                        return;
                    }
                    cDash = 2;
                };
        }

        #endregion

        #region Properties

        private static bool CanCastQCir => posDash.IsValid() && posDash.DistanceToPlayer() < 150;

        private static List<Obj_AI_Base> GetQCirObj
            =>
                Common.ListEnemies(true)
                    .Where(i => i.IsValidTarget() && Q3.GetPredPosition(i).Distance(posDash) < Q3.Range)
                    .ToList();

        private static List<Obj_AI_Base> GetQCirTarget
            =>
                Variables.TargetSelector.GetTargets(Q3.Range, Q.DamageType, true, posDash.ToVector3())
                    .Where(i => Q3.GetPredPosition(i).Distance(posDash) < Q3.Range)
                    .Cast<Obj_AI_Base>()
                    .ToList();

        private static List<Obj_AI_Hero> GetRTarget
            => GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(R.Range) && HaveR(i)).ToList();

        private static bool IsDashing
            => (lastE > 0 && Variables.TickCount - lastE <= 100) || Player.IsDashing() || posDash.IsValid();

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || IsDashing
                || (haveQ3 && !MainMenu["Hybrid"]["AutoQ3"]))
            {
                return;
            }
            if (!haveQ3)
            {
                Q.CastingBestTarget(true);
            }
            else
            {
                CastQ3();
            }
        }

        private static bool CanCastDelayR(Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Knockback))
            {
                return true;
            }
            var buff = target.Buffs.FirstOrDefault(i => i.IsValid && i.Type == BuffType.Knockup);
            if (buff == null)
            {
                return false;
            }
            var dur = buff.EndTime - buff.StartTime;
            return buff.EndTime - Game.Time <= (dur <= 0.75 ? 0.3 : 0.235) * dur;
        }

        private static bool CanDash(
            Obj_AI_Base target,
            bool inQCir = false,
            bool underTower = true,
            Vector3 pos = new Vector3())
        {
            if (HaveE(target))
            {
                return false;
            }
            if (!pos.IsValid())
            {
                pos = target.ServerPosition;
            }
            var posAfterE = GetPosAfterDash(target);
            return (underTower || !posAfterE.IsUnderEnemyTurret())
                   && posAfterE.Distance(pos) < (inQCir ? Q3.Range : pos.DistanceToPlayer())
                   && Evade.IsSafePoint(posAfterE.ToVector2());
        }

        private static bool CastQ3()
        {
            var targets = Variables.TargetSelector.GetTargets(Q2.Range + Q2.Width / 2, Q2.DamageType);
            if (targets.Count == 0)
            {
                return false;
            }
            var posCast = new Vector3();
            foreach (var pred in
                targets.Select(i => Q2.GetPrediction(i, true, -1, CollisionableObjects.YasuoWall))
                    .Where(
                        i =>
                        i.Hitchance >= Q2.MinHitChance || (i.Hitchance >= HitChance.High && i.AoeTargetsHitCount > 1))
                    .OrderByDescending(i => i.AoeTargetsHitCount))
            {
                posCast = pred.CastPosition;
                break;
            }
            return posCast.IsValid() && Q2.Cast(posCast);
        }

        private static bool CastQCir(List<Obj_AI_Base> obj)
        {
            if (obj.Count == 0)
            {
                return false;
            }
            var target = obj.FirstOrDefault();
            return target != null && Q3.Cast(!haveQ3 ? Q.GetPredPosition(target) : Q2.GetPredPosition(target));
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"].GetValue<MenuKeyBind>().Active && R.IsReady())
            {
                var targetR = GetRTarget;
                if (targetR.Count > 0)
                {
                    var targets = (from enemy in targetR
                                   let nearEnemy =
                                       GameObjects.EnemyHeroes.Where(
                                           i => i.IsValidTarget(RWidth, true, enemy.ServerPosition) && HaveR(i))
                                       .ToList()
                                   where
                                       (nearEnemy.Count > 1 && enemy.Health + enemy.PhysicalShield <= R.GetDamage(enemy))
                                       || nearEnemy.Sum(i => i.HealthPercent) / nearEnemy.Count
                                       <= MainMenu["Combo"]["RHpU"] || nearEnemy.Count >= MainMenu["Combo"]["RCountA"]
                                   orderby nearEnemy.Count descending
                                   select enemy).ToList();
                    if (MainMenu["Combo"]["RDelay"])
                    {
                        targets = targets.Where(CanCastDelayR).ToList();
                    }
                    if (targets.Count > 0)
                    {
                        var target = targets.MaxOrDefault(i => new Priority().GetPriority(i));
                        if (target != null && R.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                }
            }
            if (MainMenu["Combo"]["EGap"] && E.IsReady())
            {
                var underTower = MainMenu["Combo"]["ETower"];
                if (MainMenu["Combo"]["EMode"].GetValue<MenuList>().Index == 0)
                {
                    var listDashObj = GetDashObj(underTower);
                    var target = E.GetTarget(Q3.Range);
                    if (target != null && haveQ3 && Q.IsReady(50))
                    {
                        var nearObj = GetBestObj(listDashObj, target, true);
                        if (nearObj != null
                            && (GetPosAfterDash(nearObj).CountEnemyHeroesInRange(Q3.Range) > 1
                                || Player.CountEnemyHeroesInRange(Q.Range + E.Range / 2) == 1) && E.CastOnUnit(nearObj))
                        {
                            lastE = Variables.TickCount;
                            return;
                        }
                    }
                    target = E.GetTarget();
                    if (target != null
                        && ((cDash > 0 && CanDash(target, false, underTower))
                            || (haveQ3 && Q.IsReady(50) && CanDash(target, true, underTower))) && E.CastOnUnit(target))
                    {
                        lastE = Variables.TickCount;
                        return;
                    }
                    target = Q.GetTarget(100) ?? Q2.GetTarget();
                    if (target != null && (!Player.Spellbook.IsAutoAttacking || Player.HealthPercent < 40))
                    {
                        var nearObj = GetBestObj(listDashObj, target);
                        var canDash = cDash == 0 && nearObj != null && !HaveE(target);
                        if (Q.IsReady(50))
                        {
                            var nearObjQ3 = GetBestObj(listDashObj, target, true);
                            if (nearObjQ3 != null)
                            {
                                nearObj = nearObjQ3;
                                canDash = true;
                            }
                        }
                        if (!canDash && target.DistanceToPlayer() > target.GetRealAutoAttackRange() * 0.7)
                        {
                            canDash = true;
                        }
                        if (canDash)
                        {
                            if (nearObj == null && E.IsInRange(target) && CanDash(target, false, underTower))
                            {
                                nearObj = target;
                            }
                            if (nearObj != null && E.CastOnUnit(nearObj))
                            {
                                lastE = Variables.TickCount;
                                return;
                            }
                        }
                    }
                }
                else
                {
                    var target = Variables.Orbwalker.GetTarget();
                    if (target == null || Player.Distance(target) > target.GetRealAutoAttackRange() * 0.4)
                    {
                        var obj = GetBestObjToMouse(underTower);
                        if (obj != null && E.CastOnUnit(obj))
                        {
                            lastE = Variables.TickCount;
                            return;
                        }
                    }
                }
            }
            if (Q.IsReady())
            {
                if (IsDashing)
                {
                    if (CanCastQCir)
                    {
                        if (CastQCir(GetQCirTarget))
                        {
                            return;
                        }
                        if (!haveQ3 && MainMenu["Combo"]["EGap"] && MainMenu["Combo"]["EStackQ"]
                            && Player.CountEnemyHeroesInRange(700) == 0 && CastQCir(GetQCirObj))
                        {
                            return;
                        }
                    }
                }
                else if (!haveQ3 ? Q.CastingBestTarget(true).IsCasted() : CastQ3())
                {
                    return;
                }
            }
            var subTarget = Q.GetTarget(100) ?? Q2.GetTarget();
            if (MainMenu["Combo"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget != null && MainMenu["Combo"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 25
                && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Evading(Obj_AI_Base sender)
        {
            var yasuoW = EvadeSpellDatabase.Spells.FirstOrDefault(i => i.Enable && i.IsReady && i.Slot == SpellSlot.W);
            if (yasuoW == null)
            {
                return;
            }
            var skillshot =
                Evade.SkillshotAboutToHit(
                    sender,
                    yasuoW.Delay - MainMenu["Evade"]["Spells"][yasuoW.Name]["WDelay"],
                    true).OrderByDescending(i => i.DangerLevel).FirstOrDefault(i => i.DangerLevel >= yasuoW.DangerLevel);
            if (skillshot != null)
            {
                W.Cast(sender.ServerPosition.Extend(skillshot.Start, 100));
            }
        }

        private static void Flee()
        {
            if (MainMenu["Flee"]["Q"] && Q.IsReady() && !haveQ3 && IsDashing && CanCastQCir && CastQCir(GetQCirObj))
            {
                return;
            }
            if (!E.IsReady())
            {
                return;
            }
            var obj = GetBestObjToMouse();
            if (obj != null && E.CastOnUnit(obj))
            {
                lastE = Variables.TickCount;
            }
        }

        private static Obj_AI_Base GetBestObj(List<Obj_AI_Base> obj, Obj_AI_Hero target, bool inQCir = false)
        {
            obj.RemoveAll(i => i.Compare(target));
            if (obj.Count == 0)
            {
                return null;
            }
            var pos = E.GetPredPosition(target, true);
            return obj.Where(i => CanDash(i, inQCir, true, pos)).MinOrDefault(i => GetPosAfterDash(i).Distance(pos));
        }

        private static Obj_AI_Base GetBestObjToMouse(bool underTower = true)
        {
            var pos = Game.CursorPos;
            return
                GetDashObj(underTower)
                    .Where(i => CanDash(i, false, true, pos))
                    .MinOrDefault(i => GetPosAfterDash(i).Distance(pos));
        }

        private static List<Obj_AI_Base> GetDashObj(bool underTower = false)
        {
            return
                Common.ListEnemies()
                    .Where(i => i.IsValidTarget(E.Range) && (underTower || !GetPosAfterDash(i).IsUnderEnemyTurret()))
                    .ToList();
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return E.GetDamage(target) + E.GetDamage(target, DamageStage.Buff) - 5;
        }

        private static Vector3 GetPosAfterDash(Obj_AI_Base target)
        {
            return Player.ServerPosition.Extend(target.ServerPosition, E.Range);
        }

        private static float GetQDelay(bool isQ3)
        {
            var delayOri = !isQ3 ? QDelay : Q2Delay;
            var delayMax = !isQ3 ? QDelays : Q2Delays;
            var perReduce = 1 - delayMax / delayOri;
            var delayReal =
                Math.Max(
                    delayOri * (1 - Math.Min((Player.AttackSpeedMod - 1) * (perReduce / 1.1f), perReduce)),
                    delayMax);
            return (float)Math.Round((decimal)delayReal, 3, MidpointRounding.AwayFromZero);
        }

        private static double GetQDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Items.HasItem((int)ItemId.Sheen) && (Items.CanUseItem((int)ItemId.Sheen) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Items.HasItem((int)ItemId.Trinity_Force)
                && (Items.CanUseItem((int)ItemId.Trinity_Force) || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            if (dmgItem > 0)
            {
                dmgItem = Player.CalculateDamage(target, DamageType.Physical, dmgItem);
            }
            double dmgQ = Q.GetDamage(target);
            if (Math.Abs(Player.Crit - 1) < float.Epsilon)
            {
                dmgQ += Player.CalculateDamage(
                    target,
                    Q.DamageType,
                    (Items.HasItem((int)ItemId.Infinity_Edge) ? 0.875 : 0.5) * Player.TotalAttackDamage);
            }
            return dmgQ + dmgItem;
        }

        private static bool HaveE(Obj_AI_Base target)
        {
            return target.HasBuff("YasuoDashWrapper");
        }

        private static bool HaveR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup);
        }

        private static void Hybrid()
        {
            if (!Q.IsReady() || IsDashing)
            {
                return;
            }
            if (!haveQ3)
            {
                var state = Q.CastingBestTarget(true);
                if (state.IsCasted())
                {
                    return;
                }
                if (state == CastStates.InvalidTarget && MainMenu["Hybrid"]["QLastHit"] && Q.GetTarget(50) == null
                    && !Player.Spellbook.IsAutoAttacking)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i => (i.IsMinion() || i.IsPet(false)) && IsInRangeQ(i) && Q.CanLastHit(i, GetQDmg(i)))
                            .MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        Q.Casting(minion);
                    }
                }
            }
            else if (MainMenu["Hybrid"]["Q3"])
            {
                CastQ3();
            }
        }

        private static bool IsInRangeQ(Obj_AI_Minion minion)
        {
            return minion.IsValidTarget(Math.Max(475 + minion.BoundingRadius / 3 - 7, 475));
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"] && Q.IsReady())
            {
                if (IsDashing)
                {
                    if (CanCastQCir)
                    {
                        var targets = GetQCirTarget.Where(i => i.Health + i.PhysicalShield <= GetQDmg(i)).ToList();
                        if (CastQCir(targets))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var target = !haveQ3 ? Q.GetTarget(Q.Width / 2) : Q2.GetTarget(Q2.Width / 2);
                    if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target))
                    {
                        if (!haveQ3)
                        {
                            if (Q.Casting(target).IsCasted())
                            {
                                return;
                            }
                        }
                        else if (Q2.Casting(target, false, CollisionableObjects.YasuoWall).IsCasted())
                        {
                            return;
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                var targets = Variables.TargetSelector.GetTargets(E.Range, E.DamageType).Where(i => !HaveE(i)).ToList();
                if (targets.Count > 0)
                {
                    var target = targets.FirstOrDefault(i => i.Health + i.MagicalShield <= GetEDmg(i));
                    if (target != null)
                    {
                        if (E.CastOnUnit(target))
                        {
                            lastE = Variables.TickCount;
                            return;
                        }
                    }
                    else if (MainMenu["KillSteal"]["Q"] && Q.IsReady(50))
                    {
                        target = targets.Where(i => i.Distance(GetPosAfterDash(i)) < Q3.Range).FirstOrDefault(
                            i =>
                                {
                                    var dmgE = GetEDmg(i) - i.MagicalShield;
                                    return i.Health - (dmgE > 0 ? dmgE : 0) + i.PhysicalShield <= GetQDmg(i);
                                });
                        if (target != null && E.CastOnUnit(target))
                        {
                            lastE = Variables.TickCount;
                            return;
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["R"] && R.IsReady())
            {
                var targets = GetRTarget;
                if (targets.Count > 0)
                {
                    var target =
                        targets.Where(
                            i =>
                            MainMenu["KillSteal"]["RCast" + i.ChampionName]
                            && (i.Health + i.PhysicalShield <= R.GetDamage(i)
                                || (Q.IsReady(1000) && i.Health + i.PhysicalShield <= R.GetDamage(i) + GetQDmg(i)))
                            && !Invulnerable.Check(i, R.DamageType))
                            .MaxOrDefault(i => new Priority().GetDefaultPriority(i));
                    if (target != null)
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var useQ = MainMenu["LaneClear"]["Q"];
            var useQ3 = MainMenu["LaneClear"]["Q3"];
            if (MainMenu["LaneClear"]["E"] && E.IsReady())
            {
                var minions =
                    Common.ListMinions()
                        .Where(
                            i =>
                            i.IsValidTarget(E.Range) && !HaveE(i)
                            && (!GetPosAfterDash(i).IsUnderEnemyTurret() || MainMenu["LaneClear"]["ETower"])
                            && Evade.IsSafePoint(GetPosAfterDash(i).ToVector2()))
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minions.Count > 0)
                {
                    var minion = minions.FirstOrDefault(i => E.CanLastHit(i, GetEDmg(i)));
                    if (useQ && minion == null && Q.IsReady(50) && (!haveQ3 || useQ3))
                    {
                        var sub = new List<Obj_AI_Minion>();
                        foreach (var mob in minions)
                        {
                            if ((E2.CanLastHit(mob, GetQDmg(mob), GetEDmg(mob)) || mob.Team == GameObjectTeam.Neutral)
                                && mob.Distance(GetPosAfterDash(mob)) < Q3.Range)
                            {
                                sub.Add(mob);
                            }
                            if (MainMenu["LaneClear"]["ELastHit"])
                            {
                                continue;
                            }
                            var nearMinion =
                                Common.ListMinions()
                                    .Where(i => i.IsValidTarget(Q3.Range, true, GetPosAfterDash(mob)))
                                    .ToList();
                            if (nearMinion.Count > 2 || nearMinion.Count(i => mob.Health <= GetQDmg(mob)) > 1)
                            {
                                sub.Add(mob);
                            }
                        }
                        minion = sub.FirstOrDefault();
                    }
                    if (minion != null && E.CastOnUnit(minion))
                    {
                        lastE = Variables.TickCount;
                        return;
                    }
                }
            }
            if (useQ && Q.IsReady() && (!haveQ3 || useQ3))
            {
                if (IsDashing)
                {
                    if (CanCastQCir)
                    {
                        var minions = GetQCirObj.Where(i => i is Obj_AI_Minion).ToList();
                        if (minions.Any(i => i.Health <= GetQDmg(i) || i.Team == GameObjectTeam.Neutral)
                            || minions.Count > 2)
                        {
                            CastQCir(minions);
                        }
                    }
                }
                else
                {
                    var minions =
                        Common.ListMinions()
                            .Where(i => !haveQ3 ? IsInRangeQ(i) : i.IsValidTarget(Q2.Range - i.BoundingRadius / 2))
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (minions.Count == 0)
                    {
                        return;
                    }
                    if (!haveQ3)
                    {
                        var minion = minions.FirstOrDefault(i => Q.CanLastHit(i, GetQDmg(i)));
                        if (minion != null)
                        {
                            Q.Casting(minion);
                        }
                        else
                        {
                            var pos = Q.GetLineFarmLocation(minions);
                            if (pos.MinionsHit > 0)
                            {
                                Q.Cast(pos.Position);
                            }
                        }
                    }
                    else
                    {
                        var pos = Q2.GetLineFarmLocation(minions);
                        if (pos.MinionsHit > 0)
                        {
                            Q2.Cast(pos.Position);
                        }
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (MainMenu["LastHit"]["Q"] && Q.IsReady() && !IsDashing && (!haveQ3 || MainMenu["LastHit"]["Q3"]))
            {
                if (!haveQ3)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i => (i.IsMinion() || i.IsPet(false)) && IsInRangeQ(i) && Q.CanLastHit(i, GetQDmg(i)))
                            .MaxOrDefault(i => i.MaxHealth);
                    if (minion != null && Q.Casting(minion).IsCasted())
                    {
                        return;
                    }
                }
                else
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i =>
                            (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q2.Range - i.BoundingRadius / 2)
                            && Q2.CanLastHit(i, GetQDmg(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null && Q2.Casting(minion, false, CollisionableObjects.YasuoWall).IsCasted())
                    {
                        return;
                    }
                }
            }
            if (MainMenu["LastHit"]["E"] && E.IsReady() && !Player.Spellbook.IsAutoAttacking)
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(E.Range) && !HaveE(i)
                        && E.CanLastHit(i, GetEDmg(i)) && Evade.IsSafePoint(GetPosAfterDash(i).ToVector2())
                        && (!GetPosAfterDash(i).IsUnderEnemyTurret() || MainMenu["LastHit"]["ETower"]))
                        .MaxOrDefault(i => i.MaxHealth);
                if (minion != null && E.CastOnUnit(minion))
                {
                    lastE = Variables.TickCount;
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
                Render.Circle.DrawCircle(
                    Player.Position,
                    (IsDashing ? Q3 : (!haveQ3 ? Q : Q2)).Range,
                    Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (R.Level > 0)
            {
                if (MainMenu["Draw"]["R"] && R.IsReady())
                {
                    Render.Circle.DrawCircle(
                        Player.Position,
                        R.Range,
                        GetRTarget.Count > 0 ? Color.LimeGreen : Color.IndianRed);
                }
                if (MainMenu["Draw"]["UseR"])
                {
                    var menuR = MainMenu["Combo"]["R"].GetValue<MenuKeyBind>();
                    var pos = Drawing.WorldToScreen(Player.Position);
                    var text = $"Use R In Combo: {(menuR.Active ? "On" : "Off")} [{menuR.Key}]";
                    Drawing.DrawText(
                        pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                        pos.Y + 40,
                        menuR.Active ? Color.White : Color.Gray,
                        text);
                }
            }
            if (MainMenu["Draw"]["StackQ"] && Q.Level > 0)
            {
                var menu = MainMenu["StackQ"].GetValue<MenuKeyBind>();
                var text =
                    $"Auto Stack Q: {(menu.Active ? (haveQ3 ? "Full" : (Q.IsReady() ? "Ready" : "Not Ready")) : "Off")} [{menu.Key}]";
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(
                    pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                    pos.Y + 20,
                    menu.Active ? Color.White : Color.Gray,
                    text);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.Hybrid:
                    Hybrid();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["Flee"]["E"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        Flee();
                    }
                    break;
            }
            if (Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Combo
                && Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
            if (!MainMenu["Flee"]["E"].GetValue<MenuKeyBind>().Active)
            {
                StackQ();
            }
        }

        private static void StackQ()
        {
            if (!MainMenu["StackQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || haveQ3 || IsDashing)
            {
                return;
            }
            var state = Q.CastingBestTarget(true);
            if (state.IsCasted() || state != CastStates.InvalidTarget)
            {
                return;
            }
            var minions = Common.ListMinions().Where(IsInRangeQ).OrderByDescending(i => i.MaxHealth).ToList();
            if (minions.Count == 0)
            {
                return;
            }
            var minion = minions.FirstOrDefault(i => Q.CanLastHit(i, GetQDmg(i))) ?? minions.FirstOrDefault();
            if (minion == null)
            {
                return;
            }
            Q.Casting(minion);
        }

        private static void TryEvading(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel = hitBy.Select(i => i.DangerLevel).Concat(new[] { 0 }).Max();
            var yasuoE =
                EvadeSpellDatabase.Spells.FirstOrDefault(
                    i => i.Enable && dangerLevel >= i.DangerLevel && i.IsReady && i.Slot == SpellSlot.E);
            if (yasuoE == null)
            {
                return;
            }
            yasuoE.Speed = (int)E.Speed;
            var target =
                yasuoE.GetEvadeTargets(false, true)
                    .OrderBy(i => GetPosAfterDash(i).CountEnemyHeroesInRange(400))
                    .ThenBy(i => GetPosAfterDash(i).Distance(to))
                    .FirstOrDefault();
            if (target != null && E.CastOnUnit(target))
            {
                lastE = Variables.TickCount;
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
            if (Youmuu.IsReady && Player.CountEnemyHeroesInRange(Q.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Player.CountEnemyHeroesInRange(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Player.CountEnemyHeroesInRange(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (Titanic.IsReady && !Player.Spellbook.IsAutoAttacking && Variables.Orbwalker.GetTarget() != null)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            #endregion

            #region Methods

            internal static void Init()
            {
                LoadSpellData();
                var evadeMenu = MainMenu.Add(new Menu("EvadeTarget", "Evade Target"));
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
                    new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { ChampionName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
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
                    new SpellData
                        { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                        { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
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
                DetectedTargets.RemoveAll(i => i.Obj.Compare(missile));
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
                if (!MainMenu["EvadeTarget"]["W"] || !W.IsReady() || DetectedTargets.Count == 0)
                {
                    return;
                }
                DetectedTargets.Where(i => W.IsInRange(i.Obj, 450))
                    .OrderBy(i => i.Obj.Distance(Player))
                    .ForEach(i => W.Cast(Player.ServerPosition.Extend(i.Start, 100)));
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

                public string MissileName => this.SpellNames.First();

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
namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers.Damages;
    using LeagueSharp.SDK.Modes;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;
    using Skillshot = Valvrave_Sharp.Evade.Skillshot;

    #endregion

    internal class Yasuo : Program
    {
        #region Constants

        private const int QCirWidth = 275, RWidth = 400;

        #endregion

        #region Static Fields

        private static int cDash;

        private static bool haveQ3, haveR;

        private static Vector3 posDash;

        private static Spell spellQ;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 475).SetSkillshot(0.4f, 20, float.MaxValue, false, SkillshotType.SkillshotLine);
            spellQ = new Spell(Q.Slot).SetSkillshot(Q.Delay, Q.Width, Q.Speed, false, Q.Type);
            Q2 = new Spell(Q.Slot, 1100).SetSkillshot(Q.Delay, 90, 1200, true, Q.Type);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475).SetSkillshot(0, 1, 1025, false, Q.Type);
            R = new Spell(SpellSlot.R, 1200);
            Q.DamageType = Q2.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.VeryHigh;

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
                comboMenu.Bool("R", "Use R");
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
                ksMenu.Separator("Extra R Settings");
                GameObjects.EnemyHeroes.ForEach(
                    i => ksMenu.Bool("RCast" + i.ChampionName, "Cast On " + i.ChampionName, false));
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
                        return;
                    }
                    var qDelay = 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58552631578947f, 0.6675f));
                    if (!Q.Delay.Equals(qDelay))
                    {
                        Q.Delay = Q2.Delay = spellQ.Delay = qDelay;
                    }
                    var eSpeed = 1025 + (Player.MoveSpeed - 345);
                    if (!E.Speed.Equals(eSpeed))
                    {
                        E.Speed = eSpeed;
                    }
                };
            Variables.Orbwalker.OnAction += (sender, args) =>
                {
                    if (!Q.IsReady() || haveQ3 || args.Type != OrbwalkingType.AfterAttack
                        || Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.LaneClear)
                    {
                        return;
                    }
                    var target = args.Target;
                    if (target.Type == GameObjectType.obj_AI_Hero || target.Type == GameObjectType.obj_AI_Minion)
                    {
                        return;
                    }
                    if (Q.GetTarget(100) != null
                        || GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                               .Concat(GameObjects.Jungle)
                               .Count(i => i.IsValidTarget(Q.Range + i.BoundingRadius * 2)) > 0)
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
                    posDash = args.EndPos.ToVector3();
                };
            Obj_AI_Base.OnBuffUpdateCount += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe || args.Buff.DisplayName != "YasuoDashScalar")
                    {
                        return;
                    }
                    cDash = args.Buff.Count;
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
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
                        case "YasuoRArmorPen":
                            haveR = true;
                            break;
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
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
            AttackableUnit.OnDamage += (sender, args) =>
                {
                    if (!haveR || Player.NetworkId != args.SourceNetworkId)
                    {
                        return;
                    }
                    DelayAction.Add(
                        200,
                        () =>
                            {
                                haveR = false;
                                DelayAction.Add(
                                    100,
                                    () =>
                                    Player.IssueOrder(
                                        GameObjectOrder.MoveTo,
                                        Player.ServerPosition.Extend(Game.CursorPos, 100 + Player.BoundingRadius)));
                            });
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.Slot != SpellSlot.Q || !Player.IsDashing())
                    {
                        return;
                    }
                    Variables.Orbwalker.SetAttackState(false);
                    DelayAction.Add(
                        200,
                        () =>
                            {
                                Variables.Orbwalker.SetAttackState(true);
                                DelayAction.Add(
                                    100,
                                    () =>
                                    Player.IssueOrder(
                                        GameObjectOrder.MoveTo,
                                        Player.ServerPosition.Extend(Game.CursorPos, 100 + Player.BoundingRadius)));
                            });
                };
            Spellbook.OnCastSpell += (sender, args) =>
                {
                    if (!sender.Owner.IsMe || args.Slot != SpellSlot.Q || !Player.IsDashing())
                    {
                        return;
                    }
                    switch (Variables.Orbwalker.GetActiveMode())
                    {
                        case OrbwalkingMode.Combo:
                            if (haveQ3 && GetQCirTarget.Count == 0)
                            {
                                Game.PrintChat("Block Q: Combo");
                                args.Process = false;
                            }
                            break;
                        case OrbwalkingMode.LaneClear:
                            if (GetQCirObj.Count == 0)
                            {
                                Game.PrintChat("Block Q: Clear");
                                args.Process = false;
                            }
                            break;
                    }
                };
        }

        #endregion

        #region Properties

        private static bool CanCastQCir => posDash.IsValid() && posDash.DistanceToPlayer() < 100;

        private static List<Obj_AI_Base> GetDashObj
            =>
                GameObjects.EnemyHeroes.Cast<Obj_AI_Base>()
                    .Concat(GameObjects.Jungle)
                    .Concat(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false)))
                    .Where(CanCastE)
                    .ToList();

        private static List<Obj_AI_Base> GetQCirObj
            =>
                GameObjects.EnemyHeroes.Cast<Obj_AI_Base>()
                    .Concat(GameObjects.Jungle)
                    .Concat(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet()))
                    .Where(i => i.IsValidTarget(QCirWidth, true, posDash))
                    .ToList();

        private static List<Obj_AI_Hero> GetQCirTarget
            => Variables.TargetSelector.GetTargets(QCirWidth, Q.DamageType, false, posDash);

        private static List<Obj_AI_Hero> GetRTarget
            => GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(R.Range) && HaveR(i)).ToList();

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || Player.IsDashing()
                || (haveQ3 && !MainMenu["Hybrid"]["AutoQ3"]))
            {
                return;
            }
            if (!haveQ3)
            {
                CastQHero();
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
            return buff != null
                   && buff.EndTime - Game.Time
                   <= (buff.EndTime - buff.StartTime <= 0.75 ? 0.35 : 0.2) * (buff.EndTime - buff.StartTime);
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return target.IsValidTarget(E.Range) && !HaveE(target);
        }

        private static bool CanDash(
            Obj_AI_Base target,
            bool inQCir = false,
            bool underTower = true,
            Vector3 pos = new Vector3())
        {
            if (!pos.IsValid())
            {
                pos = target.ServerPosition;
            }
            var posAfterE = GetPosAfterDash(target);
            return (underTower || !posAfterE.IsUnderEnemyTurret())
                   && posAfterE.Distance(pos) < (inQCir ? QCirWidth : pos.DistanceToPlayer())
                   && Evade.IsSafePoint(posAfterE).IsSafe;
        }

        private static bool CastQ(Obj_AI_Base target)
        {
            spellQ.Range = Q.Range + target.BoundingRadius - 10;
            spellQ.Width = Q.Width;
            if (target.Type == GameObjectType.obj_AI_Hero)
            {
                spellQ.Width = Math.Min(Q.Width + target.BoundingRadius / 2, 40);
                spellQ.Range += spellQ.Width / 2;
            }
            var pred = spellQ.VPrediction(target, true);
            return pred.Hitchance >= Q.MinHitChance && Q.Cast(pred.CastPosition);
        }

        private static bool CastQ3()
        {
            var targets = Variables.TargetSelector.GetTargets(Q2.Range + Q2.Width / 2, Q2.DamageType);
            if (targets.Count == 0)
            {
                return false;
            }
            var hit = -1;
            var predPos = new Vector3();
            targets.Select(i => Q2.VPrediction(i, true, CollisionableObjects.YasuoWall))
                .Where(i => i.Hitchance >= Q2.MinHitChance && i.AoeTargetsHitCount > hit)
                .ForEach(
                    i =>
                        {
                            hit = i.AoeTargetsHitCount;
                            predPos = i.CastPosition;
                        });
            return predPos.IsValid() && Q.Cast(predPos);
        }

        private static CastStates CastQHero(bool onlyKill = false)
        {
            var targets = Variables.TargetSelector.GetTargets(600, Q.DamageType, onlyKill);
            if (onlyKill)
            {
                targets = targets.Where(i => i.Health + i.PhysicalShield <= GetQDmg(i)).ToList();
            }
            return targets.Count == 0
                       ? CastStates.InvalidTarget
                       : (targets.Any(CastQ) ? CastStates.SuccessfullyCasted : CastStates.NotCasted);
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"] && R.IsReady())
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
                                   select enemy).OrderByDescending(
                                       i =>
                                       GameObjects.EnemyHeroes.Count(
                                           a => a.IsValidTarget(RWidth, true, i.ServerPosition) && HaveR(a)))
                        .ThenByDescending(i => new Priority().GetDefaultPriority(i))
                        .ToList();
                    if (targets.Count > 0)
                    {
                        var target = !MainMenu["Combo"]["RDelay"]
                                         ? targets.FirstOrDefault()
                                         : targets.FirstOrDefault(CanCastDelayR);
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
                    var dashObj = GetDashObj.Where(i => underTower || !GetPosAfterDash(i).IsUnderEnemyTurret()).ToList();
                    var targetE = E.GetTarget(QCirWidth);
                    if (targetE != null && haveQ3 && Q.IsReady(100))
                    {
                        var nearObj = GetBestObj(dashObj, targetE, true);
                        if (nearObj != null
                            && (GetPosAfterDash(nearObj).CountEnemyHeroesInRange(QCirWidth) > 1
                                || Player.CountEnemyHeroesInRange(Q.Range + E.Range / 2) == 1) && E.CastOnUnit(nearObj))
                        {
                            return;
                        }
                    }
                    targetE = E.GetTarget();
                    if (targetE != null && !HaveE(targetE)
                        && ((cDash > 0 && CanDash(targetE, false, underTower))
                            || (haveQ3 && Q.IsReady(100) && CanDash(targetE, true, underTower)))
                        && E.CastOnUnit(targetE))
                    {
                        return;
                    }
                    var target = Q.GetTarget(100) ?? Q2.GetTarget();
                    if (target != null && target.DistanceToPlayer() > target.GetRealAutoAttackRange() * 0.4)
                    {
                        var nearObj = GetBestObj(dashObj, target, true) ?? GetBestObj(dashObj, target);
                        if (nearObj != null)
                        {
                            E.CastOnUnit(nearObj);
                        }
                        else if (!HaveE(target) && CanDash(target, false, underTower) && E.CastOnUnit(target))
                        {
                            return;
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
                            return;
                        }
                    }
                }
            }
            if (Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    if (CanCastQCir)
                    {
                        if (GetQCirTarget.Count > 0 && Q.Cast(GetQCirTarget.First().ServerPosition))
                        {
                            return;
                        }
                        if (!haveQ3 && MainMenu["Combo"]["EGap"] && MainMenu["Combo"]["EStackQ"]
                            && Q.GetTarget(100) == null && GetQCirObj.Count > 0
                            && Q.Cast(GetQCirObj.First().ServerPosition))
                        {
                            return;
                        }
                    }
                }
                else if (!haveQ3 ? CastQHero().IsCasted() : CastQ3())
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
                sender.Spellbook.CastSpell(yasuoW.Slot, sender.ServerPosition.Extend(skillshot.Start, 100));
            }
        }

        private static void Flee()
        {
            if (MainMenu["Flee"]["Q"] && Q.IsReady() && !haveQ3 && Player.IsDashing() && CanCastQCir
                && GetQCirObj.Count > 0 && Q.Cast(GetQCirObj.First().ServerPosition))
            {
                return;
            }
            if (!E.IsReady())
            {
                return;
            }
            var obj = GetBestObjToMouse();
            if (obj != null)
            {
                E.CastOnUnit(obj);
            }
        }

        private static Obj_AI_Base GetBestObj(List<Obj_AI_Base> obj, Obj_AI_Hero target, bool inQCir = false)
        {
            var pos = E.VPrediction(target).UnitPosition;
            return
                obj.Where(i => !i.Compare(target) && CanDash(i, inQCir, true, pos))
                    .MinOrDefault(i => GetPosAfterDash(i).Distance(pos));
        }

        private static Obj_AI_Base GetBestObjToMouse(bool underTower = true)
        {
            var pos = Game.CursorPos;
            return
                GetDashObj.Where(i => CanDash(i, false, underTower, pos))
                    .MinOrDefault(i => GetPosAfterDash(i).Distance(pos));
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return E.GetDamage(target) + E.GetDamage(target, Damage.DamageStage.Buff);
        }

        private static Vector2 GetPosAfterDash(Obj_AI_Base target)
        {
            return Player.ServerPosition.ToVector2().Extend(target.ServerPosition, E.Range);
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
            return Q.GetDamage(target) + dmgItem;
        }

        private static float GetQHpPred(Obj_AI_Minion minion)
        {
            return Health.GetPrediction(minion, (int)(Q.Delay * 1000 - 100));
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
            if (!Q.IsReady() || Player.IsDashing())
            {
                return;
            }
            if (!haveQ3)
            {
                var state = CastQHero();
                if (state.IsCasted())
                {
                    return;
                }
                if (state == CastStates.InvalidTarget && MainMenu["Hybrid"]["QLastHit"] && Q.GetTarget(100) == null)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i =>
                            (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range + i.BoundingRadius / 2)
                            && GetQHpPred(i) > 0 && GetQHpPred(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        CastQ(minion);
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
                if (Player.IsDashing())
                {
                    if (CanCastQCir && GetQCirTarget.Any(i => i.Health + i.PhysicalShield <= GetQDmg(i))
                        && Q.Cast(GetQCirTarget.First().ServerPosition))
                    {
                        return;
                    }
                }
                else
                {
                    if (!haveQ3)
                    {
                        if (CastQHero(true).IsCasted())
                        {
                            return;
                        }
                    }
                    else
                    {
                        var target = Q2.GetTarget(Q2.Width / 2);
                        if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target))
                        {
                            var pred = Q2.VPrediction(target, true, CollisionableObjects.YasuoWall);
                            if (pred.Hitchance >= Q2.MinHitChance && Q.Cast(pred.CastPosition))
                            {
                                return;
                            }
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
                        E.CastOnUnit(target);
                    }
                    else if (MainMenu["KillSteal"]["Q"] && Q.IsReady(100))
                    {
                        target = targets.Where(i => i.Distance(GetPosAfterDash(i)) < QCirWidth).FirstOrDefault(
                            i =>
                                {
                                    var dmgE = GetEDmg(i) - i.MagicalShield;
                                    return (i.Health - (dmgE > 0 ? dmgE : 0)) + i.PhysicalShield <= GetQDmg(i);
                                });
                        if (target != null && E.CastOnUnit(target))
                        {
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
                                || i.Health + i.PhysicalShield <= R.GetDamage(i) + GetQDmg(i))
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
                    GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                        .Concat(GameObjects.Jungle)
                        .Where(
                            i =>
                            CanCastE(i) && (!GetPosAfterDash(i).IsUnderEnemyTurret() || MainMenu["LaneClear"]["ETower"])
                            && Evade.IsSafePoint(GetPosAfterDash(i)).IsSafe)
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minions.Count > 0)
                {
                    var minion =
                        minions.FirstOrDefault(
                            i => E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i));
                    if (useQ && minion == null && Q.IsReady(100) && (!haveQ3 || useQ3))
                    {
                        var sub = new List<Obj_AI_Minion>();
                        foreach (var mob in minions)
                        {
                            if ((E.GetHealthPrediction(mob) > 0
                                 && E.GetHealthPrediction(mob) - GetEDmg(mob) <= GetQDmg(mob)
                                 || mob.Team == GameObjectTeam.Neutral)
                                && mob.Distance(GetPosAfterDash(mob)) < QCirWidth)
                            {
                                sub.Add(mob);
                            }
                            if (MainMenu["LaneClear"]["ELastHit"])
                            {
                                continue;
                            }
                            var nearMinion =
                                GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                                    .Concat(GameObjects.Jungle)
                                    .Where(i => i.IsValidTarget(QCirWidth, true, GetPosAfterDash(mob).ToVector3()))
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
                        return;
                    }
                }
            }
            if (useQ && Q.IsReady() && (!haveQ3 || useQ3))
            {
                if (Player.IsDashing())
                {
                    if (CanCastQCir)
                    {
                        var minion = GetQCirObj.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                        if (minion.Any(i => i.Health <= GetQDmg(i) || i.Team == GameObjectTeam.Neutral)
                            || minion.Count > 2)
                        {
                            Q.Cast(minion.First().ServerPosition);
                        }
                    }
                }
                else
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                            .Concat(GameObjects.Jungle)
                            .Where(
                                i =>
                                i.IsValidTarget(
                                    !haveQ3 ? Q.Range + i.BoundingRadius / 2 : Q2.Range - i.BoundingRadius / 2))
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (minions.Count == 0)
                    {
                        return;
                    }
                    if (!haveQ3)
                    {
                        var minion = minions.FirstOrDefault(i => GetQHpPred(i) > 0 && GetQHpPred(i) <= GetQDmg(i));
                        if (minion != null)
                        {
                            CastQ(minion);
                        }
                        else
                        {
                            minion = minions.FirstOrDefault();
                            if (minion != null)
                            {
                                CastQ(minion);
                            }
                        }
                    }
                    else
                    {
                        var pos = Q2.VLineFarmLocation(minions);
                        if (pos.MinionsHit > 0)
                        {
                            Q.Cast(pos.Position);
                        }
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (MainMenu["LastHit"]["Q"] && Q.IsReady() && !Player.IsDashing() && (!haveQ3 || MainMenu["LastHit"]["Q3"]))
            {
                if (!haveQ3)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i =>
                            (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range + i.BoundingRadius / 2)
                            && GetQHpPred(i) > 0 && GetQHpPred(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null && CastQ(minion))
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
                            && Q2.GetHealthPrediction(i) > 0 && Q2.GetHealthPrediction(i) <= GetQDmg(i)
                            && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                                || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                                || i.Health > Player.GetAutoAttackDamage(i))).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        var pred = Q2.VPrediction(minion, true, CollisionableObjects.YasuoWall);
                        if (pred.Hitchance >= Q2.MinHitChance && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
            }
            if (MainMenu["LastHit"]["E"] && E.IsReady())
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        (i.IsMinion() || i.IsPet(false)) && CanCastE(i) && E.GetHealthPrediction(i) > 0
                        && E.GetHealthPrediction(i) <= GetEDmg(i) && Evade.IsSafePoint(GetPosAfterDash(i)).IsSafe
                        && (!GetPosAfterDash(i).IsUnderEnemyTurret() || MainMenu["LastHit"]["ETower"]))
                        .MaxOrDefault(i => i.MaxHealth);
                if (minion != null)
                {
                    E.CastOnUnit(minion);
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
                    Player.IsDashing() ? QCirWidth : (!haveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"] && R.IsReady())
            {
                Drawing.DrawCircle(Player.Position, R.Range, GetRTarget.Count > 0 ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["StackQ"] && Q.Level > 0)
            {
                var useQ = MainMenu["StackQ"].GetValue<MenuKeyBind>().Active;
                var qReady = Q.IsReady();
                var text =
                    $"Auto Stack Q: {(useQ ? (haveQ3 ? "Full" : (qReady ? "Ready" : "Not Ready")) : "Off")} [{MainMenu["StackQ"].GetValue<MenuKeyBind>().Key}]";
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(
                    pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                    pos.Y + 20,
                    useQ && qReady && !haveQ3 ? Color.White : Color.Gray,
                    text);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling() || haveR)
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
            if (!MainMenu["StackQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || haveQ3 || Player.IsDashing())
            {
                return;
            }
            var state = CastQHero();
            if (state.IsCasted())
            {
                return;
            }
            if (state != CastStates.InvalidTarget)
            {
                return;
            }
            var minions =
                GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                    .Concat(GameObjects.Jungle)
                    .Where(i => i.IsValidTarget(Q.Range + i.BoundingRadius / 2))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            var minion = minions.FirstOrDefault(i => GetQHpPred(i) > 0 && GetQHpPred(i) <= GetQDmg(i));
            if (minion != null)
            {
                CastQ(minion);
            }
            else
            {
                minion = minions.FirstOrDefault();
                if (minion != null)
                {
                    CastQ(minion);
                }
            }
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
            var target =
                Evader.GetEvadeTargets(
                    yasuoE.ValidTargets,
                    yasuoE.Speed,
                    yasuoE.Delay,
                    yasuoE.MaxRange,
                    false,
                    false,
                    true)
                    .Where(
                        i =>
                        Evade.IsSafePoint(GetPosAfterDash(i)).IsSafe
                        && (!GetPosAfterDash(i).IsUnderEnemyTurret()
                            || MainMenu["Evade"]["Spells"][yasuoE.Name]["ETower"]))
                    .MinOrDefault(i => i.Distance(to));
            if (target != null)
            {
                Player.Spellbook.CastSpell(yasuoE.Slot, target);
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
            if (Titanic.IsReady && Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange()) > 0)
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
                    new SpellData
                        { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemnmissile" }, Slot = SpellSlot.E });
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
                DetectedTargets.Where(i => W.IsInRange(i.Obj))
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
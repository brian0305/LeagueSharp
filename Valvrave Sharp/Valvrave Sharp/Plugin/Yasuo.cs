namespace Valvrave_Sharp.Plugin
{
    #region

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
    using LeagueSharp.SDK.Core.Wrappers.Damages;
    using LeagueSharp.SDK.Core.Wrappers.Spells;
    using LeagueSharp.SDK.Core.Wrappers.TargetSelector.Modes;

    using SharpDX;

    using Valvrave_Sharp.Core;
    using Valvrave_Sharp.Evade;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    #endregion

    internal class Yasuo : Program
    {
        #region Constants

        private const int QCirWidth = 275, RWidth = 400;

        #endregion

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 510).SetSkillshot(
                GetQ1Delay,
                20,
                float.MaxValue,
                false,
                SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 1150).SetSkillshot(GetQ2Delay, 90, 1500, true, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475).SetTargetted(0, GetESpeed);
            R = new Spell(SpellSlot.R, 1200);
            Q.DamageType = Q2.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = Q2.MinHitChance = HitChance.High;

            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Separator("Q: Always On");
                comboMenu.Separator("Sub Settings");
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
                comboMenu.Separator("E Gap Settings");
                comboMenu.Bool("EGap", "Use E");
                comboMenu.List("EMode", "Follow Mode", new[] { "Enemy", "Mouse" });
                comboMenu.Slider("ERange", "If Distance >", 300, 0, (int)E.Range);
                comboMenu.Bool("ETower", "Under Tower");
                comboMenu.Bool("EStackQ", "Stack Q While Gap", false);
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Bool("RDelay", "Delay Casting");
                comboMenu.Slider("RHpU", "If Enemy Hp < (%)", 60);
                comboMenu.Slider("RCountA", "Or Enemy >=", 2, 1, 5);
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
                lcMenu.Separator("Q: Always On");
                lcMenu.Bool("Q3", "Also Q3");
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
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    ksMenu.Bool("RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName, false);
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
                drawMenu.Bool("Q", "Q Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range");
                drawMenu.Bool("StackQ", "Auto Stack Q Status");
            }
            MainMenu.KeyBind("StackQ", "Auto Stack Q", Keys.Z, KeyBindType.Toggle);

            Evade.Evading += Evading;
            Evade.TryEvading += TryEvading;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        #endregion

        #region Properties

        private static float GetESpeed => 700 + Player.MoveSpeed;

        private static float GetQ1Delay => 0.4f * GetQDelay;

        private static float GetQ2Delay => 0.5f * GetQDelay;

        private static List<Obj_AI_Base> GetQCirObj
            =>
                Player.Distance(Player.GetDashInfo().EndPos) < 200
                    ? GameObjects.EnemyHeroes.Cast<Obj_AI_Base>()
                          .Concat(GameObjects.Jungle)
                          .Concat(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet()))
                          .Where(i => i.IsValidTarget(QCirWidth, true, Player.GetDashInfo().EndPos.ToVector3()))
                          .ToList()
                    : new List<Obj_AI_Base>();

        private static Obj_AI_Hero GetQCirTarget
            =>
                Player.Distance(Player.GetDashInfo().EndPos) < 200
                    ? Variables.TargetSelector.GetTarget(
                        QCirWidth,
                        DamageType.Physical,
                        true,
                        Player.GetDashInfo().EndPos.ToVector3())
                    : null;

        private static float GetQDelay => 1 - Math.Min((Player.AttackSpeedMod - 1) * 0.0058552631578947f, 0.6675f);

        private static List<Obj_AI_Hero> GetRTarget
            => GameObjects.EnemyHeroes.Where(i => R.IsInRange(i) && CanCastR(i)).ToList();

        private static bool HaveQ3 => Player.HasBuff("YasuoQ3W");

        private static Spell SpellQ => !HaveQ3 ? Q : Q2;

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (Player.IsDashing() || !Q.IsReady() || !MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active
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
            if (target.Buffs.Any(i => i.IsValid && i.Type == BuffType.Knockback))
            {
                return true;
            }
            var buff = target.Buffs.FirstOrDefault(i => i.IsValid && i.Type == BuffType.Knockup);
            return buff != null
                   && buff.EndTime - Game.Time
                   <= (buff.EndTime - buff.StartTime <= 0.75 ? 0.5 : 0.25) * (buff.EndTime - buff.StartTime);
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return target.IsValidTarget(E.Range) && !target.HasBuff("YasuoDashWrapper");
        }

        private static bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup);
        }

        private static void CastQ3()
        {
            var hit = -1;
            var predPos = new Vector3();
            foreach (var pred in
                Variables.TargetSelector.GetTargets(Q2.Range + Q2.Width, Q2.DamageType)
                    .Select(i => Q2.VPrediction(i, true, CollisionableObjects.YasuoWall)))
            {
                if (pred.Hitchance >= Q2.MinHitChance && pred.AoeTargetsHitCount > hit)
                {
                    hit = pred.AoeTargetsHitCount;
                    predPos = pred.CastPosition;
                }
            }
            if (predPos.IsValid())
            {
                Q.Cast(predPos);
            }
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["R"] && R.IsReady() && GetRTarget.Count > 0)
            {
                var targetList = (from enemy in GetRTarget
                                  let nearEnemy =
                                      GameObjects.EnemyHeroes.Where(i => i.Distance(enemy) < RWidth && CanCastR(i))
                                      .ToList()
                                  where
                                      (nearEnemy.Count > 1
                                       && enemy.Health + enemy.PhysicalShield
                                       <= Player.GetSpellDamage(enemy, SpellSlot.R))
                                      || nearEnemy.Sum(i => i.HealthPercent) / nearEnemy.Count
                                      <= MainMenu["Combo"]["RHpU"] || nearEnemy.Count >= MainMenu["Combo"]["RCountA"]
                                  orderby nearEnemy.Count descending
                                  select enemy).ToList();
                if (targetList.Count > 0)
                {
                    var target = !MainMenu["Combo"]["RDelay"]
                                     ? targetList.FirstOrDefault()
                                     : targetList.FirstOrDefault(CanCastDelayR);
                    if (target != null)
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
            if (MainMenu["Combo"]["EGap"] && E.IsReady())
            {
                if (MainMenu["Combo"]["EMode"].GetValue<MenuList>().Index == 0)
                {
                    var target = Q.GetTarget(QCirWidth);
                    if (target != null && HaveQ3 && Q.IsReady(20))
                    {
                        var nearObj = GetNearObj(target, true);
                        if (nearObj != null
                            && (PosAfterE(nearObj).CountEnemyHeroesInRange(QCirWidth) > 1
                                || Player.CountEnemyHeroesInRange(Q.Range + 150) < 3))
                        {
                            E.CastOnUnit(nearObj);
                        }
                    }
                    target = Q.GetTarget(Q.Width) ?? Q2.GetTarget();
                    if (target != null)
                    {
                        var nearObj = GetNearObj(target, false, MainMenu["Combo"]["ETower"], true);
                        if (nearObj != null
                            && (nearObj.Compare(target)
                                    ? !target.InAutoAttackRange()
                                    : Player.Distance(target) > MainMenu["Combo"]["ERange"]))
                        {
                            E.CastOnUnit(nearObj);
                        }
                    }
                }
                else
                {
                    var nearObj = GetNearObj(null, false, MainMenu["Combo"]["ETower"]);
                    if (nearObj != null && Player.Distance(Game.CursorPos) > MainMenu["Combo"]["ERange"])
                    {
                        E.CastOnUnit(nearObj);
                    }
                }
            }
            if (Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    if (GetQCirTarget != null)
                    {
                        Q.Cast(Player.ServerPosition);
                    }
                    if (!HaveQ3 && MainMenu["Combo"]["EGap"] && MainMenu["Combo"]["EStackQ"] && Q.GetTarget(50) == null
                        && GetQCirObj.Count > 0)
                    {
                        Q.Cast(Player.ServerPosition);
                    }
                }
                else
                {
                    if (!HaveQ3)
                    {
                        Q.CastingBestTarget(Q.Width, true);
                    }
                    else
                    {
                        CastQ3();
                    }
                }
            }
            var subTarget = Q.GetTarget(Q.Width) ?? Q2.GetTarget();
            if (MainMenu["Combo"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget == null)
            {
                return;
            }
            if (MainMenu["Combo"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 30
                && Player.Distance(subTarget) <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Evading()
        {
            var yasuoW = EvadeSpellDatabase.Spells.FirstOrDefault(i => i.Enable && i.IsReady && i.Slot == SpellSlot.W);
            if (yasuoW == null)
            {
                return;
            }
            var skillshot =
                Evade.SkillshotAboutToHit(
                    Player,
                    yasuoW.Delay - MainMenu["Evade"]["Spells"][yasuoW.Name]["WDelay"],
                    true).OrderByDescending(i => i.DangerLevel).FirstOrDefault(i => i.DangerLevel >= yasuoW.DangerLevel);
            if (skillshot != null)
            {
                Player.Spellbook.CastSpell(yasuoW.Slot, Player.ServerPosition.Extend(skillshot.Start, 100));
            }
        }

        private static void Flee()
        {
            if (Player.IsDashing() && !HaveQ3 && MainMenu["Flee"]["Q"] && Q.IsReady() && GetQCirObj.Count > 0)
            {
                Q.Cast(Player.ServerPosition);
            }
            if (E.IsReady())
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
            return Player.GetSpellDamage(target, SpellSlot.E)
                   + Player.GetSpellDamage(target, SpellSlot.E, Damage.DamageStage.Buff);
        }

        private static Obj_AI_Base GetNearObj(
            Obj_AI_Base target = null,
            bool inQCir = false,
            bool underTower = true,
            bool checkFace = false)
        {
            var pos = target != null
                          ? Prediction.GetPrediction(target, E.Delay, 1, E.Speed).UnitPosition
                          : Game.CursorPos;
            var obj = new List<Obj_AI_Base>();
            obj.AddRange(GameObjects.EnemyHeroes.Where(i => !i.InFountain()));
            obj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false)));
            obj.AddRange(GameObjects.Jungle);
            return obj.Where(
                i =>
                    {
                        var posAfterE = PosAfterE(i);
                        return CanCastE(i) && (!checkFace || Player.IsFacing(i))
                               && (underTower || !posAfterE.IsUnderEnemyTurret())
                               && posAfterE.Distance(pos) < (inQCir ? QCirWidth : Player.Distance(pos))
                               && Evade.IsSafePoint(posAfterE).IsSafe;
                    }).MinOrDefault(i => PosAfterE(i).Distance(pos));
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
            return Player.GetSpellDamage(target, SpellSlot.Q) + dmgItem;
        }

        private static void Hybrid()
        {
            if (Player.IsDashing() || !Q.IsReady())
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
                    foreach (var minion in
                        GameObjects.EnemyMinions.Where(
                            i =>
                            i.IsValidTarget(Q.Range - Q.Width) && (i.IsMinion() || i.IsPet(false))
                            && Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i))
                            .OrderByDescending(i => i.MaxHealth))
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
                if (Player.IsDashing())
                {
                    var target = GetQCirTarget;
                    if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target))
                    {
                        Q.Cast(Player.ServerPosition);
                    }
                }
                else
                {
                    var target = SpellQ.GetTarget(SpellQ.Width);
                    if (target != null && target.Health + target.PhysicalShield <= GetQDmg(target))
                    {
                        if (!HaveQ3)
                        {
                            Q.Casting(target, true);
                        }
                        else
                        {
                            var pred = Q2.VPrediction(target, true, CollisionableObjects.YasuoWall);
                            if (pred.Hitchance >= Q2.MinHitChance)
                            {
                                Q.Cast(pred.CastPosition);
                            }
                        }
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady())
            {
                var targetList = Variables.TargetSelector.GetTargets(E.Range, E.DamageType).Where(CanCastE).ToList();
                var targetE = targetList.FirstOrDefault(i => i.Health + i.MagicalShield <= GetEDmg(i));
                if (targetE != null)
                {
                    E.CastOnUnit(targetE);
                }
                else if (MainMenu["KillSteal"]["Q"] && Q.IsReady(20))
                {
                    var targetQCirE =
                        targetList.Where(i => i.Distance(PosAfterE(i)) < QCirWidth)
                            .FirstOrDefault(i => i.Health - GetEDmg(i) + i.PhysicalShield <= GetQDmg(i));
                    if (targetQCirE != null)
                    {
                        E.CastOnUnit(targetQCirE);
                    }
                }
            }
            if (MainMenu["KillSteal"]["R"] && R.IsReady())
            {
                var target =
                    GetRTarget.Where(
                        i =>
                        MainMenu["KillSteal"]["RCast" + i.ChampionName]
                        && (i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.R)
                            || i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.R) + GetQDmg(i)))
                        .MaxOrDefault(i => new Priority().GetDefaultPriority(i));
                if (target != null)
                {
                    R.CastOnUnit(target);
                }
            }
        }

        private static void LaneClear()
        {
            if (MainMenu["LaneClear"]["E"] && E.IsReady())
            {
                var minion =
                    GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                        .Concat(GameObjects.Jungle)
                        .Where(
                            i =>
                            CanCastE(i) && (!PosAfterE(i).IsUnderEnemyTurret() || MainMenu["LaneClear"]["ETower"])
                            && Evade.IsSafePoint(PosAfterE(i)).IsSafe)
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minion.Count > 0)
                {
                    var obj =
                        minion.FirstOrDefault(
                            i => E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i));
                    if (obj == null && Q.IsReady(20) && (!HaveQ3 || MainMenu["LaneClear"]["Q3"]))
                    {
                        var sub = new List<Obj_AI_Minion>();
                        foreach (var mob in minion)
                        {
                            if ((mob.Health - GetEDmg(mob) <= GetQDmg(mob) || mob.Team == GameObjectTeam.Neutral)
                                && mob.Distance(PosAfterE(mob)) < QCirWidth)
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
                                    .Where(i => i.IsValidTarget(QCirWidth, true, PosAfterE(mob).ToVector3()))
                                    .ToList();
                            if (nearMinion.Count > 2 || nearMinion.Count(i => mob.Health <= GetQDmg(mob)) > 1)
                            {
                                sub.Add(mob);
                            }
                        }
                        obj = sub.FirstOrDefault();
                    }
                    if (obj != null)
                    {
                        E.CastOnUnit(obj);
                    }
                }
            }
            if (Q.IsReady() && (!HaveQ3 || MainMenu["LaneClear"]["Q3"]))
            {
                if (Player.IsDashing())
                {
                    var minion = GetQCirObj.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                    if (minion.Any(i => i.Health <= GetQDmg(i) || i.Team == GameObjectTeam.Neutral) || minion.Count > 2)
                    {
                        Q.Cast(Player.ServerPosition);
                    }
                }
                else
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                            .Concat(GameObjects.Jungle)
                            .Where(i => i.IsValidTarget(SpellQ.Range - SpellQ.Width))
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (minion.Count == 0)
                    {
                        return;
                    }
                    if (!HaveQ3)
                    {
                        var obj =
                            minion.FirstOrDefault(
                                i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i));
                        if (obj != null)
                        {
                            Q.Casting(obj, true);
                        }
                    }
                    var posPred = new List<Vector3>();
                    foreach (var obj in minion)
                    {
                        if (!HaveQ3)
                        {
                            var pred = Q.VPrediction(obj, true);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                posPred.Add(pred.UnitPosition);
                            }
                        }
                        else
                        {
                            var pred = Q2.VPrediction(obj, true, CollisionableObjects.YasuoWall);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                posPred.Add(pred.UnitPosition);
                            }
                        }
                    }
                    if (posPred.Count == 0)
                    {
                        return;
                    }
                    var pos = SpellQ.GetLineFarmLocation(posPred.ToVector2());
                    if (pos.MinionsHit > 0)
                    {
                        Q.Cast(pos.Position);
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (!Player.IsDashing() && MainMenu["LastHit"]["Q"] && Q.IsReady() && (!HaveQ3 || MainMenu["LastHit"]["Q3"]))
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(
                        i =>
                        i.IsValidTarget(SpellQ.Range - SpellQ.Width) && (i.IsMinion() || i.IsPet(false))
                        && SpellQ.GetHealthPrediction(i) > 0 && SpellQ.GetHealthPrediction(i) <= GetQDmg(i))
                        .OrderByDescending(i => i.MaxHealth))
                {
                    if (!HaveQ3)
                    {
                        Q.Casting(minion, true);
                    }
                    else
                    {
                        var pred = Q2.VPrediction(minion, true, CollisionableObjects.YasuoWall);
                        if (pred.Hitchance >= Q2.MinHitChance)
                        {
                            Q.Cast(pred.CastPosition);
                        }
                    }
                }
            }
            if (MainMenu["LastHit"]["E"] && E.IsReady())
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        CanCastE(i) && (i.IsMinion() || i.IsPet(false)) && Evade.IsSafePoint(PosAfterE(i)).IsSafe
                        && (!PosAfterE(i).IsUnderEnemyTurret() || MainMenu["LastHit"]["ETower"])
                        && E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i))
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
                    Player.IsDashing() ? QCirWidth : (!HaveQ3 ? Q : Q2).Range,
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
                var text =
                    $"Auto Stack Q: {(MainMenu["StackQ"].GetValue<MenuKeyBind>().Active ? (HaveQ3 ? "Full" : (Q.IsReady() ? "Ready" : "Not Ready")) : "Off")}";
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
            if (!Q.Delay.Equals(GetQ1Delay))
            {
                Q.Delay = GetQ1Delay;
            }
            if (!Q2.Delay.Equals(GetQ2Delay))
            {
                Q2.Delay = GetQ2Delay;
            }
            if (!E.Speed.Equals(GetESpeed))
            {
                E.Speed = GetESpeed;
            }
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
            if (Variables.Orbwalker.GetActiveMode() < OrbwalkingMode.Combo
                && Variables.Orbwalker.GetActiveMode() > OrbwalkingMode.Hybrid)
            {
                AutoQ();
            }
            if (!MainMenu["Flee"]["E"].GetValue<MenuKeyBind>().Active)
            {
                StackQ();
            }
        }

        private static Vector2 PosAfterE(Obj_AI_Base target)
        {
            return Player.ServerPosition.ToVector2()
                .Extend(target.ServerPosition, Player.Distance(target) < 410 ? E.Range : Player.Distance(target) + 65);
        }

        private static void StackQ()
        {
            if (Player.IsDashing() || HaveQ3 || !Q.IsReady() || !MainMenu["StackQ"].GetValue<MenuKeyBind>().Active)
            {
                return;
            }
            var state = Q.CastingBestTarget(Q.Width, true);
            switch (state)
            {
                case CastStates.SuccessfullyCasted:
                    return;
                case CastStates.InvalidTarget:
                    var minions =
                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false))
                            .Concat(GameObjects.Jungle)
                            .Where(i => i.IsValidTarget(Q.Range - Q.Width))
                            .OrderByDescending(i => i.MaxHealth)
                            .ToList();
                    if (minions.Count == 0)
                    {
                        return;
                    }
                    var minion =
                        minions.FirstOrDefault(
                            i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i));
                    if (minion != null)
                    {
                        Q.Casting(minion, true);
                    }
                    var posPred =
                        minions.Select(i => Q.VPrediction(i, true))
                            .Where(i => i.Hitchance >= HitChance.High)
                            .Select(i => i.UnitPosition)
                            .ToList();
                    if (posPred.Count == 0)
                    {
                        return;
                    }
                    var pos = Q.GetLineFarmLocation(posPred.ToVector2());
                    if (pos.MinionsHit > 0)
                    {
                        Q.Cast(pos.Position);
                    }
                    break;
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
                        Evade.IsSafePoint(PosAfterE(i)).IsSafe
                        && (!PosAfterE(i).IsUnderEnemyTurret() || MainMenu["Evade"]["Spells"][yasuoE.Name]["ETower"]))
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
                    W.Cast(Player.ServerPosition.Extend(target.Start, 100));
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
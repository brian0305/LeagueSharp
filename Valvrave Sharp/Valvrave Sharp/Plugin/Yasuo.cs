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

        #region Constructors and Destructors

        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 510);
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
                Config.Separator(orbwalkMenu, "blank0", "Q/Ignite/Item: Always On");
                Config.Separator(orbwalkMenu, "blank1", "E Gap Settings");
                Config.Bool(orbwalkMenu, "EGap", "Use E");
                Config.Slider(orbwalkMenu, "ERange", "If Distance >", 300, 0, (int)E.Range);
                Config.Bool(orbwalkMenu, "ETower", "Under Tower");
                Config.Bool(orbwalkMenu, "EStackQ", "Stack Q While Gap", false);
                Config.Separator(orbwalkMenu, "blank2", "R Settings");
                Config.Bool(orbwalkMenu, "R", "Use R");
                Config.Bool(orbwalkMenu, "RDelay", "Delay Casting");
                Config.Slider(orbwalkMenu, "RHpU", "If Enemy Hp < (%)", 60);
                Config.Slider(orbwalkMenu, "RCountA", "Or Enemy >=", 2, 1, 5);
                MainMenu.Add(orbwalkMenu);
            }
            var hybridMenu = new Menu("Hybrid", "Hybrid");
            {
                Config.Separator(hybridMenu, "blank3", "Q: Always On");
                Config.Bool(hybridMenu, "Q3", "Also Q3");
                Config.Bool(hybridMenu, "QLastHit", "Last Hit (Q1/2)");
                Config.Separator(hybridMenu, "blank4", "Auto Q Settings");
                Config.KeyBind(hybridMenu, "AutoQ", "KeyBind", Keys.T, KeyBindType.Toggle);
                Config.Bool(hybridMenu, "AutoQ3", "Also Q3", false);
                MainMenu.Add(hybridMenu);
            }
            var lcMenu = new Menu("LaneClear", "Lane Clear");
            {
                Config.Separator(lcMenu, "blank5", "Q: Always On");
                Config.Bool(lcMenu, "Q3", "Also Q3");
                Config.Separator(lcMenu, "blank6", "E Settings");
                Config.Bool(lcMenu, "E", "Use E");
                Config.Bool(lcMenu, "ETower", "Under Tower", false);
                MainMenu.Add(lcMenu);
            }
            var lhMenu = new Menu("LastHit", "Last Hit");
            {
                Config.Separator(lhMenu, "blank7", "Q Settings");
                Config.Bool(lhMenu, "Q", "Use Q");
                Config.Bool(lhMenu, "Q3", "Also Q3", false);
                Config.Separator(lhMenu, "blank8", "E Settings");
                Config.Bool(lhMenu, "E", "Use E");
                Config.Bool(lhMenu, "ETower", "Under Tower", false);
                MainMenu.Add(lhMenu);
            }
            var ksMenu = new Menu("KillSteal", "Kill Steal");
            {
                Config.Bool(ksMenu, "Q", "Use Q");
                Config.Bool(ksMenu, "E", "Use E");
                Config.Bool(ksMenu, "R", "Use R");
                Config.Separator(ksMenu, "blank7", "Extra R Settings");
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    Config.Bool(ksMenu, "RCast" + enemy.ChampionName, "Cast On " + enemy.ChampionName, false);
                }
                MainMenu.Add(ksMenu);
            }
            var fleeMenu = new Menu("Flee", "Flee");
            {
                Config.KeyBind(fleeMenu, "E", "Use E", Keys.C);
                Config.Bool(fleeMenu, "Q", "Stack Q While Dash");
                MainMenu.Add(fleeMenu);
            }
            if (GameObjects.EnemyHeroes.Any())
            {
                Evade.Init();
            }
            var drawMenu = new Menu("Draw", "Draw");
            {
                Config.Bool(drawMenu, "Q", "Q Range");
                Config.Bool(drawMenu, "E", "E Range", false);
                Config.Bool(drawMenu, "R", "R Range");
                Config.Bool(drawMenu, "StackQ", "Auto Stack Q Status");
                MainMenu.Add(drawMenu);
            }
            Config.KeyBind(MainMenu, "StackQ", "Auto Stack Q", Keys.Z, KeyBindType.Toggle);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Game.OnUpdate += OnUpdateEvade;
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
                return 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static float GetQ3Delay
        {
            get
            {
                return 0.5f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static List<Obj_AI_Base> GetQCirObj
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos;
                var obj = new List<Obj_AI_Base>();
                obj.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget() && i.Distance(pos) < QCirWidth));
                obj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget() && i.Distance(pos) < QCirWidth));
                obj.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget() && i.Distance(pos) < QCirWidth));
                return obj.Count > 0 && Player.GetDashInfo().EndTick - Variables.TickCount < 100 + Game.Ping * 2
                           ? obj
                           : new List<Obj_AI_Base>();
            }
        }

        private static Obj_AI_Hero GetQCirTarget
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos.ToVector3();
                var target = TargetSelector.GetTarget(QCirWidth, DamageType.Physical, null, pos);
                return target != null && Player.GetDashInfo().EndTick - Variables.TickCount < 100 + Game.Ping * 2
                           ? target
                           : null;
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

        #endregion

        #region Methods

        private static void AutoQ()
        {
            if (!MainMenu["Hybrid"]["AutoQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || Player.IsDashing()
                || (HaveQ3 && !MainMenu["Hybrid"]["AutoQ3"].GetValue<MenuBool>().Value))
            {
                return;
            }
            if (!HaveQ3)
            {
                var target = Q.GetTarget(30);
                if (target != null)
                {
                    Common.Cast(Q, target, true);
                }
            }
            else
            {
                CastQ3();
            }
        }

        private static bool CanCastE(Obj_AI_Base target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        private static bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup);
        }

        private static bool CastQ3(Obj_AI_Hero target = null)
        {
            var q3 = Q2;
            q3.Collision = true;
            if (target != null)
            {
                var pred = Common.GetPrediction(q3, target, true, new[] { CollisionableObjects.YasuoWall });
                if (pred.Hitchance >= Q2.MinHitChance && Q.Cast(pred.CastPosition))
                {
                    return true;
                }
            }
            else
            {
                var hit = -1;
                var predPos = new Vector3();
                foreach (var hero in GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(Q2.Range)))
                {
                    var pred = Common.GetPrediction(q3, hero, true, new[] { CollisionableObjects.YasuoWall });
                    if (pred.Hitchance >= Q2.MinHitChance && pred.AoeTargetsHitCount > hit)
                    {
                        hit = pred.AoeTargetsHitCount;
                        predPos = pred.CastPosition;
                    }
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
            return target.Distance(Player.GetDashInfo().EndPos) < QCirWidth - target.BoundingRadius
                   && Q.Cast(target.Position);
        }

        private static void Flee()
        {
            if (MainMenu["Flee"]["Q"].GetValue<MenuBool>().Value && Q.IsReady() && !HaveQ3 && Player.IsDashing()
                && GetQCirObj.Count > 0 && CastQCir(GetQCirObj.MinOrDefault(i => i.Distance(Player))))
            {
                return;
            }
            var obj = GetNearObj();
            if (obj == null || !E.IsReady())
            {
                return;
            }
            E.CastOnUnit(obj);
        }

        private static double GetEDmg(Obj_AI_Base target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Magical,
                (50 + 20 * E.Level) * (1 + Math.Max(0, Player.GetBuffCount("YasuoDashScalar") * 0.25))
                + 0.6 * Player.FlatMagicDamageMod);
        }

        private static Obj_AI_Base GetNearObj(Obj_AI_Base target = null, bool inQCir = false)
        {
            var pos = target != null
                          ? Prediction.GetPrediction(target, E.Delay, 0, E.Speed).UnitPosition
                          : Game.CursorPos;
            var obj = new List<Obj_AI_Base>();
            obj.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E.Range) && CanCastE(i)));
            obj.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget(E.Range) && CanCastE(i)));
            obj.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(E.Range) && CanCastE(i)));
            return
                obj.Where(
                    i =>
                    pos.Distance(PosAfterE(i)) < (inQCir ? QCirWidth : Player.Distance(pos))
                    && Evader.IsSafePoint(PosAfterE(i).ToVector2()).IsSafe)
                    .MinOrDefault(i => pos.Distance(PosAfterE(i)));
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
            return Player.CalculateMixedDamage(
                target,
                20 * Q.Level + (dmgBonus - reduction) * k,
                Player.GetBuffCount("ItemStatikShankCharge") == 100
                    ? 100 * (Player.Crit >= 0.85f ? (Items.HasItem(3031) ? 2.25 : 1.8) : 1)
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
            if (!Q.IsReady() || Player.IsDashing())
            {
                return;
            }
            if (!HaveQ3)
            {
                var target = Q.GetTarget(30);
                if (target != null && Common.Cast(Q, target, true) == CastStates.SuccessfullyCasted)
                {
                    return;
                }
                if (MainMenu["Hybrid"]["QLastHit"].GetValue<MenuBool>().Value && Q.GetTarget(100) == null)
                {
                    var minion =
                        GameObjects.EnemyMinions.Where(
                            i =>
                            i.IsValidTarget(Q.Range - 20) && Q.GetHealthPrediction(i) > 0
                            && Q.GetHealthPrediction(i) <= GetQDmg(i)).MaxOrDefault(i => i.MaxHealth);
                    if (minion != null)
                    {
                        Common.Cast(Q, minion, true);
                    }
                }
            }
            else if (MainMenu["Hybrid"]["Q3"].GetValue<MenuBool>().Value)
            {
                CastQ3();
            }
        }

        private static void KillSteal()
        {
            if (MainMenu["KillSteal"]["Q"].GetValue<MenuBool>().Value && Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    var target = GetQCirTarget;
                    if (target != null && target.Health <= GetQDmg(target) && CastQCir(target))
                    {
                        return;
                    }
                }
                else
                {
                    var target = (!HaveQ3 ? Q : Q2).GetTarget(!HaveQ3 ? 30 : 0);
                    if (target != null && target.Health <= GetQDmg(target))
                    {
                        if (!HaveQ3)
                        {
                            if (Common.Cast(Q, target, true) == CastStates.SuccessfullyCasted)
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
            if (MainMenu["KillSteal"]["E"].GetValue<MenuBool>().Value && E.IsReady())
            {
                var target = E.GetTarget(0, false, GameObjects.EnemyHeroes.Where(i => !CanCastE(i)));
                if (target != null
                    && (target.Health <= GetEDmg(target)
                        || (MainMenu["KillSteal"]["Q"].GetValue<MenuBool>().Value && Q.IsReady(30)
                            && target.Health - GetEDmg(target) <= GetQDmg(target))) && E.CastOnUnit(target))
                {
                    return;
                }
            }
            if (MainMenu["KillSteal"]["R"].GetValue<MenuBool>().Value && R.IsReady() && GetRTarget.Count > 0)
            {
                var target =
                    GetRTarget.Where(
                        i =>
                        MainMenu["KillSteal"]["RCast" + i.ChampionName].GetValue<MenuBool>().Value
                        && i.Health <= GetRDmg(i)).MinOrDefault(TargetSelector.GetPriority);
                if (target != null)
                {
                    R.CastOnUnit(target);
                }
            }
        }

        private static void LaneClear()
        {
            if (MainMenu["LaneClear"]["E"].GetValue<MenuBool>().Value && E.IsReady())
            {
                var minion = new List<Obj_AI_Minion>();
                minion.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget(E.Range) && CanCastE(i)));
                minion.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(E.Range) && CanCastE(i)));
                minion =
                    minion.Where(
                        i => !UnderTower(PosAfterE(i)) || MainMenu["LaneClear"]["ETower"].GetValue<MenuBool>().Value)
                        .OrderByDescending(i => i.MaxHealth)
                        .ToList();
                if (minion.Count > 0)
                {
                    var obj =
                        minion.FirstOrDefault(
                            i => E.GetHealthPrediction(i) > 0 && E.GetHealthPrediction(i) <= GetEDmg(i));
                    if (obj == null && Q.IsReady(30)
                        && (!HaveQ3 || MainMenu["LaneClear"]["Q3"].GetValue<MenuBool>().Value))
                    {
                        var sub = new List<Obj_AI_Minion>();
                        foreach (var mob in minion)
                        {
                            if (((E.GetHealthPrediction(mob) > 0
                                  && E.GetHealthPrediction(mob) <= GetEDmg(mob) + GetQDmg(mob))
                                 || mob.Team == GameObjectTeam.Neutral) && mob.Distance(PosAfterE(mob)) < QCirWidth)
                            {
                                sub.Add(mob);
                            }
                            var nearMinion = new List<Obj_AI_Minion>();
                            nearMinion.AddRange(
                                GameObjects.EnemyMinions.Where(
                                    i => i.IsValidTarget() && i.Distance(PosAfterE(mob)) < QCirWidth));
                            nearMinion.AddRange(
                                GameObjects.Jungle.Where(
                                    i => i.IsValidTarget() && i.Distance(PosAfterE(mob)) < QCirWidth));
                            if (nearMinion.Count > 2
                                || nearMinion.Any(
                                    i => E.GetHealthPrediction(mob) > 0 && E.GetHealthPrediction(mob) <= GetQDmg(mob)))
                            {
                                sub.Add(mob);
                            }
                        }
                        if (sub.Count > 0)
                        {
                            obj = sub.FirstOrDefault();
                        }
                    }
                    if (obj != null && E.CastOnUnit(obj))
                    {
                        return;
                    }
                }
            }
            if (Q.IsReady() && (!HaveQ3 || MainMenu["LaneClear"]["Q3"].GetValue<MenuBool>().Value))
            {
                if (Player.IsDashing())
                {
                    var minion = GetQCirObj.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                    if (minion.Any(i => i.Health <= GetQDmg(i) || i.Team == GameObjectTeam.Neutral) || minion.Count > 2)
                    {
                        CastQCir(minion.MinOrDefault(i => i.Distance(Player)));
                    }
                }
                else
                {
                    var minion = new List<Obj_AI_Minion>();
                    minion.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - 20)));
                    minion.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - 20)));
                    minion = minion.OrderByDescending(i => i.MaxHealth).ToList();
                    if (minion.Count > 0)
                    {
                        if (!HaveQ3)
                        {
                            var obj =
                                minion.FirstOrDefault(
                                    i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i));
                            if (obj != null && Common.Cast(Q, obj, true) == CastStates.SuccessfullyCasted)
                            {
                                return;
                            }
                        }
                        var pos =
                            (!HaveQ3 ? Q : Q2).GetLineFarmLocation(
                                minion.Select(i => Common.GetPrediction(!HaveQ3 ? Q : Q2, i).UnitPosition.ToVector2())
                                    .ToList());
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
            if (MainMenu["LastHit"]["Q"].GetValue<MenuBool>().Value && Q.IsReady() && !Player.IsDashing()
                && (!HaveQ3 || MainMenu["LastHit"]["Q3"].GetValue<MenuBool>().Value))
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        i.IsValidTarget((!HaveQ3 ? Q : Q2).Range - 20) && (!HaveQ3 ? Q : Q2).GetHealthPrediction(i) > 0
                        && (!HaveQ3 ? Q : Q2).GetHealthPrediction(i) <= GetQDmg(i)).MaxOrDefault(i => i.MaxHealth);
                if (minion != null && Common.Cast(!HaveQ3 ? Q : Q2, minion, true) == CastStates.SuccessfullyCasted)
                {
                    return;
                }
            }
            if (MainMenu["LastHit"]["E"].GetValue<MenuBool>().Value && E.IsReady())
            {
                var minion =
                    GameObjects.EnemyMinions.Where(
                        i =>
                        i.IsValidTarget(E.Range) && CanCastE(i) && Evader.IsSafePoint(PosAfterE(i).ToVector2()).IsSafe
                        && (!UnderTower(PosAfterE(i)) || MainMenu["LastHit"]["ETower"].GetValue<MenuBool>().Value)
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
            if (MainMenu["Draw"]["Q"].GetValue<MenuBool>().Value && Q.Level > 0)
            {
                Drawing.DrawCircle(
                    Player.Position,
                    (!HaveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"].GetValue<MenuBool>().Value && E.Level > 0)
            {
                Drawing.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["R"].GetValue<MenuBool>().Value && R.Level > 0)
            {
                Drawing.DrawCircle(
                    Player.Position,
                    R.Range,
                    R.IsReady() && GetRTarget.Count > 0 ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["StackQ"].GetValue<MenuBool>().Value)
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                var text =
                    Drawing.GetTextExtent(
                        string.Format(
                            "Auto Stack Q: {0}",
                            (MainMenu["StackQ"].GetValue<MenuKeyBind>().Active ? "On" : "Off")));
                Drawing.DrawText(
                    pos.X - (float)text.Width / 2,
                    pos.Y + 20,
                    Orbwalker.ActiveMode == OrbwalkerMode.None && Q.IsReady() && !HaveQ3 ? Color.White : Color.Gray,
                    string.Format("Auto Stack Q: {0}", MainMenu["StackQ"].GetValue<MenuKeyBind>().Active ? "On" : "Off"));
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
                    LastHit();
                    break;
                case OrbwalkerMode.None:
                    StackQ();
                    break;
            }
            if (Orbwalker.ActiveMode != OrbwalkerMode.Orbwalk && Orbwalker.ActiveMode != OrbwalkerMode.Hybrid)
            {
                AutoQ();
            }
            if (MainMenu["Flee"]["E"].GetValue<MenuKeyBind>().Active)
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                Flee();
            }
        }

        private static void OnUpdateEvade(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
            {
                return;
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
            if (MainMenu["Orbwalk"]["R"].GetValue<MenuBool>().Value && R.IsReady() && GetRTarget.Count > 0)
            {
                var hero = (from enemy in GetRTarget
                            let nearEnemy =
                                GameObjects.EnemyHeroes.Where(i => i.Distance(enemy) < RWidth && CanCastR(i)).ToList()
                            where
                                (nearEnemy.Count > 1 && enemy.Health <= GetRDmg(enemy))
                                || nearEnemy.Any(
                                    i => i.HealthPercent < MainMenu["Orbwalk"]["RHpU"].GetValue<MenuSlider>().Value)
                                || nearEnemy.Count >= MainMenu["Orbwalk"]["RCountA"].GetValue<MenuSlider>().Value
                            orderby nearEnemy.Count descending
                            select enemy).ToList();
                if (hero.Count > 0)
                {
                    var target = !MainMenu["Orbwalk"]["RDelay"].GetValue<MenuBool>().Value
                                     ? hero.FirstOrDefault()
                                     : hero.OrderBy(TimeLeftR).FirstOrDefault(i => TimeLeftR(i) <= 220 + Game.Ping * 2);
                    if (target != null && R.CastOnUnit(target))
                    {
                        return;
                    }
                }
            }
            if (MainMenu["Orbwalk"]["EGap"].GetValue<MenuBool>().Value && E.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget();
                if (target != null)
                {
                    var nearObj = GetNearObj(target, true) ?? GetNearObj(target);
                    if (nearObj != null
                        && (!UnderTower(PosAfterE(nearObj)) || MainMenu["Orbwalk"]["ETower"].GetValue<MenuBool>().Value)
                        && (target.Compare(nearObj)
                                ? (target.Distance(PosAfterE(target)) < target.GetRealAutoAttackRange()
                                   || (HaveQ3 && target.HasBuffOfType(BuffType.SpellShield)))
                                : Player.Distance(target) > MainMenu["Orbwalk"]["ERange"].GetValue<MenuSlider>().Value)
                        && E.CastOnUnit(nearObj))
                    {
                        return;
                    }
                }
            }
            if (Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    var target = GetQCirTarget;
                    if (target != null && CastQCir(target))
                    {
                        return;
                    }
                    if (!HaveQ3 && MainMenu["Orbwalk"]["EGap"].GetValue<MenuBool>().Value
                        && MainMenu["Orbwalk"]["EStackQ"].GetValue<MenuBool>().Value && Q.GetTarget(70) == null)
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
                        var target = Q.GetTarget(30);
                        if (target != null && Common.Cast(Q, target, true) == CastStates.SuccessfullyCasted)
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
            var itemTarget = Q.GetTarget() ?? Q2.GetTarget();
            UseItem(itemTarget);
            if (itemTarget == null)
            {
                return;
            }
            if (Ignite.IsReady() && itemTarget.HealthPercent < 30 && Player.Distance(itemTarget) <= 600)
            {
                Player.Spellbook.CastSpell(Ignite, itemTarget);
            }
        }

        private static Vector3 PosAfterE(Obj_AI_Base target)
        {
            return Player.ServerPosition.Extend(
                target.ServerPosition,
                Player.Distance(target) < 410 ? E.Range : Player.Distance(target) + 65);
        }

        private static void StackQ()
        {
            if (!MainMenu["StackQ"].GetValue<MenuKeyBind>().Active || !Q.IsReady() || Player.IsDashing() || HaveQ3)
            {
                return;
            }
            var target = Q.GetTarget(30);
            if (target != null && Common.Cast(Q, target, true) == CastStates.SuccessfullyCasted)
            {
                return;
            }
            var minion = new List<Obj_AI_Minion>();
            minion.AddRange(GameObjects.EnemyMinions.Where(i => i.IsValidTarget(Q.Range - 20)));
            minion.AddRange(GameObjects.Jungle.Where(i => i.IsValidTarget(Q.Range - 20)));
            if (minion.Count == 0)
            {
                return;
            }
            var obj = minion.FirstOrDefault(i => Q.GetHealthPrediction(i) > 0 && Q.GetHealthPrediction(i) <= GetQDmg(i))
                      ?? minion.MinOrDefault(i => i.Distance(Player));
            if (obj != null)
            {
                Common.Cast(Q, obj, true);
            }
        }

        private static float TimeLeftR(Obj_AI_Hero target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockback || i.Type == BuffType.Knockup);
            return buff != null ? (buff.EndTime - Game.Time) * 1000 : -1;
        }

        private static void TryToEvade(List<Skillshot> hitBy, Vector2 to)
        {
            var dangerLevel =
                hitBy.Select(
                    i =>
                    MainMenu["Evade"][i.SpellData.ChampionName.ToLowerInvariant()][i.SpellData.SpellName]["DangerLevel"]
                        .GetValue<MenuSlider>().Value).Concat(new[] { 0 }).Max();
            foreach (var evadeSpell in
                EvadeSpellDatabase.Spells.Where(i => i.Enabled && i.DangerLevel <= dangerLevel && i.IsReady)
                    .OrderBy(i => i.DangerLevel))
            {
                if (evadeSpell.EvadeType == EvadeTypes.Dash && evadeSpell.CastType == CastTypes.Target)
                {
                    var targets =
                        Evader.GetEvadeTargets(evadeSpell)
                            .Where(
                                i =>
                                Evader.IsSafePoint(PosAfterE(i).ToVector2()).IsSafe
                                && (!UnderTower(PosAfterE(i))
                                    || MainMenu["Evade"]["Spells"][evadeSpell.Name]["ETower"].GetValue<MenuBool>().Value))
                            .ToList();
                    if (targets.Count > 0)
                    {
                        var closestTarget = targets.MinOrDefault(i => to.Distance(PosAfterE(i)));
                        Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget);
                        return;
                    }
                }
                if (evadeSpell.EvadeType == EvadeTypes.WindWall)
                {
                    var skillshots =
                        Evade.DetectedSkillshots.Where(
                            i =>
                            i.Enabled && i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)
                            && i.IsAboutToHit(
                                150 + evadeSpell.Delay
                                - MainMenu["Evade"]["Spells"][evadeSpell.Name]["WDelay"].GetValue<MenuSlider>().Value,
                                Player)).ToList();
                    if (skillshots.Count > 0)
                    {
                        var dangerousSkillshot =
                            skillshots.MaxOrDefault(
                                i =>
                                MainMenu["Evade"][i.SpellData.ChampionName.ToLowerInvariant()][i.SpellData.SpellName][
                                    "DangerLevel"].GetValue<MenuSlider>().Value);
                        Player.Spellbook.CastSpell(
                            evadeSpell.Slot,
                            Player.ServerPosition.Extend(dangerousSkillshot.Start, 100));
                    }
                }
            }
        }

        private static bool UnderTower(Vector3 pos)
        {
            return GameObjects.EnemyTurrets.Any(i => !i.IsDead && i.Distance(pos) <= 850 + Player.BoundingRadius);
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
            if (Youmuu.IsReady && Common.CountEnemy(Q.Range + E.Range) > 0)
            {
                Youmuu.Cast();
            }
            if (Tiamat.IsReady && Common.CountEnemy(Tiamat.Range) > 0)
            {
                Tiamat.Cast();
            }
            if (Hydra.IsReady && Common.CountEnemy(Hydra.Range) > 0)
            {
                Hydra.Cast();
            }
            if (!Common.CanUseSkill(OrbwalkerMode.Orbwalk))
            {
                return;
            }
            if (Titanic.IsReady && Common.CountEnemy(Titanic.Range) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion
    }
}
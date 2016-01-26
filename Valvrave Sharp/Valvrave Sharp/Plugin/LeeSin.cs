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

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    #endregion

    internal class LeeSin : Program
    {
        #region Constants

        private const int KickRange = 725;

        private const int KickWidth = 40;

        #endregion

        #region Static Fields

        private static readonly List<string> SpecialPet = new List<string>
                                                              { "jarvanivstandard", "teemomushroom", "illaoiminion" };

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.25f, 65, 1800, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 425);
            E2 = new Spell(SpellSlot.E, 570);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R).SetSkillshot(0.25f, 0, 1500, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.VeryHigh;

            WardManager.Init();
            Insec.Init();
            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.KeyBind("Star", "Star Combo", Keys.X);
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
                comboMenu.Bool("W", "Use W", false);
                comboMenu.Bool("E", "Use E");
                comboMenu.Separator("Q Settings");
                comboMenu.Bool("Q", "Use Q");
                comboMenu.Bool("Q2", "-> Also Q2");
                comboMenu.Bool("Q2Obj", "-> Q2 Even Miss", false);
                comboMenu.Bool("QCol", "Smite Collision");
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Bool("RKill", "If Kill Enemy Behind");
                comboMenu.Slider("RCountA", "Or Hit Enemy Behind >=", 1, 1, 4);
            }
            var lcMenu = MainMenu.Add(new Menu("LaneClear", "Lane Clear"));
            {
                lcMenu.Bool("Q", "Use Q");
                lcMenu.Bool("W", "Use W", false);
                lcMenu.Bool("E", "Use E");
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q1");
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
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        #endregion

        #region Properties

        private static bool CanCastInCombo
            =>
                (!MainMenu["Combo"]["Q"] || Variables.TickCount - Q.LastCastAttemptT > 200)
                && (!MainMenu["Combo"]["W"] || Variables.TickCount - W.LastCastAttemptT > 150)
                && (!MainMenu["Combo"]["E"] || Variables.TickCount - E.LastCastAttemptT > 200);

        private static bool CanCastInLaneClear
            =>
                (!MainMenu["LaneClear"]["Q"] || Variables.TickCount - Q.LastCastAttemptT > 400)
                && (!MainMenu["LaneClear"]["W"] || Variables.TickCount - W.LastCastAttemptT > 300)
                && (!MainMenu["LaneClear"]["E"] || Variables.TickCount - E.LastCastAttemptT > 400);

        private static Tuple<int, Obj_AI_Hero> GetMultiR
        {
            get
            {
                var bestHit = 0;
                Obj_AI_Hero bestTarget = null;
                var kickTargets =
                    GameObjects.EnemyHeroes.Where(
                        i =>
                        i.IsValidTarget(R.Range) && i.Health + i.PhysicalShield > R.GetDamage(i)
                        && !i.HasBuffOfType(BuffType.SpellShield) && !i.HasBuffOfType(BuffType.SpellImmunity)).ToList();
                if (kickTargets.Count == 0)
                {
                    return new Tuple<int, Obj_AI_Hero>(0, null);
                }
                foreach (var kickTarget in kickTargets)
                {
                    var realWidth = kickTarget.BoundingRadius + KickWidth;
                    R2.Range = realWidth + KickRange;
                    R2.Width = realWidth;
                    var posStart3D = kickTarget.ServerPosition;
                    var posStart = posStart3D.ToVector2();
                    var posEnd = posStart.Extend(Player.ServerPosition, -R2.Range);
                    var hitCount = 1;
                    var hitTargets =
                        GameObjects.EnemyHeroes.Where(
                            i => i.IsValidTarget(R2.Range + R2.Width, true, posStart3D) && !i.Compare(kickTarget))
                            .ToList();
                    foreach (var hitTarget in hitTargets)
                    {
                        R2.UpdateSourcePosition(posStart3D, posStart3D);
                        var pred = R2.VPrediction(hitTarget);
                        if (pred.Hitchance < HitChance.High)
                        {
                            continue;
                        }
                        if (pred.CastPosition.ToVector2().Distance(posStart, posEnd, true) > R2.Width)
                        {
                            continue;
                        }
                        if (MainMenu["Combo"]["RKill"]
                            && hitTarget.Health + hitTarget.PhysicalShield <= GetRColDmg(kickTarget, hitTarget)
                            && !Invulnerable.Check(
                                hitTarget,
                                R.DamageType,
                                false,
                                (float)GetRColDmg(kickTarget, hitTarget)))
                        {
                            return new Tuple<int, Obj_AI_Hero>(-1, kickTarget);
                        }
                        hitCount += 1;
                    }
                    if (bestHit == 0 || bestHit < hitCount)
                    {
                        bestHit = hitCount;
                        bestTarget = kickTarget;
                    }
                }
                return new Tuple<int, Obj_AI_Hero>(bestHit, bestTarget);
            }
        }

        private static Obj_AI_Base GetQ2Obj
        {
            get
            {
                return
                    GameObjects.AllGameObjects.Where(i => i.IsEnemy)
                        .Select(i => i as Obj_AI_Base)
                        .FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
            }
        }

        private static int GetSmiteDmg
            =>
                new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                    Player.Level - 1];

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - R.LastCastAttemptT < 2500;

        private static bool IsWOne => W.Instance.SData.Name.ToLower().Contains("one");

        private static int Passive => Player.GetBuffCount("BlindMonkFlurry");

        #endregion

        #region Methods

        private static bool CanE2(Obj_AI_Base target, bool isClear = false)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < (isClear ? 0.2 : 0.3) * (buff.EndTime - buff.StartTime);
        }

        private static bool CanQ2(Obj_AI_Base target, bool isClear = false)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < (isClear ? 0.2 : 0.3) * (buff.EndTime - buff.StartTime);
        }

        private static bool CanR(Obj_AI_Hero target)
        {
            var buff = target.GetBuff("blindmonkrkick");
            return buff != null && buff.EndTime - Game.Time < 0.3 * (buff.EndTime - buff.StartTime);
        }

        private static void CastE(List<Obj_AI_Minion> minions = null)
        {
            if (!E.IsReady() || (Passive == 2 && (IsEOne || Variables.TickCount - E.LastCastAttemptT <= 2500)))
            {
                return;
            }
            if (IsEOne)
            {
                var count = minions?.Count(i => i.IsValidTarget(E.Range))
                            ?? Variables.TargetSelector.GetTargets(E.Range, E.DamageType, false).Count;
                if (Player.Mana >= 70 ? count > 0 : count > 2)
                {
                    E.Cast();
                }
            }
            else
            {
                if (minions != null)
                {
                    var minion = minions.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                    if (minion.Any(i => CanE2(i, true)) || Passive == -1)
                    {
                        E.Cast();
                    }
                }
                else
                {
                    var target =
                        Variables.TargetSelector.GetTargets(E2.Range, E.DamageType, false).Where(HaveE).ToList();
                    if (target.Any(i => CanE2(i) || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50)
                        || target.Count > 2 || Passive == -1)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void CastQSmite(Obj_AI_Hero target)
        {
            var pred = Q.VPrediction(target);
            if (pred.Hitchance == HitChance.Collision)
            {
                if (MainMenu["Combo"]["QCol"] && Smite.IsReady() && !pred.CollisionObjects.Any(i => i.IsMe))
                {
                    var col = pred.CollisionObjects.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                    if (col.Count == 1 && col.Any(i => i.Health <= GetSmiteDmg && i.DistanceToPlayer() < SmiteRange)
                        && Player.Spellbook.CastSpell(Smite, col.First()))
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
            }
            else if (pred.Hitchance >= Q.MinHitChance)
            {
                Q.Cast(pred.CastPosition);
            }
        }

        private static void CastW()
        {
            if (!W.IsReady()
                || (Passive != -1 && Player.HealthPercent >= 10
                    && (IsWOne || Variables.TickCount - W.LastCastAttemptT <= 2700))
                || Variables.Orbwalker.GetTarget() == null)
            {
                return;
            }
            W.Cast();
        }

        private static void Combo()
        {
            if (CanCastInCombo)
            {
                if (MainMenu["Combo"]["Q"] && Q.IsReady())
                {
                    if (IsQOne)
                    {
                        var target = Q.GetTarget(Q.Width / 2);
                        if (target != null)
                        {
                            CastQSmite(target);
                        }
                    }
                    else if (MainMenu["Combo"]["Q2"])
                    {
                        var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
                        if (target != null)
                        {
                            if ((CanQ2(target) || (!R.IsReady() && IsRecentR && CanR(target))
                                 || target.Health + target.PhysicalShield
                                 <= Q.GetDamage(target, Damage.DamageStage.SecondCast)
                                 + Player.GetAutoAttackDamage(target)
                                 || target.DistanceToPlayer() > target.GetRealAutoAttackRange() + 100 || Passive == -1)
                                && Q.Cast())
                            {
                                return;
                            }
                        }
                        else if (GetQ2Obj != null && MainMenu["Combo"]["Q2Obj"])
                        {
                            var targetQ2 = Q2.GetTarget(200);
                            if (targetQ2 != null && GetQ2Obj.Distance(targetQ2) < targetQ2.DistanceToPlayer()
                                && !targetQ2.InAutoAttackRange() && Q.Cast())
                            {
                                return;
                            }
                        }
                    }
                }
                if (MainMenu["Combo"]["E"])
                {
                    CastE();
                }
                if (MainMenu["Combo"]["W"])
                {
                    CastW();
                }
            }
            var subTarget = W.GetTarget();
            if (MainMenu["Combo"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget != null && MainMenu["Combo"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 30
                && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Flee(Vector3 pos, bool isStar = false)
        {
            if (!W.IsReady() || !IsWOne || Variables.TickCount - W.LastCastAttemptT <= 1000)
            {
                return;
            }
            var objJump =
                GameObjects.AllyHeroes.Where(i => !i.IsMe)
                    .Cast<Obj_AI_Base>()
                    .Concat(
                        GameObjects.AllyMinions.Where(
                            i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower()))
                            .Concat(GameObjects.AllyWards.Where(i => i.IsWard())))
                    .Where(
                        i =>
                        i.IsValidTarget(W.Range, false)
                        && i.Distance(Player.ServerPosition.Extend(pos, Math.Min(pos.DistanceToPlayer(), W.Range)))
                        < (isStar ? R.Range - 50 : 250))
                    .MinOrDefault(i => i.Distance(pos));
            if (objJump != null)
            {
                W.CastOnUnit(objJump);
            }
            else
            {
                WardManager.Place(pos, false, true);
            }
        }

        private static double GetQ2Dmg(Obj_AI_Base target, double subHp)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod
                      + 0.08 * (target.MaxHealth - (target.Health - subHp));
            return Player.CalculateDamage(
                target,
                DamageType.Physical,
                target is Obj_AI_Minion ? Math.Min(dmg, 400) : dmg) + subHp;
        }

        private static double GetRColDmg(Obj_AI_Hero kickTarget, Obj_AI_Hero hitTarget)
        {
            return R.GetDamage(hitTarget)
                   + Player.CalculateDamage(
                       hitTarget,
                       DamageType.Physical,
                       new[] { 0.12, 0.15, 0.18 }[R.Level - 1] * kickTarget.BonusHealth);
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
                    var target = Q.GetTarget(Q.Width / 2);
                    if (target != null
                        && (target.Health + target.PhysicalShield <= Q.GetDamage(target)
                            || (target.Health + target.PhysicalShield
                                <= GetQ2Dmg(target, Q.GetDamage(target)) + Player.GetAutoAttackDamage(target)
                                && Player.Mana - Q.Instance.ManaCost >= 30)))
                    {
                        var pred = Q.VPrediction(
                            target,
                            false,
                            CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall);
                        if (pred.Hitchance >= Q.MinHitChance && Q.Cast(pred.CastPosition))
                        {
                            return;
                        }
                    }
                }
                else
                {
                    var target = Variables.TargetSelector.GetTargets(Q2.Range, Q2.DamageType).FirstOrDefault(HaveQ);
                    if (target != null
                        && target.Health + target.PhysicalShield
                        <= Q.GetDamage(target, Damage.DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                        && Q.Cast())
                    {
                        return;
                    }
                }
            }
            if (MainMenu["KillSteal"]["E"] && E.IsReady() && IsEOne
                && Variables.TargetSelector.GetTargets(E.Range, E.DamageType)
                       .Any(i => i.Health + i.MagicalShield <= E.GetDamage(i)) && E.Cast())
            {
                return;
            }
            if (MainMenu["KillSteal"]["R"] && R.IsReady())
            {
                var targetList =
                    Variables.TargetSelector.GetTargets(R.Range, R.DamageType)
                        .Where(i => MainMenu["KillSteal"]["RCast" + i.ChampionName])
                        .ToList();
                if (targetList.Count > 0)
                {
                    var targetR = targetList.FirstOrDefault(i => i.Health + i.PhysicalShield <= R.GetDamage(i));
                    if (targetR != null)
                    {
                        R.CastOnUnit(targetR);
                    }
                    else if (MainMenu["KillSteal"]["Q"] && Q.IsReady() && !IsQOne)
                    {
                        var targetQ2R =
                            targetList.FirstOrDefault(
                                i =>
                                HaveQ(i)
                                && i.Health + i.PhysicalShield
                                <= GetQ2Dmg(i, R.GetDamage(i)) + Player.GetAutoAttackDamage(i));
                        if (targetQ2R != null)
                        {
                            R.CastOnUnit(targetQ2R);
                        }
                    }
                }
            }
        }

        private static void LaneClear()
        {
            var minions =
                GameObjects.Jungle.Concat(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(false)))
                    .Where(i => i.IsValidTarget(Q.Range))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0 || !CanCastInLaneClear)
            {
                return;
            }
            if (MainMenu["LaneClear"]["E"])
            {
                CastE(minions);
            }
            if (MainMenu["LaneClear"]["Q"] && Q.IsReady())
            {
                if (IsQOne)
                {
                    var minionCount = minions.Count(i => i.InAutoAttackRange());
                    if ((minions.Any(i => i.InAutoAttackRange() && i.Team == GameObjectTeam.Neutral) || minionCount < 3
                         || minionCount > 4) && Passive < 2)
                    {
                        foreach (var minion in
                            minions.Where(i => !i.InAutoAttackRange() || i.Health > Q.GetDamage(i))
                                .OrderBy(i => i.DistanceToPlayer()))
                        {
                            var pred = Q.VPrediction(minion, false, CollisionableObjects.YasuoWall);
                            if (pred.Hitchance >= Q.MinHitChance)
                            {
                                Q.Cast(pred.CastPosition);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    var q2Minion = GetQ2Obj;
                    if (q2Minion != null
                        && (CanQ2(q2Minion, true)
                            || q2Minion.Health <= Q.GetDamage(q2Minion, Damage.DamageStage.SecondCast)
                            || q2Minion.DistanceToPlayer() > q2Minion.GetRealAutoAttackRange() + 100 || Passive == -1)
                        && Q.Cast())
                    {
                        return;
                    }
                }
            }
            if (MainMenu["LaneClear"]["W"])
            {
                CastW();
            }
        }

        private static void LastHit()
        {
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || !IsQOne)
            {
                return;
            }
            foreach (var pred in
                GameObjects.EnemyMinions.Where(
                    i =>
                    i.IsValidTarget(Q.Range) && (i.IsMinion() || i.IsPet(false))
                    && Q.GetHealthPrediction(i) <= Q.GetDamage(i)
                    && (!i.InAutoAttackRange() ? Q.GetHealthPrediction(i) > 0 : i.Health > Player.GetAutoAttackDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .Select(
                        i =>
                        Q.VPrediction(
                            i,
                            false,
                            CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall))
                    .Where(i => i.Hitchance >= Q.MinHitChance))
            {
                Q.Cast(pred.CastPosition);
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
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            if (!MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active && MainMenu["Combo"]["R"] && R.IsReady())
            {
                var multiR = GetMultiR;
                if (multiR.Item2 != null && (multiR.Item1 == -1 || multiR.Item1 >= MainMenu["Combo"]["RCountA"] + 1)
                    && R.CastOnUnit(multiR.Item2))
                {
                    return;
                }
            }
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
                case OrbwalkingMode.LaneClear:
                    LaneClear();
                    break;
                case OrbwalkingMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkingMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        Flee(Game.CursorPos);
                    }
                    else if (MainMenu["Combo"]["Star"].GetValue<MenuKeyBind>().Active)
                    {
                        Star();
                    }
                    else if (MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active)
                    {
                        Insec.Start(Insec.GetTarget);
                    }
                    break;
            }
        }

        private static void Star()
        {
            var target = Q.GetTarget(Q.Width / 2);
            if (!IsQOne)
            {
                target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
            }
            if (!Q.IsReady())
            {
                target = W.GetTarget();
            }
            Variables.Orbwalker.Orbwalk(target);
            if (target == null)
            {
                return;
            }
            if (Q.IsReady())
            {
                if (IsQOne)
                {
                    CastQSmite(target);
                }
                else if (HaveQ(target)
                         && (target.Health + target.PhysicalShield
                             <= Q.GetDamage(target, Damage.DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                             || (!R.IsReady() && IsRecentR && CanR(target))) && Q.Cast())
                {
                    return;
                }
            }
            if (E.CanCast(target) && IsEOne && (!HaveQ(target) || Player.Mana >= 70) && E.Cast())
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
            else if (target.DistanceToPlayer() < W.Range + R.Range - 100 && Player.Mana >= 70)
            {
                Flee(target.ServerPosition.Extend(Player.ServerPosition, R.Range / 2), true);
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
            if (Youmuu.IsReady && Player.CountEnemyHeroesInRange(W.Range + E.Range) > 0)
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

        private static class Insec
        {
            #region Static Fields

            private static bool canJumpFlash;

            private static Vector2 lastEndPos;

            private static int lastInsecTime, lastMoveTime, lastRFlashTime, lastFlashTime, lastJumpTime;

            #endregion

            #region Properties

            internal static Obj_AI_Hero GetTarget
            {
                get
                {
                    var target = Q.GetTarget(-100);
                    if ((MainMenu["Insec"]["Q"] && Q.IsReady()) || GetQ2Obj != null)
                    {
                        target = Q2.GetTarget(FlashRange);
                    }
                    return target;
                }
            }

            private static bool CanInsec
                =>
                    (WardManager.CanWardJump || (MainMenu["Insec"]["Flash"] && Flash.IsReady()) || IsRecent)
                    && R.IsReady();

            private static bool CanJumpFlash
                =>
                    MainMenu["Insec"]["Flash"] && MainMenu["Insec"]["FlashJump"] && WardManager.CanWardJump
                    && Flash.IsReady();

            private static bool CanRFlash => Flash.IsReady() && Variables.TickCount - lastRFlashTime < 5000 && IsRecentR
                ;

            private static bool IsRecent
                =>
                    Variables.TickCount - lastJumpTime < 5000
                    || (MainMenu["Insec"]["Flash"] && Variables.TickCount - lastFlashTime < 5000)
                    || Variables.TickCount - WardManager.LastPlaceTime < 5000;

            #endregion

            #region Methods

            internal static void Init()
            {
                var insecMenu = MainMenu.Add(new Menu("Insec", "Insec"));
                {
                    insecMenu.Slider("Dist", "Extra Distance Behind (%)", 20);
                    insecMenu.Bool("Line", "Draw Line");
                    insecMenu.List("Mode", "Mode", new[] { "Tower/Hero/Current", "Mouse Position", "Current Position" });
                    insecMenu.Separator("Flash Settings");
                    insecMenu.Bool("Flash", "Use Flash");
                    insecMenu.Bool("PriorFlash", "Priorize Flash Over WardJump", false);
                    insecMenu.List("FlashMode", "Flash Mode", new[] { "R-Flash", "Flash-R", "Both" });
                    insecMenu.Bool("FlashJump", "Use WardJump To Gap For Flash");
                    insecMenu.Separator("Q Settings");
                    insecMenu.Bool("Q", "Use Q");
                    insecMenu.Bool("QCol", "Smite Collision");
                    insecMenu.Bool("QObj", "Use Q On Near Object");
                    insecMenu.Separator("Keybinds");
                    insecMenu.KeyBind("Insec", "Insec", Keys.T);
                }

                Game.OnUpdate += args =>
                    {
                        if (Player.IsDead)
                        {
                            return;
                        }
                        if (lastInsecTime > 0 && Variables.TickCount - lastInsecTime > 10000)
                        {
                            lastEndPos = new Vector2();
                            lastInsecTime = 0;
                            Variables.TargetSelector.SetTarget(null);
                        }
                        if (lastMoveTime > 0 && Variables.TickCount - lastMoveTime > 1000 && !R.IsReady())
                        {
                            lastMoveTime = 0;
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || !MainMenu["Insec"]["Line"] || R.Level == 0 || !CanInsec)
                        {
                            return;
                        }
                        var target = GetTarget;
                        if (target == null)
                        {
                            return;
                        }
                        Drawing.DrawCircle(target.Position, target.BoundingRadius * 1.5f, Color.BlueViolet);
                        Drawing.DrawLine(
                            Drawing.WorldToScreen(target.Position),
                            Drawing.WorldToScreen(GetPositionKickTo(target).ToVector3()),
                            1,
                            Color.BlueViolet);
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe || !MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active)
                        {
                            return;
                        }
                        if (args.SData.Name == "summonerflash" && MainMenu["Insec"]["Flash"])
                        {
                            lastFlashTime = Variables.TickCount;
                        }
                        if (args.Slot == SpellSlot.W && args.SData.Name.ToLower().Contains("one"))
                        {
                            if (Variables.TickCount - lastJumpTime < 1250)
                            {
                                lastJumpTime = Variables.TickCount;
                            }
                            if (canJumpFlash)
                            {
                                canJumpFlash = false;
                            }
                        }
                        if (args.Slot == SpellSlot.R && CanRFlash)
                        {
                            var target = args.Target as Obj_AI_Hero;
                            if (target != null && target.Health + target.PhysicalShield > R.GetDamage(target))
                            {
                                Player.Spellbook.CastSpell(
                                    Flash,
                                    target.ServerPosition.ToVector2()
                                        .Extend(GetPositionKickTo(target), -GetDistBehind(target))
                                        .ToVector3());
                            }
                        }
                    };
            }

            internal static void Start(Obj_AI_Hero target)
            {
                if (CanRFlash)
                {
                    var lastSpell = Player.GetLastCastedSpell();
                    if (lastSpell.IsValid && lastSpell.Name == R.Instance.SData.Name
                        && Variables.TickCount - lastSpell.StartTime < 2000)
                    {
                        var targetRFlash = lastSpell.Target as Obj_AI_Hero;
                        if (targetRFlash != null && targetRFlash.IsValidTarget(FlashRange)
                            && targetRFlash.Health + targetRFlash.PhysicalShield > R.GetDamage(targetRFlash))
                        {
                            Player.Spellbook.CastSpell(
                                Flash,
                                targetRFlash.ServerPosition.ToVector2()
                                    .Extend(GetPositionKickTo(targetRFlash), -GetDistBehind(targetRFlash))
                                    .ToVector3());
                            return;
                        }
                    }
                }
                if (Variables.Orbwalker.CanMove() && Variables.TickCount - lastMoveTime > 250)
                {
                    if (target != null && lastMoveTime > 0 && CanInsec
                        && GetPositionKickTo(target).DistanceToPlayer() > target.Distance(GetPositionKickTo(target)))
                    {
                        Variables.Orbwalker.Move(
                            target.ServerPosition.Extend(GetPositionKickTo(target), -GetDistBehind(target)));
                    }
                    else
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                    }
                }
                if (target == null || !CanInsec)
                {
                    return;
                }
                if (!IsRecent)
                {
                    var checkFlash = GapCheck(target, true);
                    var checkJump = GapCheck(target);
                    if (!Player.HasBuff("blindmonkqtwodash") && !canJumpFlash && !checkFlash.Item2 && !checkJump.Item2
                        && CanJumpFlash
                        && target.DistanceToPlayer()
                        < WardManager.WardRange
                        + (MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index == 0 ? R.Range : FlashRange)
                        - GetDistBehind(target))
                    {
                        canJumpFlash = true;
                    }
                    if (!canJumpFlash)
                    {
                        if (MainMenu["Insec"]["PriorFlash"])
                        {
                            if (MainMenu["Insec"]["Flash"] && checkFlash.Item2)
                            {
                                GapByFlash(target, checkFlash.Item1);
                            }
                            else if (checkJump.Item2)
                            {
                                GapByWardJump(target, checkJump.Item1);
                            }
                        }
                        else
                        {
                            if (checkJump.Item2)
                            {
                                GapByWardJump(target, checkJump.Item1);
                            }
                            else if (MainMenu["Insec"]["Flash"] && checkFlash.Item2)
                            {
                                GapByFlash(target, checkFlash.Item1);
                            }
                        }
                    }
                    else
                    {
                        Flee(target.ServerPosition.Extend(GetPositionKickTo(target), -GetDistBehind(target)));
                    }
                }
                if (R.IsInRange(target))
                {
                    var posEnd = GetPositionKickTo(target);
                    if (posEnd.DistanceToPlayer() > target.Distance(posEnd))
                    {
                        var posTarget = target.ServerPosition.ToVector2();
                        var project = posTarget.Extend(Player.ServerPosition, -KickRange)
                            .ProjectOn(posTarget, posEnd.Extend(posTarget, -(KickRange * 0.5f)));
                        if (project.IsOnSegment && project.SegmentPoint.Distance(posEnd) <= KickRange * 0.5f
                            && R.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                }
                if (!CanJumpFlash || !canJumpFlash)
                {
                    GapByQ(target);
                }
            }

            private static void GapByFlash(Obj_AI_Hero target, Vector3 posGap)
            {
                switch (MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        GapByRFlash(target);
                        break;
                    case 1:
                        GapByFlashR(target, posGap);
                        break;
                    case 2:
                        if (!posGap.IsValid())
                        {
                            GapByRFlash(target);
                        }
                        else
                        {
                            GapByFlashR(target, posGap);
                        }
                        break;
                }
            }

            private static void GapByFlashR(Obj_AI_Hero target, Vector3 posGap)
            {
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                Player.Spellbook.CastSpell(Flash, posGap);
            }

            private static void GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Insec"]["Q"] || !Q.IsReady())
                {
                    return;
                }
                var minDist = WardManager.WardRange - GetDistBehind(target);
                if (IsQOne)
                {
                    var pred = Q.VPrediction(target);
                    if (pred.Hitchance == HitChance.Collision || pred.Hitchance == HitChance.OutOfRange)
                    {
                        if (pred.Hitchance == HitChance.Collision && MainMenu["Insec"]["QCol"] && Smite.IsReady()
                            && !pred.CollisionObjects.Any(i => i.IsMe))
                        {
                            var col =
                                pred.CollisionObjects.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                            if (col.Count == 1
                                && col.Any(i => i.Health <= GetSmiteDmg && i.DistanceToPlayer() < SmiteRange)
                                && Player.Spellbook.CastSpell(Smite, col.First()))
                            {
                                Q.Cast(pred.CastPosition);
                                return;
                            }
                        }
                        if (MainMenu["Insec"]["QObj"])
                        {
                            foreach (var predNear in
                                GameObjects.EnemyHeroes.Where(i => i.NetworkId != target.NetworkId)
                                    .Cast<Obj_AI_Base>()
                                    .Concat(
                                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet())
                                            .Concat(GameObjects.Jungle))
                                    .Where(
                                        i =>
                                        i.IsValidTarget(Q.Range) && Q.GetHealthPrediction(i) > Q.GetDamage(i)
                                        && target.DistanceToPlayer() > i.Distance(target)
                                        && i.Distance(target) < minDist - 80)
                                    .OrderBy(i => i.Distance(target))
                                    .Select(i => Q.VPrediction(i))
                                    .Where(i => i.Hitchance >= Q.MinHitChance)
                                    .OrderByDescending(i => i.Hitchance))
                            {
                                Q.Cast(predNear.CastPosition);
                            }
                        }
                    }
                    else if (pred.Hitchance >= Q.MinHitChance)
                    {
                        Q.Cast(pred.CastPosition);
                    }
                }
                else if (target.DistanceToPlayer() > minDist
                         && (HaveQ(target) || (GetQ2Obj != null && target.Distance(GetQ2Obj) < minDist - 80))
                         && ((WardManager.CanWardJump && Player.Mana >= 80)
                             || (MainMenu["Insec"]["Flash"] && Flash.IsReady())) && Q.Cast())
                {
                    Variables.TargetSelector.SetTarget(target);
                }
            }

            private static void GapByRFlash(Obj_AI_Hero target)
            {
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = Variables.TickCount;
                lastRFlashTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                R.CastOnUnit(target);
            }

            private static void GapByWardJump(Obj_AI_Hero target, Vector3 posGap)
            {
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                    Variables.Orbwalker.Move(
                        posGap.Extend(GetPositionKickTo(target), -(GetDistBehind(target) + Player.BoundingRadius / 2)));
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = Variables.TickCount;
                lastJumpTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                WardManager.Place(posGap, true);
            }

            private static Tuple<Vector3, bool> GapCheck(Obj_AI_Hero target, bool useFlash = false)
            {
                if (useFlash && Flash.IsReady() && R.IsInRange(target)
                    && MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index != 1)
                {
                    return new Tuple<Vector3, bool>(new Vector3(), true);
                }
                var posEnd = GetPositionKickTo(target);
                var posBehind = target.ServerPosition.ToVector2().Extend(posEnd, -GetDistBehind(target)).ToVector3();
                var isReady = target.Distance(posBehind) <= R.Range
                              && target.Distance(posBehind) < posEnd.Distance(posBehind);
                if (!useFlash)
                {
                    return !WardManager.CanWardJump || posBehind.DistanceToPlayer() > WardManager.WardRange || !isReady
                               ? new Tuple<Vector3, bool>(new Vector3(), false)
                               : new Tuple<Vector3, bool>(posBehind, true);
                }
                var posFlash = Player.ServerPosition.ToVector2().Extend(posBehind, FlashRange);
                return MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index == 0 || !Flash.IsReady()
                       || posBehind.DistanceToPlayer() > FlashRange || target.Distance(posFlash) <= 50
                       || target.Distance(posFlash) >= posEnd.Distance(posFlash) || !isReady
                           ? new Tuple<Vector3, bool>(new Vector3(), false)
                           : new Tuple<Vector3, bool>(posBehind, true);
            }

            private static float GetDistBehind(Obj_AI_Hero target)
            {
                return
                    Math.Min(
                        (Player.BoundingRadius + target.BoundingRadius + 60) * (100 + MainMenu["Insec"]["Dist"]) / 100,
                        R.Range);
            }

            private static Vector2 GetPositionAfterKick(Obj_AI_Hero target)
            {
                return target.ServerPosition.ToVector2().Extend(GetPositionKickTo(target), KickRange);
            }

            private static Vector2 GetPositionKickTo(Obj_AI_Hero target)
            {
                if (lastEndPos.IsValid() && target.Distance(lastEndPos) <= KickRange + 700)
                {
                    return lastEndPos;
                }
                var pos = Player.ServerPosition;
                switch (MainMenu["Insec"]["Mode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        var turret =
                            GameObjects.AllyTurrets.Where(
                                i =>
                                !i.IsDead && target.Distance(i) <= KickRange + 500
                                && i.Distance(target) - KickRange <= 950 && i.Distance(target) > 250)
                                .MinOrDefault(i => i.DistanceToPlayer());
                        if (turret != null)
                        {
                            pos = turret.ServerPosition;
                        }
                        else
                        {
                            var hero =
                                GameObjects.AllyHeroes.Where(
                                    i =>
                                    i.IsValidTarget(KickRange + 700, false, target.ServerPosition) && !i.IsMe
                                    && i.HealthPercent > 10 && i.Distance(target) > 250)
                                    .MaxOrDefault(i => new Priority().GetDefaultPriority(i));
                            if (hero != null)
                            {
                                pos = hero.ServerPosition;
                            }
                        }
                        break;
                    case 1:
                        pos = Game.CursorPos;
                        break;
                }
                return pos.ToVector2();
            }

            #endregion
        }

        private static class WardManager
        {
            #region Constants

            internal const int WardRange = 600;

            #endregion

            #region Static Fields

            internal static int LastPlaceTime;

            private static Vector2 lastJumpPos;

            private static int lastJumpTime;

            #endregion

            #region Properties

            internal static bool CanWardJump => CanCastWard && W.IsReady() && IsWOne;

            private static bool CanCastWard => Variables.TickCount - lastJumpTime > 1250 && Common.GetWardSlot() != null
                ;

            private static bool IsTryingToJump => lastJumpPos.IsValid() && Variables.TickCount - lastJumpTime < 1250;

            #endregion

            #region Methods

            internal static void Init()
            {
                Game.OnUpdate += args =>
                    {
                        if (IsTryingToJump)
                        {
                            Jump(lastJumpPos);
                        }
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe)
                        {
                            return;
                        }
                        if (args.Slot == SpellSlot.W && args.SData.Name.ToLower().Contains("one")
                            && lastJumpPos.IsValid())
                        {
                            lastJumpPos = new Vector2();
                        }
                    };
                GameObject.OnCreate += (sender, args) =>
                    {
                        var ward = sender as Obj_AI_Minion;
                        if (ward == null || ward.IsEnemy || !ward.IsWard())
                        {
                            return;
                        }
                        if (Variables.TickCount - LastPlaceTime < 1250)
                        {
                            LastPlaceTime = Variables.TickCount;
                        }
                        if (IsTryingToJump && W.IsReady() && IsWOne && ward.Distance(lastJumpPos) < 80)
                        {
                            W.CastOnUnit(ward);
                        }
                    };
            }

            internal static void Place(Vector3 pos, bool isInsecByWard = false, bool isFlee = false)
            {
                if (!CanWardJump)
                {
                    return;
                }
                var ward = Common.GetWardSlot();
                if (ward == null)
                {
                    return;
                }
                var posEnd = Player.ServerPosition.Extend(pos, Math.Min(pos.DistanceToPlayer(), WardRange));
                Player.Spellbook.CastSpell(ward.SpellSlot, posEnd);
                if (isInsecByWard)
                {
                    LastPlaceTime = Variables.TickCount;
                }
                lastJumpPos = posEnd.ToVector2();
                lastJumpTime = Variables.TickCount;
                if (isFlee)
                {
                    lastJumpTime += 1100;
                }
            }

            private static void Jump(Vector2 pos)
            {
                if (!W.IsReady() || !IsWOne)
                {
                    return;
                }
                var wardObj =
                    GameObjects.AllyWards.Where(i => i.IsValidTarget(W.Range, false) && i.IsWard())
                        .MinOrDefault(i => i.Distance(pos));
                if (wardObj != null && wardObj.Distance(pos) < 250)
                {
                    W.CastOnUnit(wardObj);
                }
            }

            #endregion
        }
    }
}
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

        private static int cPassive;

        private static bool isDashing;

        private static Obj_AI_Base objQ;

        private static Obj_AI_Hero objR;

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.275f, 60, 1900, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 425);
            E2 = new Spell(SpellSlot.E, 570);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R).SetSkillshot(0.25f, 0, 1500, false, Q.Type);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.VeryHigh;

            WardManager.Init();
            Insec.Init();
            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
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
                comboMenu.Separator("Star Combo Settings");
                comboMenu.KeyBind("Star", "Star Combo", Keys.X);
                comboMenu.Bool("StarKill", "Auto Star Combo If Killable", false);
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
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.Slot != SpellSlot.Q || args.SData.Name.ToLower().Contains("one"))
                    {
                        return;
                    }
                    isDashing = true;
                };
            Obj_AI_Base.OnBuffUpdateCount += (sender, args) =>
                {
                    if (!sender.IsMe || args.Buff.DisplayName != "BlindMonkFlurry")
                    {
                        return;
                    }
                    cPassive = args.Buff.Count;
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    /*if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy && sender.DistanceToPlayer() < 500)
                    {
                        Game.PrintChat(
                            "A => {0}: {1} ({2}) - {3}",
                            sender.CharData.BaseSkinName,
                            args.Buff.DisplayName,
                            args.Buff.Name,
                            args.Buff.Caster.IsMe);
                    }*/
                    if (sender.IsMe)
                    {
                        if (args.Buff.DisplayName == "BlindMonkFlurry")
                        {
                            cPassive = 2;
                        }
                        else if (args.Buff.DisplayName == "blindmonkqtwodash")
                        {
                            isDashing = true;
                        }
                    }
                    else if (sender.IsEnemy && args.Buff.Caster.IsMe)
                    {
                        if (args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            objQ = sender;
                        }
                        else if (args.Buff.Name == "blindmonkrroot")
                        {
                            objR = sender as Obj_AI_Hero;
                        }
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        if (args.Buff.DisplayName == "BlindMonkFlurry")
                        {
                            cPassive = 0;
                        }
                        else if (args.Buff.DisplayName == "blindmonkqtwodash")
                        {
                            isDashing = false;
                        }
                    }
                    else if (sender.IsEnemy && args.Buff.Caster.IsMe)
                    {
                        if (args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            objQ = null;
                        }
                        else if (args.Buff.Name == "blindmonkrroot")
                        {
                            objR = null;
                        }
                    }
                };
        }

        #endregion

        #region Properties

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
                            i => i.IsValidTarget(R2.Range + R2.Width / 2, true, posStart3D) && !i.Compare(kickTarget))
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
                        if (MainMenu["Combo"]["RKill"])
                        {
                            var dmgR = GetRColDmg(kickTarget, hitTarget);
                            if (hitTarget.Health + hitTarget.PhysicalShield <= dmgR
                                && !Invulnerable.Check(hitTarget, R.DamageType, false, dmgR))
                            {
                                return new Tuple<int, Obj_AI_Hero>(-1, kickTarget);
                            }
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

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - R.LastCastAttemptT < 2500;

        private static bool IsWOne => W.Instance.SData.Name.ToLower().Contains("one");

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
            var buff = target.GetBuff("BlindMonkDragonsRage");
            return buff != null && buff.EndTime - Game.Time < 0.3 * (buff.EndTime - buff.StartTime);
        }

        private static void CastE(List<Obj_AI_Minion> minions = null)
        {
            if (!E.IsReady() || (cPassive == 2 && (IsEOne || Variables.TickCount - E.LastCastAttemptT <= 2500)))
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
                    if (minion.Any(i => CanE2(i, true)) || cPassive == 0)
                    {
                        E.Cast();
                    }
                }
                else
                {
                    var target =
                        Variables.TargetSelector.GetTargets(E2.Range, E.DamageType, false).Where(HaveE).ToList();
                    if (target.Any(i => CanE2(i) || !i.InAutoAttackRange()) || target.Count > 2 || cPassive == 0)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void CastQSmite(Obj_AI_Hero target)
        {
            var pred = Q.VPrediction(target, false, CollisionableObjects.YasuoWall);
            if (pred.Hitchance < Q.MinHitChance)
            {
                return;
            }
            var col = pred.VCollision();
            if (col.Count == 0)
            {
                Q.Cast(pred.CastPosition);
            }
            else if (Smite.IsReady() && col.Count == 1 && MainMenu["Combo"]["QCol"])
            {
                var obj = col.First();
                if (obj.Health <= Common.GetSmiteDmg && obj.DistanceToPlayer() < SmiteRange
                    && Player.Spellbook.CastSpell(Smite, obj))
                {
                    Q.Cast(pred.CastPosition);
                }
            }
        }

        private static void CastW()
        {
            if (!W.IsReady() || Variables.TickCount - W.LastCastAttemptT <= 300
                || (cPassive > 0 && Player.HealthPercent >= 10
                    && (IsWOne || Variables.TickCount - W.LastCastAttemptT <= 2700))
                || Variables.Orbwalker.GetTarget() == null)
            {
                return;
            }
            W.Cast();
        }

        private static void Combo()
        {
            if (MainMenu["Combo"]["StarKill"] && R.IsReady() && MainMenu["Combo"]["Q"] && Q.IsReady() && !IsQOne
                && MainMenu["Combo"]["Q2"])
            {
                var target = Variables.TargetSelector.GetTargets(Q2.Range, Q2.DamageType).FirstOrDefault(HaveQ);
                if (target != null
                    && target.Health + target.PhysicalShield
                    <= GetQ2Dmg(target, R.GetDamage(target)) + Player.GetAutoAttackDamage(target))
                {
                    if (R.IsInRange(target))
                    {
                        R.CastOnUnit(target);
                    }
                    else if (target.DistanceToPlayer() < W.Range + R.Range - 100 && Player.Mana >= 70)
                    {
                        Flee(target.ServerPosition.Extend(Player.ServerPosition, R.Range / 2), true);
                    }
                }
            }
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
                else if (MainMenu["Combo"]["Q2"] && !Player.IsDashing())
                {
                    var target = Variables.TargetSelector.GetTargets(Q2.Range, Q2.DamageType).FirstOrDefault(HaveQ);
                    if (target != null)
                    {
                        if ((CanQ2(target) || (!R.IsReady() && IsRecentR && CanR(target))
                             || target.Health + target.PhysicalShield
                             <= Q.GetDamage(target, Damage.DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                             || target.DistanceToPlayer() > target.GetRealAutoAttackRange() + 100 || cPassive == 0)
                            && Q.Cast())
                        {
                            return;
                        }
                    }
                    else if (objQ.IsValidTarget(Q2.Range) && MainMenu["Combo"]["Q2Obj"])
                    {
                        var targetQ2 = Q2.GetTarget(200);
                        if (targetQ2 != null && objQ.Distance(targetQ2) < targetQ2.DistanceToPlayer()
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
            var objJump = GameObjects.AllyHeroes.Where(i => !i.IsMe)
                .Cast<Obj_AI_Base>()
                .Concat(
                    GameObjects.AllyMinions.Where(
                        i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower()))
                        .Concat(GameObjects.AllyWards.Where(i => i.IsWard())))
                .Where(
                    i =>
                        {
                            var posP = Player.ServerPosition.ToVector2();
                            return i.IsValidTarget(W.Range, false)
                                   && i.Distance(posP.Extend(pos, Math.Min(pos.Distance(posP), W.Range)))
                                   < (isStar ? R.Range - 50 : 250);
                        }).MinOrDefault(i => i.Distance(pos));
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

        private static float GetRColDmg(Obj_AI_Hero kickTarget, Obj_AI_Hero hitTarget)
        {
            return R.GetDamage(hitTarget)
                   + (float)
                     Player.CalculateDamage(
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
                else if (!Player.IsDashing())
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
            if (minions.Count == 0)
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
                         || minionCount > 4) && cPassive < 2)
                    {
                        foreach (var minion in
                            minions.Where(i => !i.InAutoAttackRange() || i.Health > Q.GetDamage(i))
                                .OrderBy(i => i.DistanceToPlayer()))
                        {
                            var pred = Q.VPrediction(minion, false, CollisionableObjects.YasuoWall);
                            if (pred.Hitchance >= Q.MinHitChance && Q.Cast(pred.CastPosition))
                            {
                                break;
                            }
                        }
                    }
                }
                else if (!Player.IsDashing())
                {
                    var q2Minion = objQ;
                    if (q2Minion.IsValidTarget(Q2.Range)
                        && (CanQ2(q2Minion, true)
                            || q2Minion.Health <= Q.GetDamage(q2Minion, Damage.DamageStage.SecondCast)
                            || q2Minion.DistanceToPlayer() > q2Minion.GetRealAutoAttackRange() + 100 || cPassive == 0)
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
            var minions =
                GameObjects.EnemyMinions.Where(
                    i =>
                    (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.GetHealthPrediction(i) > 0
                    && Q.GetHealthPrediction(i) <= Q.GetDamage(i)
                    && (i.IsUnderAllyTurret() || (i.IsUnderEnemyTurret() && !Player.IsUnderEnemyTurret())
                        || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50
                        || i.Health > Player.GetAutoAttackDamage(i))).OrderByDescending(i => i.MaxHealth).ToList();
            if (minions.Count == 0)
            {
                return;
            }
            foreach (var pred in
                minions.Select(
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
            Variables.Orbwalker.SetAttackState(!MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active);
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
                target = Variables.TargetSelector.GetTargets(Q2.Range, Q2.DamageType).FirstOrDefault(HaveQ);
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
                else if (!Player.IsDashing() && HaveQ(target)
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

            private static Vector2 lastEndPos, lastGapPos;

            private static int lastInsecTime, lastMoveTime, lastRFlashTime, lastFlashTime, lastJumpTime;

            private static Obj_AI_Base lastObjQ;

            #endregion

            #region Properties

            internal static Obj_AI_Hero GetTarget
            {
                get
                {
                    var target = Q.GetTarget(-100);
                    if ((MainMenu["Insec"]["Q"] && Q.IsReady()) || objQ.IsValidTarget(Q2.Range))
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
                    insecMenu.Bool("Draw", "Draw");
                    insecMenu.List("Mode", "Mode", new[] { "Tower/Hero/Current", "Mouse Position", "Current Position" });
                    insecMenu.Separator("Flash Settings");
                    insecMenu.Bool("Flash", "Use Flash");
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
                            lastEndPos = lastGapPos = new Vector2();
                            lastInsecTime = 0;
                            canJumpFlash = false;
                            Variables.TargetSelector.SetTarget(null);
                        }
                        if (lastMoveTime > 0 && Variables.TickCount - lastMoveTime > 1000 && !R.IsReady())
                        {
                            lastMoveTime = 0;
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || !MainMenu["Insec"]["Draw"] || R.Level == 0 || !CanInsec)
                        {
                            return;
                        }
                        var target = GetTarget;
                        if (target == null)
                        {
                            return;
                        }
                        Drawing.DrawCircle(target.Position, target.BoundingRadius, Color.BlueViolet);
                        Drawing.DrawCircle(GetPositionBehind(target), target.BoundingRadius, Color.BlueViolet);
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
                        if (args.SData.Name == "summonerflash" && MainMenu["Insec"]["Flash"]
                            && Variables.TickCount - lastFlashTime < 1250 && lastGapPos.IsValid()
                            && args.End.Distance(lastGapPos) <= 100)
                        {
                            lastFlashTime = Variables.TickCount;
                            DelayAction.Add(
                                Game.Ping / 2 + 30,
                                () =>
                                    {
                                        var target = Variables.TargetSelector.GetSelectedTarget();
                                        if (target.IsValidTarget())
                                        {
                                            R.CastOnUnit(target);
                                        }
                                    });
                            return;
                        }
                        if (args.Slot == SpellSlot.W && args.SData.Name.ToLower().Contains("one") && args.Target != null)
                        {
                            var ward = args.Target as Obj_AI_Minion;
                            if (ward == null || !ward.IsWard())
                            {
                                return;
                            }
                            if (Variables.TickCount - lastJumpTime < 1250 && lastGapPos.IsValid()
                                && ward.Distance(lastGapPos) <= 100)
                            {
                                lastJumpTime = Variables.TickCount;
                            }
                            if (canJumpFlash)
                            {
                                canJumpFlash = false;
                            }
                        }
                    };
                Obj_AI_Base.OnDoCast += (sender, args) =>
                    {
                        if (!sender.IsMe || args.Slot != SpellSlot.R)
                        {
                            return;
                        }
                        lastEndPos = lastGapPos = new Vector2();
                        lastInsecTime = 0;
                        canJumpFlash = false;
                        Variables.TargetSelector.SetTarget(null);
                    };
                Obj_AI_Base.OnBuffAdd += (sender, args) =>
                    {
                        if (sender.IsEnemy && args.Buff.Caster.IsMe && args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            lastObjQ = sender;
                        }
                    };
            }

            internal static void Start(Obj_AI_Hero target)
            {
                if (target != null && CanRFlash(target) && objR.Health + objR.PhysicalShield > R.GetDamage(objR)
                    && Player.Spellbook.CastSpell(Flash, GetPositionBehind(objR)))
                {
                    return;
                }
                if (Variables.Orbwalker.CanMove() && Variables.TickCount - lastMoveTime > 1000)
                {
                    if (target != null && lastMoveTime > 0 && CanInsec)
                    {
                        var posEnd = GetPositionKickTo(target);
                        Variables.Orbwalker.Move(
                            posEnd.DistanceToPlayer() > target.Distance(posEnd)
                                ? GetPositionBehind(target)
                                : Game.CursorPos);
                    }
                    else
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                    }
                }
                if (target == null)
                {
                    return;
                }
                if (MainMenu["Insec"]["Q"] && Q.IsReady() && !IsQOne && HaveQ(target) && !R.IsReady() && IsRecentR
                    && (CanR(target) || CanQ2(target, true)) && Q.Cast())
                {
                    return;
                }
                if (!CanInsec)
                {
                    return;
                }
                if (!IsRecent)
                {
                    var checkFlash = GapCheck(target, true);
                    var checkJump = GapCheck(target);
                    if (!canJumpFlash && !checkFlash.Item2 && !checkJump.Item2 && CanJumpFlash
                        && (!isDashing
                            || (!lastObjQ.Compare(target)
                                && lastObjQ.Distance(target) > WardManager.WardRange - GetDistBehind(target)))
                        && target.DistanceToPlayer() < WardManager.WardRange + R.Range - 80)
                    {
                        canJumpFlash = true;
                    }
                    if (!canJumpFlash)
                    {
                        if (checkJump.Item2)
                        {
                            GapByWardJump(target, checkJump.Item1);
                            return;
                        }
                        if (checkFlash.Item2)
                        {
                            GapByFlash(target, checkFlash.Item1);
                            return;
                        }
                    }
                    else if (WardManager.Place(target.ServerPosition, false, true))
                    {
                        return;
                    }
                }
                if ((!CanJumpFlash || !canJumpFlash) && GapByQ(target))
                {
                    return;
                }
                if (R.IsInRange(target))
                {
                    var posEnd = GetPositionKickTo(target);
                    if (posEnd.DistanceToPlayer() > target.Distance(posEnd))
                    {
                        var posTarget = target.ServerPosition.ToVector2();
                        var project = posTarget.Extend(Player.ServerPosition, -KickRange)
                            .ProjectOn(posTarget, posEnd.Extend(posTarget, -(KickRange * 0.5f)));
                        if (project.IsOnSegment && project.SegmentPoint.Distance(posEnd) <= KickRange * 0.5f)
                        {
                            R.CastOnUnit(target);
                        }
                    }
                }
            }

            private static bool CanRFlash(Obj_AI_Hero target)
            {
                return Flash.IsReady() && Variables.TickCount - lastRFlashTime < 5000 && objR.IsValidTarget()
                       && objR.Compare(target);
            }

            private static void GapByFlash(Obj_AI_Hero target, Vector2 posBehind)
            {
                switch (MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        GapByRFlash(target);
                        break;
                    case 1:
                        GapByFlashR(target, posBehind);
                        break;
                    case 2:
                        if (!posBehind.IsValid())
                        {
                            GapByRFlash(target);
                        }
                        else
                        {
                            GapByFlashR(target, posBehind);
                        }
                        break;
                }
            }

            private static void GapByFlashR(Obj_AI_Hero target, Vector2 posBehind)
            {
                if (!Player.Spellbook.CastSpell(Flash, posBehind.ToVector3()))
                {
                    return;
                }
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                }
                lastGapPos = posBehind;
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastFlashTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
            }

            private static bool GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Insec"]["Q"] || !Q.IsReady())
                {
                    return false;
                }
                var minDist = CanJumpFlash
                                  ? WardManager.WardRange + R.Range
                                  : WardManager.WardRange - GetDistBehind(target);
                if (IsQOne)
                {
                    var pred = Q.VPrediction(target, false, CollisionableObjects.YasuoWall);
                    if (pred.Hitchance >= Q.MinHitChance)
                    {
                        var col = pred.VCollision();
                        if (col.Count == 0 && Q.Cast(pred.CastPosition))
                        {
                            return true;
                        }
                        if (Smite.IsReady() && col.Count == 1 && MainMenu["Insec"]["QCol"])
                        {
                            var obj = col.First();
                            if (obj.Health <= Common.GetSmiteDmg && obj.DistanceToPlayer() < SmiteRange
                                && Player.Spellbook.CastSpell(Smite, obj))
                            {
                                Q.Cast(pred.CastPosition);
                                return true;
                            }
                        }
                    }
                    if (!MainMenu["Insec"]["QObj"])
                    {
                        return false;
                    }
                    if (
                        GameObjects.EnemyHeroes.Where(i => !i.Compare(target))
                            .Cast<Obj_AI_Base>()
                            .Concat(
                                GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet())
                                    .Concat(GameObjects.Jungle))
                            .Where(
                                i =>
                                i.IsValidTarget(Q.Range) && Q.GetHealthPrediction(i) > Q.GetDamage(i)
                                && target.DistanceToPlayer() > i.Distance(target) && i.Distance(target) < minDist - 80)
                            .OrderBy(i => i.Distance(target))
                            .Select(i => Q.VPrediction(i))
                            .Where(i => i.Hitchance >= Q.MinHitChance)
                            .OrderByDescending(i => i.Hitchance)
                            .Any(i => Q.Cast(i.CastPosition)))
                    {
                        return true;
                    }
                }
                else if (!Player.IsDashing() && target.DistanceToPlayer() > minDist
                         && (HaveQ(target) || (objQ.IsValidTarget(Q2.Range) && target.Distance(objQ) < minDist - 80))
                         && ((WardManager.CanWardJump && Player.Mana >= 80)
                             || (MainMenu["Insec"]["Flash"] && Flash.IsReady())) && Q.Cast())
                {
                    Variables.TargetSelector.SetTarget(target);
                    return true;
                }
                return false;
            }

            private static void GapByRFlash(Obj_AI_Hero target)
            {
                if (!R.CastOnUnit(target))
                {
                    return;
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastRFlashTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
            }

            private static void GapByWardJump(Obj_AI_Hero target, Vector2 posBehind)
            {
                if (!WardManager.Place(posBehind, true))
                {
                    return;
                }
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                }
                lastGapPos = posBehind;
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastJumpTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
            }

            private static Tuple<Vector2, bool> GapCheck(Obj_AI_Hero target, bool useFlash = false)
            {
                if (useFlash && (!MainMenu["Insec"]["Flash"] || !Flash.IsReady()))
                {
                    return new Tuple<Vector2, bool>(new Vector2(), false);
                }
                var flashMode = MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index;
                if (useFlash && flashMode != 1 && R.IsInRange(target))
                {
                    return new Tuple<Vector2, bool>(new Vector2(), true);
                }
                var posEnd = GetPositionKickTo(target);
                var posBehind = target.ServerPosition.ToVector2().Extend(posEnd, -GetDistBehind(target));
                if (target.Distance(posBehind) >= R.Range || target.Distance(posBehind) >= posEnd.Distance(posBehind))
                {
                    return new Tuple<Vector2, bool>(new Vector2(), false);
                }
                if (!useFlash)
                {
                    if (WardManager.CanWardJump && posBehind.DistanceToPlayer() < WardManager.WardRange)
                    {
                        return new Tuple<Vector2, bool>(posBehind, true);
                    }
                }
                else if (flashMode != 0 && posBehind.DistanceToPlayer() < FlashRange)
                {
                    var posFlash = Player.ServerPosition.ToVector2().Extend(posBehind, FlashRange);
                    var distFlashT = target.Distance(posFlash);
                    if (distFlashT > 50 && distFlashT < posEnd.Distance(posFlash))
                    {
                        return new Tuple<Vector2, bool>(posBehind, true);
                    }
                }
                return new Tuple<Vector2, bool>(new Vector2(), false);
            }

            private static float GetDistBehind(Obj_AI_Hero target)
            {
                return
                    Math.Min(
                        (Player.BoundingRadius + target.BoundingRadius + 40) * (100 + MainMenu["Insec"]["Dist"]) / 100,
                        R.Range);
            }

            private static Vector2 GetPositionAfterKick(Obj_AI_Hero target)
            {
                return target.ServerPosition.ToVector2().Extend(GetPositionKickTo(target), KickRange);
            }

            private static Vector3 GetPositionBehind(Obj_AI_Hero target)
            {
                return
                    target.ServerPosition.ToVector2()
                        .Extend(GetPositionKickTo(target), -GetDistBehind(target))
                        .ToVector3();
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
                        if (!sender.IsMe || args.Slot != SpellSlot.W || !args.SData.Name.ToLower().Contains("one")
                            || args.Target == null || !lastJumpPos.IsValid())
                        {
                            return;
                        }
                        var ward = args.Target as Obj_AI_Minion;
                        if (ward == null || !ward.IsWard() || ward.Distance(lastJumpPos) > 100)
                        {
                            return;
                        }
                        lastJumpPos = new Vector2();
                    };
                GameObject.OnCreate += (sender, args) =>
                    {
                        var ward = sender as Obj_AI_Minion;
                        if (ward == null || ward.IsEnemy || !ward.IsWard() || !lastJumpPos.IsValid()
                            || ward.Distance(lastJumpPos) > 100)
                        {
                            return;
                        }
                        if (Variables.TickCount - LastPlaceTime < 1250)
                        {
                            LastPlaceTime = Variables.TickCount;
                        }
                        if (IsTryingToJump && W.IsReady() && IsWOne)
                        {
                            W.CastOnUnit(ward);
                        }
                    };
            }

            internal static bool Place(Vector3 pos, bool isInsecByWard = false, bool isFlee = false)
            {
                return Place(pos.ToVector2(), isInsecByWard, isFlee);
            }

            internal static bool Place(Vector2 pos, bool isInsecByWard = false, bool isFlee = false)
            {
                if (!CanWardJump)
                {
                    return false;
                }
                var ward = Common.GetWardSlot();
                if (ward == null)
                {
                    return false;
                }
                var posP = Player.ServerPosition.ToVector2();
                var posEnd = posP.Extend(pos, Math.Min(pos.Distance(posP), WardRange));
                if (!Player.Spellbook.CastSpell(ward.SpellSlot, posEnd.ToVector3()))
                {
                    return false;
                }
                if (isInsecByWard)
                {
                    LastPlaceTime = Variables.TickCount;
                }
                lastJumpPos = posEnd;
                lastJumpTime = Variables.TickCount;
                if (isFlee)
                {
                    lastJumpTime += 1100;
                }
                return true;
            }

            private static void Jump(Vector2 pos)
            {
                if (!W.IsReady() || !IsWOne || Variables.TickCount - W.LastCastAttemptT <= 1000)
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
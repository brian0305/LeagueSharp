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

        #endregion

        #region Static Fields

        private static readonly List<string> SpecialPet = new List<string>
                                                              { "jarvanivstandard", "teemomushroom", "illaoiminion" };

        private static int cPassive;

        private static bool isDashing;

        private static int lastW, lastR;

        private static Obj_AI_Base objQ;

        private static Obj_AI_Hero objR;

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.275f, 60, 1850, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(Q.Slot, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 425).SetTargetted(0.275f, float.MaxValue);
            E2 = new Spell(E.Slot, 570);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(R.Slot).SetSkillshot(0.25f, 40, 1500, false, Q.Type);
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
                comboMenu.Bool("Q2", "Also Q2");
                comboMenu.Bool("Q2Obj", "Q2 Even Miss", false);
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
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe || args.Slot != SpellSlot.R)
                    {
                        return;
                    }
                    lastR = Variables.TickCount;
                };
            Spellbook.OnCastSpell += (sender, args) =>
                {
                    if (!sender.Owner.IsMe || args.Slot != SpellSlot.W || !IsWOne)
                    {
                        return;
                    }
                    lastW = Variables.TickCount;
                };
        }

        #endregion

        #region Properties

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - lastR < 2500;

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

        private static void CastECombo()
        {
            if (!E.IsReady() || Player.Spellbook.IsCastingSpell)
            {
                return;
            }
            if (IsEOne)
            {
                var target =
                    Variables.TargetSelector.GetTargets(E.Range, E.DamageType, false)
                        .Where(i => E.CanHitCircle(i))
                        .ToList();
                if (target.Count == 0)
                {
                    return;
                }
                if ((cPassive == 0 && Player.Mana >= 70) || target.Count > 2
                    || (Variables.Orbwalker.GetTarget() == null
                            ? target.Any(i => i.DistanceToPlayer() > Player.GetRealAutoAttackRange() + 100)
                            : cPassive < 2))
                {
                    E.Cast();
                }
            }
            else
            {
                var target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                if (target.Count == 0)
                {
                    return;
                }
                if (cPassive == 0 || target.Count > 2
                    || target.Any(i => CanE2(i) || i.DistanceToPlayer() > Player.GetRealAutoAttackRange() + 150))
                {
                    E.Cast();
                }
            }
        }

        private static void CastELaneClear(List<Obj_AI_Minion> minions)
        {
            if (!E.IsReady() || Player.Spellbook.IsCastingSpell)
            {
                return;
            }
            if (IsEOne)
            {
                if (cPassive > 0)
                {
                    return;
                }
                var count = minions.Count(i => i.IsValidTarget(E.Range));
                if (count > 0 && (Player.Mana >= 70 || count > 2))
                {
                    E.Cast();
                }
            }
            else
            {
                var minion = minions.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                if (minion.Count > 0 && (cPassive == 0 || minion.Any(i => CanE2(i, true))))
                {
                    E.Cast();
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
            if (!W.IsReady() || Variables.TickCount - lastW <= 300 || Player.Spellbook.IsCastingSpell)
            {
                return;
            }
            var target = Variables.Orbwalker.GetTarget();
            if (target == null)
            {
                return;
            }
            if (Player.HealthPercent < 10)
            {
                W.Cast();
            }
            else if ((!IsWOne && Variables.TickCount - lastW > 2700) || cPassive == 0
                     || (Player.HealthPercent < target.HealthPercent && Player.HealthPercent < 30))
            {
                W.Cast();
            }
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
                        Flee(target.ServerPosition.ToVector2().Extend(Player.ServerPosition, R.Range / 2), true);
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
                else if (MainMenu["Combo"]["Q2"] && !Player.IsDashing() && objQ.IsValidTarget(Q2.Range))
                {
                    var target = objQ as Obj_AI_Hero;
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
                    else if (MainMenu["Combo"]["Q2Obj"])
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
                CastECombo();
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
            Flee(pos.ToVector2(), isStar);
        }

        private static void Flee(Vector2 pos, bool isStar = false)
        {
            if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 1000)
            {
                return;
            }
            var posPlayer = Player.ServerPosition.ToVector2();
            var posJump = posPlayer.Extend(pos, Math.Min(pos.Distance(posPlayer), W.Range));
            var objJump =
                GameObjects.AllyHeroes.Where(i => !i.IsMe)
                    .Cast<Obj_AI_Base>()
                    .Concat(
                        GameObjects.AllyMinions.Where(
                            i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower()))
                            .Concat(GameObjects.AllyWards.Where(i => i.IsWard())))
                    .Where(i => i.IsValidTarget(W.Range, false) && i.Distance(posJump) < (isStar ? R.Range - 50 : 200))
                    .MinOrDefault(i => i.Distance(posJump));
            if (objJump != null)
            {
                W.CastOnUnit(objJump);
            }
            else
            {
                WardManager.Place(pos, false, true);
            }
        }

        private static Tuple<int, Obj_AI_Hero> GetMultiR(Vector3 from)
        {
            var bestHit = 0;
            Obj_AI_Hero bestTarget = null;
            var kickTargets =
                GameObjects.EnemyHeroes.Where(
                    i =>
                    i.IsValidTarget(R.Range, true, from) && i.Health + i.PhysicalShield > R.GetDamage(i)
                    && !i.HasBuffOfType(BuffType.SpellShield) && !i.HasBuffOfType(BuffType.SpellImmunity)).ToList();
            if (kickTargets.Count == 0)
            {
                return new Tuple<int, Obj_AI_Hero>(0, null);
            }
            foreach (var kickTarget in kickTargets)
            {
                var realWidth = kickTarget.BoundingRadius + R2.Width;
                R2.Range = realWidth + KickRange;
                var posStart3D = kickTarget.ServerPosition;
                var posStart = posStart3D.ToVector2();
                var posEnd = posStart.Extend(from, -R2.Range);
                var hitCount = 1;
                var hitTargets =
                    GameObjects.EnemyHeroes.Where(
                        i => i.IsValidTarget(R2.Range + realWidth / 2, true, posStart3D) && !i.Compare(kickTarget))
                        .ToList();
                if (hitTargets.Count == 0)
                {
                    continue;
                }
                foreach (var hitTarget in hitTargets)
                {
                    R2.UpdateSourcePosition(posStart3D, posStart3D);
                    var pred = R2.VPrediction(hitTarget);
                    if (pred.Hitchance < HitChance.High)
                    {
                        continue;
                    }
                    if (pred.UnitPosition.ToVector2().Distance(posStart, posEnd, true) > realWidth)
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
                       .Any(i => E.CanHitCircle(i) && i.Health + i.MagicalShield <= E.GetDamage(i)) && E.Cast())
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
                    .Where(i => i.IsValidTarget(Q2.Range))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            if (MainMenu["LaneClear"]["E"])
            {
                CastELaneClear(minions);
            }
            if (MainMenu["LaneClear"]["Q"] && Q.IsReady())
            {
                if (IsQOne)
                {
                    var minionCount = minions.Count(i => i.InAutoAttackRange());
                    if (
                        (minions.Any(
                            i =>
                            i.InAutoAttackRange()
                            && (i.Team == GameObjectTeam.Neutral || i.GetMinionType().HasFlag(MinionTypes.Siege)
                                || i.GetMinionType().HasFlag(MinionTypes.Super))) || minionCount < 3 || minionCount > 4)
                        && cPassive < 2)
                    {
                        foreach (var minion in
                            minions.Where(i => Q.IsInRange(i) && (!i.InAutoAttackRange() || i.Health > Q.GetDamage(i)))
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
            if (!MainMenu["LastHit"]["Q"] || !Q.IsReady() || !IsQOne || Player.Spellbook.IsAutoAttacking)
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
                var multiR = GetMultiR(Player.ServerPosition);
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
            if (E.IsReady() && IsEOne && E.CanHitCircle(target) && (!HaveQ(target) || Player.Mana >= 70) && E.Cast())
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
                Flee(target.ServerPosition.ToVector2().Extend(Player.ServerPosition, R.Range / 2), true);
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

            internal static bool IsWardFlash;

            private static Vector2 lastEndPos, lastFlashPos;

            private static int lastInsecTime, lastMoveTime, lastRFlashTime, lastFlashRTime;

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

            private static bool CanWardFlash
                =>
                    MainMenu["Insec"]["Flash"] && MainMenu["Insec"]["FlashJump"] && WardManager.CanWardJump
                    && Flash.IsReady();

            private static bool IsRecent
                =>
                    Variables.TickCount - WardManager.LastInsecJumpTme < 5000
                    || (MainMenu["Insec"]["Flash"] && Variables.TickCount - lastFlashRTime < 5000)
                    || Variables.TickCount - WardManager.LastInsecWardTime < 5000;

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
                            lastEndPos = lastFlashPos = new Vector2();
                            lastInsecTime = 0;
                            IsWardFlash = false;
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
                Obj_AI_Base.OnBuffAdd += (sender, args) =>
                    {
                        if (sender.IsEnemy && args.Buff.Caster.IsMe && args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            lastObjQ = sender;
                        }
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe || !MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active
                            || !lastFlashPos.IsValid() || args.SData.Name != "summonerflash"
                            || !MainMenu["Insec"]["Flash"] || Variables.TickCount - lastFlashRTime > 1250
                            || args.End.Distance(lastFlashPos) > 100)
                        {
                            return;
                        }
                        lastFlashRTime = Variables.TickCount;
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
                    };
                Obj_AI_Base.OnDoCast += (sender, args) =>
                    {
                        if (!sender.IsMe || args.Slot != SpellSlot.R)
                        {
                            return;
                        }
                        lastEndPos = lastFlashPos = new Vector2();
                        lastInsecTime = 0;
                        IsWardFlash = false;
                        Variables.TargetSelector.SetTarget(null);
                    };
            }

            internal static void Start(Obj_AI_Hero target)
            {
                if (target != null && CanRFlash(target) && objR.Health + objR.PhysicalShield > R.GetDamage(objR)
                    && Player.Spellbook.CastSpell(Flash, GetPositionBehind(objR)))
                {
                    return;
                }
                if (Variables.Orbwalker.CanMove() && Variables.TickCount - lastMoveTime > 500)
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
                    if (!IsWardFlash && !checkFlash.Item2 && !checkJump.Item2 && CanWardFlash
                        && (!isDashing
                            || (!lastObjQ.Compare(target)
                                && lastObjQ.Distance(target) > WardManager.WardRange - GetDistBehind(target)))
                        && target.DistanceToPlayer() < WardManager.WardRange + R.Range - 80)
                    {
                        IsWardFlash = true;
                    }
                    if (!IsWardFlash)
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
                    else if (WardManager.Place(target.ServerPosition.ToVector2(), false, true))
                    {
                        return;
                    }
                }
                if ((!CanWardFlash || !IsWardFlash) && GapByQ(target))
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
                lastFlashPos = posBehind;
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastFlashRTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
            }

            private static bool GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Insec"]["Q"] || !Q.IsReady())
                {
                    return false;
                }
                var minDist = CanWardFlash
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
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = Variables.TickCount;
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
                        (Player.BoundingRadius + target.BoundingRadius + 45) * (100 + MainMenu["Insec"]["Dist"]) / 100,
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

            internal static int LastInsecWardTime, LastInsecJumpTme;

            private static Vector2 lastPlacePos;

            private static int lastPlaceTime;

            #endregion

            #region Properties

            internal static bool CanWardJump => CanCastWard && W.IsReady() && IsWOne;

            private static bool CanCastWard
                => Variables.TickCount - lastPlaceTime > 1250 && Common.GetWardSlot() != null;

            private static bool IsTryingToJump => lastPlacePos.IsValid() && Variables.TickCount - lastPlaceTime < 1250;

            #endregion

            #region Methods

            internal static void Init()
            {
                Game.OnUpdate += args =>
                    {
                        if (IsTryingToJump)
                        {
                            Jump(lastPlacePos);
                        }
                    };
                Spellbook.OnCastSpell += (sender, args) =>
                    {
                        if (!sender.Owner.IsMe || !lastPlacePos.IsValid() || args.Slot != SpellSlot.W || !IsWOne
                            || args.Target == null)
                        {
                            return;
                        }
                        var ward = args.Target as Obj_AI_Minion;
                        if (ward == null || !ward.IsWard() || ward.Distance(lastPlacePos) > 100)
                        {
                            return;
                        }
                        if (Variables.TickCount - LastInsecJumpTme < 1250)
                        {
                            LastInsecJumpTme = Variables.TickCount;
                        }
                        Insec.IsWardFlash = false;
                        lastPlacePos = new Vector2();
                    };
                GameObject.OnCreate += (sender, args) =>
                    {
                        if (!lastPlacePos.IsValid())
                        {
                            return;
                        }
                        var ward = sender as Obj_AI_Minion;
                        if (ward == null || ward.IsEnemy || !ward.IsWard() || ward.Distance(lastPlacePos) > 100)
                        {
                            return;
                        }
                        if (Variables.TickCount - LastInsecWardTime < 1250)
                        {
                            LastInsecWardTime = Variables.TickCount;
                        }
                        if (Variables.TickCount - lastPlaceTime < 1250 && W.IsReady() && IsWOne)
                        {
                            W.CastOnUnit(ward);
                        }
                    };
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
                var posPlayer = Player.ServerPosition.ToVector2();
                var posPlace = posPlayer.Extend(pos, Math.Min(pos.Distance(posPlayer), WardRange));
                if (!Player.Spellbook.CastSpell(ward.SpellSlot, posPlace.ToVector3()))
                {
                    return false;
                }
                if (isInsecByWard)
                {
                    LastInsecWardTime = LastInsecJumpTme = Variables.TickCount;
                }
                lastPlacePos = posPlace;
                lastPlaceTime = Variables.TickCount;
                if (isFlee)
                {
                    lastPlaceTime += 1100;
                }
                return true;
            }

            private static void Jump(Vector2 pos)
            {
                if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 1000)
                {
                    return;
                }
                var wardObj =
                    GameObjects.AllyWards.Where(
                        i => i.IsValidTarget(W.Range, false) && i.IsWard() && i.Distance(pos) < 200)
                        .MinOrDefault(i => i.Distance(pos));
                if (wardObj != null)
                {
                    W.CastOnUnit(wardObj);
                }
            }

            #endregion
        }
    }
}
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

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.UI.Menu;

    #endregion

    internal class LeeSin : Program
    {
        #region Constants

        private const int RKickRange = 725;

        #endregion

        #region Static Fields

        private static readonly List<string> SpecialPet = new List<string>
                                                              { "jarvanivstandard", "teemomushroom", "illaoiminion" };

        private static int cPassive;

        private static bool isDashing;

        private static int lastBubbaKush;

        private static int lastW, lastW2, lastE2, lastR;

        private static Obj_AI_Base objQ;

        private static Vector3 posBubbaKush;

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.25f, 60, 1800, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(Q.Slot, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 425).SetTargetted(0.25f, float.MaxValue);
            E2 = new Spell(E.Slot, 570);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(R.Slot, RKickRange).SetSkillshot(0.32f, 0, 900, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.VeryHigh;

            WardManager.Init();
            Insec.Init();
            var kuMenu = MainMenu.Add(new Menu("KnockUp", "Auto Knock Up"));
            {
                kuMenu.KeyBind("R", "Keybind", Keys.L, KeyBindType.Toggle);
                kuMenu.Bool("RKill", "If Kill Enemy Behind");
                kuMenu.Slider("RCountA", "Or Hit Enemy Behind >=", 1, 1, 4);
            }
            var bkMenu = MainMenu.Add(new Menu("BubbaKush", "Bubba Kush"));
            {
                bkMenu.KeyBind("R", "Keybind (R-Flash)", Keys.XButton2);
                bkMenu.Bool("RKill", "Priority To Kill Enemy");
                bkMenu.Slider("RCountA", "Or Hit Enemy >=", 1, 1, 4);
            }
            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
                comboMenu.Separator("Q Settings");
                comboMenu.Bool("Q", "Use Q");
                comboMenu.Bool("Q2", "Also Q2");
                comboMenu.Bool("Q2Obj", "Q2 Even Miss", false);
                comboMenu.Bool("QCol", "Smite Collision");
                comboMenu.Separator("W Settings");
                comboMenu.Bool("W", "Use W", false);
                comboMenu.Bool("W2", "Also W2", false);
                comboMenu.Separator("E Settings");
                comboMenu.Bool("E", "Use E");
                comboMenu.Bool("E2", "Also E2");
                comboMenu.Separator("Star Combo Settings");
                comboMenu.KeyBind("Star", "Star Combo", Keys.X);
                comboMenu.Bool("StarKill", "Auto Star Combo If Killable", false);
                comboMenu.Bool("StarKillWJ", "-> Ward Jump In Auto Star Combo", false);
            }
            var lcMenu = MainMenu.Add(new Menu("LaneClear", "Lane Clear"));
            {
                lcMenu.Bool("W", "Use W", false);
                lcMenu.Bool("E", "Use E");
                lcMenu.Separator("Q Settings");
                lcMenu.Bool("Q", "Use Q");
                lcMenu.Bool("QBig", "Only Q Big Mob In Jungle");
            }
            var lhMenu = MainMenu.Add(new Menu("LastHit", "Last Hit"));
            {
                lhMenu.Bool("Q", "Use Q1");
            }
            var ksMenu = MainMenu.Add(new Menu("KillSteal", "Kill Steal"));
            {
                ksMenu.Bool("E", "Use E");
                ksMenu.Bool("R", "Use R");
                ksMenu.Separator("Q Settings");
                ksMenu.Bool("Q", "Use Q");
                ksMenu.Bool("Q2", "Also Q2");
                if (GameObjects.EnemyHeroes.Any())
                {
                    ksMenu.Separator("Extra R Settings");
                    GameObjects.EnemyHeroes.ForEach(
                        i => ksMenu.Bool("RCast" + i.ChampionName, "Cast On " + i.ChampionName, false));
                }
            }
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range", false);
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
                drawMenu.Bool("KnockUp", "Auto Knock Up Status");
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);
            MainMenu.KeyBind("RFlash", "R-Flash To Mouse", Keys.Z);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        switch (args.Buff.DisplayName)
                        {
                            case "BlindMonkFlurry":
                                cPassive = 2;
                                break;
                            case "BlindMonkQTwoDash":
                                isDashing = true;
                                break;
                        }
                    }
                    else if (sender.IsEnemy)
                    {
                        if (args.Buff.DisplayName == "BlindMonkSonicWave")
                        {
                            objQ = sender;
                        }
                        else if (args.Buff.Name == "blindmonkrroot" && Common.CanFlash)
                        {
                            CastRFlash(sender);
                        }
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (sender.IsMe)
                    {
                        switch (args.Buff.DisplayName)
                        {
                            case "BlindMonkFlurry":
                                cPassive = 0;
                                break;
                            case "BlindMonkQTwoDash":
                                isDashing = false;
                                break;
                        }
                    }
                    else if (sender.IsEnemy && args.Buff.DisplayName == "BlindMonkSonicWave")
                    {
                        objQ = null;
                    }
                };
            Obj_AI_Base.OnBuffUpdateCount += (sender, args) =>
                {
                    if (!sender.IsMe || args.Buff.DisplayName != "BlindMonkFlurry")
                    {
                        return;
                    }
                    cPassive = args.Buff.Count;
                };
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    if (args.Slot == SpellSlot.R)
                    {
                        lastR = Variables.TickCount;
                    }
                    else if (args.SData.Name == "SummonerFlash" && posBubbaKush.IsValid())
                    {
                        posBubbaKush = new Vector3();
                    }
                };
        }

        #endregion

        #region Properties

        private static bool IsDashing => (lastW > 0 && Variables.TickCount - lastW <= 100) || Player.IsDashing();

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - lastR < 2500;

        private static bool IsWOne => W.Instance.SData.Name.ToLower().Contains("one");

        #endregion

        #region Methods

        private static void AutoKnockUp()
        {
            if (!R.IsReady())
            {
                return;
            }
            var multi = GetMultiHit(MainMenu["KnockUp"]["RKill"], MainMenu["KnockUp"]["RCountA"], 0);
            if (multi.Item1 != null && multi.Item2 != 0)
            {
                R.CastOnUnit(multi.Item1);
            }
        }

        private static void BubbaKush()
        {
            if (!R.IsReady())
            {
                return;
            }
            var multi = GetMultiHit(MainMenu["BubbaKush"]["RKill"], MainMenu["BubbaKush"]["RCountA"], 0);
            if (multi.Item1 != null && multi.Item2 != 0)
            {
                R.CastOnUnit(multi.Item1);
                Variables.Orbwalker.SetMovementState(false);
                DelayAction.Add(100, () => Variables.Orbwalker.SetMovementState(true));
                Game.PrintChat("BK => R");
            }
            else if (Variables.TickCount - lastBubbaKush > 1500)
            {
                var multiW = WardManager.CanWardJump
                                 ? GetMultiHit(MainMenu["BubbaKush"]["RKill"], MainMenu["BubbaKush"]["RCountA"], 2)
                                 : new Tuple<Obj_AI_Hero, int, Vector3>(null, 0, new Vector3());
                var multiF = Common.CanFlash && !posBubbaKush.IsValid()
                                 ? GetMultiHit(MainMenu["BubbaKush"]["RKill"], MainMenu["BubbaKush"]["RCountA"], 1)
                                 : new Tuple<Obj_AI_Hero, int, Vector3>(null, 0, new Vector3());
                if (multiW.Item1 != null && multiW.Item2 != 0)
                {
                    lastBubbaKush = Variables.TickCount;
                    WardManager.Place(
                        multiW.Item1.ServerPosition.Extend(
                            multiW.Item3,
                            -(Player.BoundingRadius / 2 + multiW.Item1.BoundingRadius + 50)));
                    Game.PrintChat("BK => Jump-R");
                }
                else if (multiF.Item1 != null && multiF.Item2 != 0 && R.CastOnUnit(multiF.Item1))
                {
                    lastBubbaKush = Variables.TickCount;
                    posBubbaKush = multiF.Item3;
                    Variables.TargetSelector.SetTarget(multiF.Item1);
                    Game.PrintChat("BK => R-Flash");
                }
            }
        }

        private static bool CanE2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < 0.25 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanQ2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.25 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanR(Obj_AI_Hero target)
        {
            var buff = target.GetBuff("BlindMonkDragonsRage");
            return buff != null && buff.EndTime - Game.Time <= 0.75 * (buff.EndTime - buff.StartTime);
        }

        private static void CastE(List<Obj_AI_Minion> minions = null)
        {
            if (!E.IsReady() || isDashing || Variables.TickCount - lastW <= 250 || Variables.TickCount - lastW2 <= 150)
            {
                return;
            }
            if (minions == null)
            {
                CastECombo();
            }
            else
            {
                CastELaneClear(minions);
            }
        }

        private static void CastECombo()
        {
            if (IsEOne)
            {
                var target =
                    Variables.TargetSelector.GetTargets(E.Range + 20, E.DamageType)
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
            else if (MainMenu["Combo"]["E2"])
            {
                var target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                if (target.Count == 0)
                {
                    return;
                }
                if ((cPassive == 0 || target.Count > 2
                     || target.Any(i => CanE2(i) || i.DistanceToPlayer() > i.GetRealAutoAttackRange() + 50))
                    && E2.Cast())
                {
                    lastE2 = Variables.TickCount;
                }
            }
        }

        private static void CastELaneClear(List<Obj_AI_Minion> minions)
        {
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
                if (minion.Count > 0 && (cPassive == 0 || minion.Any(CanE2)) && E2.Cast())
                {
                    lastE2 = Variables.TickCount;
                }
            }
        }

        private static void CastRFlash(Obj_AI_Base target)
        {
            var targetSelect = Variables.TargetSelector.GetSelectedTarget();
            if (!targetSelect.IsValidTarget() || !targetSelect.Compare(target)
                || target.Health + target.PhysicalShield <= R.GetDamage(target))
            {
                return;
            }
            var pos = new Vector3();
            if (MainMenu["RFlash"].GetValue<MenuKeyBind>().Active)
            {
                pos = Game.CursorPos;
            }
            else if (MainMenu["BubbaKush"]["R"].GetValue<MenuKeyBind>().Active && posBubbaKush.IsValid())
            {
                pos = posBubbaKush;
            }
            else if (MainMenu["Insec"]["R"].GetValue<MenuKeyBind>().Active && Insec.IsRecentRFlash)
            {
                pos = Insec.GetPositionKickTo((Obj_AI_Hero)target);
            }
            if (pos.IsValid())
            {
                Player.Spellbook.CastSpell(
                    Flash,
                    target.ServerPosition.Extend(pos, -(Player.BoundingRadius / 2 + target.BoundingRadius + 50)));
            }
        }

        private static void CastW(List<Obj_AI_Minion> minions = null)
        {
            if (!W.IsReady() || Variables.TickCount - lastW <= 300 || isDashing || Variables.TickCount - lastE2 <= 250)
            {
                return;
            }
            var hero = Variables.Orbwalker.GetTarget() as Obj_AI_Hero;
            Obj_AI_Minion minion = null;
            if (minions != null && minions.Count > 0)
            {
                minion = minions.FirstOrDefault(i => i.InAutoAttackRange());
            }
            if (hero == null && minion == null)
            {
                return;
            }
            if (hero != null && !IsWOne && !MainMenu["Combo"]["W2"])
            {
                return;
            }
            if (hero != null && Player.HealthPercent < hero.HealthPercent && Player.HealthPercent < 30)
            {
                if (IsWOne)
                {
                    if (W.Cast())
                    {
                        lastW = Variables.TickCount;
                        return;
                    }
                }
                else if (W.Cast())
                {
                    lastW2 = Variables.TickCount;
                    return;
                }
            }
            if (Player.HealthPercent < (minions == null ? 8 : 5) || (!IsWOne && Variables.TickCount - lastW > 2600)
                || cPassive == 0
                || (minion != null && minion.Team == GameObjectTeam.Neutral
                    && minion.GetJungleType() != JungleType.Small && Player.HealthPercent < 40 && IsWOne))
            {
                if (IsWOne)
                {
                    if (W.Cast())
                    {
                        lastW = Variables.TickCount;
                    }
                }
                else if (W.Cast())
                {
                    lastW2 = Variables.TickCount;
                }
            }
        }

        private static void Combo()
        {
            if (R.IsReady() && MainMenu["Combo"]["StarKill"] && Q.IsReady() && !IsQOne && MainMenu["Combo"]["Q"]
                && MainMenu["Combo"]["Q2"])
            {
                var target = Variables.TargetSelector.GetTargets(Q2.Range, Q2.DamageType).FirstOrDefault(HaveQ);
                if (target != null
                    && target.Health + target.PhysicalShield
                    > Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                    && target.Health + target.PhysicalShield
                    <= GetQ2Dmg(target, R.GetDamage(target)) + Player.GetAutoAttackDamage(target))
                {
                    if (R.CastOnUnit(target))
                    {
                        return;
                    }
                    if (MainMenu["Combo"]["StarKillWJ"] && !R.IsInRange(target)
                        && target.DistanceToPlayer() < WardManager.WardRange + R.Range - 50 && Player.Mana >= 80
                        && !isDashing)
                    {
                        Flee(target.ServerPosition, true);
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
                        Q.CastSpellSmite(target, MainMenu["Combo"]["QCol"]);
                    }
                }
                else if (MainMenu["Combo"]["Q2"] && !IsDashing && objQ.IsValidTarget(Q2.Range))
                {
                    var target = objQ as Obj_AI_Hero;
                    if (target != null)
                    {
                        if ((CanQ2(target) || (!R.IsReady() && IsRecentR && CanR(target))
                             || target.Health + target.PhysicalShield
                             <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                             || ((R.IsReady()
                                  || (!target.HasBuff("BlindMonkDragonsRage") && Variables.TickCount - lastR > 1000))
                                 && target.DistanceToPlayer() > target.GetRealAutoAttackRange() + 100) || cPassive == 0)
                            && Q2.Cast())
                        {
                            isDashing = true;
                            return;
                        }
                    }
                    else if (MainMenu["Combo"]["Q2Obj"])
                    {
                        var targetQ2 = Q2.GetTarget(200);
                        if (targetQ2 != null && objQ.Distance(targetQ2) < targetQ2.DistanceToPlayer()
                            && !targetQ2.InAutoAttackRange() && Q2.Cast())
                        {
                            isDashing = true;
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
            if (subTarget != null && MainMenu["Combo"]["Ignite"] && Common.CanIgnite && subTarget.HealthPercent < 30
                && subTarget.DistanceToPlayer() <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Flee(Vector3 pos, bool isStar = false)
        {
            if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 500)
            {
                return;
            }
            var posPlayer = Player.ServerPosition;
            var posJump = pos.Distance(posPlayer) < W.Range ? pos : posPlayer.Extend(pos, W.Range);
            var objJumps = new List<Obj_AI_Base>();
            objJumps.AddRange(GameObjects.AllyHeroes.Where(i => !i.IsMe));
            objJumps.AddRange(GameObjects.AllyWards.Where(i => i.IsWard()));
            objJumps.AddRange(
                GameObjects.AllyMinions.Where(
                    i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower())));
            var objJump =
                objJumps.Where(
                    i => i.IsValidTarget(W.Range, false) && i.Distance(posJump) < (isStar ? R.Range - 50 : 200))
                    .MinOrDefault(i => i.Distance(posJump));
            if (objJump != null)
            {
                if (W.CastOnUnit(objJump))
                {
                    lastW = Variables.TickCount;
                }
            }
            else
            {
                WardManager.Place(posJump);
            }
        }

        private static Tuple<Obj_AI_Hero, int, Vector3> GetMultiHit(bool checkKill, int minHit, int mode)
        {
            var bestHit = 0;
            Obj_AI_Hero bestTarget = null;
            var posAfter = new Vector3();
            var targetKicks =
                GameObjects.EnemyHeroes.Where(
                    i =>
                    i.IsValidTarget(
                        R.Range
                        + (mode == 2
                               ? WardManager.WardRange - (Player.BoundingRadius / 2 + i.BoundingRadius + 50) - 80
                               : 0)) && i.Health + i.PhysicalShield > R.GetDamage(i)
                    && !i.HasBuffOfType(BuffType.SpellShield) && !i.HasBuffOfType(BuffType.SpellImmunity))
                    .OrderByDescending(i => i.BonusHealth)
                    .ToList();
            foreach (var targetKick in targetKicks)
            {
                var posTarget = targetKick.ServerPosition;
                R2.Width = targetKick.BoundingRadius;
                R2.Range = RKickRange + R2.Width / 2;
                R2.UpdateSourcePosition(posTarget, posTarget);
                var targetHits =
                    GameObjects.EnemyHeroes.Where(
                        i =>
                        !i.Compare(targetKick) && i.IsValidTarget(R2.Range + R2.Width / 2, true, R2.From)
                        && i.Distance(R2.From) > R2.Width / 2 - 10)
                        .OrderByDescending(i => new Priority().GetDefaultPriority(i))
                        .Select(
                            i =>
                            new Tuple<Obj_AI_Hero, Vector3>(
                                i,
                                Movement.GetPrediction(i, R2.Delay, R2.Width, R2.Speed).UnitPosition))
                        .ToList();
                if (mode == 0)
                {
                    var posEnd = R2.From.Extend(Player.ServerPosition, -R2.Range);
                    targetHits = targetHits.Where(i => R2.WillHit(i.Item2, posEnd)).ToList();
                }
                else
                {
                    var hits = new List<Tuple<Obj_AI_Hero, Vector3>>();
                    foreach (var targetHit in targetHits)
                    {
                        var posEnd = R2.From.Extend(targetHit.Item2, R2.Range);
                        if (!R2.WillHit(targetHit.Item2, posEnd))
                        {
                            continue;
                        }
                        var list = new List<Tuple<Obj_AI_Hero, Vector3>> { targetHit };
                        list.AddRange(
                            targetHits.Where(i => !i.Item1.Compare(targetHit.Item1) && R2.WillHit(i.Item2, posEnd)));
                        if (list.Count > hits.Count)
                        {
                            hits = list;
                        }
                    }
                    targetHits = hits;
                }
                if (targetHits.Count == 0)
                {
                    continue;
                }
                if (checkKill)
                {
                    foreach (var targetHit in targetHits)
                    {
                        var dmgR = GetRColDmg(targetKick, targetHit.Item1);
                        if (targetHit.Item1.Health + targetHit.Item1.PhysicalShield <= dmgR
                            && !Invulnerable.Check(targetHit.Item1, R.DamageType, true, dmgR))
                        {
                            return new Tuple<Obj_AI_Hero, int, Vector3>(targetKick, -1, targetHit.Item2);
                        }
                    }
                }
                if (targetHits.Count > bestHit)
                {
                    bestTarget = targetKick;
                    bestHit = targetHits.Count;
                    posAfter = targetHits[0].Item2;
                }
            }
            return new Tuple<Obj_AI_Hero, int, Vector3>(bestTarget, bestHit >= minHit ? 1 : 0, posAfter);
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
                            || (MainMenu["KillSteal"]["Q2"]
                                && target.Health + target.PhysicalShield
                                <= GetQ2Dmg(target, Q.GetDamage(target)) + Player.GetAutoAttackDamage(target)
                                && Player.Mana - Q.Instance.ManaCost >= 30))
                        && Q.Casting(
                            target,
                            false,
                            CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
                               .IsCasted())
                    {
                        return;
                    }
                }
                else if (MainMenu["KillSteal"]["Q2"] && !IsDashing)
                {
                    var target = objQ as Obj_AI_Hero;
                    if (target != null
                        && target.Health + target.PhysicalShield
                        <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target) && Q2.Cast())
                    {
                        isDashing = true;
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
                    Variables.TargetSelector.GetTargets(R.Range, R.DamageType, false)
                        .Where(i => MainMenu["KillSteal"]["RCast" + i.ChampionName])
                        .ToList();
                if (targetList.Count > 0)
                {
                    var targetR = targetList.FirstOrDefault(i => i.Health + i.PhysicalShield <= R.GetDamage(i));
                    if (targetR != null)
                    {
                        R.CastOnUnit(targetR);
                    }
                    else if (MainMenu["KillSteal"]["Q"] && MainMenu["KillSteal"]["Q2"] && Q.IsReady() && !IsQOne)
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
                Common.ListMinions().Where(i => i.IsValidTarget(Q2.Range)).OrderByDescending(i => i.MaxHealth).ToList();
            if (minions.Count == 0)
            {
                return;
            }
            if (MainMenu["LaneClear"]["E"])
            {
                CastE(minions);
            }
            if (MainMenu["LaneClear"]["W"])
            {
                CastW(minions);
            }
            if (MainMenu["LaneClear"]["Q"] && Q.IsReady())
            {
                if (IsQOne)
                {
                    if (cPassive < 2)
                    {
                        var minionQ = minions.Where(i => i.DistanceToPlayer() < Q.Range - 10).ToList();
                        if (minionQ.Count > 0)
                        {
                            var minionJungle =
                                minionQ.Where(i => i.Team == GameObjectTeam.Neutral)
                                    .OrderByDescending(i => i.MaxHealth)
                                    .ThenBy(i => i.DistanceToPlayer())
                                    .ToList();
                            if (MainMenu["LaneClear"]["QBig"] && minionJungle.Count > 0 && Player.Health > 100)
                            {
                                minionJungle =
                                    minionJungle.Where(
                                        i =>
                                        i.GetJungleType() == JungleType.Legendary
                                        || i.GetJungleType() == JungleType.Large || i.Name.Contains("Crab")).ToList();
                            }
                            if (minionJungle.Count > 0)
                            {
                                minionJungle.ForEach(i => Q.Casting(i));
                            }
                            else
                            {
                                var minionLane =
                                    minionQ.Where(i => i.Team != GameObjectTeam.Neutral)
                                        .OrderByDescending(i => i.GetMinionType().HasFlag(MinionTypes.Siege))
                                        .ThenBy(i => i.GetMinionType().HasFlag(MinionTypes.Super))
                                        .ThenBy(i => i.Health)
                                        .ThenByDescending(i => i.MaxHealth)
                                        .ToList();
                                if (minionLane.Count == 0)
                                {
                                    return;
                                }
                                foreach (var minion in minionLane)
                                {
                                    if (minion.InAutoAttackRange())
                                    {
                                        if (Q.GetHealthPrediction(minion) > Q.GetDamage(minion)
                                            && Q.Casting(minion).IsCasted())
                                        {
                                            return;
                                        }
                                    }
                                    else if ((Variables.Orbwalker.GetTarget() != null
                                                  ? Q.CanLastHit(minion, Q.GetDamage(minion))
                                                  : Q.GetHealthPrediction(minion) > Q.GetDamage(minion))
                                             && Q.Casting(minion).IsCasted())
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (!IsDashing)
                {
                    var q2Minion = objQ;
                    if (q2Minion.IsValidTarget(Q2.Range)
                        && (CanQ2(q2Minion) || q2Minion.Health <= Q.GetDamage(q2Minion, DamageStage.SecondCast)
                            || q2Minion.DistanceToPlayer() > q2Minion.GetRealAutoAttackRange() + 100 || cPassive == 0)
                        && Q2.Cast())
                    {
                        isDashing = true;
                    }
                }
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
                    i => (i.IsMinion() || i.IsPet(false)) && i.IsValidTarget(Q.Range) && Q.CanLastHit(i, Q.GetDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .ToList();
            if (minions.Count == 0)
            {
                return;
            }
            minions.ForEach(
                i =>
                Q.Casting(
                    i,
                    false,
                    CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall));
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
                    (IsQOne ? Q : Q2).Range,
                    Q.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["W"] && W.Level > 0 && IsWOne)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (MainMenu["Draw"]["E"] && E.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position,
                    (IsEOne ? E : E2).Range,
                    E.IsReady() ? Color.LimeGreen : Color.IndianRed);
            }
            if (R.Level > 0)
            {
                if (MainMenu["Draw"]["R"])
                {
                    Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.LimeGreen : Color.IndianRed);
                }
                if (MainMenu["Draw"]["KnockUp"])
                {
                    var menu = MainMenu["KnockUp"]["R"].GetValue<MenuKeyBind>();
                    var text =
                        $"Auto Knock Up: {(menu.Active ? "On" : "Off")} <{MainMenu["KnockUp"]["RCountA"].GetValue<MenuSlider>().Value}> [{menu.Key}]";
                    var pos = Drawing.WorldToScreen(Player.Position);
                    Drawing.DrawText(
                        pos.X - (float)Drawing.GetTextExtent(text).Width / 2,
                        pos.Y + 20,
                        menu.Active ? Color.White : Color.Gray,
                        text);
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || MenuGUI.IsShopOpen || Player.IsRecalling())
            {
                return;
            }
            KillSteal();
            Variables.Orbwalker.SetAttackState(!MainMenu["Insec"]["R"].GetValue<MenuKeyBind>().Active);
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
                    else if (MainMenu["RFlash"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        if (R.IsReady() && Common.CanFlash)
                        {
                            var target =
                                Variables.TargetSelector.GetTargets(R.Range, R.DamageType)
                                    .Where(i => i.Health + i.PhysicalShield > R.GetDamage(i))
                                    .MaxOrDefault(i => new Priority().GetPriority(i));
                            if (target != null && R.CastOnUnit(target))
                            {
                                Variables.TargetSelector.SetTarget(target);
                            }
                        }
                    }
                    else if (MainMenu["BubbaKush"]["R"].GetValue<MenuKeyBind>().Active)
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                        BubbaKush();
                    }
                    else if (MainMenu["Combo"]["Star"].GetValue<MenuKeyBind>().Active)
                    {
                        StarCombo();
                    }
                    else if (MainMenu["Insec"]["R"].GetValue<MenuKeyBind>().Active)
                    {
                        Insec.Start(Insec.GetTarget);
                    }
                    break;
            }
            if (MainMenu["KnockUp"]["R"].GetValue<MenuKeyBind>().Active
                && !MainMenu["BubbaKush"]["R"].GetValue<MenuKeyBind>().Active
                && !MainMenu["Insec"]["R"].GetValue<MenuKeyBind>().Active)
            {
                AutoKnockUp();
            }
        }

        private static void StarCombo()
        {
            var target = Q.GetTarget(Q.Width / 2);
            if (!IsQOne)
            {
                target = objQ as Obj_AI_Hero;
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
                    Q.CastSpellSmite(target, false);
                }
                else if (!IsDashing && HaveQ(target)
                         && (target.Health + target.PhysicalShield
                             <= Q.GetDamage(target, DamageStage.SecondCast) + Player.GetAutoAttackDamage(target)
                             || (!R.IsReady() && IsRecentR && CanR(target))) && Q2.Cast())
                {
                    isDashing = true;
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
            else if (target.DistanceToPlayer() < WardManager.WardRange + R.Range - 50 && Player.Mana >= 70 && !isDashing)
            {
                Flee(target.ServerPosition, true);
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
            if (Titanic.IsReady && !Player.Spellbook.IsAutoAttacking && Variables.Orbwalker.GetTarget() != null)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class Insec
        {
            #region Static Fields

            internal static bool IsWardFlash;

            private static Vector3 lastEndPos, lastFlashPos;

            private static int lastInsecTime, lastMoveTime, lastRFlashTime, lastFlashRTime;

            private static Obj_AI_Base lastObjQ;

            #endregion

            #region Properties

            internal static Obj_AI_Hero GetTarget
            {
                get
                {
                    Obj_AI_Hero target = null;
                    if (MainMenu["Insec"]["TargetSelect"])
                    {
                        var sub = Variables.TargetSelector.GetSelectedTarget();
                        if (sub.IsValidTarget())
                        {
                            target = sub;
                        }
                    }
                    else
                    {
                        target = Q.GetTarget(-100);
                        if ((MainMenu["Insec"]["Q"] && Q.IsReady()) || objQ.IsValidTarget(Q2.Range))
                        {
                            target = Q2.GetTarget(FlashRange);
                        }
                    }
                    return target;
                }
            }

            internal static bool IsRecentRFlash => Variables.TickCount - lastRFlashTime < 5000;

            private static bool CanInsec
                =>
                    (WardManager.CanWardJump || (MainMenu["Insec"]["Flash"] && Common.CanFlash) || IsRecent)
                    && R.IsReady();

            private static bool CanWardFlash
                =>
                    MainMenu["Insec"]["Flash"] && MainMenu["Insec"]["FlashJump"] && WardManager.CanWardJump
                    && Common.CanFlash;

            private static bool IsRecent
                =>
                    IsRecentWardJump
                    || (MainMenu["Insec"]["Flash"] && (IsRecentRFlash || Variables.TickCount - lastFlashRTime < 5000));

            private static bool IsRecentWardJump
                =>
                    Variables.TickCount - WardManager.LastInsecWardTime < 5000
                    || Variables.TickCount - WardManager.LastInsecJumpTme < 5000;

            #endregion

            #region Methods

            internal static Vector3 GetPositionKickTo(Obj_AI_Hero target)
            {
                if (lastEndPos.IsValid() && target.Distance(lastEndPos) <= RKickRange + 700)
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
                                target.Distance(i) <= RKickRange + 500 && i.Distance(target) - RKickRange <= 950
                                && i.Distance(target) > 225).MinOrDefault(i => i.DistanceToPlayer());
                        if (turret != null)
                        {
                            pos = turret.ServerPosition;
                        }
                        else
                        {
                            var hero =
                                GameObjects.AllyHeroes.Where(
                                    i =>
                                    i.IsValidTarget(RKickRange + 700, false, target.ServerPosition) && !i.IsMe
                                    && i.HealthPercent > 10 && i.Distance(target) > 325)
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
                return pos;
            }

            internal static void Init()
            {
                var insecMenu = MainMenu.Add(new Menu("Insec", "Insec"));
                {
                    insecMenu.KeyBind("R", "Keybind", Keys.T);
                    insecMenu.Bool("TargetSelect", "Only Insec Target Selected", false);
                    insecMenu.List("Mode", "Mode", new[] { "Tower/Hero/Current", "Mouse Position", "Current Position" });
                    insecMenu.Separator("Draw Settings");
                    insecMenu.Bool("DLine", "Line");
                    insecMenu.Bool("DWardFlash", "WardJump Flash Range");
                    insecMenu.Separator("Flash Settings");
                    insecMenu.Bool("Flash", "Use Flash");
                    insecMenu.List("FlashMode", "Flash Mode", new[] { "R-Flash", "Flash-R", "Both" });
                    insecMenu.Bool("FlashJump", "Use WardJump To Gap For Flash");
                    insecMenu.Separator("Q Settings");
                    insecMenu.Bool("Q", "Use Q");
                    insecMenu.Bool("QCol", "Smite Collision");
                    insecMenu.Bool("QObj", "Use Q On Near Object");
                }

                Game.OnUpdate += args =>
                    {
                        if (lastInsecTime > 0 && Variables.TickCount - lastInsecTime > 5000)
                        {
                            CleanData();
                        }
                        if (lastMoveTime > 0 && Variables.TickCount - lastMoveTime > 1000 && !R.IsReady())
                        {
                            lastMoveTime = 0;
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || R.Level == 0 || !CanInsec)
                        {
                            return;
                        }
                        if (MainMenu["Insec"]["DLine"])
                        {
                            var target = GetTarget;
                            if (target != null)
                            {
                                var posTarget = target.Position;
                                var posEnd = GetPositionKickTo(target);
                                Render.Circle.DrawCircle(posTarget, target.BoundingRadius * 1.35f, Color.BlueViolet);
                                Render.Circle.DrawCircle(
                                    GetPositionBehind(target, posEnd),
                                    target.BoundingRadius * 1.35f,
                                    Color.BlueViolet);
                                Drawing.DrawLine(
                                    Drawing.WorldToScreen(posTarget),
                                    Drawing.WorldToScreen(posEnd),
                                    1,
                                    Color.BlueViolet);
                            }
                        }
                        if (MainMenu["Insec"]["DWardFlash"] && CanWardFlash)
                        {
                            Render.Circle.DrawCircle(Player.Position, GetRange(null, true), Color.Orange);
                        }
                    };
                Obj_AI_Base.OnBuffAdd += (sender, args) =>
                    {
                        if (!sender.IsEnemy || args.Buff.DisplayName != "BlindMonkSonicWave")
                        {
                            return;
                        }
                        lastObjQ = sender;
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!lastFlashPos.IsValid() || !sender.IsMe
                            || !MainMenu["Insec"]["R"].GetValue<MenuKeyBind>().Active
                            || args.SData.Name != "SummonerFlash" || !MainMenu["Insec"]["Flash"]
                            || Variables.TickCount - lastFlashRTime > 1250 || args.End.Distance(lastFlashPos) > 80)
                        {
                            return;
                        }
                        lastFlashRTime = Variables.TickCount;
                        var target = Variables.TargetSelector.GetSelectedTarget();
                        if (target.IsValidTarget())
                        {
                            DelayAction.Add(5, () => R.CastOnUnit(target));
                        }
                    };
                Obj_AI_Base.OnDoCast += (sender, args) =>
                    {
                        if (!sender.IsMe || args.Slot != SpellSlot.R)
                        {
                            return;
                        }
                        CleanData();
                    };
            }

            internal static void Start(Obj_AI_Hero target)
            {
                if (Variables.Orbwalker.CanMove() && Variables.TickCount - lastMoveTime > 250)
                {
                    var posMove = Game.CursorPos;
                    if (target != null && lastMoveTime > 0 && CanInsec)
                    {
                        var posEnd = GetPositionKickTo(target);
                        if (posEnd.DistanceToPlayer() > target.Distance(posEnd))
                        {
                            posMove = GetPositionBehind(target, posEnd);
                        }
                    }
                    Variables.Orbwalker.Move(posMove);
                }
                if (target == null || !CanInsec)
                {
                    return;
                }
                if (!IsRecent)
                {
                    if (!IsWardFlash)
                    {
                        var checkJump = GapCheck(target);
                        if (checkJump.Item2)
                        {
                            GapByWardJump(target, checkJump.Item1);
                        }
                        else
                        {
                            var checkFlash = GapCheck(target, true);
                            if (checkFlash.Item2)
                            {
                                GapByFlash(target, checkFlash.Item1);
                            }
                            else if (CanWardFlash)
                            {
                                var posTarget = target.ServerPosition;
                                if (posTarget.DistanceToPlayer() < GetRange(target, true)
                                    && (!isDashing
                                        || (!lastObjQ.Compare(target) && lastObjQ.Distance(posTarget) > GetRange(target))))
                                {
                                    IsWardFlash = true;
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Variables.TargetSelector.SetTarget(target);
                        WardManager.Place(target.ServerPosition);
                        return;
                    }
                }
                if (R.IsInRange(target))
                {
                    var posEnd = GetPositionKickTo(target);
                    var posTarget = target.ServerPosition;
                    var posPlayer = Player.ServerPosition;
                    if (posPlayer.Distance(posEnd) > posTarget.Distance(posEnd))
                    {
                        var segment = posTarget.Extend(posPlayer, -RKickRange)
                            .ProjectOn(posTarget, posEnd.Extend(posTarget, -(RKickRange * 0.5f)));
                        if (segment.IsOnSegment && segment.SegmentPoint.Distance(posEnd) <= RKickRange * 0.5f
                            && R.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                }
                GapByQ(target);
            }

            private static void CleanData()
            {
                lastEndPos = lastFlashPos = new Vector3();
                lastInsecTime = 0;
                IsWardFlash = false;
                Variables.TargetSelector.SetTarget(null);
            }

            private static void GapByFlash(Obj_AI_Hero target, Vector3 posBehind)
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

            private static void GapByFlashR(Obj_AI_Hero target, Vector3 posBehind)
            {
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                }
                lastFlashPos = posBehind;
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = lastFlashRTime = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                Player.Spellbook.CastSpell(Flash, posBehind);
            }

            private static void GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Insec"]["Q"] || !Q.IsReady() || IsDashing)
                {
                    return;
                }
                if (CanWardFlash && (IsWardFlash || (IsQOne && Player.Mana < 50 + 80)))
                {
                    return;
                }
                var minDist = GetRange(target, CanWardFlash);
                if (IsQOne)
                {
                    Q.CastSpellSmite(target, MainMenu["Insec"]["QCol"]);
                    if (!MainMenu["Insec"]["QObj"])
                    {
                        return;
                    }
                    var nearObj =
                        Common.ListEnemies(true)
                            .Where(
                                i =>
                                !i.Compare(target) && i.IsValidTarget(Q.Range)
                                && Q.GetHealthPrediction(i) > Q.GetDamage(i)
                                && i.Distance(target) < target.DistanceToPlayer() && i.Distance(target) < minDist - 80)
                            .OrderBy(i => i.Distance(target))
                            .ThenByDescending(i => i.Health)
                            .ToList();
                    if (nearObj.Count == 0)
                    {
                        return;
                    }
                    nearObj.ForEach(i => Q.Casting(i));
                }
                else if (target.DistanceToPlayer() > minDist
                         && (HaveQ(target) || (objQ.IsValidTarget(Q2.Range) && target.Distance(objQ) < minDist - 80))
                         && ((WardManager.CanWardJump && Player.Mana >= 80)
                             || (MainMenu["Insec"]["Flash"] && Common.CanFlash)) && Q2.Cast())
                {
                    isDashing = true;
                    Variables.TargetSelector.SetTarget(target);
                }
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

            private static void GapByWardJump(Obj_AI_Hero target, Vector3 posBehind)
            {
                if (Variables.Orbwalker.CanMove())
                {
                    lastMoveTime = Variables.TickCount;
                }
                lastEndPos = GetPositionAfterKick(target);
                lastInsecTime = WardManager.LastInsecWardTime = WardManager.LastInsecJumpTme = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                WardManager.Place(posBehind, 1);
            }

            private static Tuple<Vector3, bool> GapCheck(Obj_AI_Hero target, bool useFlash = false)
            {
                if (!useFlash ? !WardManager.CanWardJump : !MainMenu["Insec"]["Flash"] || !Common.CanFlash)
                {
                    return new Tuple<Vector3, bool>(new Vector3(), false);
                }
                var posEnd = GetPositionKickTo(target);
                var posPlayer = Player.ServerPosition;
                var posTarget = target.ServerPosition;
                if (!useFlash)
                {
                    var posBehind = posTarget.Extend(posEnd, -GetDistance(target));
                    if (posBehind.Distance(posPlayer) < WardManager.WardRange
                        && posTarget.Distance(posBehind) < posEnd.Distance(posBehind))
                    {
                        return new Tuple<Vector3, bool>(posBehind, true);
                    }
                }
                else
                {
                    var flashMode = MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index;
                    if (flashMode != 1 && posTarget.Distance(posPlayer) < R.Range)
                    {
                        return new Tuple<Vector3, bool>(new Vector3(), true);
                    }
                    if (flashMode > 0)
                    {
                        var posBehind = posTarget.Extend(posEnd, -GetDistance(target));
                        var posFlash = posPlayer.Extend(posBehind, FlashRange);
                        if (posBehind.Distance(posPlayer) < FlashRange
                            && posTarget.Distance(posBehind) < posEnd.Distance(posBehind)
                            && posFlash.Distance(posTarget) > 50
                            && posFlash.Distance(posTarget) < posFlash.Distance(posEnd))
                        {
                            return new Tuple<Vector3, bool>(posBehind, true);
                        }
                    }
                }
                return new Tuple<Vector3, bool>(new Vector3(), false);
            }

            private static float GetDistance(Obj_AI_Hero target)
            {
                return Math.Min((Player.BoundingRadius + target.BoundingRadius + 50) * 1.4f, 300);
            }

            private static Vector3 GetPositionAfterKick(Obj_AI_Hero target)
            {
                return target.ServerPosition.Extend(GetPositionKickTo(target), RKickRange);
            }

            private static Vector3 GetPositionBehind(Obj_AI_Hero target, Vector3 to)
            {
                return target.ServerPosition.Extend(to, -GetDistance(target));
            }

            private static float GetRange(Obj_AI_Hero target, bool isWardFlash = false)
            {
                return !isWardFlash
                           ? (WardManager.CanWardJump ? WardManager.WardRange : FlashRange) - GetDistance(target)
                           : WardManager.WardRange + R.Range - ((target?.BoundingRadius ?? Player.BoundingRadius) + 20);
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

            private static Vector3 lastPlacePos;

            private static int lastPlaceTime;

            #endregion

            #region Properties

            internal static bool CanWardJump => CanCastWard && W.IsReady() && IsWOne;

            private static bool CanCastWard => Variables.TickCount - lastPlaceTime > 1250 && Items.GetWardSlot() != null
                ;

            private static bool IsTryingToJump => lastPlacePos.IsValid() && Variables.TickCount - lastPlaceTime < 1250;

            #endregion

            #region Methods

            internal static void Init()
            {
                Game.OnUpdate += args =>
                    {
                        if (lastPlacePos.IsValid() && Variables.TickCount - lastPlaceTime > 1500)
                        {
                            lastPlacePos = new Vector3();
                        }
                        if (Player.IsDead)
                        {
                            return;
                        }
                        if (IsTryingToJump)
                        {
                            Jump(lastPlacePos);
                        }
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!lastPlacePos.IsValid() || !sender.IsMe || args.Slot != SpellSlot.W
                            || !args.SData.Name.ToLower().Contains("one"))
                        {
                            return;
                        }
                        var ward = args.Target as Obj_AI_Minion;
                        if (ward == null || !ward.IsValid() || !ward.IsWard() || ward.Distance(lastPlacePos) > 80)
                        {
                            return;
                        }
                        var tick = Variables.TickCount;
                        if (tick - LastInsecJumpTme < 1250)
                        {
                            LastInsecJumpTme = tick;
                        }
                        Insec.IsWardFlash = false;
                        lastPlacePos = new Vector3();
                    };
                GameObjectNotifier<Obj_AI_Minion>.OnCreate += (sender, minion) =>
                    {
                        if (!lastPlacePos.IsValid() || minion.Distance(lastPlacePos) > 80 || !minion.IsAlly
                            || !minion.IsWard() || !W.IsInRange(minion))
                        {
                            return;
                        }
                        var tick = Variables.TickCount;
                        if (tick - LastInsecWardTime < 1250)
                        {
                            LastInsecWardTime = tick;
                        }
                        if (tick - lastPlaceTime < 1250 && W.IsReady() && IsWOne && W.CastOnUnit(minion))
                        {
                            lastW = tick;
                        }
                    };
            }

            internal static void Place(Vector3 pos, int mode = 0)
            {
                if (!CanWardJump)
                {
                    return;
                }
                var ward = Items.GetWardSlot();
                var posPlayer = Player.ServerPosition;
                var posPlace = pos.Distance(posPlayer) < WardRange ? pos : posPlayer.Extend(pos, WardRange);
                Player.Spellbook.CastSpell(ward.SpellSlot, posPlace);
                switch (mode)
                {
                    case 0:
                        lastPlaceTime = Variables.TickCount + 1100;
                        break;
                    case 1:
                        lastPlaceTime = LastInsecWardTime = LastInsecJumpTme = Variables.TickCount;
                        break;
                }
                lastPlacePos = posPlace;
            }

            private static void Jump(Vector3 pos)
            {
                if (!W.IsReady() || !IsWOne || Variables.TickCount - lastW <= 500)
                {
                    return;
                }
                var wardObj =
                    GameObjects.AllyWards.Where(
                        i => i.IsValidTarget(W.Range, false) && i.IsWard() && i.Distance(pos) < 80)
                        .MinOrDefault(i => i.Distance(pos));
                if (wardObj != null && W.CastOnUnit(wardObj))
                {
                    lastW = Variables.TickCount;
                }
            }

            #endregion
        }
    }
}
namespace Valvrave_Sharp.Plugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;
    using LeagueSharp.SDK.Core.Wrappers.Damages;

    using SharpDX;

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class LeeSin : Program
    {
        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.25f, 65, 1800, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            W2 = new Spell(SpellSlot.W, 1200).SetSkillshot(0.05f, 100, 1500, false, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 400);
            E2 = new Spell(SpellSlot.E, 550);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R, 825).SetSkillshot(0.25f, 65, 1200, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.High;

            WardManager.Init();
            Insec.Init();
            var orbwalkMenu = MainMenu.Add(new Menu("Orbwalk", "Orbwalk"));
            {
                orbwalkMenu.KeyBind("Star", "Star Combo", Keys.X);
                orbwalkMenu.Bool("W", "Use W", false);
                orbwalkMenu.Bool("E", "Use E");
                orbwalkMenu.Separator("Q Settings");
                orbwalkMenu.Bool("Q", "Use Q");
                orbwalkMenu.Bool("Q2", "-> Also Q2");
                orbwalkMenu.Bool("QCol", "Smite Collision");
                orbwalkMenu.Separator("R Settings");
                orbwalkMenu.Bool("R", "Use R");
                orbwalkMenu.Bool("RKill", "If Kill Enemy Behind");
                orbwalkMenu.Slider("RCountA", "Or Hit Enemy Behind >=", 1, 1, 4);
                orbwalkMenu.Separator("Sub Settings");
                orbwalkMenu.Bool("Ignite", "Use Ignite");
                orbwalkMenu.Bool("Item", "Use Item");
            }
            var farmMenu = MainMenu.Add(new Menu("Farm", "Farm"));
            {
                farmMenu.Bool("Q", "Use Q1");
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
            var drawMenu = MainMenu.Add(new Menu("Draw", "Draw"));
            {
                drawMenu.Bool("Q", "Q Range", false);
                drawMenu.Bool("W", "W Range");
                drawMenu.Bool("E", "E Range", false);
                drawMenu.Bool("R", "R Range", false);
            }
            MainMenu.KeyBind("FleeW", "Use W To Flee", Keys.C);

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += args =>
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
                };
        }

        #endregion

        #region Properties

        private static bool CanCastInOrbwalk
            =>
                (!MainMenu["Orbwalk"]["Q"] || Variables.TickCount - Q.LastCastAttemptT > 300)
                && (!MainMenu["Orbwalk"]["W"] || Variables.TickCount - W.LastCastAttemptT > 300)
                && (!MainMenu["Orbwalk"]["E"] || Variables.TickCount - E.LastCastAttemptT > 300);

        private static Tuple<int, Obj_AI_Hero> GetMultiR
        {
            get
            {
                var bestCount = 0;
                Obj_AI_Hero bestTarget = null;
                foreach (var target in
                    GameObjects.EnemyHeroes.Where(
                        i =>
                        i.IsValidTarget(R.Range) && i.Health + i.PhysicalShield > Player.GetSpellDamage(i, SpellSlot.R))
                    )
                {
                    var posStart = target.ServerPosition.ToVector2();
                    var posEnd = posStart.Extend(Player.ServerPosition, -R2.Range);
                    R2.UpdateSourcePosition(target.ServerPosition, target.ServerPosition);
                    var listPos = new List<Tuple<Vector2, float>>();
                    foreach (var subTarget in
                        (from enemy in
                             GameObjects.EnemyHeroes.Where(
                                 i => i.IsValidTarget(R2.Range, true, R2.From) && !i.Compare(target))
                         let project = enemy.ServerPosition.ToVector2().ProjectOn(posStart, posEnd)
                         where
                             project.IsOnSegment
                             && enemy.Distance(project.SegmentPoint) <= 1.8 * (enemy.BoundingRadius + R2.Width)
                         select enemy))
                    {
                        var posPred = R2.VPrediction(subTarget, true).CastPosition.ToVector2();
                        if (MainMenu["Orbwalk"]["RKill"]
                            && subTarget.Health + subTarget.PhysicalShield <= GetRDmg(target, subTarget))
                        {
                            var project = posPred.ProjectOn(posStart, posEnd);
                            if (project.IsOnSegment
                                && project.SegmentPoint.Distance(posPred) <= target.BoundingRadius + R2.Width)
                            {
                                return new Tuple<int, Obj_AI_Hero>(-1, target);
                            }
                        }
                        listPos.Add(new Tuple<Vector2, float>(posPred, target.BoundingRadius));
                    }
                    var count = 1 + (from pos in listPos
                                     let vector = pos.Item1
                                     let project = vector.ProjectOn(posStart, posEnd)
                                     where
                                         project.IsOnSegment
                                         && project.SegmentPoint.Distance(vector) <= pos.Item2 + R2.Width
                                     select pos).Count();
                    if (bestCount == 0)
                    {
                        bestCount = count;
                        bestTarget = target;
                    }
                    else if (bestCount < count)
                    {
                        bestCount = count;
                        bestTarget = target;
                    }
                }
                return new Tuple<int, Obj_AI_Hero>(bestCount, bestTarget);
            }
        }

        private static Obj_AI_Base GetQObj
        {
            get
            {
                return
                    GameObjects.EnemyHeroes.Cast<Obj_AI_Base>()
                        .Concat(GameObjects.EnemyMinions.Where(i => i.IsMinion()).Concat(GameObjects.Jungle))
                        .FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
            }
        }

        private static int GetSmiteDmg
            =>
                new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                    Player.Level - 1];

        private static bool IsEOne => E.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsQOne => Q.Instance.SData.Name.ToLower().Contains("one");

        private static bool IsRecentR => Variables.TickCount - R.LastCastAttemptT < 2000;

        private static bool IsWOne => W.Instance.SData.Name.ToLower().Contains("one");

        private static int Passive => Player.GetBuffCount("BlindMonkFlurry");

        #endregion

        #region Methods

        private static bool CanE2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < 0.3 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanQ2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.2 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanR(Obj_AI_Hero target)
        {
            var buff = target.GetBuff("blindmonkrkick");
            return buff != null && buff.EndTime - Game.Time < 0.4 * (buff.EndTime - buff.StartTime);
        }

        private static void Farm()
        {
            if (!MainMenu["Farm"]["Q"] || !Q.IsReady() || !IsQOne)
            {
                return;
            }
            foreach (var pred in
                GameObjects.EnemyMinions.Where(
                    i =>
                    i.IsValidTarget(Q.Range) && i.IsMinion()
                    && Q.GetHealthPrediction(i) <= Player.GetSpellDamage(i, SpellSlot.Q)
                    && (!i.InAutoAttackRange() ? Q.GetHealthPrediction(i) > 0 : i.Health > Player.GetAutoAttackDamage(i)))
                    .OrderByDescending(i => i.MaxHealth)
                    .Select(i => Q.VPrediction(i))
                    .Where(i => i.Hitchance >= Q.MinHitChance)
                    .OrderByDescending(i => i.Hitchance))
            {
                Q.Cast(pred.CastPosition);
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
                            i =>
                            i.IsMinion() || i.CharData.BaseSkinName == "jarvanivstandard"
                            || i.CharData.BaseSkinName == "teemomushroom" || i.CharData.BaseSkinName == "kalistaspawn"
                            || i.CharData.BaseSkinName == "illaoiminion").Concat(GameObjects.AllyWards))
                    .Where(
                        i =>
                        i.IsValidTarget(W.Range, false)
                        && i.Distance(Player.ServerPosition.Extend(pos, Math.Min(Player.Distance(pos), W.Range)))
                        <= (isStar ? R.Range - 50 : 250))
                    .MinOrDefault(i => i.Distance(pos));
            if (objJump != null)
            {
                W.CastOnUnit(objJump);
            }
            else
            {
                WardManager.Place(pos);
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

        private static double GetRDmg(Obj_AI_Hero kickTarget, Obj_AI_Hero hitTarget)
        {
            return Player.CalculateDamage(
                hitTarget,
                DamageType.Physical,
                new[] { 200, 400, 600 }[R.Level - 1] + 2 * Player.FlatPhysicalDamageMod
                + new[] { 0.12, 0.15, 0.18 }[R.Level - 1] * kickTarget.BonusHealth);
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
                    var targets =
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            i.IsValidTarget(Q.Range)
                            && (i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.Q)
                                || (i.Health + i.PhysicalShield
                                    <= GetQ2Dmg(i, Player.GetSpellDamage(i, SpellSlot.Q))
                                    + Player.GetAutoAttackDamage(i) && Player.Mana - Q.Instance.ManaCost >= 30)))
                            .ToList();
                    if (targets.Count > 0
                        && targets.Select(i => Q.VPrediction(i))
                               .Where(i => i.Hitchance >= Q.MinHitChance)
                               .Any(i => Q.Cast(i.CastPosition)))
                    {
                        return;
                    }
                }
                else if (
                    GameObjects.EnemyHeroes.Any(
                        i =>
                        i.IsValidTarget(Q2.Range) && HaveQ(i)
                        && i.Health + i.PhysicalShield
                        <= Player.GetSpellDamage(i, SpellSlot.Q, Damage.DamageStage.SecondCast)
                        + Player.GetAutoAttackDamage(i)))
                {
                    Q.Cast();
                }
                if (MainMenu["KillSteal"]["E"] && E.IsReady() && IsEOne
                    && GameObjects.EnemyHeroes.Any(
                        i =>
                        i.IsValidTarget(E.Range) && i.Health + i.MagicalShield <= Player.GetSpellDamage(i, SpellSlot.E)))
                {
                    E.Cast();
                }
                if (MainMenu["KillSteal"]["R"] && R.IsReady())
                {
                    var targets =
                        GameObjects.EnemyHeroes.Where(
                            i =>
                            i.IsValidTarget(R.Range) && MainMenu["KillSteal"]["RCast" + i.ChampionName]
                            && (i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.R)
                                || (MainMenu["KillSteal"]["Q"] && Q.IsReady() && !IsQOne && HaveQ(i)
                                    && i.Health + i.PhysicalShield
                                    <= GetQ2Dmg(i, Player.GetSpellDamage(i, SpellSlot.R))
                                    + Player.GetAutoAttackDamage(i)))).ToList();
                    if (targets.Count > 0)
                    {
                        R.CastOnUnit(TargetSelector.GetTarget(targets));
                    }
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
            if (MainMenu["Orbwalk"]["R"] && R.IsReady())
            {
                var multiR = GetMultiR;
                if ((multiR.Item1 == -1 || multiR.Item1 >= MainMenu["Orbwalk"]["RCountA"] + 1) && multiR.Item2 != null)
                {
                    R.CastOnUnit(multiR.Item2);
                }
            }
            switch (Orbwalker.ActiveMode)
            {
<<<<<<< HEAD
                case OrbwalkingMode.Combo:
<<<<<<< HEAD
                    Orbwalk();
=======
                    Combo();
>>>>>>> adc404e28daddc8ad6cfcc3b2f2dc7db70547f3c
=======
                case OrbwalkerMode.Orbwalk:
                    Orbwalk();
>>>>>>> origin/master
                    break;
                case OrbwalkerMode.LastHit:
                    Farm();
                    break;
                case OrbwalkerMode.None:
                    if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                    {
                        Orbwalker.MoveOrder(Game.CursorPos);
                        Flee(Game.CursorPos);
                    }
                    else if (MainMenu["Orbwalk"]["Star"].GetValue<MenuKeyBind>().Active)
                    {
                        Star();
                    }
                    else if (MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active)
                    {
                        Insec.Start();
                    }
                    break;
            }
        }

        private static void Orbwalk()
        {
            if (Orbwalker.GetTarget(OrbwalkingMode.Combo) == null || (!Orbwalker.CanAttack && Orbwalker.CanMove))
            {
                if (MainMenu["Orbwalk"]["W"] && W.IsReady() && CanCastInOrbwalk && Passive == -1
                    && Orbwalker.GetTarget(OrbwalkingMode.Combo) != null)
                {
                    W.Cast();
                }
                if (MainMenu["Orbwalk"]["Q"] && Q.IsReady() && CanCastInOrbwalk)
                {
                    if (IsQOne)
                    {
                        var target = Q.GetTarget(Q.Width);
                        if (target != null)
                        {
                            var pred = Q.VPrediction(target);
                            if (pred.Hitchance == HitChance.Collision)
                            {
                                if (MainMenu["Orbwalk"]["QCol"] && Smite.IsReady()
                                    && !pred.CollisionObjects.All(i => i.IsMe))
                                {
                                    var col = pred.CollisionObjects.Cast<Obj_AI_Minion>().ToList();
                                    if (col.Count == 1
                                        && col.Any(i => i.Health <= GetSmiteDmg && Player.Distance(i) < SmiteRange)
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
                    }
                    else if (MainMenu["Orbwalk"]["Q2"])
                    {
                        var target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
                        if (target != null)
                        {
                            if (CanQ2(target) || (!R.IsReady() && IsRecentR && CanR(target))
                                || target.Health + target.PhysicalShield
                                <= Player.GetSpellDamage(target, SpellSlot.Q, Damage.DamageStage.SecondCast)
                                + Player.GetAutoAttackDamage(target)
                                || Player.Distance(target) > target.GetRealAutoAttackRange() + 100 || Passive == -1)
                            {
                                Q.Cast();
                            }
                        }
                        else if (GetQObj != null)
                        {
                            var targetQ2 = Q2.GetTarget(200);
                            if (targetQ2 != null && GetQObj.Distance(targetQ2) < Player.Distance(GetQObj)
                                && !targetQ2.InAutoAttackRange())
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
                if (MainMenu["Orbwalk"]["E"] && E.IsReady() && CanCastInOrbwalk)
                {
                    if (IsEOne)
                    {
                        if (E.GetTarget() != null && Player.Mana >= 70)
                        {
                            E.Cast();
                        }
                    }
                    else
                    {
                        var e2Target =
                            GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                        if (e2Target.Any(i => CanE2(i) || Player.Distance(i) > i.GetRealAutoAttackRange() + 50)
                            || e2Target.Count > 2 || Passive == -1)
                        {
                            E.Cast();
                        }
                    }
                }
            }
<<<<<<< HEAD
=======
            if (MainMenu["Orbwalk"]["W"] && W.IsReady() && CanCastInOrbwalk
                && Variables.TickCount - W.LastCastAttemptT >= 300 && !E.IsReady() && Passive == -1
                && Orbwalker.GetTarget(OrbwalkerMode.Orbwalk) != null && W.Cast())
            {
                return;
            }
>>>>>>> adc404e28daddc8ad6cfcc3b2f2dc7db70547f3c
            var subTarget = W.GetTarget();
            if (MainMenu["Orbwalk"]["Item"])
            {
                UseItem(subTarget);
            }
            if (subTarget == null)
            {
                return;
            }
            if (MainMenu["Orbwalk"]["Ignite"] && Ignite.IsReady() && subTarget.HealthPercent < 30
                && Player.Distance(subTarget) <= IgniteRange)
            {
                Player.Spellbook.CastSpell(Ignite, subTarget);
            }
        }

        private static void Star()
        {
            var target = Q.GetTarget(Q.Width);
            if (!IsQOne)
            {
                target = GameObjects.EnemyHeroes.FirstOrDefault(i => i.IsValidTarget(Q2.Range) && HaveQ(i));
            }
            if (!Q.IsReady())
            {
                target = W.GetTarget();
            }
            Orbwalker.Orbwalk(target);
            if (target == null)
            {
                return;
            }
            if (Q.IsReady())
            {
                if (IsQOne)
                {
                    var pred = Q.VPrediction(target);
                    if (pred.Hitchance == HitChance.Collision)
                    {
                        if (MainMenu["Orbwalk"]["QCol"] && Smite.IsReady() && !pred.CollisionObjects.All(i => i.IsMe))
                        {
                            var col = pred.CollisionObjects.Cast<Obj_AI_Minion>().ToList();
                            if (col.Count == 1
                                && col.Any(i => i.Health <= GetSmiteDmg && Player.Distance(i) < SmiteRange)
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
                else if (HaveQ(target)
                         && (target.Health + target.PhysicalShield
                             <= Player.GetSpellDamage(target, SpellSlot.Q, Damage.DamageStage.SecondCast)
                             + Player.GetAutoAttackDamage(target) || (!R.IsReady() && IsRecentR && CanR(target))))
                {
                    Q.Cast();
                }
            }
            if (E.CanCast(target) && IsEOne && (!HaveQ(target) || Player.Mana >= 70))
            {
                E.Cast();
            }
            if (!R.IsReady() || !Q.IsReady() || IsQOne || !HaveQ(target))
            {
                return;
            }
            if (R.IsInRange(target))
            {
                R.CastOnUnit(target);
            }
            else if (Player.Distance(target) <= W.Range + R.Range - 150 && Player.Mana >= 70)
            {
                Flee(W2.VPrediction(target).CastPosition.Extend(Player.ServerPosition, R.Range / 2), true);
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
            if (Youmuu.IsReady && Player.CountEnemy(W.Range + E.Range) > 0)
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
            if (Titanic.IsReady && Player.CountEnemy(Player.GetRealAutoAttackRange()) > 0)
            {
                Titanic.Cast();
            }
        }

        #endregion

        private static class Insec
        {
            #region Static Fields

            private static Obj_AI_Hero insecTarget;

            private static int lastSettingPos, lastGapClose, lastRFlash, lastFlash, lastJump;

            private static Vector3 posSetting;

            #endregion

            #region Properties

            private static bool IsDoingRFlash
                => Variables.TickCount - lastRFlash < 5000 && Variables.TickCount - R.LastCastAttemptT < 5000;

            private static bool IsReady
                =>
                    (WardManager.CanWardJump || (MainMenu["Insec"]["Flash"] && Flash.IsReady()) || IsRecent)
                    && R.IsReady();

            private static bool IsRecent
                =>
                    Variables.TickCount - lastJump < 5000
                    || (MainMenu["Insec"]["Flash"] && Variables.TickCount - lastFlash < 5000)
                    || Variables.TickCount - WardManager.LastCreateTime < 5000;

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
                    insecMenu.Bool("Flash", "Use Flash If Can't WardJump");
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
                        insecTarget = Q2.GetTarget();
                        if (Variables.TickCount - lastSettingPos > 10000 && lastSettingPos > 0)
                        {
                            posSetting = new Vector3();
                            lastSettingPos = 0;
                            TargetSelector.SelectedTarget = null;
                        }
                        if (IsDoingRFlash && Flash.IsReady() && insecTarget != null)
                        {
                            Player.Spellbook.CastSpell(
                                Flash,
                                insecTarget.Position.Extend(ExpectedEndPosition(insecTarget), -DistBehind(insecTarget)));
                        }
                    };
                Drawing.OnDraw += args =>
                    {
                        if (Player.IsDead || !MainMenu["Insec"]["Line"] || R.Level == 0 || insecTarget == null
                            || !IsReady)
                        {
                            return;
                        }
                        Drawing.DrawCircle(insecTarget.Position, insecTarget.BoundingRadius * 1.5f, Color.BlueViolet);
                        Drawing.DrawLine(
                            Drawing.WorldToScreen(insecTarget.Position),
                            Drawing.WorldToScreen(ExpectedEndPosition(insecTarget)),
                            2,
                            Color.BlueViolet);
                    };
                Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                    {
                        if (!sender.IsMe)
                        {
                            return;
                        }
                        if (MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active)
                        {
                            if (args.SData.Name == "summonerflash")
                            {
                                lastFlash = Variables.TickCount;
                            }
                            if (args.Slot == SpellSlot.W && args.SData.Name.ToLower().Contains("one")
                                && Variables.TickCount - lastJump < 1000)
                            {
                                lastJump = Variables.TickCount;
                            }
                        }
                        if (args.Slot == SpellSlot.R && IsDoingRFlash && Flash.IsReady() && insecTarget != null)
                        {
                            var target = args.Target as Obj_AI_Hero;
                            if (target != null && target.Compare(insecTarget))
                            {
                                Player.Spellbook.CastSpell(
                                    Flash,
                                    target.Position.Extend(ExpectedEndPosition(target), -DistBehind(target)));
                            }
                        }
                    };
            }

            internal static void Start()
            {
                var target = insecTarget;
                if (Orbwalker.CanMove && Variables.TickCount - lastGapClose > 250)
                {
                    if (target != null && IsReady
                        && Player.Distance(ExpectedEndPosition(target)) > target.Distance(ExpectedEndPosition(target)))
                    {
                        Orbwalker.MoveOrder(
                            target.ServerPosition.Extend(ExpectedEndPosition(target), -DistBehind(target)));
                    }
                    else
                    {
                        Orbwalker.MoveOrder(Game.CursorPos);
                    }
                }
                if (target == null || !IsReady)
                {
                    return;
                }
                GapByQ(target);
                if (!IsRecent && (!Player.HasBuff("blindmonkqtwodash") || R.IsInRange(target)))
                {
                    if (Player.Distance(target) < WardManager.WardRange - DistBehind(target))
                    {
                        if (MainMenu["Insec"]["PriorFlash"])
                        {
                            if (MainMenu["Insec"]["Flash"] && Flash.IsReady())
                            {
                                GapByFlash(target);
                            }
                            else if (WardManager.CanWardJump)
                            {
                                GapByWardJump(target);
                            }
                        }
                        else
                        {
                            if (WardManager.CanWardJump)
                            {
                                GapByWardJump(target);
                            }
                            else if (MainMenu["Insec"]["Flash"] && Flash.IsReady())
                            {
                                GapByFlash(target);
                            }
                        }
                    }
                    else if (Player.Distance(target) < WardManager.WardRange + FlashRange - DistBehind(target)
                             && MainMenu["Insec"]["Flash"] && Flash.IsReady() && MainMenu["Insec"]["FlashJump"])
                    {
                        Flee(target.ServerPosition);
                    }
                }
                if (R.IsInRange(target)
                    && Player.Distance(ExpectedEndPosition(target)) > target.Distance(ExpectedEndPosition(target)))
                {
                    var project =
                        target.ServerPosition.Extend(Player.ServerPosition, -R2.Range)
                            .ToVector2()
                            .ProjectOn(
                                target.ServerPosition.ToVector2(),
                                ExpectedEndPosition(target)
                                    .Extend(target.ServerPosition, -(R2.Range * 0.5f))
                                    .ToVector2());
                    if (project.IsOnSegment
                        && project.SegmentPoint.Distance(ExpectedEndPosition(target)) <= R2.Range * 0.5f)
                    {
                        R.CastOnUnit(target);
                    }
                }
            }

            private static float DistBehind(Obj_AI_Hero target)
            {
                return
                    Math.Min(
                        (Player.BoundingRadius + target.BoundingRadius + 50) * (100 + MainMenu["Insec"]["Dist"]) / 100,
                        R.Range);
            }

            private static Vector3 EndPosition(Obj_AI_Hero target)
            {
                return target.ServerPosition.Extend(ExpectedEndPosition(target), R2.Range);
            }

            private static Vector3 ExpectedEndPosition(Obj_AI_Hero target)
            {
                if (posSetting.IsValid() && target.Distance(posSetting) <= R2.Range + 500)
                {
                    return posSetting;
                }
                switch (MainMenu["Insec"]["Mode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        var turret =
                            GameObjects.AllyTurrets.Where(
                                i =>
                                !i.IsDead && target.Distance(i) <= R2.Range + 500
                                && i.Distance(target) - R2.Range <= 950 && i.Distance(target) > 400)
                                .MinOrDefault(i => i.Distance(Player));
                        if (turret != null)
                        {
                            return turret.ServerPosition;
                        }
                        var hero =
                            GameObjects.AllyHeroes.Where(
                                i =>
                                !i.IsMe && target.Distance(i) <= R2.Range + 500 && i.HealthPercent > 10
                                && i.Distance(target) > 400).MinOrDefault(TargetSelector.GetPriority);
                        if (hero != null)
                        {
                            return hero.ServerPosition;
                        }
                        break;
                    case 1:
                        return Game.CursorPos;
                    case 2:
                        return Player.ServerPosition;
                }
                return Player.ServerPosition;
            }

            private static void GapByFlash(Obj_AI_Hero target)
            {
                switch (MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        GapByRFlash(target);
                        break;
                    case 1:
                        GapByFlashR(target);
                        break;
                    case 2:
                        if (target.Distance(Player) <= R.Range * 1.25)
                        {
                            GapByRFlash(target);
                        }
                        else
                        {
                            GapByFlashR(target);
                        }
                        break;
                }
            }

            private static void GapByFlashR(Obj_AI_Hero target)
            {
                var posGap = target.ServerPosition.Extend(ExpectedEndPosition(target), -DistBehind(target));
                var posFlash = Player.ServerPosition.Extend(posGap, FlashRange);
                if (target.Distance(posGap) > R.Range || target.Distance(posFlash) <= 50
                    || target.Distance(posFlash) >= ExpectedEndPosition(target).Distance(posFlash)
                    || target.Distance(posGap) >= ExpectedEndPosition(target).Distance(posGap))
                {
                    return;
                }
                if (Orbwalker.CanMove)
                {
                    lastGapClose = Variables.TickCount;
                }
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                TargetSelector.SelectedTarget = target;
                Player.Spellbook.CastSpell(Flash, posGap);
            }

            private static void GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Orbwalk"]["Q"] || !Q.IsReady()
                    || Player.Distance(target)
                    < WardManager.WardRange
                    + (MainMenu["Insec"]["Flash"] && Flash.IsReady() && MainMenu["Insec"]["FlashJump"] ? FlashRange : 0)
                    - DistBehind(target))
                {
                    return;
                }
                if (IsQOne)
                {
                    var pred = Q.VPrediction(target);
                    if (pred.Hitchance == HitChance.Collision || pred.Hitchance == HitChance.OutOfRange)
                    {
                        if (pred.Hitchance == HitChance.Collision && MainMenu["Insec"]["QCol"] && Smite.IsReady()
                            && !pred.CollisionObjects.All(i => i.IsMe))
                        {
                            var col = pred.CollisionObjects.Cast<Obj_AI_Minion>().ToList();
                            if (col.Count == 1
                                && col.Any(i => i.Health <= GetSmiteDmg && Player.Distance(i) < SmiteRange)
                                && Player.Spellbook.CastSpell(Smite, col.First()))
                            {
                                Q.Cast(pred.CastPosition);
                            }
                        }
                        if (MainMenu["Insec"]["QObj"])
                        {
                            foreach (var predNear in
                                GameObjects.EnemyHeroes.Where(i => i.NetworkId != target.NetworkId)
                                    .Cast<Obj_AI_Base>()
                                    .Concat(
                                        GameObjects.EnemyMinions.Where(i => i.IsMinion()).Concat(GameObjects.Jungle))
                                    .Where(
                                        i =>
                                        i.IsValidTarget(Q.Range) && i.Health > Player.GetSpellDamage(i, SpellSlot.Q)
                                        && Player.Distance(target) > i.Distance(target)
                                        && i.Distance(target) < WardManager.WardRange - DistBehind(target) - 80)
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
                else if ((WardManager.CanWardJump && Player.Mana >= 80)
                         || (MainMenu["Insec"]["Flash"] && Flash.IsReady()))
                {
                    if (HaveQ(target))
                    {
                        TargetSelector.SelectedTarget = target;
                        Q.Cast();
                    }
                    else if (GetQObj != null
                             && GetQObj.Distance(target) < WardManager.WardRange - DistBehind(target) - 80)
                    {
                        TargetSelector.SelectedTarget = target;
                        Q.Cast();
                    }
                }
            }

            private static void GapByRFlash(Obj_AI_Hero target)
            {
                if (!R.IsInRange(target))
                {
                    return;
                }
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                lastRFlash = Variables.TickCount;
                TargetSelector.SelectedTarget = target;
                R.CastOnUnit(target);
            }

            private static void GapByWardJump(Obj_AI_Hero target)
            {
                var posGap = W2.VPrediction(target)
                    .CastPosition.Extend(ExpectedEndPosition(target), -DistBehind(target));
                if (Player.Distance(posGap) > WardManager.WardRange || target.Distance(posGap) > R.Range
                    || target.Distance(posGap) >= ExpectedEndPosition(target).Distance(posGap))
                {
                    return;
                }
                if (Orbwalker.CanMove)
                {
                    lastGapClose = Variables.TickCount;
                    Orbwalker.MoveOrder(
                        posGap.Extend(ExpectedEndPosition(target), -(DistBehind(target) + Player.BoundingRadius / 2)));
                }
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                lastJump = Variables.TickCount;
                TargetSelector.SelectedTarget = target;
                var objJump =
                    GameObjects.AllyHeroes.Where(i => !i.IsMe)
                        .Cast<Obj_AI_Base>()
                        .Concat(
                            GameObjects.AllyMinions.Where(
                                i =>
                                i.IsMinion() || i.CharData.BaseSkinName == "jarvanivstandard"
                                || i.CharData.BaseSkinName == "teemomushroom"
                                || i.CharData.BaseSkinName == "kalistaspawn"
                                || i.CharData.BaseSkinName == "illaoiminion").Concat(GameObjects.AllyWards))
                        .Where(i => i.IsValidTarget(W.Range, false) && i.Distance(posGap) <= 250)
                        .MinOrDefault(i => i.Distance(posGap));
                if (objJump != null && objJump.Distance(target) < objJump.Distance(ExpectedEndPosition(target)))
                {
                    W.CastOnUnit(objJump);
                }
                else
                {
                    WardManager.Place(posGap, true);
                }
            }

            #endregion
        }

        private static class WardManager
        {
            #region Constants

            internal const int WardRange = 600;

            #endregion

            #region Static Fields

            internal static int LastCreateTime;

            private static Vector3 lastJumpPos;

            private static int lastJumpTime;

            #endregion

            #region Properties

            internal static bool CanWardJump => CanCastWard && W.IsReady() && IsWOne;

            private static bool CanCastWard => Variables.TickCount - lastJumpTime > 1250 && Items.GetWardSlot() != null;

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
                            lastJumpPos = new Vector3();
                        }
                    };
                GameObject.OnCreate += (sender, args) =>
                    {
                        var ward = sender as Obj_AI_Minion;
                        if (ward == null || ward.IsEnemy || !ward.GetMinionType().HasFlag(MinionTypes.Ward))
                        {
                            return;
                        }
                        if (MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active
                            && Variables.TickCount - LastCreateTime < 1000)
                        {
                            LastCreateTime = Variables.TickCount;
                        }
                        if (IsTryingToJump && W.IsReady() && IsWOne && ward.Distance(lastJumpPos) < 80)
                        {
                            W.CastOnUnit(ward);
                        }
                    };
            }

            internal static void Place(Vector3 pos, bool isInsecByWard = false)
            {
                if (!CanWardJump)
                {
                    return;
                }
                var posEnd = Player.ServerPosition.Extend(pos, Math.Min(Player.Distance(pos), WardRange));
                var ward = Items.GetWardSlot();
                if (ward == null)
                {
                    return;
                }
                Player.Spellbook.CastSpell(ward.SpellSlot, posEnd);
                if (MainMenu["Insec"]["Insec"].GetValue<MenuKeyBind>().Active && isInsecByWard)
                {
                    LastCreateTime = Variables.TickCount;
                }
                lastJumpPos = posEnd;
                lastJumpTime = Variables.TickCount;
                if (MainMenu["FleeW"].GetValue<MenuKeyBind>().Active)
                {
                    lastJumpTime += 1100;
                }
            }

            private static void Jump(Vector3 pos)
            {
                if (!W.IsReady() || !IsWOne)
                {
                    return;
                }
                var wardObj =
                    GameObjects.AllyWards.Where(i => i.IsValidTarget(W.Range, false)).MinOrDefault(i => i.Distance(pos));
                if (wardObj != null && wardObj.Distance(pos) < 250)
                {
                    W.CastOnUnit(wardObj);
                }
            }

            #endregion
        }
    }
}
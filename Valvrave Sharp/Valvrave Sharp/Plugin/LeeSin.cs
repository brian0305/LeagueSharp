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
    using LeagueSharp.SDK.Core.Wrappers.Damages;
    using LeagueSharp.SDK.Core.Wrappers.Spells;
    using LeagueSharp.SDK.Core.Wrappers.TargetSelector.Modes;

    using SharpDX;

    using Valvrave_Sharp.Core;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    #endregion

    internal class LeeSin : Program
    {
        #region Static Fields

        private static readonly List<string> SpecialPet = new List<string>
                                                              { "jarvanivstandard", "teemomushroom", "kalistaspawn" };

        #endregion

        #region Constructors and Destructors

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100).SetSkillshot(0.25f, 60, 1800, true, SkillshotType.SkillshotLine);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 425);
            E2 = new Spell(SpellSlot.E, 570);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R, 825).SetSkillshot(0.4f, 75, 600, false, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = W.DamageType = R.DamageType = DamageType.Physical;
            E.DamageType = DamageType.Magical;
            Q.MinHitChance = HitChance.High;

            WardManager.Init();
            Insec.Init();
            var comboMenu = MainMenu.Add(new Menu("Combo", "Combo"));
            {
                comboMenu.KeyBind("Star", "Star Combo", Keys.X);
                comboMenu.Bool("W", "Use W", false);
                comboMenu.Bool("E", "Use E");
                comboMenu.Separator("Q Settings");
                comboMenu.Bool("Q", "Use Q");
                comboMenu.Bool("Q2", "-> Also Q2");
                comboMenu.Bool("QCol", "Smite Collision");
                comboMenu.Separator("R Settings");
                comboMenu.Bool("R", "Use R");
                comboMenu.Bool("RKill", "If Kill Enemy Behind");
                comboMenu.Slider("RCountA", "Or Hit Enemy Behind >=", 1, 1, 4);
                comboMenu.Separator("Sub Settings");
                comboMenu.Bool("Ignite", "Use Ignite");
                comboMenu.Bool("Item", "Use Item");
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
            Drawing.OnDraw += OnDraw;
        }

        #endregion

        #region Properties

        private static bool CanCastInOrbwalk
            =>
                (!MainMenu["Combo"]["Q"] || Variables.TickCount - Q.LastCastAttemptT > 200)
                && (!MainMenu["Combo"]["W"] || Variables.TickCount - W.LastCastAttemptT > 150)
                && (!MainMenu["Combo"]["E"] || Variables.TickCount - E.LastCastAttemptT > 200);

        private static Tuple<int, Obj_AI_Hero> GetMultiR
        {
            get
            {
                var bestHit = 0;
                Obj_AI_Hero bestTarget = null;
                foreach (var kickTarget in
                    Variables.TargetSelector.GetTargets(R.Range, R.DamageType)
                        .Where(i => i.Health + i.PhysicalShield > Player.GetSpellDamage(i, SpellSlot.R)))
                {
                    var radius = kickTarget.BoundingRadius + R2.Width;
                    var posStart = kickTarget.ServerPosition.ToVector2();
                    var posEnd = posStart.Extend(Player.ServerPosition, -R2.Range);
                    var hitList =
                        (from enemy in
                             GameObjects.EnemyHeroes.Where(
                                 i =>
                                 i.IsValidTarget(R2.Range, true, kickTarget.ServerPosition) && !i.Compare(kickTarget))
                         let project = enemy.ServerPosition.ToVector2().ProjectOn(posStart, posEnd)
                         where project.IsOnSegment && enemy.Distance(project.SegmentPoint) <= 1.8 * radius
                         select enemy).ToList();
                    if (hitList.Count == 0)
                    {
                        break;
                    }
                    var posHitList = new List<Vector2>();
                    foreach (var hitTarget in hitList)
                    {
                        R2.UpdateSourcePosition(kickTarget.ServerPosition, kickTarget.ServerPosition);
                        var pred = R2.VPrediction(hitTarget);
                        if (pred.Hitchance < HitChance.High)
                        {
                            continue;
                        }
                        var posPred = pred.CastPosition.ToVector2();
                        if (MainMenu["Combo"]["RKill"]
                            && hitTarget.Health + hitTarget.PhysicalShield <= GetRColDmg(kickTarget, hitTarget))
                        {
                            var project = posPred.ProjectOn(posStart, posEnd);
                            if (project.IsOnSegment && project.SegmentPoint.Distance(posPred) <= radius)
                            {
                                return new Tuple<int, Obj_AI_Hero>(-1, kickTarget);
                            }
                        }
                        posHitList.Add(posPred);
                    }
                    var hit = 1 + (from pos in posHitList
                                   let project = pos.ProjectOn(posStart, posEnd)
                                   where project.IsOnSegment && project.SegmentPoint.Distance(pos) <= radius
                                   select pos).Count();
                    if (bestHit == 0 || bestHit < hit)
                    {
                        bestHit = hit;
                        bestTarget = kickTarget;
                    }
                }
                return new Tuple<int, Obj_AI_Hero>(bestHit, bestTarget);
            }
        }

        private static Obj_AI_Base GetQObj
        {
            get
            {
                return
                    GameObjects.EnemyHeroes.Cast<Obj_AI_Base>()
                        .Concat(
                            GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet()).Concat(GameObjects.Jungle))
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
            return buff != null && buff.EndTime - Game.Time < 0.4 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanQ2(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.3 * (buff.EndTime - buff.StartTime);
        }

        private static bool CanR(Obj_AI_Hero target)
        {
            var buff = target.GetBuff("blindmonkrkick");
            return buff != null && buff.EndTime - Game.Time < 0.3 * (buff.EndTime - buff.StartTime);
        }

        private static void CastQSmite(Obj_AI_Hero target)
        {
            var pred = Q.VPrediction(target);
            if (pred.Hitchance == HitChance.Collision)
            {
                if (MainMenu["Combo"]["QCol"] && Smite.IsReady() && !pred.CollisionObjects.All(i => i.IsMe))
                {
                    var col = pred.CollisionObjects.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
                    if (col.Count == 1 && col.Any(i => i.Health <= GetSmiteDmg && Player.Distance(i) < SmiteRange)
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

        private static void Combo()
        {
            if (MainMenu["Combo"]["Q"] && Q.IsReady() && CanCastInOrbwalk)
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget(Q.Width);
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
                        if (targetQ2 != null && GetQObj.Distance(targetQ2) < Player.Distance(targetQ2)
                            && !targetQ2.InAutoAttackRange())
                        {
                            Q.Cast();
                        }
                    }
                }
            }
            if (MainMenu["Combo"]["E"] && E.IsReady() && CanCastInOrbwalk)
            {
                if (IsEOne)
                {
                    if (Player.Mana >= 70)
                    {
                        if (E.GetTarget() != null)
                        {
                            E.Cast();
                        }
                    }
                }
                else
                {
                    var e2Target = GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(E2.Range) && HaveE(i)).ToList();
                    if (e2Target.Any(i => CanE2(i) || Player.Distance(i) > i.GetRealAutoAttackRange() + 50)
                        || e2Target.Count > 2 || Passive == -1)
                    {
                        E.Cast();
                    }
                }
            }
            if (MainMenu["Combo"]["W"] && W.IsReady() && CanCastInOrbwalk && Passive == -1
                && Variables.Orbwalker.GetTarget() != null)
            {
                W.Cast();
            }
            var subTarget = W.GetTarget();
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
                        && i.Distance(Player.ServerPosition.Extend(pos, Math.Min(Player.Distance(pos), W.Range)))
                        <= (isStar ? R.Range - 50 : 250))
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
            return Player.GetSpellDamage(hitTarget, SpellSlot.R)
                   + Player.CalculateDamage(
                       hitTarget,
                       DamageType.Physical,
                       new[] { 0.12, 0.15, 0.18 }[R.Level - 1] * kickTarget.MaxHealth);
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
                    var target = Q.GetTarget(Q.Width);
                    if (target != null
                        && (target.Health + target.PhysicalShield <= Player.GetSpellDamage(target, SpellSlot.Q)
                            || (target.Health + target.PhysicalShield
                                <= GetQ2Dmg(target, Player.GetSpellDamage(target, SpellSlot.Q))
                                + Player.GetAutoAttackDamage(target) && Player.Mana - Q.Instance.ManaCost >= 30)))
                    {
                        var pred = Q.VPrediction(
                            target,
                            false,
                            CollisionableObjects.Heroes | CollisionableObjects.Minions | CollisionableObjects.YasuoWall);
                        if (pred.Hitchance >= Q.MinHitChance)
                        {
                            Q.Cast(pred.CastPosition);
                        }
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
                if (MainMenu["KillSteal"]["E"] && E.IsReady() && IsEOne)
                {
                    var target = E.GetTarget();
                    if (target != null
                        && target.Health + target.MagicalShield <= Player.GetSpellDamage(target, SpellSlot.E))
                    {
                        E.Cast();
                    }
                }
                if (MainMenu["KillSteal"]["R"] && R.IsReady())
                {
                    var targetList =
                        Variables.TargetSelector.GetTargets(R.Range, R.DamageType)
                            .Where(i => MainMenu["KillSteal"]["RCast" + i.ChampionName])
                            .ToList();
                    var targetR =
                        targetList.FirstOrDefault(
                            i => i.Health + i.PhysicalShield <= Player.GetSpellDamage(i, SpellSlot.R));
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
                                <= GetQ2Dmg(i, Player.GetSpellDamage(i, SpellSlot.R)) + Player.GetAutoAttackDamage(i));
                        if (targetQ2R != null)
                        {
                            R.CastOnUnit(targetQ2R);
                        }
                    }
                }
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
                    && Q.GetHealthPrediction(i) <= Player.GetSpellDamage(i, SpellSlot.Q)
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
                if ((multiR.Item1 == -1 || multiR.Item1 >= MainMenu["Combo"]["RCountA"] + 1) && multiR.Item2 != null)
                {
                    R.CastOnUnit(multiR.Item2);
                }
            }
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
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
                        Insec.Start();
                    }
                    break;
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
                             <= Player.GetSpellDamage(target, SpellSlot.Q, Damage.DamageStage.SecondCast)
                             + Player.GetAutoAttackDamage(target) || (!R.IsReady() && IsRecentR && CanR(target))))
                {
                    Q.Cast();
                }
            }
            if (E2.CanCast(target) && IsEOne && (!HaveQ(target) || Player.Mana >= 70) && E.IsInRange(target))
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
            else if (Player.Distance(target) <= W.Range + R.Range - 100 && Player.Mana >= 70)
            {
                Flee(
                    target.ServerPosition.ToVector2()
                        .Extend(Player.ServerPosition, R.Range / 2)
                        .ToVector3(target.ServerPosition.Z),
                    true);
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

            private static Obj_AI_Hero insecTarget;

            private static int lastSettingPos, lastGapClose, lastRFlash, lastFlash, lastJump;

            private static Vector2 posSetting;

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
                        insecTarget = Q.GetTarget(-100);
                        if ((MainMenu["Insec"]["Q"] && Q.IsReady()) || GetQObj != null)
                        {
                            insecTarget = Q2.GetTarget(FlashRange);
                        }
                        if (lastSettingPos > 0 && Variables.TickCount - lastSettingPos > 10000)
                        {
                            posSetting = new Vector2();
                            lastSettingPos = 0;
                            Variables.TargetSelector.SetTarget(null);
                        }
                        if (lastGapClose > 0 && Variables.TickCount - lastGapClose > 1000 && !R.IsReady())
                        {
                            lastGapClose = 0;
                        }
                        if (IsDoingRFlash && Flash.IsReady() && insecTarget != null)
                        {
                            Player.Spellbook.CastSpell(
                                Flash,
                                insecTarget.ServerPosition.ToVector2()
                                    .Extend(ExpectedEndPosition(insecTarget), -DistBehind(insecTarget))
                                    .ToVector3(insecTarget.ServerPosition.Z));
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
                            Drawing.WorldToScreen(
                                ExpectedEndPosition(insecTarget).ToVector3(insecTarget.ServerPosition.Z)),
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
                                    target.ServerPosition.ToVector2()
                                        .Extend(ExpectedEndPosition(target), -DistBehind(target))
                                        .ToVector3(target.ServerPosition.Z));
                            }
                        }
                    };
            }

            internal static void Start()
            {
                var target = insecTarget;
                if (Variables.Orbwalker.CanMove() && Variables.TickCount - lastGapClose > 250)
                {
                    if (target != null && lastGapClose > 0 && IsReady
                        && Player.Distance(ExpectedEndPosition(target)) > target.Distance(ExpectedEndPosition(target)))
                    {
                        Variables.Orbwalker.Move(
                            target.ServerPosition.Extend(ExpectedEndPosition(target), -DistBehind(target)));
                    }
                    else
                    {
                        Variables.Orbwalker.Move(Game.CursorPos);
                    }
                }
                if (target == null || !IsReady)
                {
                    return;
                }
                if (!IsRecent)
                {
                    var checkFlash = GapCheck(target, true);
                    var checkJump = GapCheck(target);
                    if (MainMenu["Insec"]["PriorFlash"])
                    {
                        if (MainMenu["Insec"]["Flash"] && Flash.IsReady() && checkFlash.Item2)
                        {
                            GapByFlash(target, checkFlash.Item1);
                        }
                        else if (WardManager.CanWardJump && checkJump.Item2)
                        {
                            GapByWardJump(target, checkJump.Item1);
                        }
                    }
                    else
                    {
                        if (WardManager.CanWardJump && checkJump.Item2)
                        {
                            GapByWardJump(target, checkJump.Item1);
                        }
                        else if (MainMenu["Insec"]["Flash"] && Flash.IsReady() && checkFlash.Item2)
                        {
                            GapByFlash(target, checkFlash.Item1);
                        }
                    }
                    if (!checkFlash.Item2 && !checkJump.Item2 && !Player.HasBuff("blindmonkqtwodash")
                        && !Player.IsDashing()
                        && Player.Distance(target) < WardManager.WardRange + FlashRange - DistBehind(target)
                        && WardManager.CanWardJump && MainMenu["Insec"]["Flash"] && Flash.IsReady()
                        && MainMenu["Insec"]["FlashJump"])
                    {
                        Flee(target.ServerPosition);
                    }
                }
                GapByQ(target);
                if (R.IsInRange(target)
                    && Player.Distance(ExpectedEndPosition(target)) > target.Distance(ExpectedEndPosition(target)))
                {
                    var posTarget = target.ServerPosition.ToVector2();
                    var project = posTarget.Extend(Player.ServerPosition, -R2.Range)
                        .ProjectOn(posTarget, ExpectedEndPosition(target).Extend(posTarget, -(R2.Range * 0.5f)));
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
                        (Player.BoundingRadius + target.BoundingRadius + 60) * (100 + MainMenu["Insec"]["Dist"]) / 100,
                        R.Range);
            }

            private static Vector2 EndPosition(Obj_AI_Hero target)
            {
                return target.ServerPosition.ToVector2().Extend(ExpectedEndPosition(target), R2.Range);
            }

            private static Vector2 ExpectedEndPosition(Obj_AI_Hero target)
            {
                if (posSetting.IsValid() && target.Distance(posSetting) <= R2.Range + 500)
                {
                    return posSetting;
                }
                var pos = Player.ServerPosition;
                switch (MainMenu["Insec"]["Mode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        var turret =
                            GameObjects.AllyTurrets.Where(
                                i =>
                                !i.IsDead && target.Distance(i) <= R2.Range + 500
                                && i.Distance(target) - R2.Range <= 950 && i.Distance(target) > 250)
                                .MinOrDefault(i => i.Distance(Player));
                        if (turret != null)
                        {
                            pos = turret.ServerPosition;
                        }
                        else
                        {
                            var hero =
                                GameObjects.AllyHeroes.Where(
                                    i =>
                                    !i.IsMe && target.Distance(i) <= R2.Range + 500 && i.HealthPercent > 10
                                    && i.Distance(target) > 250).MaxOrDefault(i => new Priority().GetDefaultPriority(i));
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
                    lastGapClose = Variables.TickCount;
                }
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                Player.Spellbook.CastSpell(Flash, posGap);
            }

            private static void GapByQ(Obj_AI_Hero target)
            {
                if (!MainMenu["Insec"]["Q"] || !Q.IsReady())
                {
                    return;
                }
                var minDist = WardManager.WardRange
                              + (WardManager.CanWardJump && MainMenu["Insec"]["Flash"] && Flash.IsReady()
                                 && MainMenu["Insec"]["FlashJump"]
                                     ? FlashRange
                                     : 0) - DistBehind(target);
                if (IsQOne)
                {
                    var pred = Q.VPrediction(target);
                    if (pred.Hitchance == HitChance.Collision || pred.Hitchance == HitChance.OutOfRange)
                    {
                        if (pred.Hitchance == HitChance.Collision && MainMenu["Insec"]["QCol"] && Smite.IsReady()
                            && !pred.CollisionObjects.All(i => i.IsMe))
                        {
                            var col =
                                pred.CollisionObjects.Select(i => i as Obj_AI_Minion).Where(i => i.IsValid()).ToList();
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
                                        GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet())
                                            .Concat(GameObjects.Jungle))
                                    .Where(
                                        i =>
                                        i.IsValidTarget(Q.Range) && i.Health > Player.GetSpellDamage(i, SpellSlot.Q)
                                        && Player.Distance(target) > i.Distance(target)
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
                else if (((WardManager.CanWardJump && Player.Mana >= 80)
                          || (MainMenu["Insec"]["Flash"] && Flash.IsReady()))
                         && (HaveQ(target) || (GetQObj != null && GetQObj.Distance(target) < minDist - 80)))
                {
                    Variables.TargetSelector.SetTarget(target);
                    Q.Cast();
                }
            }

            private static void GapByRFlash(Obj_AI_Hero target)
            {
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                lastRFlash = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                R.CastOnUnit(target);
            }

            private static void GapByWardJump(Obj_AI_Hero target, Vector3 posGap)
            {
                if (Variables.Orbwalker.CanMove())
                {
                    lastGapClose = Variables.TickCount;
                    Variables.Orbwalker.Move(
                        posGap.Extend(ExpectedEndPosition(target), -(DistBehind(target) + Player.BoundingRadius / 2)));
                }
                posSetting = EndPosition(target);
                lastSettingPos = Variables.TickCount;
                lastJump = Variables.TickCount;
                Variables.TargetSelector.SetTarget(target);
                var objJump =
                    GameObjects.AllyHeroes.Where(i => !i.IsMe)
                        .Cast<Obj_AI_Base>()
                        .Concat(
                            GameObjects.AllyMinions.Where(
                                i => i.IsMinion() || i.IsPet() || SpecialPet.Contains(i.CharData.BaseSkinName.ToLower()))
                                .Concat(GameObjects.AllyWards.Where(i => i.IsWard())))
                        .Where(i => i.IsValidTarget(W.Range, false) && i.Distance(posGap) <= target.BoundingRadius)
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

            private static Tuple<Vector3, bool> GapCheck(Obj_AI_Hero target, bool useFlash = false)
            {
                if (useFlash && R.IsInRange(target) && MainMenu["Insec"]["FlashMode"].GetValue<MenuList>().Index != 1)
                {
                    return new Tuple<Vector3, bool>(new Vector3(), true);
                }
                var posBehind =
                    target.ServerPosition.ToVector2()
                        .Extend(ExpectedEndPosition(target), -DistBehind(target))
                        .ToVector3(target.ServerPosition.Z);
                if (!useFlash)
                {
                    return Player.Distance(posBehind) > WardManager.WardRange || target.Distance(posBehind) > R.Range
                           || target.Distance(posBehind) >= ExpectedEndPosition(target).Distance(posBehind)
                               ? new Tuple<Vector3, bool>(new Vector3(), false)
                               : new Tuple<Vector3, bool>(posBehind, true);
                }
                var posFlash = Player.ServerPosition.ToVector2().Extend(posBehind, FlashRange);
                return Player.Distance(posBehind) > FlashRange || target.Distance(posBehind) > R.Range
                       || target.Distance(posFlash) <= 50
                       || target.Distance(posFlash) >= ExpectedEndPosition(target).Distance(posFlash)
                       || target.Distance(posBehind) >= ExpectedEndPosition(target).Distance(posBehind)
                           ? new Tuple<Vector3, bool>(new Vector3(), false)
                           : new Tuple<Vector3, bool>(posBehind, true);
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
                        if (ward == null || ward.IsEnemy || !ward.GetMinionType().HasFlag(MinionTypes.Ward)
                            || ward.MaxHealth.Equals(1))
                        {
                            return;
                        }
                        if (Variables.TickCount - LastCreateTime < 1000)
                        {
                            LastCreateTime = Variables.TickCount;
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
                var posEnd = Player.ServerPosition.Extend(pos, Math.Min(Player.Distance(pos), WardRange));
                Player.Spellbook.CastSpell(ward.SpellSlot, posEnd);
                if (isInsecByWard)
                {
                    LastCreateTime = Variables.TickCount;
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
                if (wardObj != null && wardObj.Distance(pos) <= 250)
                {
                    W.CastOnUnit(wardObj);
                }
            }

            #endregion
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class LeeSin : Helper
    {
        private static int _limitWard;

        public LeeSin()
        {
            Q = new Spell(SpellSlot.Q, 1100);
            Q2 = new Spell(SpellSlot.Q, 1300);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 430, TargetSelector.DamageType.Magical);
            E2 = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 375);
            R2 = new Spell(SpellSlot.R, 800);
            Q.SetSkillshot(0.25f, 65, 1800, true, SkillshotType.SkillshotLine);
            R2.SetSkillshot(0.25f, 0, 1500, false, SkillshotType.SkillshotLine);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                Insec.Init(champMenu);
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "P", "Use Passive", false);
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "QCol", "-> Smite Collision");
                    AddBool(comboMenu, "W", "Use W");
                    AddSlider(comboMenu, "WHpU", "-> If Hp <", 30);
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddBool(comboMenu, "RBehind", "-> Kill Enemy Behind");
                    champMenu.AddSubMenu(comboMenu);
                }
                //var harassMenu = new Menu("Harass", "Harass");
                //{
                //    AddBool(harassMenu, "Q", "Use Q");
                //    AddSlider(harassMenu, "Q2HpA", "-> Q2 If Hp Above", 30);
                //    AddBool(harassMenu, "E", "Use E");
                //    AddBool(harassMenu, "W", "Use W Jump Back");
                //    AddBool(harassMenu, "WWard", "-> Ward Jump", false);
                //    champMenu.AddSubMenu(harassMenu);
                //}
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "W", "Use W");
                    AddBool(fleeMenu, "PinkWard", "-> Ward Jump Use Pink Ward", false);
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "R", "Use R");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "R", "Use R");
                        AddBool(interruptMenu, "RGap", "-> Use W To Gap Closer");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "Q", "Q Range", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    AddBool(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
            GameObject.OnCreate += OnCreateWardForFlee;
        }

        private static bool HaveP
        {
            get { return Player.HasBuff("BlindMonkFlurry"); }
        }

        private static bool IsQOne
        {
            get { return Q.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static bool IsWOne
        {
            get { return W.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static bool IsEOne
        {
            get { return E.Instance.SData.Name.ToLower().Contains("one"); }
        }

        private static IEnumerable<Obj_AI_Base> ObjHaveQ
        {
            get { return ObjectManager.Get<Obj_AI_Base>().Where(i => i.IsValidTarget(Q2.Range) && HaveQ(i)); }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalk.CurrentMode)
            {
                case Orbwalker.Mode.Combo:
                    Fight("Combo");
                    break;
                case Orbwalker.Mode.Clear:
                    SmiteMob();
                    break;
                case Orbwalker.Mode.LastHit:
                    LastHit();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee(Game.CursorPos);
                    break;
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            KillSteal();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, (IsQOne ? Q : Q2).Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0 && IsWOne)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position, (IsEOne ? E : E2).Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Insec", "Draw") && R.Level > 0 && Insec.IsReady)
            {
                Drawing.DrawLine(
                    Drawing.WorldToScreen(Insec.Target.ServerPosition), Drawing.WorldToScreen(Insec.PosAfterKick), 2,
                    Color.BlueViolet);
                Render.Circle.DrawCircle(Insec.Target.ServerPosition, Insec.Target.BoundingRadius, Color.BlueViolet);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "R") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !R.IsReady())
            {
                return;
            }
            if (R.IsInRange(unit))
            {
                R.CastOnUnit(unit, PacketCast);
            }
            else if (GetValue<bool>("Interrupt", "RGap") && W.IsReady() && IsWOne &&
                     Utils.GameTimeTickCount - _limitWard > 1000)
            {
                var posPred = Prediction.GetPrediction(unit, 0.05f, 0, 2000)
                    .UnitPosition.Randomize(0, (int) R.Range - 75);
                var posJump = Player.ServerPosition.Extend(posPred, Math.Min(W.Range, Player.Distance(posPred)));
                var objNear = new List<Obj_AI_Base>();
                objNear.AddRange(HeroManager.Allies.Where(i => i.IsValidTarget(W.Range, false) && !i.IsMe));
                objNear.AddRange(GetMinions(W.Range, MinionTypes.All, MinionTeam.Ally));
                objNear.AddRange(
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(i => i.IsValidTarget(W.Range, false) && i.IsAlly && IsWard(i)));
                if (
                    objNear.Where(i => i.Distance(posJump) < 200)
                        .OrderBy(i => i.Distance(posJump))
                        .Any(i => W.CastOnUnit(i, PacketCast)))
                {
                    _limitWard = Utils.GameTimeTickCount + 800;
                }
            }
        }

        private static void OnCreateWardForFlee(GameObject sender, EventArgs args)
        {
            if (Orbwalk.CurrentMode != Orbwalker.Mode.Flee || !W.IsReady() || !IsWOne ||
                !sender.IsValid<Obj_AI_Minion>())
            {
                return;
            }
            var ward = (Obj_AI_Minion) sender;
            if (!ward.IsAlly || !IsWard(ward) || !W.IsInRange(ward) || Utils.GameTimeTickCount - _limitWard > 1000)
            {
                return;
            }
            Utility.DelayAction.Add(
                50, () =>
                {
                    var buff = ward.GetBuff("sharedstealthwardbuff") ?? ward.GetBuff("sharedvisionwardbuff");
                    if (buff != null && buff.Caster.IsMe)
                    {
                        W.CastOnUnit(ward, PacketCast);
                    }
                });
        }

        private static void Fight(string mode)
        {
            if (GetValue<bool>(mode, "P") && HaveP && Orbwalk.GetBestHeroTarget != null && Orbwalk.CanAttack)
            {
                return;
            }
            if (GetValue<bool>(mode, "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget();
                    if (target != null)
                    {
                        var state = Q.Cast(target, PacketCast);
                        if (state.IsCasted())
                        {
                            return;
                        }
                        if (state == Spell.CastStates.Collision && GetValue<bool>(mode, "QCol") && Smite.IsReady())
                        {
                            var pred = Q.GetPrediction(target);
                            if (
                                pred.CollisionObjects.Count(
                                    i => i.IsValid<Obj_AI_Minion>() && IsSmiteable((Obj_AI_Minion) i)) == 1 &&
                                CastSmite(pred.CollisionObjects.First()) && Q.Cast(pred.CastPosition, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    var target = Q2.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                    if (target != null &&
                        (QAgain(target) ||
                         ((target.HasBuffOfType(BuffType.Knockback) || target.HasBuffOfType(BuffType.Knockup)) &&
                          Player.Distance(target) > 300 && !R.IsReady()) || Q.IsKillable(target, 1) ||
                         !Orbwalk.InAutoAttackRange(target, 100) || (Orbwalk.InAutoAttackRange(target) && !HaveP)) &&
                        Q2.Cast(PacketCast))
                    {
                        return;
                    }
                    if (target == null)
                    {
                        var sub = Q2.GetTarget();
                        if (sub != null && ObjHaveQ.Any(i => i.Distance(sub) < Player.Distance(sub)) &&
                            Q2.Cast(PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                if (IsEOne)
                {
                    if (E.GetTarget() != null && E.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else if (
                    HeroManager.Enemies.Where(i => i.IsValidTarget(E2.Range) && HaveE(i))
                        .Any(
                            i =>
                                EAgain(i) || !Orbwalk.InAutoAttackRange(i, 50) ||
                                (Orbwalk.InAutoAttackRange(i) && !HaveP)) && Player.Mana >= 50 &&
                    E2.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "R") && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                if (GetValue<bool>(mode, "Q") && Q.IsReady() && !IsQOne && target != null)
                {
                    if (CanKill(target, GetQ2Dmg(target, R.GetDamage(target))) && R.CastOnUnit(target, PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    target = R.GetTarget();
                    if (target != null && R.IsKillable(target))
                    {
                        if (R.CastOnUnit(target, PacketCast))
                        {
                            return;
                        }
                    }
                    else if (GetValue<bool>(mode, "RBehind"))
                    {
                        foreach (
                            var enemy in HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range) && !R.IsKillable(i)))
                        {
                            R2.UpdateSourcePosition(enemy.ServerPosition, enemy.ServerPosition);
                            if (
                                HeroManager.Enemies.Any(
                                    i =>
                                        i.IsValidTarget(R2.Range) && i.NetworkId != enemy.NetworkId &&
                                        R2.WillHit(
                                            i, enemy.ServerPosition.Extend(Player.ServerPosition, -R2.Range),
                                            (int) enemy.BoundingRadius + 50) && R.IsKillable(i)) &&
                                R.CastOnUnit(enemy, PacketCast))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady() && Orbwalk.GetBestHeroTarget != null)
            {
                if (IsWOne)
                {
                    if (Player.HealthPercent < GetValue<Slider>(mode, "WHpU").Value)
                    {
                        W.Cast(PacketCast);
                    }
                }
                else if (!Player.HasBuff("BlindMonkSafeguard") &&
                         (Player.HealthPercent < GetValue<Slider>(mode, "WHpU").Value || !HaveP))
                {
                    W.Cast(PacketCast);
                }
            }
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || !Q.IsReady() || !IsQOne)
            {
                return;
            }
            var obj =
                GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Where(
                        i =>
                            Q.GetPrediction(i).Hitchance >= Q.MinHitChance &&
                            (!Orbwalk.InAutoAttackRange(i) || i.Health > Player.GetAutoAttackDamage(i, true)))
                    .FirstOrDefault(i => Q.IsKillable(i));
            if (obj == null)
            {
                return;
            }
            Q.Cast(obj, PacketCast);
        }

        private static void Flee(Vector3 pos)
        {
            if (!GetValue<bool>("Flee", "W") || !W.IsReady() || !IsWOne || Utils.GameTimeTickCount - _limitWard <= 1000)
            {
                return;
            }
            var posJump = Player.ServerPosition.Extend(pos, Math.Min(W.Range, Player.Distance(pos)));
            var objNear = new List<Obj_AI_Base>();
            objNear.AddRange(HeroManager.Allies.Where(i => i.IsValidTarget(W.Range, false) && !i.IsMe));
            objNear.AddRange(GetMinions(W.Range, MinionTypes.All, MinionTeam.Ally));
            objNear.AddRange(
                ObjectManager.Get<Obj_AI_Minion>().Where(i => i.IsValidTarget(W.Range, false) && i.IsAlly && IsWard(i)));
            var objJump = objNear.Where(i => i.Distance(posJump) < 200).MinOrDefault(i => i.Distance(posJump));
            if (objJump != null)
            {
                if (W.CastOnUnit(objJump, PacketCast))
                {
                    _limitWard = Utils.GameTimeTickCount + 800;
                }
            }
            else if (GetWardSlot != null)
            {
                var posPlace = Player.ServerPosition.Extend(pos, Math.Min(GetWardRange - 10, Player.Distance(pos)));
                if (Player.Spellbook.CastSpell(GetWardSlot.SpellSlot, posPlace))
                {
                    _limitWard = Utils.GameTimeTickCount;
                }
            }
        }

        private static void KillSteal()
        {
            if (GetValue<bool>("KillSteal", "Ignite") && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Smite") &&
                (CurrentSmiteType == SmiteType.Blue || CurrentSmiteType == SmiteType.Red))
            {
                var target = TargetSelector.GetTarget(760, TargetSelector.DamageType.True);
                if (target != null && CastSmite(target))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "Q") && Q.IsReady())
            {
                if (IsQOne)
                {
                    var target = Q.GetTarget();
                    if (target != null && Q.IsKillable(target) && Q.Cast(target, PacketCast).IsCasted())
                    {
                        return;
                    }
                }
                else
                {
                    var target = Q2.GetTarget(0, HeroManager.Enemies.Where(i => !HaveQ(i)));
                    if (target != null && Q.IsKillable(target, 1) && Q2.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady() && IsEOne)
            {
                var target = E.GetTarget();
                if (target != null && E.IsKillable(target) && E.Cast(PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "R") && R.IsReady())
            {
                var target = R.GetTarget();
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static double GetQ2Dmg(Obj_AI_Base target, double subHp = 0)
        {
            var dmg = new[] { 50, 80, 110, 140, 170 }[Q.Level - 1] + 0.9 * Player.FlatPhysicalDamageMod +
                      0.08 * (target.MaxHealth - (target.Health - subHp));
            return
                Player.CalcDamage(
                    target, Damage.DamageType.Physical, target.IsValid<Obj_AI_Minion>() ? Math.Min(dmg, 400) : dmg) +
                subHp;
        }

        private static bool HaveQ(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkSonicWave");
        }

        private static bool HaveE(Obj_AI_Base target)
        {
            return target.HasBuff("BlindMonkTempest");
        }

        private static bool QAgain(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkSonicWave");
            return buff != null && buff.EndTime - Game.Time < 0.5f;
        }

        private static bool EAgain(Obj_AI_Base target)
        {
            var buff = target.GetBuff("BlindMonkTempest");
            return buff != null && buff.EndTime - Game.Time < 0.5f;
        }

        protected class Insec
        {
            private const int RKickRange = 800;
            public static Obj_AI_Hero Target;
            private static Vector3 InsecPos { get; set; }
            private static int LastWard { get; set; }
            private static int LastFlash { set; get; }

            public static bool IsReady
            {
                get
                {
                    return ((W.IsReady() && IsWOne && GetWardSlot != null) || Flash.IsReady() || JumpRecent(4000)) &&
                           R.IsReady() && Target != null && PosKickTo.IsValid();
                }
            }

            private static Vector3 PosKickTo
            {
                get
                {
                    if (InsecPos.IsValid())
                    {
                        return InsecPos;
                    }
                    var pos = new Vector3();
                    switch (GetValue<StringList>("Insec", "Mode").SelectedIndex)
                    {
                        case 0:
                            var hero =
                                HeroManager.Allies.Where(
                                    i => i.IsValidTarget(RKickRange + 500, false, Target.ServerPosition) && !i.IsMe)
                                    .MinOrDefault(i => i.Distance(Target));
                            var turret =
                                ObjectManager.Get<Obj_AI_Turret>()
                                    .Where(
                                        i =>
                                            i.IsAlly && !i.IsDead && i.Distance(Player) < 3000 &&
                                            i.Distance(Target) - RKickRange < 1100)
                                    .MinOrDefault(i => i.Distance(Target));
                            if (turret != null)
                            {
                                pos = turret.ServerPosition;
                            }
                            if (!pos.IsValid() && hero != null)
                            {
                                pos = hero.ServerPosition +
                                      (Target.ServerPosition - hero.ServerPosition).Normalized() *
                                      (hero.AttackRange + hero.BoundingRadius) / 2;
                            }
                            if (!pos.IsValid())
                            {
                                pos = Player.ServerPosition;
                            }
                            break;
                        case 1:
                            pos = Game.CursorPos;
                            break;
                        case 2:
                            pos = Player.ServerPosition;
                            break;
                    }
                    return pos;
                }
            }

            public static Vector3 PosAfterKick
            {
                get { return Target.ServerPosition.Extend(PosKickTo, RKickRange); }
            }

            private static float DistBehind
            {
                get
                {
                    return
                        Math.Min(
                            (Player.BoundingRadius + Target.BoundingRadius + 80) *
                            (GetValue<Slider>("Insec", "ExtraDist").Value + 100) / 100, R.Range);
                }
            }

            public static void Init(Menu menu)
            {
                var insecMenu = new Menu("Insec", "Insec");
                {
                    AddKeybind(insecMenu, "AdvancedInsec", "Insec Advanced (R-Flash)", "Z");
                    AddKeybind(insecMenu, "NormalInsec", "Insec Normal", "T");
                    AddBool(insecMenu, "Q", "Use Q");
                    AddBool(insecMenu, "PriorFlash", "Priorize Flash Over WardJump", false);
                    AddList(insecMenu, "Mode", "Mode", new[] { "Turrret/Hero", "Mouse Position", "Player Position" });
                    AddSlider(insecMenu, "ExtraDist", "Extra Distance Behind (%)", 20, 0);
                    AddBool(insecMenu, "Draw", "Draw Line", false);
                }
                menu.AddSubMenu(insecMenu);
                InsecPos = new Vector3();
                LastWard = 0;
                LastFlash = 0;
                Game.OnUpdate += OnUpdateInsec;
                GameObject.OnCreate += OnCreateWardForJump;
            }

            private static void OnUpdateInsec(EventArgs args)
            {
                if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
                {
                    return;
                }
                Target = Q2.GetTarget(200);
                if (!GetValue<KeyBind>("Insec", "AdvancedInsec").Active &&
                    !GetValue<KeyBind>("Insec", "NormalInsec").Active)
                {
                    return;
                }
                Orbwalker.MoveTo(Game.CursorPos);
                if (IsReady && (GetValue<KeyBind>("Insec", "NormalInsec").Active || Flash.IsReady()))
                {
                    Start(GetValue<KeyBind>("Insec", "NormalInsec").Active);
                }
            }

            private static void OnCreateWardForJump(GameObject sender, EventArgs args)
            {
                if (!GetValue<KeyBind>("Insec", "NormalInsec").Active || !IsReady || !W.IsReady() || !IsWOne ||
                    !sender.IsValid<Obj_AI_Minion>())
                {
                    return;
                }
                var ward = (Obj_AI_Minion) sender;
                if (!ward.IsAlly || !IsWard(ward) || !W.IsInRange(ward) || Utils.GameTimeTickCount - LastWard > 1000)
                {
                    return;
                }
                Utility.DelayAction.Add(
                    50, () =>
                    {
                        var buff = ward.GetBuff("sharedstealthwardbuff") ?? ward.GetBuff("sharedvisionwardbuff");
                        if (buff != null && buff.Caster.IsMe)
                        {
                            W.CastOnUnit(ward, PacketCast);
                        }
                    });
            }

            private static bool JumpRecent(int tick = 1000)
            {
                return (LastWard != 0 && Utils.GameTimeTickCount - LastWard < tick) ||
                       (LastFlash != 0 && Utils.GameTimeTickCount - LastFlash < tick);
            }

            private static void JumpBehind(bool isFlash = false)
            {
                var posPred = Prediction.GetPrediction(Target, 0.05f, 0, !isFlash ? 2000 : float.MaxValue).UnitPosition;
                var posBehind = posPred.Extend(PosAfterKick, -DistBehind);
                if (posBehind.Distance(PosAfterKick) <= Target.Distance(PosAfterKick))
                {
                    return;
                }
                if (isFlash)
                {
                    if (Player.Distance(posBehind) >= 425)
                    {
                        return;
                    }
                    InsecPos = PosAfterKick;
                    Utility.DelayAction.Add(5000, () => InsecPos = new Vector3());
                    if (CastFlash(posBehind))
                    {
                        LastFlash = Utils.GameTimeTickCount;
                    }
                }
                else if (Player.Distance(posBehind) < GetWardRange)
                {
                    InsecPos = PosAfterKick;
                    Utility.DelayAction.Add(5000, () => InsecPos = new Vector3());
                    if (PlaceWard(posBehind))
                    {
                        LastWard = Utils.GameTimeTickCount;
                    }
                }
            }

            private static bool PlaceWard(Vector3 pos)
            {
                if (Utils.GameTimeTickCount - LastWard <= 1000)
                {
                    return false;
                }
                return Player.Spellbook.CastSpell(
                    GetWardSlot.SpellSlot,
                    Player.ServerPosition.Extend(pos, Math.Min(GetWardRange, Player.Distance(pos))));
            }

            private static void Start(bool isNormal = true)
            {
                var minDistToJump = 600 - DistBehind;
                if (GetValue<bool>("Insec", "Q") && Q.IsReady())
                {
                    if (IsQOne)
                    {
                        var state = Q.Cast(Target, PacketCast);
                        if (state.IsCasted())
                        {
                            return;
                        }
                        if (state == Spell.CastStates.OutOfRange || state == Spell.CastStates.Collision ||
                            state == Spell.CastStates.LowHitChance)
                        {
                            var nearObj = new List<Obj_AI_Base>();
                            nearObj.AddRange(
                                HeroManager.Enemies.Where(i => i.IsValidTarget(Q.Range) && !Q.IsKillable(i)));
                            nearObj.AddRange(
                                GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly).Where(i => !Q.IsKillable(i)));
                            if (
                                nearObj.Where(
                                    i =>
                                        Q.GetPrediction(i).Hitchance >= Q.MinHitChance &&
                                        i.Distance(Target) < minDistToJump)
                                    .OrderBy(i => i.Distance(Target))
                                    .Any(i => Q.Cast(i, PacketCast).IsCasted()))
                            {
                                return;
                            }
                        }
                    }
                    else if (Player.Distance(Target) > minDistToJump &&
                             ObjHaveQ.Any(i => i.Distance(Target) < minDistToJump) &&
                             ((isNormal && W.IsReady() && IsWOne && GetWardSlot != null && Player.Mana >= 80) ||
                              Flash.IsReady()) && Q2.Cast(PacketCast))
                    {
                        return;
                    }
                }
                if (!isNormal)
                {
                    var posBehind = Target.ServerPosition.Extend(PosAfterKick, -DistBehind);
                    if (R.IsInRange(Target) && Player.Distance(posBehind) < 425)
                    {
                        InsecPos = PosAfterKick;
                        Utility.DelayAction.Add(5000, () => InsecPos = new Vector3());
                        if (R.CastOnUnit(Target, PacketCast))
                        {
                            Utility.DelayAction.Add(
                                125, () =>
                                {
                                    if (Player.LastCastedSpellName() == "BlindMonkRKick" && CastFlash(posBehind))
                                    {
                                        LastFlash = Utils.GameTimeTickCount;
                                    }
                                });
                        }
                    }
                }
                else
                {
                    if (R.IsInRange(Target) && Player.Distance(PosAfterKick) > Target.Distance(PosAfterKick) &&
                        PosAfterKick.Distance(
                            PosAfterKick.To2D()
                                .ProjectOn(
                                    Player.ServerPosition.To2D(),
                                    Player.ServerPosition.Extend(Target.ServerPosition, RKickRange).To2D())
                                .LinePoint.To3D()) < RKickRange * 0.5f && R.CastOnUnit(Target, PacketCast))
                    {
                        return;
                    }
                    if (Player.Distance(Target) < minDistToJump &&
                        Player.Distance(PosAfterKick) < Target.Distance(PosAfterKick) && !JumpRecent())
                    {
                        if (GetValue<bool>("Insec", "PriorFlash"))
                        {
                            if (Flash.IsReady())
                            {
                                JumpBehind(true);
                            }
                            else if (W.IsReady() && IsWOne && GetWardSlot != null)
                            {
                                JumpBehind();
                            }
                        }
                        else
                        {
                            if (W.IsReady() && IsWOne && GetWardSlot != null)
                            {
                                JumpBehind();
                            }
                            else if (Flash.IsReady())
                            {
                                JumpBehind(true);
                            }
                        }
                    }
                }
            }
        }
    }
}
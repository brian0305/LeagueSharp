using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Maokai : Helper
    {
        private const int QKnockUpWidth = 250;

        public Maokai()
        {
            Q = new Spell(SpellSlot.Q, 600, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W, 525, TargetSelector.DamageType.Magical);
            E = new Spell(SpellSlot.E, 1100, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R, 475, TargetSelector.DamageType.Magical);
            Q.SetSkillshot(0.5f, 110, 1200, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(1, 225, 1500, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RMpA", "-> If Mp >=", 20);
                    AddSlider(comboMenu, "RHpU", "-> If Enemy Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    AddBool(comboMenu, "RKill", "-> Cancel When Killable");
                    AddSlider(comboMenu, "RKillCountA", "--> If Can Kill >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddKeybind(harassMenu, "AutoQ", "Auto Q", "H", KeyBindType.Toggle);
                    AddSlider(harassMenu, "AutoQMpA", "-> If Mp >=", 50);
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddSlider(harassMenu, "WHpA", "-> If Hp >=", 20);
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "W", "Use W");
                    AddBool(fleeMenu, "Q", "Use Q To Slow Enemy");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "W", "Use W");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "Q", "Use Q");
                        AddBool(antiGapMenu, "QSlow", "-> Slow If Cant Knockback (Skillshot)");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddBool(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddBool(interruptMenu, "Q", "Use Q");
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
                    AddKeybind(miscMenu, "Gank", "Gank", "Z");
                    AddBool(miscMenu, "WTower", "Auto W If Enemy Under Tower");
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
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
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
                case Orbwalker.Mode.Harass:
                    Fight("Harass");
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.Flee:
                    Flee();
                    break;
            }
            if (GetValue<KeyBind>("Misc", "Gank").Active)
            {
                Fight("Gank");
            }
            if (GetValue<bool>("SmiteMob", "Auto") && Orbwalk.CurrentMode != Orbwalker.Mode.Clear)
            {
                SmiteMob();
            }
            AutoQ();
            KillSteal();
            AutoWUnderTower();
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (GetValue<bool>("Draw", "Q") && Q.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "R") && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !Q.CanCast(gapcloser.Sender))
            {
                return;
            }
            if (Player.Distance(gapcloser.Sender) <= QKnockUpWidth)
            {
                Q.Cast(gapcloser.Sender.ServerPosition, PacketCast);
            }
            else if (GetValue<bool>("AntiGap", "QSlow") && gapcloser.SkillType == GapcloserType.Skillshot &&
                     Player.Distance(gapcloser.End) > QKnockUpWidth)
            {
                Q.Cast(gapcloser.Sender, PacketCast);
            }
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "Q") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !Q.IsReady())
            {
                return;
            }
            if (Player.Distance(unit) > QKnockUpWidth && W.CanCast(unit) &&
                Player.Mana >= Q.Instance.ManaCost + W.Instance.ManaCost && W.CastOnUnit(unit, PacketCast))
            {
                return;
            }
            if (Player.Distance(unit) <= QKnockUpWidth)
            {
                Q.Cast(unit.ServerPosition, PacketCast);
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo" && GetValue<bool>(mode, "R") && R.IsReady())
            {
                var obj = HeroManager.Enemies.Where(i => i.IsValidTarget(R.Range)).ToList();
                if (!Player.HasBuff("MaokaiDrain3"))
                {
                    if (Player.ManaPercent >= GetValue<Slider>(mode, "RMpA").Value &&
                        (obj.Any(i => i.HealthPercent < GetValue<Slider>(mode, "RHpU").Value) ||
                         obj.Count >= GetValue<Slider>(mode, "RCountA").Value) && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
                else
                {
                    if (GetValue<bool>(mode, "RKill") &&
                        obj.Count(i => CanKill(i, GetRDmg(i))) >= GetValue<Slider>(mode, "RKillCountA").Value &&
                        R.Cast(PacketCast))
                    {
                        return;
                    }
                    if (Player.ManaPercent < GetValue<Slider>(mode, "RMpA").Value && R.Cast(PacketCast))
                    {
                        return;
                    }
                }
            }
            if (mode == "Gank")
            {
                var target = W.GetTarget(100);
                CustomOrbwalk(target);
                if (target == null)
                {
                    return;
                }
                if (W.IsReady())
                {
                    if (E.IsReady() && E.Cast(target, PacketCast).IsCasted())
                    {
                        return;
                    }
                    W.CastOnUnit(target, PacketCast);
                }
                else if (!Player.IsDashing() && Q.IsReady())
                {
                    Q.Cast(target, PacketCast);
                }
            }
            else
            {
                if (GetValue<bool>(mode, "E") && E.CastOnBestTarget(E.Width / 2, PacketCast, true).IsCasted())
                {
                    return;
                }
                if (GetValue<bool>(mode, "W") &&
                    (mode == "Combo" || Player.HealthPercent >= GetValue<Slider>(mode, "WHpA").Value) &&
                    W.CastOnBestTarget(0, PacketCast).IsCasted())
                {
                    return;
                }
                if (GetValue<bool>(mode, "Q"))
                {
                    Q.CastOnBestTarget(0, PacketCast, true);
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj =
                GetMinions(E.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Cast<Obj_AI_Base>()
                    .ToList();
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                (minionObj.Count > 2 || minionObj.Any(i => i.MaxHealth >= 1200)))
            {
                var pos = E.GetCircularFarmLocation(minionObj);
                if (pos.MinionsHit > 0 && E.Cast(pos.Position, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady())
            {
                var pos = Q.GetLineFarmLocation(minionObj.Where(i => Q.IsInRange(i)).ToList());
                if (pos.MinionsHit > 0 && Q.Cast(pos.Position, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady())
            {
                var obj = minionObj.Where(i => W.IsInRange(i)).FirstOrDefault(i => i.MaxHealth >= 1200);
                if (obj == null && !minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 40)))
                {
                    obj = minionObj.Where(i => W.IsInRange(i)).MinOrDefault(i => i.Health);
                }
                if (obj != null)
                {
                    W.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void Flee()
        {
            if (GetValue<bool>("Flee", "W") && W.IsReady())
            {
                var pos = Player.ServerPosition.Extend(
                    Game.CursorPos, Math.Min(W.Range, Player.Distance(Game.CursorPos)));
                var obj =
                    (Obj_AI_Base)
                        HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range) && i.Distance(pos) < 200)
                            .MinOrDefault(i => i.Distance(pos)) ??
                    GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(i => i.Distance(pos) < 200)
                        .MinOrDefault(i => i.Distance(pos));
                if (obj != null && W.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Flee", "Q"))
            {
                Q.CastOnBestTarget(0, PacketCast, true);
            }
        }

        private static void AutoQ()
        {
            if (!GetValue<KeyBind>("Harass", "AutoQ").Active ||
                Player.ManaPercent < GetValue<Slider>("Harass", "AutoQMpA").Value)
            {
                return;
            }
            Q.CastOnBestTarget(0, PacketCast, true);
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
                var target = Q.GetTarget();
                if (target != null && Q.IsKillable(target) && Q.Cast(target, PacketCast).IsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>("KillSteal", "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null && W.IsKillable(target))
                {
                    W.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void AutoWUnderTower()
        {
            if (!GetValue<bool>("Misc", "WTower") || !W.IsReady())
            {
                return;
            }
            var target = HeroManager.Enemies.Where(i => i.IsValidTarget(W.Range)).MinOrDefault(i => i.Distance(Player));
            var tower =
                ObjectManager.Get<Obj_AI_Turret>()
                    .FirstOrDefault(i => i.IsAlly && !i.IsDead && i.Distance(Player) <= 850);
            if (target != null && tower != null && target.Distance(tower) <= 850)
            {
                W.CastOnUnit(target, PacketCast);
            }
        }

        private static double GetRDmg(Obj_AI_Hero target)
        {
            return Player.CalcDamage(
                target, Damage.DamageType.Magical,
                new[] { 100, 150, 200 }[R.Level - 1] + 0.5 * Player.FlatMagicDamageMod + R.Instance.Ammo);
        }
    }
}
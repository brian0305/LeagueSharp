using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Rammus : Helper
    {
        public Rammus()
        {
            Q = new Spell(SpellSlot.Q, 200, TargetSelector.DamageType.Magical);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 375, TargetSelector.DamageType.Magical);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "EW", "-> Only Have W");
                    AddBool(comboMenu, "R", "Use R");
                    AddList(comboMenu, "RMode", "-> Mode", new[] { "Always", "# Enemy" });
                    AddSlider(comboMenu, "RCountA", "--> If Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "W", "Use W");
                    AddBool(clearMenu, "E", "Use E");
                    AddSlider(clearMenu, "EHpA", "-> If Hp >=", 50);
                    AddBool(clearMenu, "EW", "-> Only Have W");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddBool(fleeMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "Q", "Use Q");
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
                        AddBool(interruptMenu, "E", "Use E");
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

        private static bool HaveQ
        {
            get { return Player.HasBuff("PowerBall"); }
        }

        private static bool HaveW
        {
            get { return Player.HasBuff("DefensiveBallCurl"); }
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
                    Fight();
                    break;
                case Orbwalker.Mode.Clear:
                    Clear();
                    break;
                case Orbwalker.Mode.Flee:
                    if (GetValue<bool>("Flee", "Q") && !HaveQ && Q.Cast(PacketCast))
                    {
                        return;
                    }
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
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) || !Q.IsReady())
            {
                return;
            }
            if (!HaveQ)
            {
                Q.Cast(PacketCast);
            }
            Player.IssueOrder(GameObjectOrder.MoveTo, gapcloser.Sender.ServerPosition);
        }

        private static void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "E") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.CanCast(unit) || HaveQ)
            {
                return;
            }
            E.CastOnUnit(unit, PacketCast);
        }

        private static void Fight()
        {
            if (GetValue<bool>("Combo", "R") && R.IsReady())
            {
                switch (GetValue<StringList>("Combo", "RMode").SelectedIndex)
                {
                    case 0:
                        if (R.GetTarget() != null && R.Cast(PacketCast))
                        {
                            return;
                        }
                        break;
                    case 1:
                        if (Player.CountEnemiesInRange(R.Range) >= GetValue<Slider>("Combo", "RCountA").Value &&
                            R.Cast(PacketCast))
                        {
                            return;
                        }
                        break;
                }
            }
            if (HaveQ)
            {
                return;
            }
            if (GetValue<bool>("Combo", "Q") && Q.IsReady() && Q.GetTarget(600) != null &&
                ((GetValue<bool>("Combo", "E") && E.IsReady() && E.GetTarget() == null) || !HaveW) && Q.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && (!GetValue<bool>("Combo", "EW") || HaveW) &&
                E.CastOnBestTarget(0, PacketCast).IsCasted())
            {
                return;
            }
            if (GetValue<bool>("Combo", "W") && Q.GetTarget(100) != null)
            {
                W.Cast(PacketCast);
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(600, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any() || HaveQ)
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && !HaveW &&
                (minionObj.Count(i => Q.IsInRange(i)) > 2 || minionObj.Any(i => i.MaxHealth >= 1200 && Q.IsInRange(i)) ||
                 !minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 40))) && Q.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                Player.HealthPercent >= GetValue<Slider>("Clear", "EHpA").Value &&
                (!GetValue<bool>("Clear", "EW") || HaveW))
            {
                var obj = minionObj.FirstOrDefault(i => E.IsInRange(i) && i.Team == GameObjectTeam.Neutral);
                if (obj != null && E.CastOnUnit(obj, PacketCast))
                {
                    return;
                }
            }
            if (GetValue<bool>("Clear", "W") && W.IsReady() &&
                (minionObj.Count(i => Orbwalk.InAutoAttackRange(i)) > 2 ||
                 minionObj.Any(i => i.MaxHealth >= 1200 && Orbwalk.InAutoAttackRange(i))))
            {
                W.Cast(PacketCast);
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
                if (target != null)
                {
                    CastSmite(target);
                }
            }
        }
    }
}
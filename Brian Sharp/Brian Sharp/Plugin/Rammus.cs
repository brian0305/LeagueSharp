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
                    AddItem(comboMenu, "Q", "Use Q");
                    AddItem(comboMenu, "W", "Use W");
                    AddItem(comboMenu, "E", "Use E");
                    AddItem(comboMenu, "EW", "-> Only Have W");
                    AddItem(comboMenu, "R", "Use R");
                    AddItem(comboMenu, "RMode", "-> Mode", new[] { "Always", "# Enemy" });
                    AddItem(comboMenu, "RCountA", "--> If Enemy Above", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMobMenu(clearMenu);
                    AddItem(clearMenu, "Q", "Use Q");
                    AddItem(clearMenu, "W", "Use W");
                    AddItem(clearMenu, "E", "Use E");
                    AddItem(clearMenu, "EHpA", "-> If Hp Above", 50);
                    AddItem(comboMenu, "EW", "-> Only Have W");
                    champMenu.AddSubMenu(clearMenu);
                }
                var fleeMenu = new Menu("Flee", "Flee");
                {
                    AddItem(fleeMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(fleeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddItem(killStealMenu, "Ignite", "Use Ignite");
                        AddItem(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddItem(antiGapMenu, "Q", "Use Q");
                        foreach (var spell in
                            AntiGapcloser.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                antiGapMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(antiGapMenu);
                    }
                    var interruptMenu = new Menu("Interrupt", "Interrupt");
                    {
                        AddItem(interruptMenu, "E", "Use E");
                        foreach (var spell in
                            Interrupter.Spells.Where(
                                i => HeroManager.Enemies.Any(a => i.ChampionName == a.ChampionName)))
                        {
                            AddItem(
                                interruptMenu, spell.ChampionName + "_" + spell.Slot,
                                "-> Skill " + spell.Slot + " Of " + spell.ChampionName);
                        }
                        miscMenu.AddSubMenu(interruptMenu);
                    }
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddItem(drawMenu, "E", "E Range", false);
                    AddItem(drawMenu, "R", "R Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += OnPossibleToInterrupt;
        }

        private void OnUpdate(EventArgs args)
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
                    if (GetValue<bool>("Flee", "Q") && !Player.HasBuff("PowerBall") && Q.Cast(PacketCast))
                    {
                        return;
                    }
                    break;
            }
            KillSteal();
        }

        private void OnDraw(EventArgs args)
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

        private void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "Q") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) || !Q.IsReady())
            {
                return;
            }
            if (!Player.HasBuff("PowerBall"))
            {
                Q.Cast(PacketCast);
            }
            Player.IssueOrder(GameObjectOrder.MoveTo, gapcloser.Sender.ServerPosition);
        }

        private void OnPossibleToInterrupt(Obj_AI_Hero unit, InterruptableSpell spell)
        {
            if (Player.IsDead || !GetValue<bool>("Interrupt", "E") ||
                !GetValue<bool>("Interrupt", unit.ChampionName + "_" + spell.Slot) || !E.CanCast(unit) ||
                Player.HasBuff("PowerBall"))
            {
                return;
            }
            E.CastOnUnit(unit, PacketCast);
        }

        private void Fight()
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
            if (Player.HasBuff("PowerBall"))
            {
                return;
            }
            if (GetValue<bool>("Combo", "Q") && Q.IsReady() && Q.GetTarget(600) != null &&
                ((GetValue<bool>("Combo", "E") && E.IsReady() && E.GetTarget() == null) ||
                 !Player.HasBuff("DefensiveBallCurl")) && Q.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Combo", "E") && E.IsReady() &&
                (!GetValue<bool>("Combo", "EW") || Player.HasBuff("DefensiveBallCurl")) &&
                E.CastOnBestTarget(0, PacketCast).IsCasted())
            {
                return;
            }
            if (GetValue<bool>("Combo", "W") && Q.GetTarget(100) != null)
            {
                W.Cast(PacketCast);
            }
        }

        private void Clear()
        {
            SmiteMob();
            var minionObj = MinionManager.GetMinions(
                600, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any() || Player.HasBuff("PowerBall"))
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && Q.IsReady() && !Player.HasBuff("DefensiveBallCurl") &&
                (minionObj.Count(i => Q.IsInRange(i)) > 2 || minionObj.Any(i => i.MaxHealth >= 1200 && Q.IsInRange(i)) ||
                 !minionObj.Any(i => Orbwalk.InAutoAttackRange(i, 40))) && Q.Cast(PacketCast))
            {
                return;
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady() &&
                Player.HealthPercentage() >= GetValue<Slider>("Clear", "EHpA").Value &&
                (!GetValue<bool>("Clear", "EW") || Player.HasBuff("DefensiveBallCurl")))
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

        private void KillSteal()
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
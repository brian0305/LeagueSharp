using System;
using System.Drawing;
using System.Linq;
using BrianSharp.Common;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalk = BrianSharp.Common.Orbwalker;

namespace BrianSharp.Plugin
{
    internal class Nasus : Helper
    {
        public Nasus()
        {
            Q = new Spell(SpellSlot.Q, Orbwalk.GetAutoAttackRange());
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 650, TargetSelector.DamageType.Magical);
            R = new Spell(SpellSlot.R);
            E.SetSkillshot(0.25f, 190, float.MaxValue, false, SkillshotType.SkillshotCircle);

            var champMenu = new Menu("Plugin", Player.ChampionName + "_Plugin");
            {
                var comboMenu = new Menu("Combo", "Combo");
                {
                    AddBool(comboMenu, "Q", "Use Q");
                    AddBool(comboMenu, "W", "Use W");
                    AddBool(comboMenu, "E", "Use E");
                    AddBool(comboMenu, "R", "Use R");
                    AddSlider(comboMenu, "RHpU", "-> If Player Hp <", 60);
                    AddSlider(comboMenu, "RCountA", "-> Or Enemy >=", 2, 1, 5);
                    champMenu.AddSubMenu(comboMenu);
                }
                var harassMenu = new Menu("Harass", "Harass");
                {
                    AddBool(harassMenu, "Q", "Use Q");
                    AddBool(harassMenu, "W", "Use W");
                    AddBool(harassMenu, "E", "Use E");
                    champMenu.AddSubMenu(harassMenu);
                }
                var clearMenu = new Menu("Clear", "Clear");
                {
                    AddSmiteMob(clearMenu);
                    AddBool(clearMenu, "Q", "Use Q");
                    AddBool(clearMenu, "E", "Use E");
                    champMenu.AddSubMenu(clearMenu);
                }
                var lastHitMenu = new Menu("Last Hit", "LastHit");
                {
                    AddBool(lastHitMenu, "Q", "Use Q");
                    champMenu.AddSubMenu(lastHitMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    var killStealMenu = new Menu("Kill Steal", "KillSteal");
                    {
                        AddBool(killStealMenu, "Q", "Use Q");
                        AddBool(killStealMenu, "E", "Use E");
                        AddBool(killStealMenu, "Ignite", "Use Ignite");
                        AddBool(killStealMenu, "Smite", "Use Smite");
                        miscMenu.AddSubMenu(killStealMenu);
                    }
                    var antiGapMenu = new Menu("Anti Gap Closer", "AntiGap");
                    {
                        AddBool(antiGapMenu, "W", "Use W");
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
                    champMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    AddBool(drawMenu, "QKillObj", "Minion Killable By Q", false);
                    AddBool(drawMenu, "W", "W Range", false);
                    AddBool(drawMenu, "E", "E Range", false);
                    champMenu.AddSubMenu(drawMenu);
                }
                MainMenu.AddSubMenu(champMenu);
            }
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnEnemyGapcloser += OnEnemyGapcloser;
            Orbwalk.AfterAttack += AfterAttack;
        }

        private static bool HaveQ
        {
            get { return Player.HasBuff("NasusQ"); }
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
                case Orbwalker.Mode.LastHit:
                    LastHit();
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
            if (GetValue<bool>("Draw", "QKillObj") && Q.Level > 0)
            {
                var minionObj = GetMinions(Q.Range + 300, MinionTypes.All, MinionTeam.NotAlly);
                foreach (var obj in minionObj.Where(i => CanKill(i, GetBonusDmg(i))))
                {
                    Render.Circle.DrawCircle(obj.Position, obj.BoundingRadius, Color.MediumPurple);
                }
            }
            if (GetValue<bool>("Draw", "W") && W.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
            }
            if (GetValue<bool>("Draw", "E") && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Player.IsDead || !GetValue<bool>("AntiGap", "W") ||
                !GetValue<bool>("AntiGap", gapcloser.Sender.ChampionName + "_" + gapcloser.Slot) ||
                !W.CanCast(gapcloser.Sender))
            {
                return;
            }
            W.CastOnUnit(gapcloser.Sender, PacketCast);
        }

        private static void AfterAttack(AttackableUnit target)
        {
            if (!Q.IsReady())
            {
                return;
            }
            if (Orbwalk.CurrentMode == Orbwalker.Mode.Harass && GetValue<bool>("Harass", "Q") && target is Obj_AI_Hero &&
                Q.Cast(PacketCast))
            {
                Player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (GetValue<bool>(mode, "R") && R.IsReady() && !Player.InFountain() &&
                    (Player.HealthPercent < GetValue<Slider>(mode, "RHpU").Value ||
                     Player.CountEnemiesInRange(E.Range) >= GetValue<Slider>(mode, "RCountA").Value) &&
                    R.Cast(PacketCast))
                {
                    return;
                }
                if (GetValue<bool>(mode, "Q") && (Q.IsReady() || HaveQ))
                {
                    var target = Orbwalk.GetBestHeroTarget;
                    if (target != null)
                    {
                        if (!HaveQ)
                        {
                            Q.Cast(PacketCast);
                        }
                        Orbwalk.Move = false;
                        Orbwalk.Attack = false;
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        Orbwalk.Move = true;
                        Orbwalk.Attack = true;
                    }
                }
            }
            if (GetValue<bool>(mode, "E") && E.IsReady())
            {
                var target = E.GetTarget(E.Width / 2);
                if (target != null && (mode == "Combo" || Orbwalk.InAutoAttackRange(target, 50)) &&
                    E.Cast(target, PacketCast).IsCasted())
                {
                    return;
                }
            }
            if (GetValue<bool>(mode, "W") && W.IsReady())
            {
                var target = W.GetTarget();
                if (target != null &&
                    ((mode == "Combo" && (!Orbwalk.InAutoAttackRange(target, 50) || target.HealthPercent > 30)) ||
                     (mode == "Harass" && Orbwalk.InAutoAttackRange(target, 50))))
                {
                    W.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void Clear()
        {
            SmiteMob();
            var minionObj = GetMinions(
                E.Range + E.Width / 2, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
            if (!minionObj.Any())
            {
                return;
            }
            if (GetValue<bool>("Clear", "Q") && (Q.IsReady() || HaveQ))
            {
                var obj =
                    (Obj_AI_Base)
                        ObjectManager.Get<Obj_AI_Turret>()
                            .FirstOrDefault(i => Orbwalk.InAutoAttackRange(i) && CanKill(i, GetBonusDmg(i))) ??
                    minionObj.Where(i => Orbwalk.InAutoAttackRange(i))
                        .FirstOrDefault(
                            i =>
                                CanKill(i, GetBonusDmg(i)) ||
                                !CanKill(
                                    i,
                                    GetBonusDmg(i) +
                                    Player.GetAutoAttackDamage(i, true) *
                                    Math.Floor(Q.Instance.Cooldown / 1 / Player.AttackDelay)));
                if (obj != null)
                {
                    if (!HaveQ)
                    {
                        Q.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Orbwalk.Attack = false;
                    Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
                    Orbwalk.Move = true;
                    Orbwalk.Attack = true;
                }
            }
            if (GetValue<bool>("Clear", "E") && E.IsReady())
            {
                var pos = E.GetCircularFarmLocation(minionObj.Cast<Obj_AI_Base>().ToList());
                if (pos.MinionsHit > 1)
                {
                    E.Cast(pos.Position, PacketCast);
                }
                else
                {
                    var obj = minionObj.FirstOrDefault(i => i.MaxHealth >= 1200);
                    if (obj != null)
                    {
                        E.Cast(obj, PacketCast);
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (!GetValue<bool>("LastHit", "Q") || (!Q.IsReady() && !HaveQ))
            {
                return;
            }
            var obj =
                GetMinions(Q.Range + 100, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                    .Where(i => Orbwalk.InAutoAttackRange(i))
                    .FirstOrDefault(i => CanKill(i, GetBonusDmg(i)));
            if (obj == null)
            {
                return;
            }
            if (!HaveQ)
            {
                Q.Cast(PacketCast);
            }
            Orbwalk.Move = false;
            Orbwalk.Attack = false;
            Player.IssueOrder(GameObjectOrder.AttackUnit, obj);
            Orbwalk.Move = true;
            Orbwalk.Attack = true;
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
            if (GetValue<bool>("KillSteal", "Q") && (Q.IsReady() || HaveQ))
            {
                var target = Orbwalk.GetBestHeroTarget;
                if (target != null && CanKill(target, GetBonusDmg(target)))
                {
                    if (!HaveQ)
                    {
                        Q.Cast(PacketCast);
                    }
                    Orbwalk.Move = false;
                    Orbwalk.Attack = false;
                    Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                    Orbwalk.Move = true;
                    Orbwalk.Attack = true;
                }
            }
            if (GetValue<bool>("KillSteal", "E") && E.IsReady())
            {
                var target = E.GetTarget(E.Width);
                if (target != null && E.IsKillable(target))
                {
                    E.Cast(target, PacketCast);
                }
            }
        }

        private static double GetBonusDmg(Obj_AI_Base target)
        {
            var dmgItem = 0d;
            if (Sheen.IsOwned() && (Sheen.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Iceborn.IsOwned() && (Iceborn.IsReady() || Player.HasBuff("ItemFrozenFist")))
            {
                dmgItem = Player.BaseAttackDamage * 1.25;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady() || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            return (Q.IsReady() ? Q.GetDamage(target) : 0) + Player.GetAutoAttackDamage(target, true) +
                   (dmgItem > 0 ? Player.CalcDamage(target, Damage.DamageType.Physical, dmgItem) : 0);
        }
    }
}
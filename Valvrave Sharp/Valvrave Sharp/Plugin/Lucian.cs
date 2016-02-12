namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    #endregion

    internal class Lucian : Program
    {
        #region Static Fields

        private static bool haveR;

        #endregion

        #region Constructors and Destructors

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 700);
            Q2 = new Spell(SpellSlot.Q, 1150).SetSkillshot(
                0.35f,
                65,
                float.MaxValue,
                false,
                SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 1000).SetSkillshot(0.35f, 55, 1700, true, SkillshotType.SkillshotLine);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1400).SetSkillshot(0.1f, 110, 2700, true, SkillshotType.SkillshotLine);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            W.DamageType = DamageType.Magical;
            Q2.MinHitChance = W.MinHitChance = R.MinHitChance = HitChance.High;

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
                    {
                        return;
                    }
                    if (args.Buff.DisplayName == "LucianR")
                    {
                        haveR = true;
                    }
                };
            Obj_AI_Base.OnBuffRemove += (sender, args) =>
                {
                    if (!sender.IsMe || !args.Buff.Caster.IsMe)
                    {
                        return;
                    }
                    if (args.Buff.DisplayName == "LucianR")
                    {
                        haveR = false;
                    }
                };
            Obj_AI_Base.OnDoCast += (sender, args) =>
                {
                    if (!sender.IsMe || (args.Slot != SpellSlot.Q && args.Slot != SpellSlot.W))
                    {
                        return;
                    }
                    //Variables.Orbwalker.ResetSwingTimer();
                };
            Variables.Orbwalker.OnAction += (sender, args) =>
                {
                    if (args.Type != OrbwalkingType.AfterAttack
                        || Variables.Orbwalker.GetActiveMode() != OrbwalkingMode.Combo)
                    {
                        return;
                    }
                    var target = args.Target as Obj_AI_Hero;
                    if (target == null)
                    {
                        return;
                    }
                    if (E.IsReady())
                    {
                        const float Angle = 65 * ((float)Math.PI / 180);
                        var posTemp = Vector3.Subtract(target.ServerPosition, Player.ServerPosition).ToVector2();
                        var posDash =
                            Vector2.Add(
                                new Vector2(
                                    (float)(posTemp.X * Math.Cos(Angle) - posTemp.Y * Math.Sin(Angle)) / 4,
                                    (float)(posTemp.X * Math.Sin(Angle) + posTemp.Y * Math.Cos(Angle)) / 4),
                                Player.ServerPosition.ToVector2());
                        E.Cast(posDash);
                    }
                    else
                    {
                        if (Q.IsReady())
                        {
                            Q.CastOnUnit(target);
                        }
                        else if (W.IsReady())
                        {
                            W.Cast(target.ServerPosition);
                        }
                    }
                };
        }

        #endregion

        #region Methods

        private static void Combo()
        {
            if (Variables.Orbwalker.GetTarget() != null)
            {
                return;
            }
            if (Q.IsReady())
            {
                var target = Q.GetTarget() ?? Q2.GetTarget(Q2.Width / 2);
                if (target != null)
                {
                    if (Q.IsInRange(target))
                    {
                        if (Q.CastOnUnit(target))
                        {
                            return;
                        }
                    }
                    else
                    {
                        var pred = Q2.VPrediction(target);
                        if (pred.Hitchance >= Q2.MinHitChance)
                        {
                            var objs = new List<Obj_AI_Base>();
                            objs.AddRange(GameObjects.EnemyHeroes.Where(i => !i.Compare(target)));
                            objs.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet()));
                            objs.AddRange(GameObjects.Jungle);
                            var obj =
                                objs.Where(i => i.IsValidTarget(Q.Range))
                                    .FirstOrDefault(
                                        i =>
                                        pred.UnitPosition.ToVector2()
                                            .DistanceSquared(
                                                pred.Input.From.ToVector2(),
                                                pred.Input.From.ToVector2().Extend(i.ServerPosition, Q2.Range),
                                                true) <= Math.Pow(pred.Input.RealRadius, 2));
                            if (obj != null && Q.CastOnUnit(obj))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (W.IsReady() && !Player.IsDashing())
            {
                var target = W.GetTarget(W.Width / 2);
                if (target != null && (!Q.IsInRange(target) || !Q.IsReady()))
                {
                    var pred = W.VPrediction(target, true, CollisionableObjects.YasuoWall);
                    if (pred.Hitchance >= W.MinHitChance)
                    {
                        W.Cast(pred.CastPosition);
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
            Variables.Orbwalker.SetAttackState(!haveR);
            if (haveR)
            {
                return;
            }
            switch (Variables.Orbwalker.GetActiveMode())
            {
                case OrbwalkingMode.Combo:
                    Combo();
                    break;
            }
        }

        #endregion
    }
}
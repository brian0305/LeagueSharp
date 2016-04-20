namespace Valvrave_Sharp.Plugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    using Valvrave_Sharp.Core;

    #endregion

    internal class Lucian : Program
    {
        #region Static Fields

        private static bool haveR;

        private static int lastE;

        #endregion

        #region Constructors and Destructors

        public Lucian()
        {
            Q = new Spell(SpellSlot.Q, 700);
            Q2 = new Spell(Q.Slot, 1150).SetSkillshot(0.35f, 60, float.MaxValue, false, SkillshotType.SkillshotLine);
            W = new Spell(SpellSlot.W, 1000).SetSkillshot(0.35f, 55, 1650, true, Q2.Type);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1200).SetSkillshot(0.1f, 110, 2700, true, Q2.Type);
            Q.DamageType = Q2.DamageType = E.DamageType = R.DamageType = DamageType.Physical;
            W.DamageType = DamageType.Magical;
            Q2.MinHitChance = W.MinHitChance = R.MinHitChance = HitChance.VeryHigh;

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
            Obj_AI_Base.OnProcessSpellCast += (sender, args) =>
                {
                    if (!sender.IsMe)
                    {
                        return;
                    }
                    if (args.Slot == SpellSlot.Q)
                    {
                        Player.IssueOrder(
                            GameObjectOrder.AttackTo,
                            args.Start.Extend(args.Target.Position, Player.BoundingRadius + 30));
                    }
                    else if (args.Slot == SpellSlot.W)
                    {
                        Player.IssueOrder(
                            GameObjectOrder.AttackTo,
                            args.Start.Extend(args.End, Player.BoundingRadius + 30));
                    }
                };
            Variables.Orbwalker.OnAction += (sender, args) =>
                {
                    if (args.Type != OrbwalkingType.AfterAttack)
                    {
                        return;
                    }
                    if (Variables.Orbwalker.GetActiveMode() == OrbwalkingMode.Combo)
                    {
                        var target = args.Target as Obj_AI_Hero;
                        if (target != null)
                        {
                            AfterAttackCombo(target);
                        }
                    }
                };
        }

        #endregion

        #region Properties

        private static bool IsDashing => Variables.TickCount - lastE <= 100 || Player.IsDashing();

        #endregion

        #region Methods

        private static void AfterAttackCombo(Obj_AI_Hero target)
        {
            if (E.IsReady())
            {
                var posPlayer = Player.ServerPosition.ToVector2();
                var posTarget = target.ServerPosition.ToVector2();
                var posDashTo = new Vector2();
                var posAfterE =
                    CheckDashPos(posPlayer.CircleCircleIntersection(posTarget, E.Range, 500 + Player.BoundingRadius));
                if (posAfterE.IsValid())
                {
                    posDashTo = posAfterE;
                }
                else
                {
                    posAfterE = posPlayer.Extend(posTarget, -E.Range);
                    if (Player.HealthPercent >= 80 || !posAfterE.IsUnderEnemyTurret()
                        || posAfterE.CountEnemyHeroesInRange(E.Range, target)
                        < posAfterE.CountAllyHeroesInRange(E.Range, Player))
                    {
                        posDashTo = posAfterE;
                    }
                }
                if (!posDashTo.IsValid())
                {
                    posDashTo = Game.CursorPos.ToVector2();
                }
                if (E.Cast(posDashTo))
                {
                    lastE = Variables.TickCount;
                }
            }
            else if (Q.IsReady())
            {
                Q.CastOnUnit(target);
            }
            else if (W.IsReady())
            {
                W.Cast(W.GetPredPosition(target));
            }
        }

        private static Vector2 CheckDashPos(Vector2[] vector)
        {
            if (vector.Length == 0)
            {
                return new Vector2();
            }
            foreach (var pos in vector.OrderBy(i => i.Distance(Game.CursorPos)))
            {
                if (Player.HealthPercent >= 75)
                {
                    return pos;
                }
                if (pos.IsUnderEnemyTurret() && pos.CountEnemyHeroesInRange(500) > pos.CountAllyHeroesInRange(E.Range))
                {
                    continue;
                }
                return pos;
            }
            return new Vector2();
        }

        private static void Combo()
        {
            if (Variables.Orbwalker.GetTarget() != null || IsDashing || Player.Spellbook.IsAutoAttacking)
            {
                return;
            }
            if (Q.IsReady())
            {
                var target = Q2.GetTarget(Q2.Width / 2);
                if (target != null && !Q.IsInRange(target))
                {
                    var pred = Q2.GetPrediction(target);
                    if (pred.Hitchance >= Q2.MinHitChance)
                    {
                        var objs = new List<Obj_AI_Base>();
                        objs.AddRange(GameObjects.EnemyHeroes.Where(i => !i.Compare(target)));
                        objs.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet()));
                        objs.AddRange(GameObjects.Jungle);
                        /*var obj =
                            objs.Where(i => i.IsValidTarget(Q.Range))
                                .FirstOrDefault(
                                    i =>
                                    pred.UnitPosition.ToVector2()
                                        .DistanceSquared(
                                            pred.Input.From,
                                            pred.Input.From.Extend(i.ServerPosition, Q2.Range),
                                            true) <= Math.Pow(pred.Input.RealRadius, 2));
                        if (obj != null && Q.CastOnUnit(obj))
                        {
                            return;
                        }*/
                    }
                }
            }
            if (W.IsReady())
            {
                var target = W.GetTarget(W.Width / 2);
                if (target != null && (!Q.IsInRange(target) || !Q.IsReady()))
                {
                    var pred = W.GetPrediction(target, true, -1, CollisionableObjects.YasuoWall);
                    if (pred.Hitchance >= W.MinHitChance)
                    {
                        var col = pred.GetCollision();
                        if (col.Count == 0)
                        {
                            W.Cast(pred.CastPosition);
                        }
                        else
                        {
                            foreach (var predCol in
                                col.Select(i => W.GetPrediction(i))
                                    .Where(
                                        i =>
                                        i.Hitchance >= W.MinHitChance
                                        && i.UnitPosition.Distance(pred.UnitPosition) < 250))
                            {
                                W.Cast(predCol.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        private static double GetRDmg(Obj_AI_Hero target)
        {
            return R.GetDamage(target) * new[] { 20, 25, 30 }[R.Level - 1];
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
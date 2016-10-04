namespace Valvrave_Sharp.Core
{
    #region

    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Data.Enumerations;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Enumerations;
    using LeagueSharp.SDK.Utils;

    using SharpDX;

    #endregion

    internal static class Common
    {
        #region Properties

        internal static bool CanFlash => Program.Flash != SpellSlot.Unknown && Program.Flash.IsReady();

        internal static bool CanIgnite => Program.Ignite != SpellSlot.Unknown && Program.Ignite.IsReady();

        internal static bool CanSmite => Program.Smite != SpellSlot.Unknown && Program.Smite.IsReady();

        internal static bool CantAttack
            =>
                (Variables.Orbwalker.GetTarget() == null || !Variables.Orbwalker.CanAttack)
                && Variables.Orbwalker.CanMove;

        private static int GetSmiteDmg
            =>
                new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                    Program.Player.Level - 1];

        #endregion

        #region Methods

        internal static bool CanHitCircle(this Spell spell, Obj_AI_Base unit)
        {
            return spell.IsInRange(spell.GetPredPosition(unit));
        }

        internal static bool CanLastHit(this Spell spell, Obj_AI_Base unit, double dmg, double subDmg = 0)
        {
            var hpPred = spell.GetHealthPrediction(unit);
            return hpPred > 0 && hpPred - subDmg < dmg;
        }

        internal static CastStates Casting(
            this Spell spell,
            Obj_AI_Base unit,
            bool aoe = false,
            CollisionableObjects collisionable = CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
        {
            if (!unit.IsValidTarget())
            {
                return CastStates.InvalidTarget;
            }
            if (!spell.IsReady())
            {
                return CastStates.NotReady;
            }
            if (spell.CastCondition != null && !spell.CastCondition())
            {
                return CastStates.FailedCondition;
            }
            var pred = spell.GetPrediction(unit, aoe, -1, collisionable);
            if (pred.CollisionObjects.Count > 0)
            {
                return CastStates.Collision;
            }
            if (spell.RangeCheckFrom.DistanceSquared(pred.CastPosition) > spell.RangeSqr)
            {
                return CastStates.OutOfRange;
            }
            if (pred.Hitchance < spell.MinHitChance
                && (!pred.Input.AoE || pred.Hitchance < HitChance.High || pred.AoeTargetsHitCount < 2))
            {
                return CastStates.LowHitChance;
            }
            if (!Program.Player.Spellbook.CastSpell(spell.Slot, pred.CastPosition))
            {
                return CastStates.NotCasted;
            }
            spell.LastCastAttemptT = Variables.TickCount;
            return CastStates.SuccessfullyCasted;
        }

        internal static CastStates CastingBestTarget(
            this Spell spell,
            bool aoe = false,
            CollisionableObjects collisionable = CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
        {
            return spell.Casting(spell.GetTarget(spell.Width / 2), aoe, collisionable);
        }

        internal static bool CastSpellSmite(this Spell spell, Obj_AI_Hero target, bool smiteCol)
        {
            var pred1 = spell.GetPrediction(target, false, -1, CollisionableObjects.YasuoWall);
            if (pred1.Hitchance < spell.MinHitChance)
            {
                return false;
            }
            var pred2 = spell.GetPrediction(target, false, -1, CollisionableObjects.Minions);
            return pred2.Hitchance == HitChance.Collision
                       ? smiteCol && CastSmiteKillCollision(pred2.CollisionObjects) && spell.Cast(pred1.CastPosition)
                       : pred2.Hitchance >= spell.MinHitChance && spell.Cast(pred2.CastPosition);
        }

        internal static void CastTiamatHydra()
        {
            if (Program.Tiamat.IsReady && Program.Player.CountEnemyHeroesInRange(Program.Tiamat.Range) > 0)
            {
                Program.Tiamat.Cast();
            }
            if (Program.Ravenous.IsReady && Program.Player.CountEnemyHeroesInRange(Program.Ravenous.Range) > 0)
            {
                Program.Ravenous.Cast();
            }
            if (Program.Titanic.IsReady && Variables.Orbwalker.GetTarget() != null)
            {
                Program.Titanic.Cast();
            }
        }

        internal static Vector3 GetPredPosition(this Spell spell, Obj_AI_Base unit, bool useRange = false)
        {
            var pos = Movement.GetPrediction(unit, spell.Delay, 1, spell.Speed).UnitPosition;
            return useRange && !spell.IsInRange(pos) ? unit.ServerPosition : pos;
        }

        internal static bool IsCasted(this CastStates state)
        {
            return state == CastStates.SuccessfullyCasted;
        }

        internal static bool IsWard(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Ward) && minion.CharData.BaseSkinName != "BlueTrinket";
        }

        internal static List<Obj_AI_Base> ListEnemies(bool includeClones = false)
        {
            var list = new List<Obj_AI_Base>();
            list.AddRange(GameObjects.EnemyHeroes);
            list.AddRange(ListMinions(includeClones));
            return list;
        }

        internal static List<Obj_AI_Minion> ListMinions(bool includeClones = false)
        {
            var list = new List<Obj_AI_Minion>();
            list.AddRange(GameObjects.Jungle);
            list.AddRange(GameObjects.EnemyMinions.Where(i => i.IsMinion() || i.IsPet(includeClones)));
            return list;
        }

        private static bool CastSmiteKillCollision(List<Obj_AI_Base> col)
        {
            if (col.Count > 1 || !CanSmite)
            {
                return false;
            }
            var obj = col.First();
            return obj.Health <= GetSmiteDmg && obj.DistanceToPlayer() < Program.SmiteRange
                   && Program.Player.Spellbook.CastSpell(Program.Smite, obj);
        }

        #endregion
    }
}
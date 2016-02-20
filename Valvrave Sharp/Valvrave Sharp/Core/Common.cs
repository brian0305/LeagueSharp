namespace Valvrave_Sharp.Core
{
    #region

    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    #endregion

    internal static class Common
    {
        #region Properties

        internal static int GetSmiteDmg
            =>
                new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                    Program.Player.Level - 1];

        #endregion

        #region Methods

        internal static bool CanHitCircle(this Spell spell, Obj_AI_Hero unit)
        {
            return spell.VPredictionPos(unit).DistanceToPlayer() < spell.Range;
        }

        internal static CastStates Casting(this Spell spell, Obj_AI_Base unit, bool areaOfEffect = false)
        {
            if (!unit.IsValidTarget())
            {
                return CastStates.InvalidTarget;
            }
            if (!spell.IsReady())
            {
                return CastStates.NotReady;
            }
            var prediction = spell.VPrediction(unit, areaOfEffect);
            if (prediction.CollisionObjects.Count > 0)
            {
                return CastStates.Collision;
            }
            if (prediction.Hitchance < spell.MinHitChance)
            {
                return CastStates.LowHitChance;
            }
            spell.LastCastAttemptT = Variables.TickCount;
            return !Program.Player.Spellbook.CastSpell(spell.Slot, prediction.CastPosition)
                       ? CastStates.NotCasted
                       : CastStates.SuccessfullyCasted;
        }

        internal static CastStates CastingBestTarget(this Spell spell, float extraRange = 0, bool aoe = false)
        {
            return spell.Casting(spell.GetTarget(extraRange), aoe);
        }

        internal static InventorySlot GetWardSlot()
        {
            var wardIds = new[] { 2049, 2045, 2301, 2302, 2303, 3711, 1408, 1409, 1410, 1411, 3932, 3340, 2043 };
            return
                wardIds.Where(Items.CanUseItem)
                    .Select(i => Program.Player.InventoryItems.First(slot => slot.Id == (ItemId)i))
                    .FirstOrDefault();
        }

        internal static bool IsCasted(this CastStates state)
        {
            return state == CastStates.SuccessfullyCasted;
        }

        internal static bool IsWard(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Ward) && minion.CharData.BaseSkinName != "BlueTrinket";
        }

        internal static List<Obj_AI_Base> VCollision(
            this Prediction.PredictionOutput pred,
            CollisionableObjects collisionable = CollisionableObjects.Minions)
        {
            var input = pred.Input;
            input.CollisionObjects = collisionable;
            var originalUnit = input.Unit;
            var col = Prediction.Collisions.GetCollision(new List<Vector3> { pred.UnitPosition }, input);
            col.RemoveAll(i => i.Compare(originalUnit));
            return col;
        }

        internal static FarmLocation VLineFarmLocation(this Spell spell, List<Obj_AI_Minion> minion)
        {
            return
                spell.GetLineFarmLocation(
                    minion.Select(i => spell.VPrediction(i, false, CollisionableObjects.YasuoWall))
                        .Where(i => i.Hitchance >= HitChance.VeryHigh)
                        .Select(i => i.UnitPosition)
                        .ToList()
                        .ToVector2());
        }

        internal static Prediction.PredictionOutput VPrediction(
            this Spell spell,
            Obj_AI_Base unit,
            bool aoe = false,
            CollisionableObjects collisionable = CollisionableObjects.Minions | CollisionableObjects.YasuoWall)
        {
            return
                Prediction.GetPrediction(
                    new Prediction.PredictionInput
                        {
                            Unit = unit, Delay = spell.Delay, Radius = spell.Width, Speed = spell.Speed, From = spell.From,
                            Range = spell.Range, Collision = spell.Collision, Type = spell.Type,
                            RangeCheckFrom = spell.RangeCheckFrom, AoE = aoe, CollisionObjects = collisionable
                        });
        }

        internal static Vector3 VPredictionPos(this Spell spell, Obj_AI_Hero unit)
        {
            return Prediction.GetPrediction(unit, spell.Delay).UnitPosition;
        }

        #endregion
    }
}
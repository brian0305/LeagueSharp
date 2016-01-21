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
        #region Public Methods and Operators

        public static CastStates Casting(this Spell spell, Obj_AI_Base unit, bool areaOfEffect = false)
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
            return !ObjectManager.Player.Spellbook.CastSpell(spell.Slot, prediction.CastPosition)
                       ? CastStates.NotCasted
                       : CastStates.SuccessfullyCasted;
        }

        public static CastStates CastingBestTarget(this Spell spell, float extraRange = 0, bool aoe = false)
        {
            return spell.Casting(spell.GetTarget(extraRange), aoe);
        }

        public static InventorySlot GetWardSlot()
        {
            var wardIds = new[] { 2049, 2045, 2301, 2302, 2303, 3711, 1408, 1409, 1410, 1411, 3932, 3340, 2043 };
            return
                wardIds.Where(Items.CanUseItem)
                    .Select(i => ObjectManager.Player.InventoryItems.First(slot => slot.Id == (ItemId)i))
                    .FirstOrDefault();
        }

        public static bool IsWard(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Ward) && minion.CharData.BaseSkinName != "BlueTrinket";
        }

        public static List<Obj_AI_Base> VCollision(this Spell spell, Vector2 fromVector2, List<Vector2> to)
        {
            return spell.VCollision(fromVector2.ToVector3(), to.ToVector3());
        }

        public static List<Obj_AI_Base> VCollision(this Spell spell, Vector3 fromVector3, List<Vector3> to)
        {
            return Prediction.Collisions.GetCollision(
                to,
                new Prediction.PredictionInput
                    {
                        From = fromVector3, Type = spell.Type, Radius = spell.Width, Delay = spell.Delay,
                        Speed = spell.Speed
                    });
        }

        public static FarmLocation VLineFarmLocation(this Spell spell, List<Obj_AI_Minion> minion)
        {
            return
                spell.GetLineFarmLocation(
                    minion.Select(i => spell.VPrediction(i, false, CollisionableObjects.YasuoWall))
                        .Where(i => i.Hitchance >= HitChance.High)
                        .Select(i => i.UnitPosition)
                        .ToList()
                        .ToVector2());
        }

        public static Prediction.PredictionOutput VPrediction(
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

        #endregion
    }
}
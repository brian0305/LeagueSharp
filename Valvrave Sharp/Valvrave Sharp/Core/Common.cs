namespace Valvrave_Sharp.Core
{
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

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
            var pred = spell.VPrediction(unit, areaOfEffect);
            if (pred.CollisionObjects.Count > 0)
            {
                return CastStates.Collision;
            }
            if (pred.Hitchance < spell.MinHitChance)
            {
                return CastStates.LowHitChance;
            }
            spell.LastCastAttemptT = Variables.TickCount;
            return !Program.Player.Spellbook.CastSpell(spell.Slot, pred.CastPosition)
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
            return (from wardId in wardIds
                    where Items.CanUseItem(wardId)
                    select GameObjects.Player.InventoryItems.FirstOrDefault(slot => slot.Id == (ItemId)wardId))
                .FirstOrDefault();
        }

        public static bool IsWard(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Ward) && minion.CharData.BaseSkinName != "BlueTrinket";
        }

        public static Prediction.PredictionOutput VPrediction(
            this Spell spell,
            Obj_AI_Base unit,
            bool aoe = false,
            CollisionableObjects[] collisionable = null)
        {
            return
                Prediction.GetPrediction(
                    new Prediction.PredictionInput
                        {
                            Unit = unit, Delay = spell.Delay, Radius = spell.Width, Speed = spell.Speed, From = spell.From,
                            Range = spell.Range, Collision = spell.Collision, Type = spell.Type,
                            RangeCheckFrom = spell.RangeCheckFrom, AoE = aoe,
                            CollisionObjects =
                                collisionable ?? new[] { CollisionableObjects.Minions, CollisionableObjects.YasuoWall }
                        });
        }

        #endregion
    }
}
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

        private static int GetSmiteDmg
            =>
                new[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[
                    Program.Player.Level - 1];

        #endregion

        #region Methods

        internal static bool CanHitCircle(this Spell spell, Obj_AI_Base unit)
        {
            return spell.VPredictionPos(unit).DistanceToPlayer() < spell.Range;
        }

        internal static bool CanLastHit(this Spell spell, Obj_AI_Base unit, double dmg, double subDmg = 0)
        {
            var hpPred = spell.GetHealthPrediction(unit);
            return hpPred > 0 && hpPred - subDmg < dmg;
        }

        internal static CastStates Casting(
            this Spell spell,
            Obj_AI_Base unit,
            CollisionableObjects[] collisionable = null)
        {
            if (!unit.IsValidTarget())
            {
                return CastStates.InvalidTarget;
            }
            if (!spell.IsReady())
            {
                return CastStates.NotReady;
            }
            var pred = spell.VPrediction(unit, collisionable);
            if (pred.CollisionObjects.Count > 0)
            {
                return CastStates.Collision;
            }
            if (pred.Hitchance < spell.MinHitChance
                && (!pred.Input.AoE || pred.Hitchance < HitChance.High || pred.AoeTargetsHitCount < 2))
            {
                return CastStates.LowHitChance;
            }
            spell.LastCastAttemptT = Variables.TickCount;
            return Program.Player.Spellbook.CastSpell(spell.Slot, pred.CastPosition)
                       ? CastStates.SuccessfullyCasted
                       : CastStates.NotCasted;
        }

        internal static CastStates CastingBestTarget(this Spell spell, CollisionableObjects[] collisionable = null)
        {
            return spell.Casting(spell.GetTarget(spell.Width / 2), collisionable);
        }

        internal static bool CastSmiteKillCollision(List<Obj_AI_Base> col)
        {
            if (col.Count > 1 || !Program.Smite.IsReady())
            {
                return false;
            }
            var obj = col.First();
            return obj.Health <= GetSmiteDmg && obj.DistanceToPlayer() < Program.SmiteRange
                   && Program.Player.Spellbook.CastSpell(Program.Smite, obj);
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
            CollisionableObjects[] collisionable = null)
        {
            var col = Prediction.Collisions.GetCollision(
                pred.UnitPosition,
                new Prediction.PredictionInput
                    {
                        Delay = pred.Input.Delay, Radius = pred.Input.Radius, Speed = pred.Input.Speed,
                        Range = pred.Input.Range, Type = pred.Input.Type,
                        CollisionObjects = collisionable ?? new[] { CollisionableObjects.Minions }, From = pred.Input.From
                    });
            col.RemoveAll(i => i.Compare(pred.Input.Unit));
            return col;
        }

        internal static FarmLocation VLineFarmLocation(this Spell spell, List<Obj_AI_Minion> minion)
        {
            return
                spell.GetLineFarmLocation(
                    minion.Select(i => spell.VPrediction(i, new[] { CollisionableObjects.YasuoWall }))
                        .Where(i => i.Hitchance >= spell.MinHitChance)
                        .Select(i => i.UnitPosition.ToVector2())
                        .ToList());
        }

        internal static Prediction.PredictionOutput VPrediction(
            this Spell spell,
            Obj_AI_Base unit,
            CollisionableObjects[] collisionable = null)
        {
            return
                Prediction.GetPrediction(
                    new Prediction.PredictionInput
                        {
                            Unit = unit, Delay = spell.Delay, Radius = spell.Width, Speed = spell.Speed,
                            Range = spell.Range, Collision = spell.Collision, Type = spell.Type,
                            AoE = spell.Type == SkillshotType.SkillshotCircle || !spell.Collision || spell.Width > 80,
                            CollisionObjects =
                                collisionable ?? new[] { CollisionableObjects.Minions, CollisionableObjects.YasuoWall },
                            From = spell.From
                        });
        }

        internal static Vector3 VPredictionPos(this Spell spell, Obj_AI_Base unit, bool useRange = false)
        {
            var pos = Prediction.GetPrediction(unit, spell.Delay, 1, spell.Speed).UnitPosition;
            return useRange && spell.From.Distance(pos) > spell.Range ? unit.ServerPosition : pos;
        }

        #endregion
    }
}
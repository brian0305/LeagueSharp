namespace Valvrave_Sharp.Core
{
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

    internal class Common
    {
        #region Public Methods and Operators

        public static bool CanUseSkill(OrbwalkerMode mode)
        {
            return Orbwalker.CanMove && (!Orbwalker.CanAttack || Orbwalker.GetTarget(mode) == null);
        }

        public static CastStates Cast(Spell spell, Obj_AI_Base unit, bool areaOfEffect = false)
        {
            if (!unit.IsValid())
            {
                return CastStates.InvalidTarget;
            }
            if (!spell.IsReady())
            {
                return CastStates.NotReady;
            }
            var prediction = GetPrediction(spell, unit, areaOfEffect);
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

        public static int CountEnemy(float range, Vector3 pos = default(Vector3))
        {
            return GameObjects.EnemyHeroes.Count(i => i.IsValidTarget(range, true, pos));
        }

        public static Prediction.PredictionOutput GetPrediction(
            Spell spell,
            Obj_AI_Base unit,
            bool aoe = false,
            CollisionableObjects collisionable = CollisionableObjects.Heroes | CollisionableObjects.Minions)
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
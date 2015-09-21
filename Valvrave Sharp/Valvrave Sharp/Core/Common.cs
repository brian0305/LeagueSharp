namespace Valvrave_Sharp.Core
{
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;

    using SharpDX;

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

        public static int CountEnemy(this Vector2 pos, float range)
        {
            return CountEnemy(pos.ToVector3(), range);
        }

        public static int CountEnemy(this Vector3 pos, float range)
        {
            return GameObjects.EnemyHeroes.Count(i => i.IsValidTarget(range, true, pos));
        }

        public static int CountEnemy(this Obj_AI_Base unit, float range)
        {
            return CountEnemy(unit.ServerPosition, range);
        }

        public static bool IsMinion(this Obj_AI_Minion minion)
        {
            var pets = new[]
                           {
                               "annietibbers", "elisespiderling", "heimertyellow", "heimertblue", "leblanc",
                               "malzaharvoidling", "shacobox", "shaco", "yorickspectralghoul", "yorickdecayedghoul",
                               "yorickravenousghoul", "zyrathornplant", "zyragraspingplant"
                           };
            return Minion.IsMinion(minion) || pets.Contains(minion.CharData.BaseSkinName.ToLower());
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
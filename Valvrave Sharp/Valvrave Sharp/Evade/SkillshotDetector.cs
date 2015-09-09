namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Utils;

    using SharpDX;

    internal static class SkillshotDetector
    {
        #region Constructors and Destructors

        static SkillshotDetector()
        {
            GameObject.OnDelete += ObjOnDelete;
            GameObject.OnCreate += ObjMissileClientOnCreate;
            GameObject.OnDelete += ObjMissileClientOnDelete;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        #endregion

        #region Delegates

        public delegate void OnDeleteMissileH(Skillshot skillshot, MissileClient missile);

        public delegate void OnDetectSkillshotH(Skillshot skillshot);

        #endregion

        #region Public Events

        public static event OnDeleteMissileH OnDeleteMissile;

        public static event OnDetectSkillshotH OnDetectSkillshot;

        #endregion

        #region Methods

        private static void ObjMissileClientOnCreate(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var caster = missile.SpellCaster as Obj_AI_Hero;
            if (caster == null || !caster.IsValid || caster.Team == ObjectManager.Player.Team)
            {
                return;
            }
            var spellData = SpellDatabase.GetByMissileName(missile.SData.Name);
            if (spellData == null)
            {
                return;
            }
            DelayAction.Add(0, () => ObjMissileClientOnCreateDelayed(missile, spellData));
        }

        private static void ObjMissileClientOnCreateDelayed(MissileClient missile, SpellData spellData)
        {
            var unit = missile.SpellCaster as Obj_AI_Hero;
            var missilePosition = missile.Position.ToVector2();
            var unitPosition = missile.StartPosition.ToVector2();
            var endPos = missile.EndPosition.ToVector2();
            var direction = (endPos - unitPosition).Normalized();
            if (unitPosition.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = unitPosition + direction * spellData.Range;
            }
            if (spellData.ExtraRange != -1)
            {
                endPos = endPos
                         + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(unitPosition)) * direction;
            }
            var castTime = Variables.TickCount - Game.Ping / 2 - (spellData.MissileDelayed ? 0 : spellData.Delay)
                           - (int)(1000f * missilePosition.Distance(unitPosition) / spellData.MissileSpeed);
            spellData.TriggerOnDetectSkillshot(DetectionType.RecvPacket, castTime, unitPosition, endPos, unit);
        }

        private static void ObjMissileClientOnDelete(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;
            if (missile == null || !missile.IsValid)
            {
                return;
            }
            var caster = missile.SpellCaster as Obj_AI_Hero;
            if (caster == null || !caster.IsValid || caster.Team == ObjectManager.Player.Team)
            {
                return;
            }
            var spellName = missile.SData.Name;
            if (OnDeleteMissile != null)
            {
                foreach (var skillshot in
                    Evade.DetectedSkillshots.Where(
                        i =>
                        i.SpellData.MissileSpellName == spellName && i.Unit.NetworkId == caster.NetworkId
                        && (missile.EndPosition - missile.StartPosition).ToVector2().AngleBetween(i.Direction) < 10
                        && i.SpellData.CanBeRemoved))
                {
                    OnDeleteMissile(skillshot, missile);
                    break;
                }
            }
            Evade.DetectedSkillshots.RemoveAll(
                i =>
                (i.SpellData.MissileSpellName == spellName || i.SpellData.ExtraMissileNames.Contains(spellName))
                && i.Unit.NetworkId == caster.NetworkId
                && (missile.EndPosition - missile.StartPosition).ToVector2().AngleBetween(i.Direction) < 10
                && (i.SpellData.CanBeRemoved || i.SpellData.ForceRemove));
        }

        private static void ObjOnDelete(GameObject sender, EventArgs args)
        {
            if (sender == null || !sender.IsValid || sender.Team == ObjectManager.Player.Team)
            {
                return;
            }
            for (var i = Evade.DetectedSkillshots.Count - 1; i >= 0; i--)
            {
                var skillshot = Evade.DetectedSkillshots[i];
                if (skillshot.SpellData.ToggleParticleName != ""
                    && new Regex(skillshot.SpellData.ToggleParticleName).IsMatch(sender.Name))
                {
                    Evade.DetectedSkillshots.RemoveAt(i);
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var caster = sender as Obj_AI_Hero;
            if (caster == null || !caster.IsValid || caster.Team == ObjectManager.Player.Team)
            {
                return;
            }
            if (args.SData.Name == "dravenrdoublecast")
            {
                Evade.DetectedSkillshots.RemoveAll(
                    i => i.Unit.NetworkId == caster.NetworkId && i.SpellData.SpellName == "DravenRCast");
            }
            var spellData = SpellDatabase.GetByName(args.SData.Name);
            if (spellData == null)
            {
                return;
            }
            var startPos = new Vector2();
            if (spellData.FromObject != "")
            {
                foreach (var obj in GameObjects.AllGameObjects.Where(i => i.Name.Contains(spellData.FromObject)))
                {
                    startPos = obj.Position.ToVector2();
                }
            }
            else
            {
                startPos = caster.ServerPosition.ToVector2();
            }
            if (spellData.FromObjects != null && spellData.FromObjects.Length > 0)
            {
                foreach (var obj in
                    GameObjects.AllGameObjects.Where(i => i.IsEnemy && spellData.FromObject.Contains(i.Name)))
                {
                    var start = obj.Position.ToVector2();
                    var end = start + spellData.Range * (args.End - obj.Position).ToVector2().Normalized();
                    spellData.TriggerOnDetectSkillshot(
                        DetectionType.ProcessSpell,
                        Variables.TickCount - Game.Ping / 2,
                        start,
                        end,
                        caster);
                }
            }
            if (!startPos.IsValid())
            {
                return;
            }
            var endPos = args.End.ToVector2();
            if (spellData.SpellName == "LucianQ" && args.Target.Compare(ObjectManager.Player))
            {
                return;
            }
            var direction = (endPos - startPos).Normalized();
            if (startPos.Distance(endPos) > spellData.Range || spellData.FixedRange)
            {
                endPos = startPos + direction * spellData.Range;
            }
            if (spellData.ExtraRange != -1)
            {
                endPos = endPos
                         + Math.Min(spellData.ExtraRange, spellData.Range - endPos.Distance(startPos)) * direction;
            }
            spellData.TriggerOnDetectSkillshot(
                DetectionType.ProcessSpell,
                Variables.TickCount - Game.Ping / 2,
                startPos,
                endPos,
                caster);
        }

        private static void TriggerOnDetectSkillshot(
            this SpellData spellData,
            DetectionType detectionType,
            int startT,
            Vector2 start,
            Vector2 end,
            Obj_AI_Base unit)
        {
            if (OnDetectSkillshot != null)
            {
                OnDetectSkillshot(new Skillshot(detectionType, spellData, startT, start, end, unit));
            }
        }

        #endregion
    }
}
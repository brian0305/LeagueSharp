namespace vEvade.Spells
{
    #region

    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.Core;
    using vEvade.Helpers;
    using vEvade.Managers;

    #endregion

    public class SpellDetector
    {
        #region Static Fields

        private static int lastCast;

        private static int spellIdCount;

        #endregion

        #region Constructors and Destructors

        public SpellDetector()
        {
            GameObject.OnCreate += OnCreateTrap;
            GameObject.OnDelete += OnDeleteTrap;
            GameObject.OnCreate += OnCreateToggle;
            GameObject.OnDelete += OnDeleteToggle;
            GameObject.OnCreate += OnCreateMissile;
            GameObject.OnDelete += OnDeleteMissile;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            if (!Configs.Debug)
            {
                return;
            }

            Obj_AI_Base.OnNewPath += DebugOnNewPath;
            GameObject.OnCreate += DebugOnCreate;
        }

        #endregion

        #region Delegates

        public delegate void OnCreateSpellH(
            Obj_AI_Base sender,
            MissileClient missile,
            SpellData data,
            SpellArgs spellArgs);

        public delegate void OnProcessSpellH(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs);

        #endregion

        #region Public Events

        public static event OnCreateSpellH OnCreateSpell;

        public static event OnProcessSpellH OnProcessSpell;

        #endregion

        #region Public Methods and Operators

        public static void AddSpell(
            Obj_AI_Base sender,
            Vector2 spellStart,
            Vector2 spellEnd,
            SpellData data,
            MissileClient missile = null,
            SpellType type = SpellType.None,
            bool checkExplosion = true,
            int startT = 0)
        {
            if (!sender.IsVisible && !Configs.DodgeFoW)
            {
                return;
            }

            if (!Configs.Debug && Evade.PlayerPosition.Distance(spellStart) > (data.Range + data.Radius + 1000) * 1.5)
            {
                return;
            }

            if (type == SpellType.None)
            {
                type = data.Type;
            }

            var startPos = spellStart;
            var endPos = spellEnd;
            var startTime = startT > 0 ? startT : Utils.GameTimeTickCount - Game.Ping / 2;
            var endTime = data.Delay;

            if (missile == null)
            {
                if (data.BehindStart > 0)
                {
                    startPos = startPos.Extend(endPos, -data.BehindStart);
                }

                if (data.InfrontStart > 0)
                {
                    startPos = startPos.Extend(endPos, data.InfrontStart);
                }
            }
            else
            {
                if (!data.MissileDelayed)
                {
                    startTime -= data.Delay;
                }

                if (data.MissileSpeed != 0)
                {
                    startTime -= (int)(startPos.Distance(missile.Position) / data.MissileSpeed * 1000);
                }
            }

            if (type == SpellType.Cone || type == SpellType.MissileCone || data.FixedRange
                || (data.Range > 0 && endPos.Distance(startPos) > data.Range))
            {
                endPos = startPos.Extend(endPos, data.Range);
            }

            if (missile == null)
            {
                if (data.ExtraRange > 0)
                {
                    endPos = endPos.Extend(startPos, -Math.Min(data.ExtraRange, data.Range - endPos.Distance(startPos)));
                }

                if (data.Invert)
                {
                    endPos = startPos.Extend(endPos, -startPos.Distance(endPos));
                }

                if (data.Perpendicular)
                {
                    var pDir = (endPos - startPos).Normalized().Perpendicular();
                    startPos = spellEnd - pDir * data.RadiusEx;
                    endPos = spellEnd + pDir * data.RadiusEx;
                }
            }

            switch (type)
            {
                case SpellType.MissileLine:
                    endTime += data.MissileAccel != 0
                                   ? 5000
                                   : (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);
                    break;
                case SpellType.Circle:
                    if (data.MissileSpeed != 0)
                    {
                        endTime += (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);

                        if (data.Type == SpellType.MissileLine && data.HasStartExplosion)
                        {
                            endPos = startPos;
                            endTime = data.Delay;
                        }
                    }
                    else if (data.Range == 0 && data.Radius > 0)
                    {
                        endPos = startPos;
                    }
                    break;
                case SpellType.Arc:
                case SpellType.MissileCone:
                    endTime += (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);
                    break;
            }

            var dir = (endPos - startPos).Normalized();
            var alreadyAdded = false;

            if (missile == null)
            {
                if (!data.DontCheckForDuplicates)
                {
                    foreach (var spell in
                        Evade.DetectedSpells.Values.Where(
                            i =>
                            i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId
                            && dir.AngleBetween(i.Direction) < 3 && i.Start.Distance(startPos) < 100))
                    {
                        alreadyAdded = spell.MissileObject != null && spell.MissileObject.IsValid;

                        if (alreadyAdded)
                        {
                            break;
                        }
                    }
                }
            }
            else if (!data.MissileOnly)
            {
                foreach (var spell in
                    Evade.DetectedSpells.Values.Where(
                        i =>
                        i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId
                        && dir.AngleBetween(i.Direction) < 3 && i.Start.Distance(startPos) < 100
                        && i.MissileObject == null))
                {
                    spell.MissileObject = missile;
                    spell.Start = missile.StartPosition.To2D();
                    alreadyAdded = true;

                    if (Configs.Debug)
                    {
                        Console.WriteLine($"=> M: {spell.SpellId} | {Utils.GameTimeTickCount}");
                    }

                    break;
                }
            }

            if (alreadyAdded)
            {
                return;
            }

            if (checkExplosion && (data.HasStartExplosion || data.HasEndExplosion))
            {
                var newData = (SpellData)data.Clone();

                if (data.HasStartExplosion)
                {
                    newData.CollisionObjects = null;
                }

                AddSpell(sender, spellStart, spellEnd, newData, missile, SpellType.Circle, false, startT);
            }

            var newSpell = new SpellInstance(data, startTime, endTime + data.ExtraDelay, startPos, endPos, sender, type)
                               { SpellId = spellIdCount++, MissileObject = missile };
            Evade.DetectedSpells.Add(newSpell.SpellId, newSpell);

            if (Configs.Debug)
            {
                Console.WriteLine($"=> A: {newSpell.SpellId} | {Utils.GameTimeTickCount}");
            }
        }

        public static void AddSpell(
            Obj_AI_Base sender,
            Vector3 spellStart,
            Vector3 spellEnd,
            SpellData data,
            MissileClient missile = null,
            SpellType type = SpellType.None,
            bool checkExplosion = true,
            int startT = 0)
        {
            AddSpell(sender, spellStart.To2D(), spellEnd.To2D(), data, missile, type, checkExplosion, startT);
        }

        #endregion

        #region Methods

        private static void DebugOnCreate(GameObject sender, EventArgs args)
        {
            if (Evade.PlayerPosition.Distance(sender.Position) < 500)
            {
                Console.WriteLine(
                    $"{sender.Name} [{sender.Type}]: {sender.Team} - {ObjectManager.Player.Team} | {Utils.GameTimeTickCount}");
            }
        }

        private static void DebugOnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (args.IsDash && !sender.IsMe)
            {
                Console.WriteLine(
                    $"{Utils.GameTimeTickCount} {sender.CharData.BaseSkinName} [{sender.Team}] => Speed: {args.Speed}");
            }
        }

        private static void OnCreateMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;

            if (missile == null || !missile.IsValid)
            {
                return;
            }

            var caster = missile.SpellCaster;

            if (!caster.IsValid() || (!caster.IsEnemy && !Configs.Debug))
            {
                return;
            }

            if (Configs.Debug && caster.IsMe)
            {
                Console.WriteLine(
                    $"{missile.SData.Name}: {missile.SData.CastRange} | {missile.SData.CastRangeDisplayOverride} | {Utils.GameTimeTickCount - lastCast} | {missile.SData.LineWidth} | {missile.SData.MissileSpeed} | {missile.SData.MissileAccel} | {missile.SData.MissileMinSpeed} | {missile.SData.MissileMaxSpeed} | {missile.SData.CastRadius} | {missile.SData.CastRadiusSecondary}");
            }

            SpellData data;

            if (!Evade.OnMissileSpells.TryGetValue(missile.SData.Name, out data))
            {
                return;
            }

            Utility.DelayAction.Add(0, () => OnCreateMissileDelay(missile, caster, data));
        }

        private static void OnCreateMissileDelay(MissileClient missile, Obj_AI_Base caster, SpellData data)
        {
            var spellArgs = new SpellArgs();
            OnCreateSpell?.Invoke(caster, missile, data, spellArgs);

            if (spellArgs.NoProcess)
            {
                return;
            }

            if (spellArgs.NewData != null)
            {
                data = spellArgs.NewData;
            }

            AddSpell(caster, missile.StartPosition, missile.EndPosition, data, missile);
        }

        private static void OnCreateToggle(GameObject sender, EventArgs args)
        {
            var toggle = sender as Obj_GeneralParticleEmitter;

            if (toggle == null || !toggle.IsValid)
            {
                return;
            }

            var pos = toggle.Position.To2D();

            if (Configs.Debug && Evade.PlayerPosition.Distance(pos) < 500)
            {
                Console.WriteLine(
                    $"{toggle.Name}: {toggle.Team} - {ObjectManager.Player.Team} | {Utils.GameTimeTickCount}");
            }

            foreach (var spell in
                Evade.DetectedSpells.Values.Where(
                    i =>
                    !string.IsNullOrEmpty(i.Data.ToggleName) && i.MissileObject != null && i.ToggleObject == null
                    && new Regex(i.Data.ToggleName).IsMatch(toggle.Name) && i.End.Distance(pos) < 100))
            {
                spell.MissileObject = null;
                spell.ToggleObject = toggle;
                spell.End = pos;

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> T: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnCreateTrap(GameObject sender, EventArgs args)
        {
            var trap = sender as Obj_AI_Minion;

            if (trap == null || !trap.IsValid)
            {
                return;
            }

            if (!trap.IsEnemy && !Configs.Debug)
            {
                return;
            }

            if (Configs.Debug && Evade.PlayerPosition.Distance(trap) < 500)
            {
                Console.WriteLine(
                    $"{trap.CharData.BaseSkinName}: {trap.Team} - {ObjectManager.Player.Team} | {trap.BoundingRadius} | {Utils.GameTimeTickCount}");
            }

            SpellData data;

            if (Evade.OnTrapSpells.TryGetValue(trap.CharData.BaseSkinName, out data))
            {
                Utility.DelayAction.Add(0, () => OnCreateTrapDelay(trap, data));
            }
        }

        private static void OnCreateTrapDelay(Obj_AI_Minion trap, SpellData data)
        {
            var pos = trap.ServerPosition.To2D();
            var caster =
                HeroManager.AllHeroes.First(i => i.ChampionName == data.ChampName && (i.IsEnemy || Configs.Debug));
            var spell = new SpellInstance(data, Utils.GameTimeTickCount - Game.Ping / 2, 0, pos, pos, caster, data.Type)
                            { SpellId = spellIdCount++, TrapObject = trap };
            Evade.DetectedSpells.Add(spell.SpellId, spell);

            if (Configs.Debug)
            {
                Console.WriteLine($"=> A-Tr: {spell.SpellId} | {Utils.GameTimeTickCount}");
            }
        }

        private static void OnDeleteMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;

            if (missile == null || !missile.IsValid)
            {
                return;
            }

            var caster = missile.SpellCaster;

            if (!caster.IsValid() || (!caster.IsEnemy && !Configs.Debug))
            {
                return;
            }

            foreach (var spell in
                Evade.DetectedSpells.Values.Where(
                    i =>
                    i.Data.CanBeRemoved && i.MissileObject != null && i.MissileObject.NetworkId == missile.NetworkId))
            {
                if (string.IsNullOrEmpty(spell.Data.ToggleName) || spell.Type != SpellType.Circle)
                {
                    Utility.DelayAction.Add(1, () => Evade.DetectedSpells.Remove(spell.SpellId));

                    if (Configs.Debug)
                    {
                        Console.WriteLine($"=> D-M: {spell.SpellId} | {Utils.GameTimeTickCount}");
                    }
                }
                else
                {
                    spell.Data.CollisionObjects = null;
                    spell.End = missile.Position.To2D();

                    Utility.DelayAction.Add(
                        100,
                        () =>
                            {
                                if (spell.ToggleObject == null)
                                {
                                    Evade.DetectedSpells.Remove(spell.SpellId);
                                }
                            });
                }
            }
        }

        private static void OnDeleteToggle(GameObject sender, EventArgs args)
        {
            var toggle = sender as Obj_GeneralParticleEmitter;

            if (toggle == null || !toggle.IsValid)
            {
                return;
            }

            foreach (var spell in
                Evade.DetectedSpells.Values.Where(
                    i =>
                    !string.IsNullOrEmpty(i.Data.ToggleName) && i.ToggleObject != null
                    && i.ToggleObject.NetworkId == toggle.NetworkId))
            {
                Utility.DelayAction.Add(1, () => Evade.DetectedSpells.Remove(spell.SpellId));

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-T: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnDeleteTrap(GameObject sender, EventArgs args)
        {
            var trap = sender as Obj_AI_Minion;

            if (trap == null || !trap.IsValid || (!trap.IsEnemy && !Configs.Debug))
            {
                return;
            }

            foreach (var spell in
                Evade.DetectedSpells.Values.Where(
                    i =>
                    !string.IsNullOrEmpty(i.Data.TrapName) && i.TrapObject != null
                    && i.TrapObject.NetworkId == trap.NetworkId))
            {
                Utility.DelayAction.Add(1, () => Evade.DetectedSpells.Remove(spell.SpellId));

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-Tr: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy && !Configs.Debug)
            {
                return;
            }

            if (Configs.Debug && sender.IsMe)
            {
                Console.WriteLine(
                    $"{args.SData.Name}: {Utils.GameTimeTickCount - lastCast} | {args.SData.CastRange} | {args.SData.CastRangeDisplayOverride} | {args.SData.LineWidth} | {args.SData.MissileSpeed} | {args.SData.CastRadius} | {args.SData.CastRadiusSecondary}");
                lastCast = Utils.GameTimeTickCount;
            }

            SpellData data;

            if (!Evade.OnProcessSpells.TryGetValue(args.SData.Name, out data) || data.MissileOnly)
            {
                return;
            }

            var spellArgs = new SpellArgs();
            OnProcessSpell?.Invoke(sender, args, data, spellArgs);

            if (spellArgs.NoProcess)
            {
                return;
            }

            if (spellArgs.NewData != null)
            {
                data = spellArgs.NewData;
            }

            AddSpell(sender, sender.ServerPosition, args.End, data);
        }

        #endregion
    }

    public class SpellArgs : EventArgs
    {
        #region Fields

        public SpellData NewData = null;

        public bool NoProcess;

        #endregion
    }
}
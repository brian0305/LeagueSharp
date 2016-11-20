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

    internal static class SpellDetector
    {
        #region Static Fields

        private static int lastCast;

        private static int spellIdCount;

        #endregion

        #region Constructors and Destructors

        static SpellDetector()
        {
            GameObject.OnCreate += OnCreateTrap;
            GameObject.OnDelete += OnDeleteTrap;
            GameObject.OnCreate += OnCreateToggle;
            GameObject.OnDelete += OnDeleteToggle;
            GameObject.OnCreate += OnCreateMissile;
            GameObject.OnDelete += OnDeleteMissile;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;

            if (Configs.Debug)
            {
                Obj_AI_Base.OnNewPath += DebugOnNewPath;
                GameObject.OnCreate += DebugOnCreate;
            }
        }

        #endregion

        #region Delegates

        public delegate void OnCreateSpellEvent(
            Obj_AI_Base sender,
            MissileClient missile,
            SpellData data,
            SpellArgs spellArgs);

        public delegate void OnProcessSpellEvent(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs);

        #endregion

        #region Public Events

        public static event OnCreateSpellEvent OnCreateSpell;

        public static event OnProcessSpellEvent OnProcessSpell;

        #endregion

        #region Public Methods and Operators

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
            if (missile != null && !sender.IsVisible && !Configs.Menu.Item("DodgeFoW").GetValue<bool>())
            {
                return;
            }

            if (Evade.PlayerPosition.Distance(spellStart) > (data.Range + data.Radius + 1000) * 1.5)
            {
                return;
            }

            if (checkExplosion)
            {
                if (data.HasStartExplosion || data.HasEndExplosion)
                {
                    AddSpell(sender, spellStart, spellEnd, data, missile, data.Type, false);
                    var newData = (SpellData)data.Clone();
                    newData.CollisionObjects = null;

                    if (data.HasEndExplosion && !newData.SpellName.EndsWith("_EndExp"))
                    {
                        newData.SpellName += "_EndExp";
                    }

                    AddSpell(sender, spellStart, spellEnd, newData, missile, SpellType.Circle, false);

                    return;
                }

                if (data.UseEndPosition)
                {
                    AddSpell(sender, spellStart, spellEnd, data, missile, data.Type, false);
                    AddSpell(sender, spellStart, spellEnd, data, missile, SpellType.Circle, false);

                    return;
                }
            }

            if (type == SpellType.None)
            {
                type = data.Type;
            }

            var startPos = spellStart.To2D();
            var endPos = spellEnd.To2D();
            var startTime = startT > 0 ? startT : Utils.GameTimeTickCount;
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
            else if (!data.MissileDelayed && data.Delay > 0)
            {
                startTime -= data.Delay;
            }

            if (data.Type == SpellType.Cone || data.Type == SpellType.MissileCone || data.FixedRange
                || (data.Range > 0 && endPos.Distance(startPos) > data.Range))
            {
                endPos = startPos.Extend(endPos, data.Range);
            }

            if (missile == null)
            {
                if (data.Invert)
                {
                    endPos = startPos.Extend(endPos, -startPos.Distance(endPos));
                    //endPos = startPos + (startPos - endPos).Normalized() * startPos.Distance(endPos);
                }

                if (data.Perpendicular)
                {
                    var dirPerpendicular = (endPos - startPos).Normalized().Perpendicular();
                    startPos = spellEnd.To2D() - dirPerpendicular * data.RadiusEx;
                    endPos = spellEnd.To2D() + dirPerpendicular * data.RadiusEx;
                }
            }

            switch (type)
            {
                case SpellType.MissileLine:
                    if (data.MissileAccel != 0)
                    {
                        endTime += 5000;
                    }
                    else
                    {
                        endTime += (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);
                    }
                    break;
                case SpellType.Circle:
                    if (data.MissileSpeed != 0)
                    {
                        endTime += (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);

                        if (data.Type == SpellType.MissileLine)
                        {
                            if (data.HasStartExplosion)
                            {
                                endPos = startPos;
                                endTime = data.Delay;
                            }
                            else if (data.UseEndPosition)
                            {
                                //endPos = spellEnd.To2D();
                                endTime = data.Delay + (int)(startPos.Distance(endPos) / data.MissileSpeed * 1000);
                            }
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

            if (missile == null ? !data.DontCheckForDuplicates : !data.MissileOnly)
            {
                foreach (var spell in
                    Evade.SpellsDetected.Values.Where(
                        i =>
                        i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId
                        && dir.AngleBetween(i.Direction) < 3 && i.Start.Distance(startPos) < 100))
                {
                    if (missile == null)
                    {
                        alreadyAdded = spell.MissileObject != null;
                    }
                    else if (spell.MissileObject == null)
                    {
                        spell.MissileObject = missile;
                        alreadyAdded = true;

                        if (Configs.Debug)
                        {
                            Console.WriteLine($"=> M: {spell.SpellId} | {Utils.GameTimeTickCount}");
                        }
                    }
                }
            }

            if (alreadyAdded)
            {
                return;
            }

            var newSpell = new SpellInstance(data, startTime, endTime, startPos, endPos, sender, type)
                               { SpellId = spellIdCount++, MissileObject = missile };
            Evade.SpellsDetected.Add(newSpell.SpellId, newSpell);

            if (Configs.Debug)
            {
                Console.WriteLine($"=> A: {newSpell.SpellId} | {Utils.GameTimeTickCount}");
            }
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
            if (!args.IsDash || sender.IsMe)
            {
                return;
            }

            Console.WriteLine($"{Utils.GameTimeTickCount} Dash => Speed: {args.Speed}");
        }

        private static void OnCreateMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;

            if (missile == null || !missile.IsValid)
            {
                return;
            }

            var caster = missile.SpellCaster;

            if (caster.IsValid() && (caster.IsEnemy || Configs.Debug))
            {
                Utility.DelayAction.Add(0, () => OnCreateMissileDelay(caster, missile, missile.SData.Name));
            }
        }

        private static void OnCreateMissileDelay(Obj_AI_Base caster, MissileClient missile, string name)
        {
            if (Configs.Debug && caster.IsMe)
            {
                Console.WriteLine(
                    $"{name}: {missile.SData.CastRange} | {Utils.GameTimeTickCount - lastCast} | {missile.SData.LineWidth} | {missile.SData.MissileSpeed} | {missile.SData.MissileAccel} | {missile.SData.MissileMinSpeed} | {missile.SData.MissileMaxSpeed}");
            }

            SpellData data;

            if (!Evade.OnMissileSpells.TryGetValue(name, out data))
            {
                return;
            }

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

            if (toggle != null && toggle.IsValid)
            {
                Utility.DelayAction.Add(0, () => OnCreateToggleDelay(toggle, toggle.Name));
            }
        }

        private static void OnCreateToggleDelay(Obj_GeneralParticleEmitter toggle, string name)
        {
            if (Configs.Debug && Evade.PlayerPosition.Distance(toggle.Position) < 500)
            {
                Console.WriteLine(
                    $"{toggle.Name}: {toggle.Team} - {ObjectManager.Player.Team} | {Utils.GameTimeTickCount}");
            }

            foreach (var spell in
                Evade.SpellsDetected.Values.Where(
                    i =>
                    i.MissileObject != null && i.ToggleObject == null && i.Data.ToggleName != ""
                    && new Regex(i.Data.ToggleName).IsMatch(name) && i.End.Distance(toggle.Position) < 100))
            {
                spell.ToggleObject = toggle;
                spell.MissileObject = null;

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

            if (trap.IsEnemy || Configs.Debug)
            {
                Utility.DelayAction.Add(0, () => OnCreateTrapDelay(trap, trap.CharData.BaseSkinName));
            }
        }

        private static void OnCreateTrapDelay(Obj_AI_Minion trap, string name)
        {
            if (Configs.Debug && Evade.PlayerPosition.Distance(trap) < 500)
            {
                Console.WriteLine(
                    $"{name}: {trap.Team} - {ObjectManager.Player.Team} | {trap.BoundingRadius} | {Utils.GameTimeTickCount}");
            }

            SpellData data;

            if (!Evade.OnTrapSpells.TryGetValue(name, out data))
            {
                return;
            }

            var trapPos = trap.ServerPosition.To2D();
            var sender =
                HeroManager.AllHeroes.First(i => i.ChampionName == data.ChampName && (i.IsEnemy || Configs.Debug));
            var alreadyAdded = false;

            foreach (var spell in
                Evade.SpellsDetected.Values.Where(
                    i =>
                    i.Data.TrapName != "" && i.Data.TrapName == name && i.End.Distance(trapPos) < 100
                    && i.Unit.NetworkId == sender.NetworkId))
            {
                if (spell.TrapObject != null)
                {
                    alreadyAdded = spell.TrapObject.NetworkId == trap.NetworkId;
                }
                else
                {
                    spell.TrapObject = trap;
                    alreadyAdded = true;

                    if (Configs.Debug)
                    {
                        Console.WriteLine($"=> Tr: {spell.SpellId} | {Utils.GameTimeTickCount}");
                    }
                }
            }

            if (alreadyAdded)
            {
                return;
            }

            var newSpell = new SpellInstance(
                data,
                Utils.GameTimeTickCount,
                data.Delay,
                trapPos,
                trapPos,
                sender,
                data.Type) { SpellId = spellIdCount++, TrapObject = trap };
            Evade.SpellsDetected.Add(newSpell.SpellId, newSpell);

            if (Configs.Debug)
            {
                Console.WriteLine($"=> A-Tr: {newSpell.SpellId} | {Utils.GameTimeTickCount}");
            }
        }

        private static void OnDeleteMissile(GameObject sender, EventArgs args)
        {
            var missile = sender as MissileClient;

            if (missile == null || !missile.IsValid)
            {
                return;
            }

            foreach (var spell in
                Evade.SpellsDetected.Values.Where(
                    i =>
                    i.MissileObject != null && i.MissileObject.NetworkId == missile.NetworkId && i.Data.CanBeRemoved))
            {
                if (spell.Data.ToggleName == "")
                {
                    Utility.DelayAction.Add(1, () => Evade.SpellsDetected.Remove(spell.SpellId));
                }
                else
                {
                    Utility.DelayAction.Add(
                        100,
                        () =>
                            {
                                if (spell.ToggleObject == null)
                                {
                                    Evade.SpellsDetected.Remove(spell.SpellId);
                                }
                            });
                }

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-M: {spell.SpellId} | {Utils.GameTimeTickCount}");
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
                Evade.SpellsDetected.Values.Where(
                    i => i.ToggleObject != null && i.ToggleObject.NetworkId == toggle.NetworkId))
            {
                Utility.DelayAction.Add(1, () => Evade.SpellsDetected.Remove(spell.SpellId));

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-T: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnDeleteTrap(GameObject sender, EventArgs args)
        {
            var trap = sender as Obj_AI_Minion;

            if (trap == null || !trap.IsValid)
            {
                return;
            }

            foreach (var spell in
                Evade.SpellsDetected.Values.Where(i => i.TrapObject != null && i.TrapObject.NetworkId == trap.NetworkId)
                )
            {
                Utility.DelayAction.Add(1, () => Evade.SpellsDetected.Remove(spell.SpellId));

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-Tr: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Configs.Debug && sender.IsMe)
            {
                Console.WriteLine(
                    $"{args.SData.Name} ({args.Slot}): {Utils.GameTimeTickCount - lastCast} | {args.SData.CastRange} | {args.SData.LineWidth} | {args.SData.MissileSpeed} | {args.SData.CastRadius} | {args.SData.CastRadiusSecondary}");
                lastCast = Utils.GameTimeTickCount;
            }

            if (!sender.IsEnemy && !Configs.Debug)
            {
                return;
            }

            if (args.SData.Name == "DravenRDoublecast")
            {
                foreach (var spell in
                    Evade.SpellsDetected.Values.Where(
                        i =>
                        i.MissileObject != null && i.Data.MenuName == "DravenR" && i.Unit.NetworkId == sender.NetworkId)
                    )
                {
                    Utility.DelayAction.Add(1, () => Evade.SpellsDetected.Remove(spell.SpellId));
                }
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
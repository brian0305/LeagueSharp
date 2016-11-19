namespace vEvade.Core
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using vEvade.EvadeSpells;
    using vEvade.Helpers;
    using vEvade.Managers;
    using vEvade.PathFinding;
    using vEvade.Spells;

    using Color = System.Drawing.Color;
    using SpellData = vEvade.Spells.SpellData;

    #endregion

    internal class Evade
    {
        #region Static Fields

        public static readonly Dictionary<string, SpellData> OnMissileSpells = new Dictionary<string, SpellData>();

        public static readonly Dictionary<string, SpellData> OnProcessSpells = new Dictionary<string, SpellData>();

        public static int LastWardJumpTick;

        public static SpellList<int, SpellInstance> SpellsDetected = new SpellList<int, SpellInstance>();

        private static Vector2 evadePos, evadeToPos;

        private static bool evading;

        private static bool forceFollowPath;

        private static bool haveSolution;

        private static int lastMoveTick, lastMoveTick2, lastEvadePosChangeTick;

        private static Vector2 previousPos;

        #endregion

        #region Public Properties

        public static bool Evading
        {
            get
            {
                return evading;
            }
            set
            {
                if (value)
                {
                    forceFollowPath = true;
                    lastMoveTick = 0;
                    evadePos.Move();
                }

                evading = value;
            }
        }

        public static Vector2 PlayerPosition => ObjectManager.Player.ServerPosition.To2D();

        #endregion

        #region Public Methods and Operators

        public static bool IsAboutToHit(int time, Obj_AI_Base unit = null)
        {
            time += 150;

            if (unit == null)
            {
                unit = ObjectManager.Player;
            }

            return SpellsDetected.Values.Any(i => i.Enable && i.IsAboutToHit(time, unit));
        }

        public static SafePath IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersects = new List<FindIntersect>();

            foreach (var spell in SpellsDetected.Values.Where(i => i.Enable))
            {
                var result = spell.IsSafePath(path, timeOffset, speed, delay);
                isSafe = isSafe && result.IsSafe;

                if (result.Intersect.Valid)
                {
                    intersects.Add(result.Intersect);
                }
            }

            if (!isSafe)
            {
                var intersect = intersects.MinOrDefault(i => i.Distance);

                return new SafePath(false, intersect.Valid ? intersect : new FindIntersect());
            }

            return new SafePath(true, new FindIntersect());
        }

        public static IsSafeResult IsSafePos(Vector2 pos)
        {
            var result = new IsSafeResult { Spells = new List<SpellInstance>() };

            foreach (var spell in SpellsDetected.Values.Where(i => i.Enable && i.IsDanger(pos)))
            {
                result.Spells.Add(spell);
            }

            result.IsSafe = result.Spells.Count == 0;

            return result;
        }

        public static bool IsSafeToBlink(Vector2 pos, int timeOffset, int delay)
        {
            return SpellsDetected.Values.Where(i => i.Enable).All(i => i.IsSafeToBlink(pos, timeOffset, delay));
        }

        public static void OnGameLoad(EventArgs args)
        {
            Configs.CreateMenu();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Drawing.OnDraw += OnDraw;
            CustomEvents.Unit.OnDash += OnDash;
            Orbwalking.BeforeAttack += BeforeAttack;
            SpellsDetected.OnAdd += (sender, eventArgs) => { Evading = false; };
            Collisions.Init();
        }

        #endregion

        #region Methods

        private static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (Evading)
            {
                args.Process = false;
            }
        }

        private static void CheckEndSpell()
        {
            foreach (var spell in SpellsDetected.Values)
            {
                if (spell.MissileObject == null && spell.ToggleObject == null
                    && HeroManager.AllHeroes.Any(i => i.IsValid() && i.IsDead && i.NetworkId == spell.Unit.NetworkId))
                {
                    Utility.DelayAction.Add(1, () => SpellsDetected.Remove(spell.SpellId));
                }

                if (spell.EndTick + spell.Data.ExtraDuration <= Utils.GameTimeTickCount)
                {
                    Utility.DelayAction.Add(1, () => SpellsDetected.Remove(spell.SpellId));

                    if (Configs.Debug)
                    {
                        Console.WriteLine($"=> D1: {spell.SpellId} | {Utils.GameTimeTickCount}");
                    }
                }
            }
        }

        private static void OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Configs.Debug)
            {
                Console.WriteLine(
                    $"{Utils.GameTimeTickCount} Dash => Speed: {args.Speed}, Dist: {args.EndPos.Distance(args.StartPos)}");
            }

            evadeToPos = args.EndPos;
        }

        private static void OnDraw(EventArgs args)
        {
            if (Configs.Menu.Item("DrawStatus").GetValue<bool>())
            {
                var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                var text = "vEvade: " + (Configs.Menu.Item("Enabled").GetValue<KeyBind>().Active ? "On" : "Off");
                Drawing.DrawText(
                    pos.X - Drawing.GetTextExtent(text).Width / 2f,
                    pos.Y,
                    text.EndsWith("On")
                        ? (Evading
                               ? Color.Red
                               : (Configs.Menu.Item("DodgeDangerous").GetValue<KeyBind>().Active
                                      ? Color.Yellow
                                      : Color.White))
                        : Color.Gray,
                    text);
            }

            if (Configs.Menu.Item("DrawSpells").GetValue<bool>())
            {
                foreach (var spell in SpellsDetected.Values)
                {
                    spell.Draw(Color.White, Color.LimeGreen);
                }
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (args.Order == GameObjectOrder.MoveTo || args.Order == GameObjectOrder.AttackTo)
            {
                evadeToPos.X = args.TargetPosition.X;
                evadeToPos.Y = args.TargetPosition.Y;
            }
            else
            {
                evadeToPos = Vector2.Zero;
            }

            if (SpellsDetected.Count == 0)
            {
                forceFollowPath = false;
            }

            if (haveSolution)
            {
                return;
            }

            if (!Configs.Menu.Item("Enabled").GetValue<KeyBind>().Active
                || !EvadeSpellDatabase.Spells.Any(i => i.MenuName == "Walking" && i.Enabled) || Util.ShieldCheck)
            {
                return;
            }

            var path = ObjectManager.Player.GetPath(args.TargetPosition).ToList().To2D();

            if (Evading || !IsSafePos(PlayerPosition).IsSafe)
            {
                if (args.Order == GameObjectOrder.MoveTo)
                {
                    var willMove = false;

                    if (Evading && Utils.GameTimeTickCount - lastEvadePosChangeTick > Configs.EvadePointChangeInterval)
                    {
                        var points = Evader.GetEvadePoints(-1, 0, false, true);

                        if (points.Count > 0)
                        {
                            evadePos = args.TargetPosition.To2D().Closest(points);
                            Evading = true;
                            lastEvadePosChangeTick = Utils.GameTimeTickCount;
                            willMove = true;
                        }
                    }

                    if (IsSafePath(path, Configs.EvadingRouteChangeTimeOffset).IsSafe
                        && IsSafePos(path[path.Count - 1]).IsSafe)
                    {
                        evadePos = path[path.Count - 1];
                        Evading = true;
                        willMove = true;
                    }

                    if (!willMove)
                    {
                        forceFollowPath = true;
                    }
                }

                args.Process = false;

                return;
            }

            if (IsSafePath(path, Configs.CrossingTimeOffset).IsSafe || args.Order == GameObjectOrder.AttackUnit)
            {
                return;
            }

            forceFollowPath = true;
            args.Process = false;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (previousPos.IsValid() && PlayerPosition.Distance(previousPos) > 200)
            {
                Evading = false;
                evadeToPos = Vector2.Zero;
            }

            previousPos = PlayerPosition;
            CheckEndSpell();

            foreach (var spell in SpellsDetected.Values)
            {
                spell.OnUpdate();
            }

            if (!Configs.Menu.Item("Enabled").GetValue<KeyBind>().Active || Util.CommonCheck)
            {
                Evading = false;
                evadeToPos = Vector2.Zero;

                return;
            }

            if (ObjectManager.Player.IsWindingUp && !Orbwalking.IsAutoAttack(ObjectManager.Player.LastCastedSpellName()))
            {
                Evading = false;

                return;
            }

            foreach (var ally in
                HeroManager.Allies.Where(
                    i =>
                    !i.IsMe && i.IsValidTarget(1000, false)
                    && Configs.Menu.Item("SA_" + i.ChampionName).GetValue<bool>()))
            {
                var checkSafe = IsSafePos(ally.ServerPosition.To2D());

                if (checkSafe.IsSafe)
                {
                    continue;
                }

                var dangerLvl =
                    checkSafe.Spells.Select(i => i.GetValue<Slider>("DangerLvl").Value).Concat(new[] { 0 }).Max();

                foreach (var evadeSpell in
                    EvadeSpellDatabase.Spells.Where(
                        i =>
                        i.IsShield && i.CanShieldAllies && ally.Distance(PlayerPosition) < i.MaxRange
                        && dangerLvl >= i.DangerLevel && i.Slot.IsReady() && IsAboutToHit(i.Delay, ally)))
                {
                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ally);
                }
            }

            var curPath = ObjectManager.Player.GetWaypoints();
            var checkPos = IsSafePos(PlayerPosition);
            var checkPath = IsSafePath(curPath, 100);
            haveSolution = false;

            if (Evading && IsSafePos(evadePos).IsSafe)
            {
                if (checkPos.IsSafe)
                {
                    Evading = false;
                }
                else
                {
                    if (Utils.GameTimeTickCount - lastMoveTick > 1000 / 15)
                    {
                        lastMoveTick = Utils.GameTimeTickCount;
                        evadePos.Move();
                    }

                    return;
                }
            }
            else if (Evading)
            {
                Evading = false;
            }

            if (!checkPath.IsSafe && !checkPos.IsSafe)
            {
                TryToEvade(checkPos.Spells, evadeToPos.IsValid() ? evadeToPos : Game.CursorPos.To2D());
            }

            if (haveSolution || Evading || !evadeToPos.IsValid() || !checkPos.IsSafe
                || !EvadeSpellDatabase.Spells.Any(i => i.MenuName == "Walking" && i.Enabled)
                || (checkPath.IsSafe && !forceFollowPath)
                || (Utils.GameTimeTickCount - lastMoveTick2 <= 1000 / 15 && PathFollow.IsFollowing))
            {
                return;
            }

            lastMoveTick2 = Utils.GameTimeTickCount;

            if (SpellsDetected.Count == 0)
            {
                if (evadeToPos.Distance(PlayerPosition) > 75)
                {
                    evadeToPos.Move();
                }

                return;
            }

            var newPath = ObjectManager.Player.GetPath(evadeToPos.To3D()).ToList().To2D();
            var checkNewPath = IsSafePath(newPath, 100);

            if (checkNewPath.IsSafe)
            {
                if (evadeToPos.Distance(PlayerPosition) > 75)
                {
                    evadeToPos.Move();
                }

                return;
            }

            var paths = Core.FindPaths(PlayerPosition, evadeToPos);

            if (paths.Count == 0)
            {
                if (!checkPath.Intersect.Valid && curPath.Count <= 1)
                {
                    checkPath = IsSafePath(newPath, 100);
                }

                if (checkPath.Intersect.Valid && checkPath.Intersect.Point.Distance(PlayerPosition) > 75)
                {
                    checkPath.Intersect.Point.Move();

                    return;
                }
            }

            PathFollow.Start(paths);
            PathFollow.KeepFollowPath();
        }

        private static void TryToEvade(List<SpellInstance> hits, Vector2 to)
        {
            var dangerLvl = hits.Select(i => i.GetValue<Slider>("DangerLvl").Value).Concat(new[] { 0 }).Max();

            foreach (var evadeSpell in EvadeSpellDatabase.Spells.Where(i => i.Enabled && dangerLvl >= i.DangerLevel))
            {
                if (evadeSpell.IsSpellShield && evadeSpell.Slot.IsReady())
                {
                    if (IsAboutToHit(evadeSpell.Delay))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ObjectManager.Player);
                    }

                    haveSolution = true;

                    return;
                }

                if (evadeSpell.MenuName == "Walking")
                {
                    var points = Evader.GetEvadePoints();

                    if (points.Count > 0)
                    {
                        evadePos = to.Closest(points);
                        Evading = true;
                        var pos = evadePos.Extend(PlayerPosition, -100);

                        if (
                            IsSafePath(
                                ObjectManager.Player.GetPath(pos.To3D()).ToList().To2D(),
                                Configs.EvadingSecondTimeOffset,
                                (int)ObjectManager.Player.MoveSpeed,
                                100).IsSafe)
                        {
                            evadePos = pos;
                        }

                        return;
                    }
                }

                if (evadeSpell.IsReady)
                {
                    if (evadeSpell.IsMovementSpeedBuff)
                    {
                        var points = Evader.GetEvadePoints((int)evadeSpell.MoveSpeedTotalAmount());

                        if (points.Count > 0)
                        {
                            evadePos = to.Closest(points);
                            Evading = true;
                            ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ObjectManager.Player);

                            return;
                        }
                    }

                    if (evadeSpell.IsDash)
                    {
                        if (evadeSpell.IsTargetted)
                        {
                            var targets = Evader.GetEvadeTargets(
                                evadeSpell.ValidTargets,
                                evadeSpell.Speed,
                                evadeSpell.Delay,
                                evadeSpell.MaxRange);

                            if (targets.Count > 0)
                            {
                                var target = targets.MinOrDefault(i => i.Distance(to));
                                evadePos = target.ServerPosition.To2D();
                                Evading = true;
                                ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, target);

                                return;
                            }

                            if (Utils.GameTimeTickCount - LastWardJumpTick < 250)
                            {
                                haveSolution = true;

                                return;
                            }

                            if (evadeSpell.ValidTargets.Contains(SpellValidTargets.AllyWards)
                                && Configs.Menu.Item("ES" + evadeSpell.MenuName + "_WardJump").GetValue<bool>())
                            {
                                var ward = Items.GetWardSlot();

                                if (ward != null)
                                {
                                    var points = Evader.GetEvadePoints(evadeSpell.Speed, evadeSpell.Delay);
                                    points.RemoveAll(i => i.Distance(PlayerPosition) > 600);

                                    if (points.Count > 0)
                                    {
                                        for (var i = 0; i < points.Count; i++)
                                        {
                                            var k = (int)(600 - PlayerPosition.Distance(points[i]));
                                            k = k - Util.Random.Next(k);
                                            var extended = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                            if (IsSafePos(extended).IsSafe)
                                            {
                                                points[i] = extended;
                                            }
                                        }

                                        ObjectManager.Player.Spellbook.CastSpell(
                                            ward.SpellSlot,
                                            to.Closest(points).To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var points = Evader.GetEvadePoints(evadeSpell.Speed, evadeSpell.Delay);
                            points.RemoveAll(i => i.Distance(PlayerPosition) > evadeSpell.MaxRange);

                            if (evadeSpell.FixedRange)
                            {
                                for (var i = 0; i < points.Count; i++)
                                {
                                    points[i] = PlayerPosition.Extend(points[i], evadeSpell.MaxRange);
                                }

                                for (var i = points.Count - 1; i > 0; i--)
                                {
                                    if (!IsSafePos(points[i]).IsSafe)
                                    {
                                        points.RemoveAt(i);
                                    }
                                }
                            }
                            else
                            {
                                for (var i = 0; i < points.Count; i++)
                                {
                                    var k = (int)(evadeSpell.MaxRange - PlayerPosition.Distance(points[i]));
                                    k -= Math.Max(Util.Random.Next(k) - 100, 0);
                                    var extended = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                    if (IsSafePos(extended).IsSafe)
                                    {
                                        points[i] = extended;
                                    }
                                }
                            }

                            if (points.Count > 0)
                            {
                                evadePos = to.Closest(points);
                                Evading = true;

                                if (!evadeSpell.Invert)
                                {
                                    if (evadeSpell.RequiresPreMove)
                                    {
                                        evadePos.Move();
                                        Utility.DelayAction.Add(
                                            Game.Ping / 2 + 100,
                                            () =>
                                            ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePos.To3D()));
                                    }
                                    else
                                    {
                                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePos.To3D());
                                    }
                                }
                                else
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(
                                        evadeSpell.Slot,
                                        (PlayerPosition - (evadePos - PlayerPosition)).To3D());
                                }

                                return;
                            }
                        }
                    }

                    if (evadeSpell.IsBlink)
                    {
                        if (evadeSpell.IsTargetted)
                        {
                            var targets = Evader.GetEvadeTargets(
                                evadeSpell.ValidTargets,
                                0,
                                evadeSpell.Delay,
                                evadeSpell.MaxRange,
                                true);

                            if (targets.Count > 0)
                            {
                                if (IsAboutToHit(evadeSpell.Delay))
                                {
                                    var target = targets.MinOrDefault(i => i.Distance(to));
                                    evadePos = target.ServerPosition.To2D();
                                    Evading = true;
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, target);
                                }

                                haveSolution = true;

                                return;
                            }

                            if (Utils.GameTimeTickCount - LastWardJumpTick < 250)
                            {
                                haveSolution = true;

                                return;
                            }

                            if (evadeSpell.ValidTargets.Contains(SpellValidTargets.AllyWards)
                                && Configs.Menu.Item("ES" + evadeSpell.MenuName + "_WardJump").GetValue<bool>())
                            {
                                var ward = Items.GetWardSlot();

                                if (ward != null)
                                {
                                    var points = Evader.GetEvadePoints(0, evadeSpell.Delay, true);
                                    points.RemoveAll(i => i.Distance(PlayerPosition) > 600);

                                    if (points.Count > 0)
                                    {
                                        for (var i = 0; i < points.Count; i++)
                                        {
                                            var k = (int)(600 - PlayerPosition.Distance(points[i]));
                                            k = k - Util.Random.Next(k);
                                            var extended = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                            if (IsSafePos(extended).IsSafe)
                                            {
                                                points[i] = extended;
                                            }
                                        }

                                        var pos = to.Closest(points);
                                        ObjectManager.Player.Spellbook.CastSpell(ward.SpellSlot, pos.To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var points = Evader.GetEvadePoints(int.MaxValue, evadeSpell.Delay, true);
                            points.RemoveAll(i => i.Distance(PlayerPosition) > evadeSpell.MaxRange);

                            for (var i = 0; i < points.Count; i++)
                            {
                                var k = (int)(evadeSpell.MaxRange - PlayerPosition.Distance(points[i]));
                                k = k - Util.Random.Next(k);
                                var extended = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                if (IsSafePos(extended).IsSafe)
                                {
                                    points[i] = extended;
                                }
                            }

                            if (points.Count > 0)
                            {
                                if (IsAboutToHit(evadeSpell.Delay))
                                {
                                    evadePos = to.Closest(points);
                                    Evading = true;
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePos.To3D());
                                }

                                haveSolution = true;

                                return;
                            }
                        }
                    }

                    if (evadeSpell.IsInvulnerability)
                    {
                        if (evadeSpell.IsTargetted)
                        {
                            var targets = Evader.GetEvadeTargets(
                                evadeSpell.ValidTargets,
                                int.MaxValue,
                                0,
                                evadeSpell.MaxRange,
                                true,
                                false,
                                true);

                            if (targets.Count > 0)
                            {
                                if (IsAboutToHit(evadeSpell.Delay))
                                {
                                    var target = targets.MinOrDefault(i => i.Distance(to));
                                    evadePos = target.ServerPosition.To2D();
                                    Evading = true;
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, target);
                                }

                                haveSolution = true;

                                return;
                            }
                        }
                        else
                        {
                            if (IsAboutToHit(evadeSpell.Delay))
                            {
                                if (evadeSpell.SelfCast)
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot);
                                }
                                else
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, PlayerPosition.To3D());
                                }
                            }
                        }

                        haveSolution = true;

                        return;
                    }
                }

                if (evadeSpell.MenuName == "Zhonyas" && Items.CanUseItem("ZhonyasHourglass"))
                {
                    if (IsAboutToHit(100))
                    {
                        Items.UseItem("ZhonyasHourglass");
                    }

                    haveSolution = true;

                    return;
                }

                if (evadeSpell.IsShield && evadeSpell.Slot.IsReady())
                {
                    if (IsAboutToHit(evadeSpell.Delay))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ObjectManager.Player);
                    }

                    haveSolution = true;

                    return;
                }
            }

            haveSolution = true;
        }

        #endregion

        public struct IsSafeResult
        {
            #region Fields

            public bool IsSafe;

            public List<SpellInstance> Spells;

            #endregion
        }
    }
}
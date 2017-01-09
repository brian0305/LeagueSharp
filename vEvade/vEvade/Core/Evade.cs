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

        public static readonly Dictionary<string, SpellData> OnTrapSpells = new Dictionary<string, SpellData>();

        public static Dictionary<int, SpellInstance> DetectedSpells = new Dictionary<int, SpellInstance>();

        public static int LastWardJumpTick;

        public static Vector2 PlayerPosition;

        public static List<Geometry.Polygon> Polygons = new List<Geometry.Polygon>();

        public static List<SpellInstance> Spells = new List<SpellInstance>();

        private static Vector2 evadePoint1, evadePoint2;

        private static bool evading;

        private static bool forceFollowPath;

        private static bool haveSolution;

        private static int lastMoveTick1, lastMoveTick2, lastEvadePointChangeTick;

        private static Vector2 prevPos;

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
                    lastMoveTick1 = 0;
                    evadePoint1.Move();
                }

                evading = value;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static bool IsAboutToHit(int time, Obj_AI_Base unit = null)
        {
            time += 150;

            if (unit == null)
            {
                unit = ObjectManager.Player;
            }

            return Spells.Any(i => i.IsAboutToHit(time, unit));
        }

        public static SafePath IsSafePath(List<Vector2> path, int time, int speed = -1, int delay = 0)
        {
            var isSafe = true;
            var intersects = new List<Intersects>();

            foreach (var spell in Spells)
            {
                var checkPath = spell.IsSafePath(path, time, speed, delay);
                isSafe = isSafe && checkPath.IsSafe;

                if (checkPath.Intersect.Valid)
                {
                    intersects.Add(checkPath.Intersect);
                }
            }

            return isSafe
                       ? new SafePath(true, new Intersects())
                       : new SafePath(false, intersects.MinOrDefault(i => i.Distance));
        }

        public static SafePoint IsSafePoint(Vector2 pos)
        {
            var result = new SafePoint { Spells = Spells.Where(i => !i.IsSafePoint(pos)).ToList() };
            result.IsSafe = result.Spells.Count == 0;

            return result;
        }

        public static bool IsSafeToBlink(Vector2 pos, int time, int delay)
        {
            return Spells.All(i => i.IsSafeToBlink(pos, time, delay));
        }

        public static void OnGameLoad(EventArgs args)
        {
            Util.CheckVersion();
            Configs.CreateMenu();
            new SpellDetector();
            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            Spellbook.OnCastSpell += OnCastSpell;
            Drawing.OnDraw += OnDraw;
            CustomEvents.Unit.OnDash += OnDash;
            Orbwalking.BeforeAttack += BeforeAttack;
            //Spellbook.OnStopCast += OnStopCast;
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
            foreach (var spell in DetectedSpells.Values)
            {
                if (spell.Data.IsDash && Utils.GameTimeTickCount - spell.StartTick > spell.Data.Delay + 100
                    && !spell.Unit.IsDashing())
                {
                    Utility.DelayAction.Add(50, () => DetectedSpells.Remove(spell.SpellId));
                }

                if (spell.TrapObject != null && spell.TrapObject.IsDead)
                {
                    Utility.DelayAction.Add(1, () => DetectedSpells.Remove(spell.SpellId));
                }

                if (spell.EndTick + spell.Data.ExtraDuration <= Utils.GameTimeTickCount)
                {
                    Utility.DelayAction.Add(1, () => DetectedSpells.Remove(spell.SpellId));

                    if (Configs.Debug)
                    {
                        Console.WriteLine($"=> D: {spell.SpellId} | {Utils.GameTimeTickCount}");
                    }
                }
            }
        }

        private static void OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!sender.Owner.IsMe)
            {
                return;
            }

            if (args.Slot == SpellSlot.Recall)
            {
                evadePoint2 = Vector2.Zero;
            }

            if (!Evading)
            {
                return;
            }

            var blockLvl = Configs.CheckBlock;

            if (blockLvl == 0)
            {
                return;
            }

            var isDangerous = false;

            foreach (var spell in Spells.Where(i => !i.IsSafePoint(PlayerPosition)))
            {
                isDangerous = spell.GetValue<bool>("IsDangerous");

                if (isDangerous)
                {
                    break;
                }
            }

            if (blockLvl == 1 && !isDangerous)
            {
                return;
            }

            args.Process = !SpellBlocker.CanBlock(args.Slot);
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

            evadePoint2 = args.EndPos;
        }

        private static void OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            if (Configs.DrawStatus)
            {
                var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                var text = "vEvade: " + (Configs.Enabled ? "On" : "Off");
                Drawing.DrawText(
                    pos.X - Drawing.GetTextExtent(text).Width / 2f,
                    pos.Y,
                    text.EndsWith("On")
                        ? (Evading ? Color.Red : (Configs.DodgeDangerous ? Color.Yellow : Color.White))
                        : Color.Gray,
                    text);
            }

            if (Configs.DrawSpells)
            {
                foreach (var spell in DetectedSpells.Values)
                {
                    spell.Draw(spell.Enable ? Color.White : Color.Red);
                }
            }

            if (Configs.Debug)
            {
                var curPaths = ObjectManager.Player.GetWaypoints();

                for (var i = 0; i < curPaths.Count - 1; i++)
                {
                    Util.DrawLine(curPaths[i], curPaths[i + 1], Color.White);
                }

                var evadePaths = Core.FindPaths(PlayerPosition, Game.CursorPos.To2D());

                for (var i = 0; i < evadePaths.Count - 1; i++)
                {
                    Util.DrawLine(evadePaths[i], evadePaths[i + 1], Color.Red);
                }

                Render.Circle.DrawCircle(evadePoint1.To3D(), 100, Color.White);
                Render.Circle.DrawCircle(evadePoint2.To3D(), 100, Color.Red);
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            evadePoint2 = args.Order == GameObjectOrder.MoveTo || args.Order == GameObjectOrder.AttackTo
                              ? args.TargetPosition.To2D()
                              : Vector2.Zero;

            if (DetectedSpells.Count == 0)
            {
                forceFollowPath = false;
            }

            if (haveSolution)
            {
                return;
            }

            if (!Configs.Enabled || !EvadeSpellDatabase.Spells.Any(i => i.MenuName == "Walking" && i.Enabled)
                || Util.ShieldCheck)
            {
                return;
            }

            var paths = ObjectManager.Player.GetPath(args.TargetPosition).ToList().To2D();

            if (Evading || !PlayerPosition.IsPointSafe().IsSafe) //!IsSafePoint(PlayerPosition).IsSafe)
            {
                if (args.Order == GameObjectOrder.MoveTo)
                {
                    var willMove = false;

                    if (Evading && Utils.GameTimeTickCount - lastEvadePointChangeTick > Configs.EvadePointChangeTime)
                    {
                        /*var points = Evader.GetEvadePoints(-1, 0, false, true);

                        if (points.Count > 0)
                        {
                            evadePoint1 = args.TargetPosition.To2D().Closest(points);
                            Evading = true;
                            willMove = true;
                            lastEvadePointChangeTick = Utils.GameTimeTickCount;
                        }*/
                        var point = Evader.GetBestPointBlock(args.TargetPosition);

                        if (point.IsValid())
                        {
                            evadePoint1 = point;
                            Evading = true;
                            willMove = true;
                            lastEvadePointChangeTick = Utils.GameTimeTickCount;
                        }
                    }

                    //if (IsSafePath(paths, Configs.EvadingRouteChangeTime).IsSafe
                    //    && IsSafePoint(paths[paths.Count - 1]).IsSafe)
                    if (paths.IsPathSafe(Configs.EvadingRouteChangeTime).IsSafe
                        && paths[paths.Count - 1].IsPointSafe().IsSafe)
                    {
                        evadePoint1 = paths[paths.Count - 1];
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

            var checkPath = paths.IsPathSafe(Configs.CrossingTime); //IsSafePath(paths, Configs.CrossingTime);

            if (checkPath.IsSafe)
            {
                return;
            }

            if (args.Order != GameObjectOrder.AttackUnit)
            {
                forceFollowPath = true;
                args.Process = false;
            }
            else
            {
                var target = args.Target as AttackableUnit;

                if (target == null || !target.IsValid || !target.IsVisible
                    || PlayerPosition.Distance(target.Position) <= Orbwalking.GetRealAutoAttackRange(target))
                {
                    return;
                }

                if (checkPath.Intersect.Valid)
                {
                    checkPath.Intersect.Point.Move();
                }

                args.Process = false;
            }
        }

        private static void OnStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            var caster = sender.Owner as Obj_AI_Hero;

            if (caster == null || !caster.IsValid || (!caster.IsEnemy && !Configs.Debug))
            {
                return;
            }

            if (!args.ForceStop && !args.StopAnimation)
            {
                return;
            }

            foreach (var spell in
                DetectedSpells.Values.Where(
                    i =>
                    i.MissileObject == null && i.ToggleObject == null && i.TrapObject == null
                    && i.Unit.CompareId(caster)))
            {
                Utility.DelayAction.Add(1, () => DetectedSpells.Remove(spell.SpellId));

                if (Configs.Debug)
                {
                    Console.WriteLine($"=> D-Stop: {spell.SpellId} | {Utils.GameTimeTickCount}");
                }
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            PlayerPosition = ObjectManager.Player.ServerPosition.To2D();

            if (prevPos.IsValid() && PlayerPosition.Distance(prevPos) > 200)
            {
                Evading = false;
                evadePoint2 = Vector2.Zero;
            }

            prevPos = PlayerPosition;
            UpdateSpells();

            if (!Configs.Enabled || Util.CommonCheck)
            {
                Evading = false;
                evadePoint2 = Vector2.Zero;

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
                var checkSafe = ally.ServerPosition.To2D().IsPointSafe(); //IsSafePoint(ally.ServerPosition.To2D());

                if (checkSafe.IsSafe)
                {
                    continue;
                }

                var dangerLvl =
                    checkSafe.Spells.Select(i => i.GetValue<Slider>("DangerLvl").Value).Concat(new[] { 0 }).Max();

                foreach (var evadeSpell in
                    EvadeSpellDatabase.Spells.Where(
                        i =>
                        i.IsReady && i.IsShield && i.CanShieldAllies && dangerLvl >= i.DangerLevel
                        && ally.Distance(PlayerPosition) < i.MaxRange && IsAboutToHit(i.Delay, ally)))
                {
                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ally);
                }
            }

            var curPaths = ObjectManager.Player.GetWaypoints();
            var checkPos = PlayerPosition.IsPointSafe(); //IsSafePoint(PlayerPosition);
            var checkPath = curPaths.IsPathSafe(100); //IsSafePath(curPaths, 100);
            haveSolution = false;

            if (Evading)
            {
                if (evadePoint1.IsPointSafe().IsSafe) //(IsSafePoint(evadePoint1).IsSafe)
                {
                    if (checkPos.IsSafe)
                    {
                        Evading = false;
                    }
                    else
                    {
                        if (Utils.GameTimeTickCount - lastMoveTick1 > 1000 / 15)
                        {
                            lastMoveTick1 = Utils.GameTimeTickCount;
                            evadePoint1.Move();
                        }

                        return;
                    }
                }
                else
                {
                    Evading = false;
                }
            }

            if (!checkPath.IsSafe && !checkPos.IsSafe)
            {
                TryToEvade(checkPos.Spells, evadePoint2.IsValid() ? evadePoint2 : Game.CursorPos.To2D());
            }

            if (haveSolution || Evading || !evadePoint2.IsValid() || !checkPos.IsSafe
                || !EvadeSpellDatabase.Spells.Any(i => i.MenuName == "Walking" && i.Enabled)
                || (checkPath.IsSafe && !forceFollowPath)
                || (Utils.GameTimeTickCount - lastMoveTick2 <= 1000 / 15 && PathFollow.IsFollowing))
            {
                return;
            }

            lastMoveTick2 = Utils.GameTimeTickCount;

            if (DetectedSpells.Count == 0)
            {
                if (evadePoint2.Distance(PlayerPosition) > 75)
                {
                    evadePoint2.Move();
                }

                return;
            }

            var paths = ObjectManager.Player.GetPath(evadePoint2.To3D()).ToList().To2D();

            if (paths.IsPathSafe(100).IsSafe) //(IsSafePath(paths, 100).IsSafe)
            {
                if (evadePoint2.Distance(PlayerPosition) > 75)
                {
                    evadePoint2.Move();
                }

                return;
            }

            var newPaths = Core.FindPaths(PlayerPosition, evadePoint2);

            if (newPaths.Count == 0)
            {
                if (!checkPath.Intersect.Valid && curPaths.Count <= 1)
                {
                    checkPath = paths.IsPathSafe(100); //IsSafePath(paths, 100);
                }

                if (checkPath.Intersect.Valid && checkPath.Intersect.Point.Distance(PlayerPosition) > 75)
                {
                    checkPath.Intersect.Point.Move();

                    return;
                }
            }

            PathFollow.Start(newPaths);
            PathFollow.KeepFollowPath();
        }

        private static void TryToEvade(List<SpellInstance> spells, Vector2 to)
        {
            var dangerLvl = spells.Select(i => i.GetValue<Slider>("DangerLvl").Value).Concat(new[] { 0 }).Max();

            foreach (var evadeSpell in EvadeSpellDatabase.Spells.Where(i => i.Enabled && dangerLvl >= i.DangerLevel))
            {
                if (evadeSpell.MenuName == "Walking")
                {
                    /*var points = Evader.GetEvadePoints();

                    if (points.Count > 0)
                    {
                        evadePoint1 = to.Closest(points);
                        var pos = evadePoint1.Extend(PlayerPosition, -100);

                        if (
                            IsSafePath(
                                ObjectManager.Player.GetPath(pos.To3D()).ToList().To2D(),
                                Configs.EvadingSecondTime,
                                -1,
                                100).IsSafe)
                        {
                            evadePoint1 = pos;
                        }

                        Evading = true;

                        return;
                    }*/
                    var point = Evader.GetBestPoint();

                    if (point.IsValid())
                    {
                        evadePoint1 = point;
                        var pos = evadePoint1.Extend(PlayerPosition, -100);

                        if (pos.IsPathSafe(Configs.EvadingSecondTime, -1, 100).IsSafe)
                        {
                            evadePoint1 = pos;
                        }

                        Evading = true;

                        return;
                    }
                }

                if (evadeSpell.IsReady)
                {
                    if (evadeSpell.IsSpellShield)
                    {
                        if (IsAboutToHit(evadeSpell.Delay))
                        {
                            ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ObjectManager.Player);
                        }

                        haveSolution = true;

                        return;
                    }

                    if (evadeSpell.IsMovementSpeedBuff)
                    {
                        /*var points = Evader.GetEvadePoints((int)evadeSpell.MoveSpeedTotalAmount(), evadeSpell.Delay);

                        if (points.Count > 0)
                        {
                            evadePoint1 = to.Closest(points);
                            Evading = true;
                            ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, ObjectManager.Player);

                            return;
                        }*/
                        var point = Evader.GetBestPoint((int)evadeSpell.MoveSpeedTotalAmount(), evadeSpell.Delay);

                        if (point.IsValid())
                        {
                            evadePoint1 = point;
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
                                evadePoint1 = target.ServerPosition.To2D();
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
                                    /*var points = Evader.GetEvadePoints(evadeSpell.Speed, evadeSpell.Delay);
                                    points.RemoveAll(i => i.Distance(PlayerPosition) > 600);

                                    if (points.Count > 0)
                                    {
                                        for (var i = 0; i < points.Count; i++)
                                        {
                                            var k = (int)(600 - PlayerPosition.Distance(points[i]));
                                            k -= Util.Random.Next(k);
                                            var extend = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                            if (IsSafePoint(extend).IsSafe)
                                            {
                                                points[i] = extend;
                                            }
                                        }

                                        ObjectManager.Player.Spellbook.CastSpell(
                                            ward.SpellSlot,
                                            to.Closest(points).To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }*/
                                    var point = Evader.GetBestPointDash(evadeSpell.Speed, evadeSpell.Delay, 600);

                                    if (point.IsValid())
                                    {
                                        ObjectManager.Player.Spellbook.CastSpell(ward.SpellSlot, point.To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            /*var points = Evader.GetEvadePoints(evadeSpell.Speed, evadeSpell.Delay);
                            points.RemoveAll(i => i.Distance(PlayerPosition) > evadeSpell.MaxRange);

                            if (evadeSpell.FixedRange)
                            {
                                for (var i = 0; i < points.Count; i++)
                                {
                                    points[i] = PlayerPosition.Extend(points[i], evadeSpell.MaxRange);
                                }

                                for (var i = points.Count - 1; i > 0; i--)
                                {
                                    if (!IsSafePoint(points[i]).IsSafe)
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
                                    var extend = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                    if (IsSafePoint(extend).IsSafe)
                                    {
                                        points[i] = extend;
                                    }
                                }
                            }

                            if (points.Count > 0)
                            {
                                evadePoint1 = to.Closest(points);
                                Evading = true;

                                if (!evadeSpell.Invert)
                                {
                                    if (evadeSpell.RequiresPreMove)
                                    {
                                        evadePoint1.Move();
                                        Utility.DelayAction.Add(
                                            Game.Ping / 2 + 100,
                                            () =>
                                            ObjectManager.Player.Spellbook.CastSpell(
                                                evadeSpell.Slot,
                                                evadePoint1.To3D()));
                                    }
                                    else
                                    {
                                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePoint1.To3D());
                                    }
                                }
                                else
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(
                                        evadeSpell.Slot,
                                        (PlayerPosition - (evadePoint1 - PlayerPosition)).To3D());
                                }

                                return;
                            }*/
                            var point = Evader.GetBestPointDash(
                                evadeSpell.Speed,
                                evadeSpell.Delay,
                                evadeSpell.MaxRange,
                                evadeSpell.FixedRange);

                            if (point.IsValid())
                            {
                                evadePoint1 = point;
                                Evading = true;

                                if (!evadeSpell.Invert)
                                {
                                    if (evadeSpell.RequiresPreMove)
                                    {
                                        evadePoint1.Move();
                                        Utility.DelayAction.Add(
                                            Game.Ping / 2 + 100,
                                            () =>
                                            ObjectManager.Player.Spellbook.CastSpell(
                                                evadeSpell.Slot,
                                                evadePoint1.To3D()));
                                    }
                                    else
                                    {
                                        ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePoint1.To3D());
                                    }
                                }
                                else
                                {
                                    ObjectManager.Player.Spellbook.CastSpell(
                                        evadeSpell.Slot,
                                        (PlayerPosition - (evadePoint1 - PlayerPosition)).To3D());
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
                                    evadePoint1 = target.ServerPosition.To2D();
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
                                    /*var points = Evader.GetEvadePoints(0, evadeSpell.Delay, true);
                                    points.RemoveAll(i => i.Distance(PlayerPosition) > 600);

                                    if (points.Count > 0)
                                    {
                                        for (var i = 0; i < points.Count; i++)
                                        {
                                            var k = (int)(600 - PlayerPosition.Distance(points[i]));
                                            k -= Util.Random.Next(k);
                                            var extend = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                            if (IsSafePoint(extend).IsSafe)
                                            {
                                                points[i] = extend;
                                            }
                                        }

                                        ObjectManager.Player.Spellbook.CastSpell(
                                            ward.SpellSlot,
                                            to.Closest(points).To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }*/
                                    var point = Evader.GetBestPointBlink(evadeSpell.Delay, 600);

                                    if (point.IsValid())
                                    {
                                        ObjectManager.Player.Spellbook.CastSpell(ward.SpellSlot, point.To3D());
                                        LastWardJumpTick = Utils.GameTimeTickCount;
                                        haveSolution = true;

                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            /*var points = Evader.GetEvadePoints(0, evadeSpell.Delay, true);
                            points.RemoveAll(i => i.Distance(PlayerPosition) > evadeSpell.MaxRange);

                            if (points.Count > 0)
                            {
                                if (IsAboutToHit(evadeSpell.Delay))
                                {
                                    for (var i = 0; i < points.Count; i++)
                                    {
                                        var k = (int)(evadeSpell.MaxRange - PlayerPosition.Distance(points[i]));
                                        k -= Util.Random.Next(k);
                                        var extend = points[i] + k * (points[i] - PlayerPosition).Normalized();

                                        if (IsSafePoint(extend).IsSafe)
                                        {
                                            points[i] = extend;
                                        }
                                    }

                                    evadePoint1 = to.Closest(points);
                                    Evading = true;
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePoint1.To3D());
                                }

                                haveSolution = true;

                                return;
                            }*/
                            var point = Evader.GetBestPointBlink(evadeSpell.Delay, evadeSpell.MaxRange);

                            if (point.IsValid())
                            {
                                if (IsAboutToHit(evadeSpell.Delay))
                                {
                                    evadePoint1 = point;
                                    Evading = true;
                                    ObjectManager.Player.Spellbook.CastSpell(evadeSpell.Slot, evadePoint1.To3D());
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
                                0,
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
                                    evadePoint1 = target.ServerPosition.To2D();
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

                            haveSolution = true;

                            return;
                        }
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

                if (evadeSpell.IsReady && evadeSpell.IsShield)
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

        private static void UpdateSpells()
        {
            CheckEndSpell();

            foreach (var spell in DetectedSpells.Values)
            {
                spell.OnUpdate();
            }

            var spells = DetectedSpells.Values.Where(i => i.Enable).ToList();
            Spells = spells;
            Polygons = Geometry.ClipPolygons(spells.Select(i => i.EvadePolygon).ToList()).ToPolygons();
        }

        #endregion
    }
}
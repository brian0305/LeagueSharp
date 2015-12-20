namespace Valvrave_Sharp.Core
{
    using System;
    using System.Linq;
    using System.Windows.Forms;

    using LeagueSharp;
    using LeagueSharp.SDK.Core;
    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.Events;
    using LeagueSharp.SDK.Core.Extensions;
    using LeagueSharp.SDK.Core.Extensions.SharpDX;
    using LeagueSharp.SDK.Core.Math.Prediction;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;
    using LeagueSharp.SDK.Core.Utils;
    using LeagueSharp.SDK.Core.Wrappers;
    using LeagueSharp.SDK.Core.Wrappers.Damages;
    using SharpDX;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal static class Orbwalker
    {
        #region Static Fields

        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static int blockOrdersUntilTick;

        private static int lastAutoAttackTick;

        private static Obj_AI_Minion lastMinion;

        private static int lastMovementOrderTick;

        private static AttackableUnit lastTarget;

        private static bool missileLaunched;

        #endregion

        #region Delegates

        internal delegate void OnActionDelegate(OrbwalkerActionArgs e);

        #endregion

        #region Events

        internal static event OnActionDelegate OnAction;

        #endregion

        #region Properties

        internal static OrbwalkingMode ActiveMode
            =>
                Program.MainMenu["Orbwalker"]["lasthitKey"].GetValue<MenuKeyBind>().Active
                    ? OrbwalkingMode.LastHit
                    : (Program.MainMenu["Orbwalker"]["laneclearKey"].GetValue<MenuKeyBind>().Active
                           ? OrbwalkingMode.LaneClear
                           : (Program.MainMenu["Orbwalker"]["hybridKey"].GetValue<MenuKeyBind>().Active
                                  ? OrbwalkingMode.Hybrid
                                  : (Program.MainMenu["Orbwalker"]["orbwalkKey"].GetValue<MenuKeyBind>().Active
                                         ? OrbwalkingMode.Combo
                                         : OrbwalkingMode.None)));

        internal static bool Attack { get; set; } = true;

        internal static bool CanAttack
        {
            get
            {
                if (!Attack)
                {
                    return false;
                }
                var atkDelay = Program.Player.AttackDelay * 1000d;
                if (Program.Player.ChampionName == "Graves")
                {
                    atkDelay = 1.0740296828 * 1000 * Program.Player.AttackDelay - 716.2381256175;
                }
                return Variables.TickCount + (Game.Ping / 2) + 25 >= lastAutoAttackTick + atkDelay
                       && (Program.Player.ChampionName != "Graves" || Program.Player.HasBuff("GravesBasicAttackAmmo1"));
            }
        }

        internal static bool CanMove
        {
            get
            {
                if (!Movement)
                {
                    return false;
                }
                if (missileLaunched && Program.MainMenu["Orbwalker"]["Advanced"]["Missile"])
                {
                    return true;
                }
                var localExtraWindup = Program.Player.ChampionName == "Rengar"
                                       && (Program.Player.HasBuff("rengarqbase") || Program.Player.HasBuff("rengarqemp"))
                                           ? 200
                                           : 0;
                return !Program.Player.CanCancelAutoAttack()
                       || Variables.TickCount + (Game.Ping / 2)
                       >= lastAutoAttackTick + (Program.Player.AttackCastDelay * 1000)
                       + Program.MainMenu["Orbwalker"]["Advanced"]["ExtraWindup"] + localExtraWindup;
            }
        }

        internal static bool Movement { get; set; } = true;

        internal static Vector3 OrbwalkPosition { get; set; }

        internal static AttackableUnit OrbwalkTarget { get; set; }

        private static bool Enabled => Program.MainMenu["Orbwalker"]["Enable"];

        private static int FarmDelay => Program.MainMenu["Orbwalker"]["Advanced"]["FarmDelay"];

        #endregion

        #region Methods

        internal static AttackableUnit GetTarget(OrbwalkingMode? modeArg)
        {
            var mode = modeArg ?? ActiveMode;
            if ((mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Hybrid)
                && !Program.MainMenu["Orbwalker"]["Advanced"]["PriorizeFarm"])
            {
                var target = TargetSelector.GetTarget();
                if (target != null)
                {
                    return target;
                }
            }
            if (mode == OrbwalkingMode.LaneClear || mode == OrbwalkingMode.Hybrid || mode == OrbwalkingMode.LastHit)
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(m => m.InAutoAttackRange() && m.IsMinion(false))
                        .OrderByDescending(m => m.GetMinionType().HasFlag(MinionTypes.Siege))
                        .ThenBy(m => m.GetMinionType().HasFlag(MinionTypes.Super))
                        .ThenBy(m => m.Health)
                        .ThenByDescending(m => m.MaxHealth))
                {
                    if (new[] { "zyrathornplant", "zyragraspingplant" }.Contains(minion.CharData.BaseSkinName.ToLower())
                        && minion.Health < 3)
                    {
                        return minion;
                    }
                    var time =
                        (int)
                        ((Program.Player.AttackCastDelay * 1000)
                         + (Math.Max(0, Program.Player.Distance(minion) - Program.Player.BoundingRadius)
                            / Program.Player.GetProjectileSpeed() * 1000) - 100 + (Game.Ping / 2f));
                    var healthPrediction = Health.GetPrediction(minion, time, FarmDelay);
                    if (healthPrediction <= 0)
                    {
                        InvokeAction(
                            new OrbwalkerActionArgs
                                {
                                    Position = minion.Position, Target = minion, Process = true,
                                    Type = OrbwalkingType.NonKillableMinion
                                });
                    }
                    if (healthPrediction > 0 && healthPrediction <= Program.Player.GetAutoAttackDamage(minion))
                    {
                        return minion;
                    }
                }
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(
                        m =>
                        m.InAutoAttackRange() && m.CharData.BaseSkinName.ToLower() == "gangplankbarrel"
                        && m.IsHPBarRendered && m.Health < 2))
                {
                    return minion;
                }
            }
            if (mode == OrbwalkingMode.LaneClear)
            {
                foreach (var turret in GameObjects.EnemyTurrets.Where(t => t.InAutoAttackRange()))
                {
                    return turret;
                }
                foreach (var inhibitor in GameObjects.EnemyInhibitors.Where(i => i.InAutoAttackRange()))
                {
                    return inhibitor;
                }
                if (GameObjects.EnemyNexus.InAutoAttackRange())
                {
                    return GameObjects.EnemyNexus;
                }
            }
            if (mode != OrbwalkingMode.LastHit)
            {
                var target = TargetSelector.GetTarget();
                if (target != null)
                {
                    return target;
                }
            }
            if (ActiveMode == OrbwalkingMode.LaneClear)
            {
                var mob = (GameObjects.JungleLegendary.FirstOrDefault(j => j.InAutoAttackRange())
                           ?? GameObjects.JungleSmall.FirstOrDefault(
                               j => j.InAutoAttackRange() && j.Name.Contains("Mini") && j.Name.Contains("SRU_Razorbeak"))
                           ?? GameObjects.JungleLarge.FirstOrDefault(j => j.InAutoAttackRange()))
                          ?? GameObjects.JungleSmall.FirstOrDefault(j => j.InAutoAttackRange());
                if (mob != null)
                {
                    return mob;
                }
            }
            if (ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Hybrid
                || ActiveMode == OrbwalkingMode.LastHit)
            {
                Obj_AI_Minion farmUnderTurretMinion = null;
                Obj_AI_Minion noneKillableMinion = null;
                var turretMinions =
                    GameObjects.EnemyMinions.Where(
                        i => i.InAutoAttackRange() && i.IsMinion(false) && i.IsUnderAllyTurret())
                        .OrderByDescending(m => m.GetMinionType().HasFlag(MinionTypes.Siege))
                        .ThenBy(m => m.GetMinionType().HasFlag(MinionTypes.Super))
                        .ThenBy(m => m.Health)
                        .ThenByDescending(m => m.MaxHealth)
                        .ToList();
                if (turretMinions.Count > 0)
                {
                    var minion = turretMinions.FirstOrDefault(Health.HasTurretAggro);
                    if (minion != null)
                    {
                        var hpLeftBeforeDie = 0;
                        var hpLeft = 0;
                        var turretAttackCount = 0;
                        var turret = Health.GetAggroTurret(minion);
                        if (turret != null)
                        {
                            var turretStarTick = Health.TurretAggroStartTick(minion);
                            var turretLandTick = turretStarTick + (int)(turret.AttackCastDelay * 1000)
                                                 + (1000
                                                    * Math.Max(
                                                        0,
                                                        (int)(minion.Distance(turret) - turret.BoundingRadius))
                                                    / (int)(turret.BasicAttack.MissileSpeed + 70));
                            for (float i = turretLandTick + 50;
                                 i < turretLandTick + (3 * turret.AttackDelay * 1000) + 50;
                                 i = i + (turret.AttackDelay * 1000))
                            {
                                var time = (int)i - Variables.TickCount + (Game.Ping / 2);
                                var predHp =
                                    (int)
                                    Health.GetPrediction(
                                        minion,
                                        time > 0 ? time : 0,
                                        70,
                                        HealthPredictionType.Simulated);
                                if (predHp > 0)
                                {
                                    hpLeft = predHp;
                                    turretAttackCount += 1;
                                    continue;
                                }
                                hpLeftBeforeDie = hpLeft;
                                hpLeft = 0;
                                break;
                            }
                            if (hpLeft == 0 && turretAttackCount != 0 && hpLeftBeforeDie != 0)
                            {
                                var damage = (int)Program.Player.GetAutoAttackDamage(minion);
                                var hits = hpLeftBeforeDie / damage;
                                var timeBeforeDie = turretLandTick
                                                    + ((turretAttackCount + 1) * (int)(turret.AttackDelay * 1000))
                                                    - Variables.TickCount;
                                var timeUntilAttackReady = lastAutoAttackTick + (int)(Program.Player.AttackDelay * 1000)
                                                           > Variables.TickCount + (Game.Ping / 2) + 25
                                                               ? lastAutoAttackTick
                                                                 + (int)(Program.Player.AttackDelay * 1000)
                                                                 - (Variables.TickCount + (Game.Ping / 2) + 25)
                                                               : 0;
                                var timeToLandAttack = Program.Player.IsMelee
                                                           ? Program.Player.AttackCastDelay * 1000
                                                           : Program.Player.AttackCastDelay * 1000
                                                             + (1000
                                                                * Math.Max(
                                                                    0,
                                                                    (minion.Distance(Program.Player)
                                                                     - Program.Player.BoundingRadius))
                                                                / Program.Player.BasicAttack.MissileSpeed);
                                if (hits >= 1
                                    && (hits * Program.Player.AttackDelay * 1000) + timeUntilAttackReady
                                    + timeToLandAttack < timeBeforeDie)
                                {
                                    farmUnderTurretMinion = minion;
                                }
                                else if (hits >= 1
                                         && (hits * Program.Player.AttackDelay * 1000) + timeUntilAttackReady
                                         + timeToLandAttack > timeBeforeDie)
                                {
                                    noneKillableMinion = minion;
                                }
                            }
                            else if (hpLeft == 0 && turretAttackCount == 0 && hpLeftBeforeDie == 0)
                            {
                                noneKillableMinion = minion;
                            }
                            if (ShouldWaitUnderTurret(noneKillableMinion))
                            {
                                return null;
                            }
                            if (farmUnderTurretMinion != null)
                            {
                                return farmUnderTurretMinion;
                            }
                            return
                                (from subMinion in
                                     turretMinions.Where(
                                         m => m.NetworkId != minion.NetworkId && !Health.HasMinionAggro(m))
                                 where
                                     (int)subMinion.Health % (int)turret.GetAutoAttackDamage(subMinion)
                                     > (int)Program.Player.GetAutoAttackDamage(subMinion)
                                 select subMinion).FirstOrDefault();
                        }
                    }
                    else
                    {
                        if (ShouldWaitUnderTurret())
                        {
                            return null;
                        }
                        return (from subMinion in turretMinions.Where(x => !Health.HasMinionAggro(x))
                                let turret =
                                    GameObjects.AllyTurrets.FirstOrDefault(
                                        t => !t.IsDead && t.Distance(subMinion) < 950)
                                where
                                    turret != null
                                    && (int)subMinion.Health % (int)turret.GetAutoAttackDamage(subMinion)
                                    > (int)GameObjects.Player.GetAutoAttackDamage(subMinion)
                                select subMinion).FirstOrDefault();
                    }
                    return null;
                }
            }
            if (ActiveMode == OrbwalkingMode.LaneClear)
            {
                var shouldWait =
                    GameObjects.EnemyMinions.Any(
                        m =>
                        m.InAutoAttackRange() && m.IsMinion(false)
                        && Health.GetPrediction(
                            m,
                            (int)((Program.Player.AttackDelay * 1000) * 2f),
                            FarmDelay,
                            HealthPredictionType.Simulated) <= Program.Player.GetAutoAttackDamage(m));
                if (!shouldWait)
                {
                    if (lastMinion.InAutoAttackRange())
                    {
                        var predHealth = Health.GetPrediction(
                            lastMinion,
                            (int)((Program.Player.AttackDelay * 1000) * 2f),
                            FarmDelay,
                            HealthPredictionType.Simulated);
                        if (predHealth >= 2 * Program.Player.GetAutoAttackDamage(lastMinion)
                            || Math.Abs(predHealth - lastMinion.Health) < float.Epsilon)
                        {
                            return lastMinion;
                        }
                    }
                    var minion = (from m in
                                      GameObjects.EnemyMinions.Where(m => m.InAutoAttackRange() && m.IsMinion(false))
                                  let predictedHealth =
                                      Health.GetPrediction(
                                          m,
                                          (int)((Program.Player.AttackDelay * 1000) * 2f),
                                          FarmDelay,
                                          HealthPredictionType.Simulated)
                                  where
                                      predictedHealth >= 2 * Program.Player.GetAutoAttackDamage(m)
                                      || Math.Abs(predictedHealth - m.Health) < float.Epsilon
                                  select m).MaxOrDefault(m => m.Health);
                    if (minion != null)
                    {
                        return lastMinion = minion;
                    }
                    return
                        GameObjects.EnemyMinions.FirstOrDefault(
                            m =>
                            m.InAutoAttackRange()
                            && new[] { "kalistaspawn", "teemomushroom" }.Contains(m.CharData.BaseSkinName.ToLower()));
                }
            }
            return null;
        }

        internal static void Init(Menu menu)
        {
            var orbwalkMenu = menu.Add(new Menu("Orbwalker", "Orbwalker"));
            {
                orbwalkMenu.Bool("Enable", "Enable Orbwalker");
                var drawMenu = orbwalkMenu.Add(new Menu("Draw", "Draw"));
                {
                    drawMenu.Bool("AARange", "Auto-Attack Range");
                    drawMenu.Bool("KillableMinion", "Killable Minion", false);
                    drawMenu.Bool("KillableMinionFade", "-> Enable Killable Minion Fade Effect", false);
                }
                var advMenu = orbwalkMenu.Add(new Menu("Advanced", "Advanced"));
                {
                    advMenu.Separator("Movement");
                    advMenu.Slider(
                        "MoveDelay",
                        "Delay Between Movement",
                        new Random(Variables.TickCount).Next(80, 121),
                        0,
                        500);
                    advMenu.Bool("MoveRandom", "Randomize Movement Location");
                    advMenu.Slider("ExtraHold", "Extra Hold Position", 25, 0, 250);
                    advMenu.Slider("MoveMaxDist", "Maximum Movement Distance", new Random().Next(500, 1201), 350, 1200);
                    advMenu.Separator("Miscellaneous");
                    advMenu.Slider("ExtraWindup", "Extra Windup", 80, 0, 200);
                    advMenu.Slider("FarmDelay", "Farm Delay", 30, 0, 200);
                    advMenu.Bool("PriorizeFarm", "Priorize Farm Over Harass");
                    advMenu.Bool("Missile", "Use Missile Checks (Ranged)");
                }
                orbwalkMenu.Separator("Key Bindings");
                orbwalkMenu.KeyBind("lasthitKey", "Farm", Keys.X);
                orbwalkMenu.KeyBind("laneclearKey", "Lane Clear", Keys.V);
                orbwalkMenu.KeyBind("hybridKey", "Hybrid", Keys.C);
                orbwalkMenu.KeyBind("orbwalkKey", "Orbwalk", Keys.Space);
            }

            Game.OnUpdate += args =>
                {
                    if (Program.Player.IsDead || InterruptableSpell.IsCastingInterruptableSpell(Program.Player, true)
                        || !Enabled)
                    {
                        return;
                    }
                    if (ActiveMode != OrbwalkingMode.None)
                    {
                        Orbwalk(OrbwalkTarget, OrbwalkPosition);
                    }
                };
            Spellbook.OnStopCast += (sender, args) =>
                {
                    if (!sender.Owner.IsMe || !Enabled)
                    {
                        return;
                    }
                    if (args.DestroyMissile && args.StopAnimation)
                    {
                        ResetAutoAttackTimer();
                    }
                };
            Obj_AI_Base.OnDoCast += (sender, args) =>
                {
                    if (!sender.IsMe || !Enabled)
                    {
                        return;
                    }
                    if (Game.Ping <= 30)
                    {
                        DelayAction.Add(30, () => OnDoCastDelayed(args));
                    }
                    else
                    {
                        OnDoCastDelayed(args);
                    }
                };
            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (!sender.IsMe || !Enabled)
                    {
                        return;
                    }
                    if (args.Buff.DisplayName == "PoppyPassiveBuff" || args.Buff.DisplayName == "SonaPassiveReady")
                    {
                        ResetAutoAttackTimer();
                    }
                };
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += OnDraw;
        }

        internal static void MoveOrder(Vector3 position)
        {
            if (Variables.TickCount - lastMovementOrderTick < Program.MainMenu["Orbwalker"]["Advanced"]["MoveDelay"])
            {
                return;
            }
            if (!CanMove)
            {
                return;
            }
            if (position.Distance(Program.Player.Position)
                < Program.Player.BoundingRadius + Program.MainMenu["Orbwalker"]["Advanced"]["ExtraHold"])
            {
                if (Program.Player.Path.Length > 0)
                {
                    Program.Player.IssueOrder(GameObjectOrder.Stop, Program.Player.ServerPosition);
                    lastMovementOrderTick = Variables.TickCount - 70;
                }
                return;
            }
            if (Program.Player.Distance(position) < Program.Player.BoundingRadius)
            {
                position = Program.Player.ServerPosition.Extend(
                    position,
                    Program.Player.BoundingRadius + Random.Next(0, 51));
            }
            var maxDist = Program.MainMenu["Orbwalker"]["Advanced"]["MoveMaxDist"];
            if (Program.Player.Distance(position) > maxDist)
            {
                position = Program.Player.ServerPosition.Extend(position, maxDist + 25 - Random.Next(0, 51));
            }
            if (Program.MainMenu["Orbwalker"]["Advanced"]["MoveRandom"] && Program.Player.Distance(position) > 350)
            {
                var randomAngle = 2 * Math.PI * Random.NextDouble();
                var radius = Program.Player.BoundingRadius / 2;
                var x = (float)(position.X + (radius * Math.Cos(randomAngle)));
                var y = (float)(position.Y + (radius * Math.Sin(randomAngle)));
                position = new Vector3(x, y, NavMesh.GetHeightForPosition(x, y));
            }
            var angle = 0f;
            var currentPath = Program.Player.GetWaypoints();
            if (currentPath.Count > 1 && currentPath.PathLength() > 100)
            {
                var movePath = Program.Player.GetPath(position);
                if (movePath.Length > 1)
                {
                    angle = (currentPath[1] - currentPath[0]).AngleBetween((movePath[1] - movePath[0]).ToVector2());
                    var distance = movePath.Last().ToVector2().DistanceSquared(currentPath.Last());
                    if ((angle < 10 && distance < 500 * 500) || distance < 50 * 50)
                    {
                        return;
                    }
                }
<<<<<<< HEAD
            }
            if (Variables.TickCount - lastMovementOrderTick < 70 + Math.Min(60, Game.Ping) && angle < 60)
            {
                return;
            }
            if (angle >= 60 && Variables.TickCount - lastMovementOrderTick < 60)
            {
                return;
            }
            var eventArgs = new OrbwalkerActionArgs
                                { Position = position, Process = true, Type = OrbwalkingType.Movement };
            InvokeAction(eventArgs);
            if (eventArgs.Process)
            {
                Program.Player.IssueOrder(GameObjectOrder.MoveTo, eventArgs.Position);
                lastMovementOrderTick = Variables.TickCount;
=======
                if (Variables.TickCount - LastMovementOrderTick < 70 + Math.Min(60, Game.Ping) && !overrideTimer
                    && angle < 60)
                {
                    return;
                }
                if (angle >= 60 && Variables.TickCount - LastMovementOrderTick < 60)
                {
                    return;
                }
                var eventArgs = new OrbwalkerActionArgs
                                    { Position = point, Process = true, Type = OrbwalkingType.Movement };
                InvokeAction(eventArgs);
                if (eventArgs.Process)
                {
                    Program.Player.IssueOrder(GameObjectOrder.MoveTo, eventArgs.Position);
                    LastMovementOrderTick = Variables.TickCount;
                }
>>>>>>> adc404e28daddc8ad6cfcc3b2f2dc7db70547f3c
            }
        }

        internal static void Orbwalk(AttackableUnit target = null, Vector3? position = null)
        {
            if (blockOrdersUntilTick - Variables.TickCount > 0)
            {
                return;
            }
            if (CanAttack)
            {
                var gTarget = target ?? GetTarget(ActiveMode);
                if (gTarget.InAutoAttackRange())
                {
                    var eventArgs = new OrbwalkerActionArgs
                                        {
                                            Target = gTarget, Position = gTarget.Position, Process = true,
                                            Type = OrbwalkingType.BeforeAttack
                                        };
                    InvokeAction(eventArgs);
                    if (eventArgs.Process)
                    {
                        if (Program.Player.CanCancelAutoAttack())
                        {
                            missileLaunched = false;
                        }
                        if (Program.Player.IssueOrder(GameObjectOrder.AttackUnit, gTarget))
                        {
                            lastTarget = gTarget;
                        }
                        blockOrdersUntilTick = Variables.TickCount + 70 + Math.Min(60, Game.Ping);
                        return;
                    }
                }
            }
            MoveOrder(position.HasValue && position.Value.IsValid() ? position.Value : Game.CursorPos);
        }

        internal static void ResetAutoAttackTimer()
        {
            lastAutoAttackTick = 0;
        }

        private static void InvokeAction(OrbwalkerActionArgs e)
        {
            OnAction?.Invoke(e);
        }

        private static void OnDoCastDelayed(GameObjectProcessSpellCastEventArgs args)
        {
            if (AutoAttack.IsAutoAttackReset(args.SData.Name))
            {
                ResetAutoAttackTimer();
            }
            if (!AutoAttack.IsAutoAttack(args.SData.Name))
            {
                return;
            }
            InvokeAction(
                new OrbwalkerActionArgs { Target = args.Target as AttackableUnit, Type = OrbwalkingType.AfterAttack });
<<<<<<< HEAD
            missileLaunched = true;
=======
            MissileLaunched = true;
>>>>>>> adc404e28daddc8ad6cfcc3b2f2dc7db70547f3c
        }

        private static void OnDraw(EventArgs args)
        {
            if (Program.Player.IsDead || !Enabled)
            {
                return;
            }
            if (Program.MainMenu["Orbwalker"]["Draw"]["AARange"])
            {
                Drawing.DrawCircle(Program.Player.Position, Program.Player.GetRealAutoAttackRange(), Color.Blue);
            }
            if (Program.MainMenu["Orbwalker"]["Draw"]["KillableMinion"])
            {
                if (Program.MainMenu["Orbwalker"]["Draw"]["KillableMinionFade"])
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(
                            m =>
                            m.IsValidTarget(1200) && m.IsMinion(false)
                            && m.Health < Program.Player.GetAutoAttackDamage(m) * 2);
                    foreach (var minion in minions)
                    {
                        var value = 255 - (minion.Health * 2);
                        value = value > 255 ? 255 : value < 0 ? 0 : value;
                        Drawing.DrawCircle(
                            minion.Position,
                            minion.BoundingRadius * 2f,
                            Color.FromArgb(255, 0, 255, (byte)(255 - value)));
                    }
                }
                else
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(
                            m =>
                            m.IsValidTarget(1200) && m.IsMinion(false)
                            && m.Health < Program.Player.GetAutoAttackDamage(m));
                    foreach (var minion in minions)
                    {
                        Drawing.DrawCircle(minion.Position, minion.BoundingRadius * 2f, Color.FromArgb(255, 0, 255, 0));
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe || !Enabled)
            {
                return;
            }
            var spellName = args.SData.Name;
            var target = args.Target as AttackableUnit;
            if (target != null && target.IsValid && AutoAttack.IsAutoAttack(spellName))
            {
                lastAutoAttackTick = Variables.TickCount - Game.Ping / 2;
                missileLaunched = false;
                lastMovementOrderTick = 0;
                if (!target.Compare(lastTarget))
                {
                    InvokeAction(new OrbwalkerActionArgs { Target = target, Type = OrbwalkingType.TargetSwitch });
<<<<<<< HEAD
                    lastTarget = target;
=======
                    LastTarget = target;
>>>>>>> adc404e28daddc8ad6cfcc3b2f2dc7db70547f3c
                }
                InvokeAction(
                    new OrbwalkerActionArgs { Target = target, Sender = sender, Type = OrbwalkingType.OnAttack });
            }
            if (AutoAttack.IsAutoAttackReset(spellName))
            {
                ResetAutoAttackTimer();
            }
        }

        private static bool ShouldWaitUnderTurret(Obj_AI_Minion noneKillableMinion = null)
        {
            return
                GameObjects.EnemyMinions.Any(
                    m =>
                    m.InAutoAttackRange() && m.IsMinion(false)
                    && (noneKillableMinion == null || noneKillableMinion.NetworkId != m.NetworkId)
                    && Health.GetPrediction(
                        m,
                        (int)((Program.Player.AttackDelay * 1000) * m.GetTimeToHit()),
                        FarmDelay,
                        HealthPredictionType.Simulated) <= Program.Player.GetAutoAttackDamage(m));
        }

        #endregion

        internal class OrbwalkerActionArgs : EventArgs
        {
            #region Properties

            internal Vector3 Position { get; set; }

            internal bool Process { get; set; }

            internal Obj_AI_Base Sender { get; set; }

            internal AttackableUnit Target { get; set; }

            internal OrbwalkingType Type { get; set; }

            #endregion
        }
    }
}
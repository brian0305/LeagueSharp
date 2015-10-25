namespace Valvrave_Sharp.Core
{
    using System;
    using System.Linq;
    using System.Reflection;
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

    using SharpDX;

    using Color = System.Drawing.Color;
    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal static class Orbwalker
    {
        #region Static Fields

        private static readonly Random Random = new Random(DateTime.Now.Millisecond);

        private static bool enabled;

        #endregion

        #region Delegates

        public delegate void OnActionDelegate(object sender, OrbwalkerActionArgs e);

        #endregion

        #region Public Events

        public static event OnActionDelegate OnAction;

        #endregion

        #region Public Properties

        public static OrbwalkerMode ActiveMode { get; set; }

        public static bool Attack { get; set; }

        public static bool CanAttack
        {
            get
            {
                return Variables.TickCount + Game.Ping / 2 + 25
                       >= LastAutoAttackTick + Program.Player.AttackDelay * 1000 && Attack;
            }
        }

        public static bool CanMove
        {
            get
            {
                if (!Movement)
                {
                    return false;
                }
                if (MissileLaunched && Program.MainMenu["Orbwalker"]["Advanced"]["Missile"])
                {
                    return true;
                }
                return !Program.Player.CanCancelAutoAttack()
                       || Variables.TickCount + Game.Ping / 2
                       >= LastAutoAttackTick + Program.Player.AttackCastDelay * 1000
                       + Program.MainMenu["Orbwalker"]["Advanced"]["ExtraWindup"];
            }
        }

        public static bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                enabled = value;
                Program.MainMenu["Orbwalker"]["Enable"].GetValue<MenuBool>().Value = value;
            }
        }

        public static int LastAutoAttackTick { get; private set; }

        public static Obj_AI_Minion LastMinion { get; private set; }

        public static int LastMovementOrderTick { get; internal set; }

        public static AttackableUnit LastTarget { get; private set; }

        public static bool Movement { get; set; }

        public static Vector3 OrbwalkPosition { get; set; }

        public static AttackableUnit OrbwalkTarget { get; set; }

        #endregion

        #region Properties

        private static int FarmDelay
        {
            get
            {
                return Program.MainMenu["Orbwalker"]["Advanced"]["FarmDelay"];
            }
        }

        private static bool MissileLaunched { get; set; }

        #endregion

        #region Public Methods and Operators

        public static AttackableUnit GetTarget(OrbwalkerMode? modeArg)
        {
            var mode = modeArg ?? ActiveMode;
            if ((mode == OrbwalkerMode.LaneClear || mode == OrbwalkerMode.Hybrid)
                && !Program.MainMenu["Orbwalker"]["Advanced"]["PriorizeFarm"])
            {
                var target = TargetSelector.GetTarget();
                if (target != null)
                {
                    return target;
                }
            }
            if (mode == OrbwalkerMode.LaneClear || mode == OrbwalkerMode.Hybrid || mode == OrbwalkerMode.LastHit)
            {
                foreach (var minion in
                    GameObjects.EnemyMinions.Where(
                        m => m.InAutoAttackRange() && (m.IsMinion() || m.CharData.BaseSkinName != "jarvanivstandard"))
                        .OrderByDescending(m => m.GetMinionType() == MinionTypes.Siege)
                        .ThenBy(m => m.GetMinionType() == MinionTypes.Super)
                        .ThenBy(m => m.Health)
                        .ThenByDescending(m => m.MaxHealth))
                {
                    var time =
                        (int)
                        (Program.Player.AttackCastDelay * 1000
                         + Math.Max(0, Program.Player.Distance(minion) - Program.Player.BoundingRadius)
                         / Program.Player.GetProjectileSpeed() * 1000 - 100 + Game.Ping / 2f);
                    var healthPrediction = Health.GetPrediction(minion, time, FarmDelay);
                    if (healthPrediction <= 0)
                    {
                        InvokeAction(
                            new OrbwalkerActionArgs
                                {
                                    Position = minion.Position, Target = minion, Process = true,
                                    Type = OrbwalkerType.NonKillableMinion
                                });
                    }
                    if (healthPrediction > 0 && healthPrediction <= Program.Player.GetAutoAttackDamage(minion, true))
                    {
                        return minion;
                    }
                }
            }
            if (mode == OrbwalkerMode.LaneClear)
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
            if (mode != OrbwalkerMode.LastHit)
            {
                var target = TargetSelector.GetTarget();
                if (target != null)
                {
                    return target;
                }
            }
            if (ActiveMode == OrbwalkerMode.LaneClear)
            {
                var shouldWait =
                    GameObjects.EnemyMinions.Any(
                        m =>
                        m.InAutoAttackRange() && (m.IsMinion() || m.CharData.BaseSkinName != "jarvanivstandard")
                        && Health.GetPrediction(
                            m,
                            (int)(Program.Player.AttackDelay * 1000 * 2f),
                            FarmDelay,
                            HealthPredictionType.Simulated) <= Program.Player.GetAutoAttackDamage(m, true));
                if (!shouldWait)
                {
                    var mob = GameObjects.JungleLegendary.FirstOrDefault(j => j.InAutoAttackRange())
                              ?? GameObjects.JungleSmall.FirstOrDefault(
                                  j =>
                                  j.InAutoAttackRange() && j.Name.Contains("Mini") && j.Name.Contains("SRU_Razorbeak"))
                              ?? GameObjects.JungleLarge.FirstOrDefault(j => j.InAutoAttackRange())
                              ?? GameObjects.JungleSmall.FirstOrDefault(j => j.InAutoAttackRange());
                    if (mob != null)
                    {
                        return mob;
                    }
                    if (LastMinion.InAutoAttackRange())
                    {
                        var predHealth = Health.GetPrediction(
                            LastMinion,
                            (int)(Program.Player.AttackDelay * 1000 * 2f),
                            FarmDelay,
                            HealthPredictionType.Simulated);
                        if (predHealth >= 2 * Program.Player.GetAutoAttackDamage(LastMinion, true)
                            || Math.Abs(predHealth - LastMinion.Health) < float.Epsilon)
                        {
                            return LastMinion;
                        }
                    }
                    var minion = (from m in
                                      GameObjects.EnemyMinions.Where(
                                          m =>
                                          m.InAutoAttackRange()
                                          && (m.IsMinion() || m.CharData.BaseSkinName != "jarvanivstandard"))
                                  let predictedHealth =
                                      Health.GetPrediction(
                                          m,
                                          (int)(Program.Player.AttackDelay * 1000 * 2f),
                                          FarmDelay,
                                          HealthPredictionType.Simulated)
                                  where
                                      predictedHealth >= 2 * Program.Player.GetAutoAttackDamage(m, true)
                                      || Math.Abs(predictedHealth - m.Health) < float.Epsilon
                                  select m).MaxOrDefault(m => m.Health);
                    if (minion != null)
                    {
                        return LastMinion = minion;
                    }
                }
            }
            return null;
        }

        public static void MoveOrder(Vector3 position, bool overrideTimer = false)
        {
            var playerPosition = Program.Player.ServerPosition;
            if (playerPosition.Distance(position)
                > Program.Player.BoundingRadius + Program.MainMenu["Orbwalker"]["Advanced"]["ExtraHold"])
            {
                var point = position;
                if (Program.Player.DistanceSquared(point) < 150 * 150)
                {
                    point = playerPosition.Extend(position, (Random.NextFloat(0.6f, 1) + 0.2f) * 400);
                }
                var angle = 0f;
                var currentPath = Program.Player.GetWaypoints();
                if (currentPath.Count > 1 && currentPath.PathLength() > 100)
                {
                    var movePath = Program.Player.GetPath(point);
                    if (movePath.Length > 1)
                    {
                        angle = (currentPath[1] - currentPath[0]).AngleBetween((movePath[1] - movePath[0]).ToVector2());
                        var distance = movePath.Last().ToVector2().DistanceSquared(currentPath.Last());
                        if ((angle < 10 && distance < 500 * 500) || distance < 50 * 50)
                        {
                            return;
                        }
                    }
                }
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
                                    { Position = point, Process = true, Type = OrbwalkerType.Movement };
                InvokeAction(eventArgs);
                if (eventArgs.Process)
                {
                    Program.Player.IssueOrder(GameObjectOrder.MoveTo, eventArgs.Position);
                    LastMovementOrderTick = Variables.TickCount;
                }
            }
        }

        public static void Orbwalk(AttackableUnit target = null, Vector3? position = null)
        {
            if (CanAttack)
            {
                var gTarget = target ?? GetTarget(ActiveMode);
                if (gTarget.IsValidTarget())
                {
                    var eventArgs = new OrbwalkerActionArgs
                                        {
                                            Target = gTarget, Position = gTarget.Position, Process = true,
                                            Type = OrbwalkerType.BeforeAttack
                                        };
                    InvokeAction(eventArgs);
                    if (eventArgs.Process)
                    {
                        if (Program.Player.CanCancelAutoAttack())
                        {
                            LastAutoAttackTick = Variables.TickCount + Game.Ping + 100
                                                 - (int)(Program.Player.AttackCastDelay * 1000);
                            MissileLaunched = false;
                            var d = gTarget.GetRealAutoAttackRange() - 65;
                            if (Program.Player.DistanceSquared(gTarget) > d * d && !Program.Player.IsMelee)
                            {
                                LastAutoAttackTick = Variables.TickCount + Game.Ping + 400
                                                     - (int)(Program.Player.AttackCastDelay * 1000);
                            }
                        }
                        if (!Program.Player.IssueOrder(GameObjectOrder.AttackUnit, gTarget))
                        {
                            ResetAutoAttackTimer();
                        }
                        LastMovementOrderTick = 0;
                        LastTarget = gTarget;
                        return;
                    }
                }
            }
            if (CanMove)
            {
                MoveOrder(position.HasValue && position.Value.IsValid() ? position.Value : Game.CursorPos);
            }
        }

        public static void ResetAutoAttackTimer()
        {
            LastAutoAttackTick = 0;
        }

        #endregion

        #region Methods

        internal static void Init(Menu menu)
        {
            var orbwalkMenu = menu.Add(new Menu("Orbwalker", "Orbwalker"));
            {
                orbwalkMenu.Bool("Enable", "Enable Orbwalker");
                var drawMenu = orbwalkMenu.Add(new Menu("Draw", "Draw"));
                {
                    drawMenu.Bool("AARange", "Auto-Attack Range");
                    drawMenu.Bool("KillableMinion", "Killable Minion", false);
                    drawMenu.Bool("KillableMinionFade", "Enable Killable Minion Fade Effect", false);
                }
                var advMenu = orbwalkMenu.Add(new Menu("Advanced", "Advanced"));
                {
                    advMenu.Separator("Movement");
                    advMenu.Slider("ExtraHold", "Extra Hold Position", 25, 0, 250);
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
            orbwalkMenu.MenuValueChanged += (sender, args) =>
                {
                    var keyBind = sender as MenuKeyBind;
                    if (keyBind != null)
                    {
                        var modeName = keyBind.Name.Substring(0, keyBind.Name.IndexOf("Key", StringComparison.Ordinal));
                        OrbwalkerMode mode;
                        ActiveMode = Enum.TryParse(modeName, true, out mode)
                                         ? keyBind.Active
                                               ? mode
                                               : mode == ActiveMode
                                                     ? Program.MainMenu["Orbwalker"]["lasthitKey"].GetValue<MenuKeyBind>
                                                           ().Active
                                                           ? OrbwalkerMode.LastHit
                                                           : Program.MainMenu["Orbwalker"]["laneclearKey"]
                                                                 .GetValue<MenuKeyBind>().Active
                                                                 ? OrbwalkerMode.LaneClear
                                                                 : Program.MainMenu["Orbwalker"]["hybridKey"]
                                                                       .GetValue<MenuKeyBind>().Active
                                                                       ? OrbwalkerMode.Hybrid
                                                                       : Program.MainMenu["Orbwalker"]["orbwalkKey"]
                                                                             .GetValue<MenuKeyBind>().Active
                                                                             ? OrbwalkerMode.Orbwalk
                                                                             : OrbwalkerMode.None
                                                     : ActiveMode
                                         : ActiveMode;
                    }
                    var boolean = sender as MenuBool;
                    if (boolean != null)
                    {
                        if (boolean.Name.Equals("Enable"))
                        {
                            enabled = boolean.Value;
                        }
                    }
                };
            Movement = Attack = true;
            enabled = Program.MainMenu["Orbwalker"]["Enable"];

            Game.OnUpdate += args =>
                {
                    if (InterruptableSpell.IsCastingInterruptableSpell(Program.Player, true) || !Enabled)
                    {
                        return;
                    }
                    if (ActiveMode != OrbwalkerMode.None)
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
                    if (!sender.IsMe || !Enabled || !AutoAttack.IsAutoAttack(args.SData.Name))
                    {
                        return;
                    }
                    if (Game.Ping <= 30)
                    {
                        DelayAction.Add(30, () => OnDoCastDelayed(args));
                        return;
                    }
                    OnDoCastDelayed(args);
                };
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Drawing.OnDraw += OnDraw;
        }

        private static void InvokeAction(OrbwalkerActionArgs e)
        {
            if (OnAction != null)
            {
                OnAction(MethodBase.GetCurrentMethod().DeclaringType, e);
            }
        }

        private static void OnDoCastDelayed(GameObjectProcessSpellCastEventArgs args)
        {
            InvokeAction(
                new OrbwalkerActionArgs { Target = args.Target as AttackableUnit, Type = OrbwalkerType.AfterAttack });
            MissileLaunched = true;
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
                            m.IsValidTarget(1200) && (m.IsMinion() || m.CharData.BaseSkinName != "jarvanivstandard")
                            && m.Health < Program.Player.GetAutoAttackDamage(m, true) * 2);
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
                            m.IsValidTarget(1200) && (m.IsMinion() || m.CharData.BaseSkinName != "jarvanivstandard")
                            && m.Health < Program.Player.GetAutoAttackDamage(m, true));
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
                LastAutoAttackTick = Variables.TickCount - Game.Ping / 2;
                MissileLaunched = false;
                if (!target.Compare(LastTarget))
                {
                    InvokeAction(new OrbwalkerActionArgs { Target = target, Type = OrbwalkerType.TargetSwitch });
                    LastTarget = target;
                }
                InvokeAction(
                    new OrbwalkerActionArgs { Target = target, Sender = sender, Type = OrbwalkerType.OnAttack });
            }
            if (AutoAttack.IsAutoAttackReset(spellName))
            {
                ResetAutoAttackTimer();
            }
        }

        #endregion

        internal class OrbwalkerActionArgs : EventArgs
        {
            #region Public Properties

            public Vector3 Position { get; set; }

            public bool Process { get; set; }

            public Obj_AI_Base Sender { get; set; }

            public AttackableUnit Target { get; set; }

            public OrbwalkerType Type { get; internal set; }

            #endregion
        }
    }
}
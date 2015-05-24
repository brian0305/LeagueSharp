using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace BrianSharp.Common
{
    internal class Orbwalker
    {
        public delegate void AfterAttackEvenH(AttackableUnit target);

        public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);

        public delegate void OnAttackEvenH(AttackableUnit target);

        public delegate void OnNonKillableMinionH(AttackableUnit minion);

        public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);

        public enum Mode
        {
            Combo,
            Harass,
            Clear,
            LastHit,
            Flee,
            None
        }

        private const float ClearWaitTime = 2;
        private static Menu _config;
        public static Obj_AI_Hero ForcedTarget = null;
        private static Obj_AI_Minion _prevMinion;
        private static bool _disableNextAttack, _missileLaunched;
        private static int _lastAttack, _lastMove;
        private static AttackableUnit _lastTarget;
        private static Spell _movePrediction;
        private static readonly Random RandomPos = new Random(DateTime.Now.Millisecond);

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public static bool Attack { get; set; }
        public static bool Move { get; set; }

        private static int GetCurrentFarmDelay
        {
            get { return _config.Item("OW_Misc_FarmDelay").GetValue<Slider>().Value; }
        }

        private static int GetCurrentWindupTime
        {
            get { return _config.Item("OW_Misc_ExtraWindUp").GetValue<Slider>().Value; }
        }

        public static Mode CurrentMode
        {
            get
            {
                if (_config.Item("OW_Combo_Key").IsActive())
                {
                    return Mode.Combo;
                }
                if (_config.Item("OW_Harass_Key").IsActive())
                {
                    return Mode.Harass;
                }
                if (_config.Item("OW_Clear_Key").IsActive())
                {
                    return Mode.Clear;
                }
                if (_config.Item("OW_LastHit_Key").IsActive())
                {
                    return Mode.LastHit;
                }
                return _config.Item("OW_Flee_Key").IsActive() ? Mode.Flee : Mode.None;
            }
        }

        private static bool IsAllowedToAttack
        {
            get
            {
                if (!Attack || _config.Item("OW_Misc_AllAttackDisabled").IsActive())
                {
                    return false;
                }
                if ((CurrentMode == Mode.Combo || CurrentMode == Mode.Harass || CurrentMode == Mode.Clear) &&
                    !_config.Item("OW_" + CurrentMode + "_Attack").IsActive())
                {
                    return false;
                }
                return CurrentMode != Mode.LastHit || _config.Item("OW_LastHit_Attack").IsActive();
            }
        }

        private static bool IsAllowedToMove
        {
            get
            {
                if (!Move || _config.Item("OW_Misc_AllMovementDisabled").IsActive())
                {
                    return false;
                }
                if ((CurrentMode == Mode.Combo || CurrentMode == Mode.Harass || CurrentMode == Mode.Clear) &&
                    !_config.Item("OW_" + CurrentMode + "_Move").IsActive())
                {
                    return false;
                }
                return CurrentMode != Mode.LastHit || _config.Item("OW_LastHit_Move").IsActive();
            }
        }

        public static bool CanAttack
        {
            get { return Utils.GameTimeTickCount + Game.Ping / 2 + 25 >= _lastAttack + Player.AttackDelay * 1000; }
        }

        private static bool CanMove
        {
            get
            {
                return _missileLaunched ||
                       Utils.GameTimeTickCount + Game.Ping / 2 >=
                       _lastAttack + Player.AttackCastDelay * 1000 + GetCurrentWindupTime;
            }
        }

        private static bool ShouldWait
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Any(
                            i =>
                                InAutoAttackRange(i) && i.Team != GameObjectTeam.Neutral &&
                                HealthPrediction.LaneClearHealthPrediction(
                                    i, (int) (Player.AttackDelay * 1000 * ClearWaitTime), GetCurrentFarmDelay) <=
                                Player.GetAutoAttackDamage(i, true));
            }
        }

        public static AttackableUnit GetPossibleTarget
        {
            get
            {
                AttackableUnit target = null;
                if (_config.Item("OW_Misc_PriorityUnit").GetValue<StringList>().SelectedIndex == 1 &&
                    (CurrentMode == Mode.Harass || CurrentMode == Mode.Clear))
                {
                    target = GetBestHeroTarget;
                    if (target.IsValidTarget())
                    {
                        return target;
                    }
                }
                if (CurrentMode == Mode.Harass || CurrentMode == Mode.Clear || CurrentMode == Mode.LastHit)
                {
                    foreach (var obj in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                i =>
                                    InAutoAttackRange(i) && i.Team != GameObjectTeam.Neutral &&
                                    MinionManager.IsMinion(i, true)))
                    {
                        var time = (int) (Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
                                   1000 * (int) (Player.Distance(obj) / Orbwalking.GetMyProjectileSpeed());
                        var hpPred = HealthPrediction.GetHealthPrediction(obj, time, GetCurrentFarmDelay);
                        if (hpPred < 1)
                        {
                            FireOnNonKillableMinion(obj);
                        }
                        if (hpPred > 0 && hpPred <= Player.GetAutoAttackDamage(obj, true))
                        {
                            return obj;
                        }
                    }
                }
                if (InAutoAttackRange(ForcedTarget))
                {
                    return ForcedTarget;
                }
                if (CurrentMode == Mode.Clear)
                {
                    foreach (var obj in ObjectManager.Get<Obj_AI_Turret>().Where(i => InAutoAttackRange(i)))
                    {
                        return obj;
                    }
                    foreach (var obj in ObjectManager.Get<Obj_BarracksDampener>().Where(i => InAutoAttackRange(i)))
                    {
                        return obj;
                    }
                    foreach (var obj in ObjectManager.Get<Obj_HQ>().Where(i => InAutoAttackRange(i)))
                    {
                        return obj;
                    }
                }
                if (CurrentMode != Mode.LastHit)
                {
                    target = GetBestHeroTarget;
                    if (target.IsValidTarget())
                    {
                        return target;
                    }
                }
                if (CurrentMode == Mode.Clear)
                {
                    target =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(i => InAutoAttackRange(i) && i.Team == GameObjectTeam.Neutral)
                            .MaxOrDefault(i => i.MaxHealth);
                    if (target != null)
                    {
                        return target;
                    }
                }
                if (CurrentMode == Mode.Clear && !ShouldWait)
                {
                    if (InAutoAttackRange(_prevMinion))
                    {
                        var hpPred = HealthPrediction.LaneClearHealthPrediction(
                            _prevMinion, (int) (Player.AttackDelay * 1000 * ClearWaitTime), GetCurrentFarmDelay);
                        if (hpPred >= 2 * Player.GetAutoAttackDamage(_prevMinion, true) ||
                            Math.Abs(hpPred - _prevMinion.Health) < float.Epsilon)
                        {
                            return _prevMinion;
                        }
                    }
                    target = (from obj in ObjectManager.Get<Obj_AI_Minion>().Where(i => InAutoAttackRange(i))
                        let hpPred =
                            HealthPrediction.LaneClearHealthPrediction(
                                obj, (int) (Player.AttackDelay * 1000 * ClearWaitTime), GetCurrentFarmDelay)
                        where
                            hpPred >= 2 * Player.GetAutoAttackDamage(obj, true) ||
                            Math.Abs(hpPred - obj.Health) < float.Epsilon
                        select obj).MaxOrDefault(i => i.Health);
                    if (target != null)
                    {
                        _prevMinion = (Obj_AI_Minion) target;
                    }
                }
                return target;
            }
        }

        public static Obj_AI_Hero GetBestHeroTarget
        {
            get
            {
                Obj_AI_Hero killableObj = null;
                var hitsToKill = double.MaxValue;
                foreach (var obj in HeroManager.Enemies.Where(i => InAutoAttackRange(i)))
                {
                    var killHits = obj.Health / Player.GetAutoAttackDamage(obj, true);
                    if (killableObj != null && (!(killHits < hitsToKill) || obj.HasBuffOfType(BuffType.Invulnerability)))
                    {
                        continue;
                    }
                    killableObj = obj;
                    hitsToKill = killHits;
                }
                return hitsToKill < 4 ? killableObj : TargetSelector.GetTarget(-1, TargetSelector.DamageType.Physical);
            }
        }

        public static event BeforeAttackEvenH BeforeAttack;
        public static event OnAttackEvenH OnAttack;
        public static event AfterAttackEvenH AfterAttack;
        public static event OnTargetChangeH OnTargetChange;
        public static event OnNonKillableMinionH OnNonKillableMinion;

        public static void AddToMainMenu(Menu mainMenu)
        {
            _config = mainMenu;
            var owMenu = new Menu("Orbwalker", "OW");
            {
                var modeMenu = new Menu("Mode", "Mode");
                {
                    var comboMenu = new Menu("Combo", "OW_Combo");
                    {
                        comboMenu.AddItem(
                            new MenuItem("OW_Combo_Key", "Key").SetValue(new KeyBind(32, KeyBindType.Press)));
                        comboMenu.AddItem(new MenuItem("OW_Combo_Move", "Movement").SetValue(true));
                        comboMenu.AddItem(new MenuItem("OW_Combo_Attack", "Attack").SetValue(true));
                        modeMenu.AddSubMenu(comboMenu);
                    }
                    var harassMenu = new Menu("Harass", "OW_Harass");
                    {
                        harassMenu.AddItem(
                            new MenuItem("OW_Harass_Key", "Key").SetValue(
                                new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                        harassMenu.AddItem(new MenuItem("OW_Harass_Move", "Movement").SetValue(true));
                        harassMenu.AddItem(new MenuItem("OW_Harass_Attack", "Attack").SetValue(true));
                        harassMenu.AddItem(new MenuItem("OW_Harass_LastHit", "Last Hit Minion").SetValue(true));
                        modeMenu.AddSubMenu(harassMenu);
                    }
                    var clearMenu = new Menu("Clear", "OW_Clear");
                    {
                        clearMenu.AddItem(
                            new MenuItem("OW_Clear_Key", "Key").SetValue(
                                new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
                        clearMenu.AddItem(new MenuItem("OW_Clear_Move", "Movement").SetValue(true));
                        clearMenu.AddItem(new MenuItem("OW_Clear_Attack", "Attack").SetValue(true));
                        modeMenu.AddSubMenu(clearMenu);
                    }
                    var lastHitMenu = new Menu("Last Hit", "OW_LastHit");
                    {
                        lastHitMenu.AddItem(
                            new MenuItem("OW_LastHit_Key", "Key").SetValue(new KeyBind(17, KeyBindType.Press)));
                        lastHitMenu.AddItem(new MenuItem("OW_LastHit_Move", "Movement").SetValue(true));
                        lastHitMenu.AddItem(new MenuItem("OW_LastHit_Attack", "Attack").SetValue(true));
                        modeMenu.AddSubMenu(lastHitMenu);
                    }
                    var fleeMenu = new Menu("Flee", "OW_Flee");
                    {
                        fleeMenu.AddItem(
                            new MenuItem("OW_Flee_Key", "Key").SetValue(
                                new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                        modeMenu.AddSubMenu(fleeMenu);
                    }
                    owMenu.AddSubMenu(modeMenu);
                }
                var miscMenu = new Menu("Misc", "Misc");
                {
                    miscMenu.AddItem(new MenuItem("OW_Misc_HoldZone", "Hold Zone").SetValue(new Slider(50, 0, 250)));
                    miscMenu.AddItem(new MenuItem("OW_Misc_FarmDelay", "Farm Delay").SetValue(new Slider(80, 0, 300)));
                    miscMenu.AddItem(
                        new MenuItem("OW_Misc_MoveDelay", "Movement Delay").SetValue(new Slider(30, 0, 250)));
                    miscMenu.AddItem(
                        new MenuItem("OW_Misc_ExtraWindUp", "Extra WindUp Time").SetValue(new Slider(60, 0, 200)));
                    miscMenu.AddItem(
                        new MenuItem("OW_Misc_PriorityUnit", "Priority Unit").SetValue(
                            new StringList(new[] { "Minion", "Hero" }, 1)));
                    miscMenu.AddItem(new MenuItem("OW_Misc_MeleeMagnet", "Melee Movement Magnet").SetValue(true));
                    miscMenu.AddItem(
                        new MenuItem("OW_Misc_AllMovementDisabled", "Disable All Movement").SetValue(false));
                    miscMenu.AddItem(new MenuItem("OW_Misc_AllAttackDisabled", "Disable All Attack").SetValue(false));
                    owMenu.AddSubMenu(miscMenu);
                }
                var drawMenu = new Menu("Draw", "Draw");
                {
                    drawMenu.AddItem(
                        new MenuItem("OW_Draw_AARange", "Player AA Range").SetValue(
                            new Circle(false, Color.FloralWhite)));
                    drawMenu.AddItem(
                        new MenuItem("OW_Draw_AARangeEnemy", "Enemy AA Range").SetValue(new Circle(false, Color.Pink)));
                    drawMenu.AddItem(
                        new MenuItem("OW_Draw_HoldZone", "Hold Zone").SetValue(new Circle(false, Color.FloralWhite)));
                    owMenu.AddSubMenu(drawMenu);
                }
                _config.AddSubMenu(owMenu);
            }
            _movePrediction = new Spell(SpellSlot.Unknown, GetAutoAttackRange());
            _movePrediction.SetTargetted(Player.BasicAttack.SpellCastTime, Player.BasicAttack.MissileSpeed);
            Attack = true;
            Move = true;
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            GameObject.OnCreate += OnCreateMissileClient;
            Spellbook.OnStopCast += OnStopCast;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || CurrentMode == Mode.None || MenuGUI.IsChatOpen || Player.IsRecalling() ||
                Player.IsCastingInterruptableSpell(true))
            {
                return;
            }
            Orbwalk(CurrentMode == Mode.Flee ? null : GetPossibleTarget);
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (_config.Item("OW_Draw_AARange").IsActive())
            {
                Render.Circle.DrawCircle(
                    Player.Position, GetAutoAttackRange(), _config.Item("OW_Draw_AARange").GetValue<Circle>().Color);
            }
            if (_config.Item("OW_Draw_AARangeEnemy").IsActive())
            {
                foreach (var obj in HeroManager.Enemies.Where(i => i.IsValidTarget(1000)))
                {
                    Render.Circle.DrawCircle(
                        obj.Position, GetAutoAttackRange(obj, Player),
                        _config.Item("OW_Draw_AARangeEnemy").GetValue<Circle>().Color);
                }
            }
            if (_config.Item("OW_Draw_HoldZone").IsActive())
            {
                Render.Circle.DrawCircle(
                    Player.Position, _config.Item("OW_Misc_HoldZone").GetValue<Slider>().Value,
                    _config.Item("OW_Draw_HoldZone").GetValue<Circle>().Color);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (Orbwalking.IsAutoAttackReset(args.SData.Name))
            {
                Utility.DelayAction.Add(250, ResetAutoAttack);
            }
            if (!args.SData.IsAutoAttack())
            {
                return;
            }
            if (args.Target is Obj_AI_Base || args.Target is Obj_BarracksDampener || args.Target is Obj_HQ)
            {
                _lastAttack = Utils.GameTimeTickCount - Game.Ping / 2;
                _missileLaunched = false;
                if (args.Target.IsValid<Obj_AI_Base>())
                {
                    var target = (AttackableUnit) args.Target;
                    FireOnTargetSwitch(target);
                    _lastTarget = target;
                    Utility.DelayAction.Add(
                        (int) (sender.AttackCastDelay * 1000 + 40), () => FireAfterAttack(_lastTarget));
                }
            }
            FireOnAttack(_lastTarget);
        }

        private static void OnCreateMissileClient(GameObject sender, EventArgs args)
        {
            if (!sender.IsValid<MissileClient>())
            {
                return;
            }
            var missile = (MissileClient) sender;
            if (!missile.SpellCaster.IsMe || !missile.SData.IsAutoAttack())
            {
                return;
            }
            _missileLaunched = true;
        }

        private static void OnStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            if (!sender.Owner.IsMe)
            {
                return;
            }
            if (args.DestroyMissile && args.StopAnimation)
            {
                ResetAutoAttack();
            }
        }

        private static void MoveTo(Vector3 pos)
        {
            if (Utils.GameTimeTickCount - _lastMove < _config.Item("OW_Misc_MoveDelay").GetValue<Slider>().Value)
            {
                return;
            }
            _lastMove = Utils.GameTimeTickCount;
            if (Player.Distance(pos) < _config.Item("OW_Misc_HoldZone").GetValue<Slider>().Value)
            {
                if (Player.Path.Count() > 1)
                {
                    Player.IssueOrder(GameObjectOrder.Stop, Player.ServerPosition);
                    Player.IssueOrder(GameObjectOrder.HoldPosition, Player.ServerPosition);
                }
                return;
            }
            Player.IssueOrder(
                GameObjectOrder.MoveTo, Player.ServerPosition.Extend(pos, (RandomPos.NextFloat(0.6f, 1) + 0.2f) * 400));
        }

        public static void Orbwalk(AttackableUnit target)
        {
            if (target.IsValidTarget() && CanAttack && IsAllowedToAttack)
            {
                _disableNextAttack = false;
                FireBeforeAttack(target);
                if (!_disableNextAttack)
                {
                    if (CurrentMode != Mode.Harass || !(target is Obj_AI_Minion) ||
                        _config.Item("OW_Harass_LastHit").IsActive())
                    {
                        _lastAttack = Utils.GameTimeTickCount + Game.Ping + 100 - (int) (Player.AttackCastDelay * 1000f);
                        _missileLaunched = false;
                        Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                        _lastTarget = target;
                        return;
                    }
                }
            }
            if (!CanMove || !IsAllowedToMove)
            {
                return;
            }
            if (Player.IsMelee() && Player.AttackRange < 200 && InAutoAttackRange(target) && target is Obj_AI_Hero &&
                _config.Item("OW_Misc_MeleeMagnet").IsActive() && ((Obj_AI_Hero) target).Distance(Game.CursorPos) < 200)
            {
                _movePrediction.Delay = Player.BasicAttack.SpellCastTime;
                _movePrediction.Speed = Player.BasicAttack.MissileSpeed;
                MoveTo(_movePrediction.GetPrediction((Obj_AI_Hero) target).CastPosition);
            }
            else
            {
                MoveTo(Game.CursorPos);
            }
        }

        private static void ResetAutoAttack()
        {
            _lastAttack = 0;
        }

        public static float GetAutoAttackRange(AttackableUnit target = null)
        {
            return GetAutoAttackRange(Player, target);
        }

        private static float GetAutoAttackRange(Obj_AI_Base source, AttackableUnit target)
        {
            return source.AttackRange + source.BoundingRadius + (target.IsValidTarget() ? target.BoundingRadius : 0);
        }

        public static bool InAutoAttackRange(AttackableUnit target, float extraRange = 0, Vector3 from = new Vector3())
        {
            return target.IsValidTarget(GetAutoAttackRange(target) + extraRange, true, from);
        }

        private static void FireBeforeAttack(AttackableUnit target)
        {
            if (BeforeAttack != null)
            {
                if (target.IsValidTarget())
                {
                    BeforeAttack(new BeforeAttackEventArgs { Target = target });
                }
            }
            else
            {
                _disableNextAttack = false;
            }
        }

        private static void FireOnAttack(AttackableUnit target)
        {
            if (OnAttack != null && target.IsValidTarget())
            {
                OnAttack(target);
            }
        }

        private static void FireAfterAttack(AttackableUnit target)
        {
            if (AfterAttack != null && target.IsValidTarget())
            {
                AfterAttack(target);
            }
        }

        private static void FireOnTargetSwitch(AttackableUnit newTarget)
        {
            if (OnTargetChange != null && (!_lastTarget.IsValidTarget() || _lastTarget.NetworkId != newTarget.NetworkId))
            {
                OnTargetChange(_lastTarget, newTarget);
            }
        }

        private static void FireOnNonKillableMinion(AttackableUnit minion)
        {
            if (OnNonKillableMinion != null && minion.IsValidTarget())
            {
                OnNonKillableMinion(minion);
            }
        }

        public class BeforeAttackEventArgs
        {
            private bool _process = true;
            public AttackableUnit Target;

            public bool Process
            {
                get { return _process; }
                set
                {
                    _disableNextAttack = !value;
                    _process = value;
                }
            }
        }
    }
}
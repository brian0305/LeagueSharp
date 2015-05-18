using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using LeagueSharp.Common.Data;
using SharpDX;

namespace BrianSharp.Common
{
    internal class Helper : Program
    {
        public enum MinionType
        {
            All,
            Minion,
            Ward
        }

        public enum SmiteType
        {
            Grey,
            Purple,
            Red,
            Blue,
            None
        }

        public static SmiteType CurrentSmiteType
        {
            get
            {
                if (Player.GetSpellSlot("s5_summonersmitequick").IsReady())
                {
                    return SmiteType.Grey;
                }
                if (Player.GetSpellSlot("itemsmiteaoe").IsReady())
                {
                    return SmiteType.Purple;
                }
                if (Player.GetSpellSlot("s5_summonersmiteduel").IsReady())
                {
                    return SmiteType.Red;
                }
                return Player.GetSpellSlot("s5_summonersmiteplayerganker").IsReady() ? SmiteType.Blue : SmiteType.None;
            }
        }

        public static bool PacketCast
        {
            get { return GetValue<bool>("Misc", "UsePacket"); }
        }

        public static InventorySlot GetWardSlot
        {
            get
            {
                var ward = Items.GetWardSlot();
                var wardPink = new[] { 3362, 2043 };
                if (GetValue<bool>("Flee", "PinkWard") && ward == null)
                {
                    foreach (var item in
                        wardPink.Where(Items.CanUseItem)
                            .Select(i => Player.InventoryItems.FirstOrDefault(a => a.Id == (ItemId) i))
                            .Where(i => i != null))
                    {
                        ward = item;
                    }
                }
                return ward;
            }
        }

        public static float GetWardRange
        {
            get
            {
                return 600 *
                       (Player.HasMastery(MasteryData.Scout) && GetWardSlot != null &&
                        new[] { 3340, 3361, 3362 }.Contains((int) GetWardSlot.Id)
                           ? 1.15f
                           : 1);
            }
        }

        public static bool IsMinion(Obj_AI_Base obj)
        {
            return obj.IsValid<Obj_AI_Minion>() && MinionManager.IsMinion((Obj_AI_Minion) obj);
        }

        public static bool IsWard(Obj_AI_Minion obj)
        {
            return !MinionManager.IsMinion(obj) &&
                   (obj.BaseSkinName.ToLower().Contains("ward") || obj.BaseSkinName.ToLower().Contains("trinket"));
        }

        public static void CustomOrbwalk(Obj_AI_Base target)
        {
            Orbwalker.Orbwalk(Orbwalker.InAutoAttackRange(target) ? target : null);
        }

        public static bool CanKill(Obj_AI_Base target, double subDmg)
        {
            return target.Health + 5 < subDmg;
        }

        public static bool CastFlash(Vector3 pos)
        {
            return Flash.IsReady() && pos.IsValid() && Player.Spellbook.CastSpell(Flash, pos);
        }

        public static bool CastSmite(Obj_AI_Base target, bool killable = true)
        {
            return Smite.IsReady() && target.IsValidTarget(760) &&
                   (!killable || target.Health < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Smite)) &&
                   Player.Spellbook.CastSpell(Smite, target);
        }

        public static bool CastIgnite(Obj_AI_Hero target)
        {
            return Ignite.IsReady() && target.IsValidTarget(600) &&
                   target.Health + 5 < Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) &&
                   Player.Spellbook.CastSpell(Ignite, target);
        }

        public static void SmiteMob()
        {
            if (!GetValue<bool>("SmiteMob", "Smite") || !Smite.IsReady())
            {
                return;
            }
            var obj =
                MinionManager.GetMinions(760, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
                    .FirstOrDefault(i => CanSmiteMob(i.Name));
            if (obj == null)
            {
                return;
            }
            CastSmite(obj);
        }

        private static bool CanSmiteMob(string name)
        {
            if (GetValue<bool>("SmiteMob", "Baron") && name.StartsWith("SRU_Baron"))
            {
                return true;
            }
            if (GetValue<bool>("SmiteMob", "Dragon") && name.StartsWith("SRU_Dragon"))
            {
                return true;
            }
            if (name.Contains("Mini"))
            {
                return false;
            }
            if (GetValue<bool>("SmiteMob", "Red") && name.StartsWith("SRU_Red"))
            {
                return true;
            }
            if (GetValue<bool>("SmiteMob", "Blue") && name.StartsWith("SRU_Blue"))
            {
                return true;
            }
            if (GetValue<bool>("SmiteMob", "Krug") && name.StartsWith("SRU_Krug"))
            {
                return true;
            }
            if (GetValue<bool>("SmiteMob", "Gromp") && name.StartsWith("SRU_Gromp"))
            {
                return true;
            }
            if (GetValue<bool>("SmiteMob", "Raptor") && name.StartsWith("SRU_Razorbeak"))
            {
                return true;
            }
            return GetValue<bool>("SmiteMob", "Wolf") && name.StartsWith("SRU_Murkwolf");
        }

        #region Menu

        public static void AddSmiteMob(Menu menu)
        {
            var smiteMob = new Menu("Smite Mob If Killable", "SmiteMob");
            AddBool(smiteMob, "Smite", "Use Smite");
            AddBool(smiteMob, "Auto", "-> Auto Smite");
            AddBool(smiteMob, "Baron", "-> Baron Nashor");
            AddBool(smiteMob, "Dragon", "-> Dragon");
            AddBool(smiteMob, "Red", "-> Red Brambleback");
            AddBool(smiteMob, "Blue", "-> Blue Sentinel");
            AddBool(smiteMob, "Krug", "-> Ancient Krug");
            AddBool(smiteMob, "Gromp", "-> Gromp");
            AddBool(smiteMob, "Raptor", "-> Crimson Raptor");
            AddBool(smiteMob, "Wolf", "-> Greater Murk Wolf");
            menu.AddSubMenu(smiteMob);
        }

        public static void AddNotif(string msg, int dur)
        {
            Notifications.AddNotification(new Notification(msg, dur, true));
        }

        public static MenuItem AddText(Menu subMenu, string item, string display)
        {
            return subMenu.AddItem(new MenuItem("_" + subMenu.Name + "_" + item, display, true));
        }

        public static MenuItem AddKeybind(Menu subMenu,
            string item,
            string display,
            string key,
            KeyBindType type = KeyBindType.Press,
            bool state = false)
        {
            return
                subMenu.AddItem(
                    new MenuItem("_" + subMenu.Name + "_" + item, display, true).SetValue(
                        new KeyBind(key.ToCharArray()[0], type, state)));
        }

        public static MenuItem AddBool(Menu subMenu, string item, string display, bool state = true)
        {
            return subMenu.AddItem(new MenuItem("_" + subMenu.Name + "_" + item, display, true).SetValue(state));
        }

        public static MenuItem AddSlider(Menu subMenu, string item, string display, int cur, int min = 1, int max = 100)
        {
            return
                subMenu.AddItem(
                    new MenuItem("_" + subMenu.Name + "_" + item, display, true).SetValue(new Slider(cur, min, max)));
        }

        public static MenuItem AddList(Menu subMenu, string item, string display, string[] text, int defaultIndex = 0)
        {
            return
                subMenu.AddItem(
                    new MenuItem("_" + subMenu.Name + "_" + item, display, true).SetValue(
                        new StringList(text, defaultIndex)));
        }

        public static T GetValue<T>(string subMenu, string item)
        {
            return MainMenu.Item("_" + subMenu + "_" + item, true).GetValue<T>();
        }

        public static MenuItem GetItem(string subMenu, string item)
        {
            return MainMenu.Item("_" + subMenu + "_" + item, true);
        }

        #endregion
    }
}
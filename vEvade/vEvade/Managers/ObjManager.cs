namespace vEvade.Managers
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using LeagueSharp;
    using LeagueSharp.Common;

    #endregion

    public class ObjManagerInfo
    {
        #region Fields

        public string Name;

        public GameObject Obj;

        public int Time;

        #endregion

        #region Constructors and Destructors

        public ObjManagerInfo(GameObject obj, string name)
        {
            this.Obj = obj;
            this.Name = name;
            this.Time = Utils.GameTimeTickCount;
        }

        #endregion
    }

    public static class ObjManager
    {
        #region Static Fields

        public static Dictionary<int, ObjManagerInfo> ObjCache = new Dictionary<int, ObjManagerInfo>();

        private static readonly List<string> CloneList = new List<string> { "leblanc", "shaco", "monkeyking" };

        private static readonly string[] LargeNameRegex =
            {
                "SRU_Murkwolf[0-9.]{1,}", "SRU_Gromp", "SRU_Blue[0-9.]{1,}",
                "SRU_Razorbeak[0-9.]{1,}", "SRU_Red[0-9.]{1,}",
                "SRU_Krug[0-9]{1,}"
            };

        private static readonly string[] LegendaryNameRegex = { "SRU_Dragon", "SRU_Baron", "SRU_RiftHerald" };

        private static readonly List<string> NormalMinionList = new List<string>
                                                                    {
                                                                        "SRU_ChaosMinionMelee", "SRU_ChaosMinionRanged",
                                                                        "SRU_OrderMinionMelee", "SRU_OrderMinionRanged",
                                                                        "HA_ChaosMinionMelee", "HA_ChaosMinionRanged",
                                                                        "HA_OrderMinionMelee", "HA_OrderMinionRanged",
                                                                        "OdinRedSuperminion", "Odin_Red_Minion_Caster",
                                                                        "OdinBlueSuperminion", "Odin_Blue_Minion_Caster"
                                                                    };

        private static readonly List<string> PetList = new List<string>
                                                           {
                                                               "annietibbers", "elisespiderling", "heimertyellow",
                                                               "heimertblue", "malzaharvoidling", "shacobox",
                                                               "yorickspectralghoul", "yorickdecayedghoul",
                                                               "yorickravenousghoul", "zyrathornplant",
                                                               "zyragraspingplant"
                                                           };

        private static readonly List<string> SiegeMinionList = new List<string>
                                                                   {
                                                                       "SRU_ChaosMinionSiege", "SRU_OrderMinionSiege",
                                                                       "HA_ChaosMinionSiege", "HA_OrderMinionSiege"
                                                                   };

        private static readonly string[] SmallNameRegex = { "SRU_[a-zA-Z](.*?)Mini", "Sru_Crab" };

        private static readonly List<string> SuperMinionList = new List<string>
                                                                   {
                                                                       "SRU_ChaosMinionSuper", "SRU_OrderMinionSuper",
                                                                       "HA_ChaosMinionSuper", "HA_OrderMinionSuper",
                                                                       "OdinRedUltraminion", "OdinBlueUltraminion"
                                                                   };

        #endregion

        #region Constructors and Destructors

        static ObjManager()
        {
            GameObject.OnCreate += HiuManager.OnCreate;
        }

        #endregion

        #region Enums

        public enum JungleType
        {
            Unknown,

            Small,

            Large,

            Legendary
        }

        [Flags]
        public enum MinionTypes
        {
            Unknown = 0,

            Normal = 1 << 0,

            Ranged = 1 << 1,

            Melee = 1 << 2,

            Siege = 1 << 3,

            Super = 1 << 4,

            Ward = 1 << 5
        }

        #endregion

        #region Public Methods and Operators

        public static bool IsJungle(this Obj_AI_Minion minion)
        {
            return minion.GetJungleType() != JungleType.Unknown;
        }

        public static bool IsMinion(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Melee)
                   || minion.GetMinionType().HasFlag(MinionTypes.Ranged);
        }

        public static bool IsPet(this Obj_AI_Minion minion, bool includeClones = true)
        {
            var name = minion.CharData.BaseSkinName.ToLower();
            return PetList.Contains(name) || (includeClones && CloneList.Contains(name));
        }

        public static bool IsValid(this Obj_AI_Base unit)
        {
            return unit != null && unit.IsValid;
        }

        public static bool IsWard(this Obj_AI_Minion minion)
        {
            return minion.GetMinionType().HasFlag(MinionTypes.Ward) && minion.CharData.BaseSkinName != "BlueTrinket";
        }

        #endregion

        #region Methods

        private static JungleType GetJungleType(this Obj_AI_Minion minion)
        {
            if (minion.Team != GameObjectTeam.Neutral)
            {
                return JungleType.Unknown;
            }

            if (SmallNameRegex.Any(regex => Regex.IsMatch(minion.Name, regex)))
            {
                return JungleType.Small;
            }

            if (LargeNameRegex.Any(regex => Regex.IsMatch(minion.Name, regex)))
            {
                return JungleType.Large;
            }

            if (LegendaryNameRegex.Any(regex => Regex.IsMatch(minion.Name, regex)))
            {
                return JungleType.Legendary;
            }

            return JungleType.Unknown;
        }

        private static MinionTypes GetMinionType(this Obj_AI_Minion minion)
        {
            var baseSkinName = minion.CharData.BaseSkinName;

            if (NormalMinionList.Any(n => baseSkinName.Equals(n)))
            {
                return MinionTypes.Normal
                       | (minion.CharData.BaseSkinName.Contains("Melee") ? MinionTypes.Melee : MinionTypes.Ranged);
            }

            if (SiegeMinionList.Any(n => baseSkinName.Equals(n)))
            {
                return MinionTypes.Siege | MinionTypes.Ranged;
            }

            if (SuperMinionList.Any(n => baseSkinName.Equals(n)))
            {
                return MinionTypes.Super | MinionTypes.Melee;
            }

            if (baseSkinName.ToLower().Contains("ward") || baseSkinName.ToLower().Contains("trinket"))
            {
                return MinionTypes.Ward;
            }

            return MinionTypes.Unknown;
        }

        #endregion
    }
}
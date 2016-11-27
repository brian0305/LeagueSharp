namespace vEvade.Helpers
{
    #region

    using System.Collections.Generic;

    using LeagueSharp;

    #endregion

    public static class SpellBlocker
    {
        #region Static Fields

        public static List<SpellSlot> Spells = new List<SpellSlot>
                                                   { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };

        #endregion

        #region Constructors and Destructors

        static SpellBlocker()
        {
            switch (ObjectManager.Player.ChampionName)
            {
                case "Aatrox":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Ahri":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Akali":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Alistar":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Amumu":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Anivia":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W };
                    break;
                case "Annie":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Ashe":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Azir":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Bard":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Blitzcrank":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Brand":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Braum":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Caitlyn":
                    Spells = new List<SpellSlot> { SpellSlot.E };
                    break;
                case "Cassiopeia":
                    Spells = new List<SpellSlot>();
                    break;
                case "Chogath":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Corki":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Darius":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Diana":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Draven":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "DrMundo":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Ekko":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Elise":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Evelynn":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Ezreal":
                    Spells = new List<SpellSlot> { SpellSlot.E };
                    break;
                case "Fiddlesticks":
                    Spells = new List<SpellSlot> { SpellSlot.Q };
                    break;
                case "Fiora":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Fizz":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E };
                    break;
                case "Galio":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Gangplank":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Garen":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Gnar":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Gragas":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Graves":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Hecarim":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Heimerdinger":
                    Spells = new List<SpellSlot> { SpellSlot.R };
                    break;
                case "Irelia":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Janna":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E };
                    break;
                case "JarvanIV":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Jax":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Jayce":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Jinx":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E };
                    break;
                case "Kalista":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Karma":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Karthus":
                    Spells = new List<SpellSlot> { SpellSlot.E };
                    break;
                case "Kassadin":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Katarina":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Kayle":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Kennen":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Khazix":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "KogMaw":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "Leblanc":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "LeeSin":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Leona":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Lissandra":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Lucian":
                    Spells = new List<SpellSlot> { SpellSlot.E };
                    break;
                case "Lulu":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Lux":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Malphite":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Malzahar":
                    Spells = new List<SpellSlot> { SpellSlot.E };
                    break;
                case "Maokai":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "MasterYi":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E, SpellSlot.R };
                    break;
                case "MissFortune":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "MonkeyKing":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Mordekaiser":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "Morgana":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Nami":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Nasus":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Nautilus":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Nidalee":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Nocturne":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Nunu":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "Olaf":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Orianna":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E };
                    break;
                case "Pantheon":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Poppy":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Quinn":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Rammus":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "RekSai":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Renekton":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Rengar":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W };
                    break;
                case "Riven":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E, SpellSlot.R };
                    break;
                case "Rumble":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "Ryze":
                    Spells = new List<SpellSlot> { SpellSlot.R };
                    break;
                case "Sejuani":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E };
                    break;
                case "Shaco":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Shen":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Shyvana":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Singed":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Sion":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Sivir":
                    Spells = new List<SpellSlot> { SpellSlot.E, SpellSlot.R };
                    break;
                case "Skarner":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Sona":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Soraka":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Swain":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Syndra":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.R };
                    break;
                case "TahmKench":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Talon":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E, SpellSlot.R };
                    break;
                case "Taric":
                    Spells = new List<SpellSlot> { SpellSlot.R };
                    break;
                case "Teemo":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Thresh":
                    Spells = new List<SpellSlot> { SpellSlot.Q };
                    break;
                case "Tristana":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "Trundle":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Tryndamere":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E, SpellSlot.R };
                    break;
                case "TwistedFate":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E };
                    break;
                case "Twitch":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Udyr":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Urgot":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Varus":
                    Spells = new List<SpellSlot>();
                    break;
                case "Vayne":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.R };
                    break;
                case "Veigar":
                    Spells = new List<SpellSlot>();
                    break;
                case "Velkoz":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.E };
                    break;
                case "Vi":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Viktor":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Vladimir":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.R };
                    break;
                case "Volibear":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Warwick":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Xerath":
                    Spells = new List<SpellSlot>();
                    break;
                case "XinZhao":
                    Spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Yasuo":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Yorick":
                    Spells = new List<SpellSlot>();
                    break;
                case "Zac":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Zed":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Ziggs":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
                case "Zilean":
                    Spells = new List<SpellSlot> { SpellSlot.W, SpellSlot.E, SpellSlot.R };
                    break;
                case "Zyra":
                    Spells = new List<SpellSlot> { SpellSlot.W };
                    break;
            }
        }

        #endregion

        #region Public Methods and Operators

        public static bool CanBlock(SpellSlot slot)
        {
            if (slot == SpellSlot.Summoner1 || slot == SpellSlot.Summoner2)
            {
                return false;
            }

            if (Spells.Contains(slot))
            {
                return false;
            }

            return slot == SpellSlot.Q || slot == SpellSlot.W || slot == SpellSlot.E || slot == SpellSlot.R;
        }

        #endregion
    }
}
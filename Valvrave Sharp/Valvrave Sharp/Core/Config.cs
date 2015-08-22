namespace Valvrave_Sharp.Core
{
    using System.Windows.Forms;

    using LeagueSharp.SDK.Core.Enumerations;
    using LeagueSharp.SDK.Core.UI.IMenu.Values;

    using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

    internal class Config
    {
        #region Public Methods and Operators

        public static MenuBool Bool(Menu subMenu, string name, string display, bool state = true)
        {
            return subMenu.Add(new MenuBool(name, display, state));
        }

        public static MenuKeyBind KeyBind(
            Menu subMenu,
            string name,
            string display,
            Keys key,
            KeyBindType type = KeyBindType.Press)
        {
            return subMenu.Add(new MenuKeyBind(name, display, key, type));
        }

        public static MenuList List(Menu subMenu, string name, string display, string[] array)
        {
            return subMenu.Add(new MenuList<string>(name, display, array));
        }

        public static MenuSeparator Separator(Menu subMenu, string name, string display)
        {
            return subMenu.Add(new MenuSeparator(name, display));
        }

        public static MenuSlider Slider(Menu subMenu, string name, string display, int cur, int min = 0, int max = 100)
        {
            return subMenu.Add(new MenuSlider(name, display, cur, min, max));
        }

        #endregion
    }
}
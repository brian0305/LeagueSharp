namespace vEvade.SpecialSpells
{
    #region

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Spells;

    using SpellData = vEvade.Spells.SpellData;

    #endregion

    public interface IChampionManager
    {
        #region Public Methods and Operators

        void LoadSpecialSpell(SpellData spellData);

        #endregion
    }

    public class AllChampions : IChampionManager
    {
        #region Static Fields

        private static bool init;

        #endregion

        #region Public Methods and Operators

        public void LoadSpecialSpell(SpellData spellData)
        {
            if (init)
            {
                return;
            }

            init = true;
            SpellDetector.OnProcessSpell += OnProcessSpell;
        }

        #endregion

        #region Methods

        private static void OnProcessSpell(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            switch (data.MenuName)
            {
                case "MalphiteR":
                    {
                        var newData = (SpellData)data.Clone();
                        newData.MissileSpeed += (int)sender.MoveSpeed;
                        spellArgs.NewData = newData;
                    }
                    break;
                case "SionR":
                    {
                        var newData = (SpellData)data.Clone();
                        newData.MissileSpeed = (int)sender.MoveSpeed;
                        spellArgs.NewData = newData;
                    }
                    break;
            }

            if (data.MultipleNumber == -1 || (data.MenuName == "KhazixW" && args.SData.Name == data.SpellName))
            {
                return;
            }

            var start = sender.ServerPosition.To2D();
            var end = args.End.To2D();
            var dir = (end - start).Normalized();

            for (var i = -(data.MultipleNumber - 1) / 2; i <= (data.MultipleNumber - 1) / 2; i++)
            {
                SpellDetector.AddSpell(sender, start, start + data.Range * dir.Rotated(data.MultipleAngle * i), data);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}
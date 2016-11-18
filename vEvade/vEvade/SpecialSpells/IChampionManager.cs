namespace vEvade.SpecialSpells
{
    #region

    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using vEvade.Core;
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
            SpellDetector.OnCreateSpell += OnCreateSpell;
        }

        #endregion

        #region Methods

        private static void OnCreateSpell(
            Obj_AI_Base sender,
            MissileClient missile,
            SpellData data,
            SpellArgs spellArgs)
        {
            var canCheck = false;
            var newData = (SpellData)data.Clone();

            switch (data.MenuName)
            {
                case "BardR":
                    if (missile.SData.Name.Contains("Fixed"))
                    {
                        canCheck = true;
                        newData.MissileSpeed = 500;
                    }
                    break;
                case "CorkiQ":
                    if (missile.SData.Name.EndsWith("Min"))
                    {
                        canCheck = true;
                        newData.DelayEx = 200;
                    }
                    break;
                case "EkkoW":
                    canCheck = true;
                    break;
            }

            if (!canCheck)
            {
                return;
            }

            var oldSpell =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i =>
                    i.MissileObject == null
                    && (i.Data.MissileName == missile.SData.Name
                        || i.Data.ExtraMissileNames.Contains(missile.SData.Name))
                    && i.Unit.NetworkId == sender.NetworkId);

            if (oldSpell == null)
            {
                spellArgs.NewData = newData;

                return;
            }

            Evade.SpellsDetected[oldSpell.SpellId] = new SpellInstance(
                newData,
                oldSpell.StartTick,
                newData.Delay + newData.DelayEx
                + (int)(oldSpell.Start.Distance(oldSpell.End) / newData.MissileSpeed * 1000),
                oldSpell.Start,
                oldSpell.End,
                oldSpell.Unit,
                oldSpell.Type) { MissileObject = missile, SpellId = oldSpell.SpellId };
            spellArgs.NoProcess = true;
        }

        private static void OnProcessSpell(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args,
            SpellData data,
            SpellArgs spellArgs)
        {
            var startPos = sender.ServerPosition;
            var endPos = args.End;
            var dir = (endPos - startPos).To2D().Normalized();
            var newData = (SpellData)data.Clone();

            switch (data.MenuName)
            {
                case "MalphiteR":
                    newData.MissileSpeed = 1500 + (int)sender.MoveSpeed;
                    spellArgs.NewData = newData;
                    break;
                case "SionR":
                    newData.MissileSpeed = (int)sender.MoveSpeed;
                    spellArgs.NewData = newData;
                    break;
            }

            if (data.MultipleNumber == -1)
            {
                return;
            }

            if (data.MenuName == "KhazixW" && args.SData.Name == data.SpellName)
            {
                return;
            }

            for (var i = -(data.MultipleNumber - 1) / 2; i <= (data.MultipleNumber - 1) / 2; i++)
            {
                SpellDetector.AddSpell(
                    sender,
                    startPos,
                    startPos + data.Range * dir.Rotated(data.MultipleAngle * i).To3D(),
                    data);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}
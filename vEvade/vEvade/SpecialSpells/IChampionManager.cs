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
            }

            if (!canCheck)
            {
                return;
            }

            var oldSpell =
                Evade.SpellsDetected.Values.FirstOrDefault(
                    i =>
                    i.MissileObject == null && i.Data.MenuName == data.MenuName && i.Unit.NetworkId == sender.NetworkId);

            if (oldSpell == null)
            {
                spellArgs.NewData = newData;

                return;
            }

            Evade.SpellsDetected[oldSpell.SpellId] = new SpellInstance(
                newData,
                oldSpell.StartTick,
                newData.Delay + (int)(oldSpell.Start.Distance(oldSpell.End) / newData.MissileSpeed * 1000),
                oldSpell.Start,
                oldSpell.End,
                oldSpell.Unit,
                oldSpell.Type) { SpellId = oldSpell.SpellId, MissileObject = missile };
            spellArgs.NoProcess = true;
        }

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

            if (data.MultipleNumber == -1 || (data.MenuName == "KhazixW" && !args.SData.Name.EndsWith("Long")))
            {
                return;
            }

            var startPos = sender.ServerPosition.To2D();
            var endPos = args.End.To2D();

            if (data.InfrontStart > 0)
            {
                startPos = startPos.Extend(endPos, data.InfrontStart);
            }

            var dir = (endPos - startPos).Normalized();
            var startTick = Utils.GameTimeTickCount;

            for (var i = -(data.MultipleNumber - 1) / 2; i <= (data.MultipleNumber - 1) / 2; i++)
            {
                SpellDetector.AddSpell(
                    sender,
                    sender.ServerPosition.To2D(),
                    startPos + dir.Rotated(data.MultipleAngle * i) * (data.Range / 2f),
                    data,
                    null,
                    SpellType.None,
                    true,
                    startTick);
            }

            spellArgs.NoProcess = true;
        }

        #endregion
    }
}
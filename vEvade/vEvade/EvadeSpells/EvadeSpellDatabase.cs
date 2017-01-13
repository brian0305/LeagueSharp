namespace vEvade.EvadeSpells
{
    #region

    using System.Collections.Generic;

    using LeagueSharp;
    using LeagueSharp.Common;

    #endregion

    internal class EvadeSpellDatabase
    {
        #region Static Fields

        public static List<EvadeSpellData> Spells = new List<EvadeSpellData>();

        #endregion

        #region Constructors and Destructors

        static EvadeSpellDatabase()
        {
            var champName = ObjectManager.Player.ChampionName;
            Spells.Add(new EvadeSpellData("Walking", 1));

            #region SpellShield

            #region Sivir

            if (champName == "Sivir")
            {
                Spells.Add(new ShieldData("SivirE", SpellSlot.E, 100, 1, true));
            }

            #endregion Sivir

            #region Nocturne

            if (champName == "Nocturne")
            {
                Spells.Add(new ShieldData("NocturneW", SpellSlot.W, 100, 1, true));
            }

            #endregion Nocturne

            #endregion SpellShield

            #region MoveSpeed

            #region Bard

            if (champName == "Bard")
            {
                Spells.Add(new MoveBuffData("BardW", SpellSlot.W, 150, 3, () => ObjectManager.Player.MoveSpeed * 1.5f));
            }

            #endregion Bard

            #region Blitzcrank

            if (champName == "Blitzcrank")
            {
                Spells.Add(
                    new MoveBuffData(
                        "BlitzcrankW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.7f, 0.75f, 0.8f, 0.85f, 0.9f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1])));
            }

            #endregion Blitzcrank

            #region Draven

            if (champName == "Draven")
            {
                Spells.Add(
                    new MoveBuffData(
                        "DravenW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.4f, 0.45f, 0.5f, 0.55f, 0.6f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1])));
            }

            #endregion Draven

            #region Evelynn

            if (champName == "Evelynn")
            {
                Spells.Add(
                    new MoveBuffData(
                        "EvelynnW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.3f, 0.4f, 0.5f, 0.6f, 0.7f }[ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1
                                 ])));
            }

            #endregion Evelynn

            #region Garen

            if (champName == "Garen")
            {
                Spells.Add(new MoveBuffData("GarenQ", SpellSlot.Q, 0, 3, () => ObjectManager.Player.MoveSpeed * 1.3f));
            }

            #endregion Garen

            #region Hecarim

            if (champName == "Hecarim")
            {
                Spells.Add(
                    new MoveBuffData("HecarimE", SpellSlot.E, 0, 3, () => ObjectManager.Player.MoveSpeed * 1.25f));
            }

            #endregion Hecarim

            #region Jayce

            if (champName == "Jayce")
            {
                Spells.Add(new MoveBuffData("JayceR", SpellSlot.R, 0, 3, () => ObjectManager.Player.MoveSpeed + 40));
            }

            #endregion Jayce

            #region Karma

            if (champName == "Karma")
            {
                Spells.Add(
                    new MoveBuffData(
                        "KarmaE",
                        SpellSlot.E,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.4f, 0.45f, 0.5f, 0.55f, 0.6f }[
                               ObjectManager.Player.GetSpell(SpellSlot.E).Level - 1])));
            }

            #endregion Karma

            #region Katarina

            if (champName == "Katarina")
            {
                Spells.Add(
                    new MoveBuffData(
                        "KatarinaW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f }[ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1
                                 ])));
            }

            #endregion Katarina

            #region Kayle

            if (champName == "Kayle")
            {
                Spells.Add(
                    new MoveBuffData(
                        "KayleW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + (new[] { 0.18f, 0.21f, 0.24f, 0.27f, 0.3f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1]
                              + ObjectManager.Player.TotalMagicalDamage / 100 * 0.07f))));
            }

            #endregion Kayle

            #region Kennen

            if (champName == "Kennen")
            {
                Spells.Add(new MoveBuffData("KennenE", SpellSlot.E, 0, 3, () => 225 + ObjectManager.Player.MoveSpeed));
            }

            #endregion Kennen

            #region Khazix

            if (champName == "Khazix")
            {
                Spells.Add(new MoveBuffData("KhazixR", SpellSlot.R, 0, 5, () => ObjectManager.Player.MoveSpeed * 1.4f));
            }

            #endregion Khazix

            #region Lulu

            if (champName == "Lulu")
            {
                Spells.Add(
                    new MoveBuffData(
                        "LuluW",
                        SpellSlot.W,
                        0,
                        5,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1 + (0.3f + ObjectManager.Player.TotalMagicalDamage / 100 * 0.05f))));
            }

            #endregion Lulu

            #region Nunu

            if (champName == "Nunu")
            {
                Spells.Add(
                    new MoveBuffData(
                        "NunuW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.08f, 0.09f, 0.1f, 0.11f, 0.12f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1])));
            }

            #endregion Nunu

            #region Rumble

            if (champName == "Rumble")
            {
                Spells.Add(
                    new MoveBuffData(
                        "RumbleW",
                        SpellSlot.W,
                        0,
                        5,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.1f, 0.15f, 0.2f, 0.25f, 0.3f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1]
                           * (ObjectManager.Player.ManaPercent >= 50 ? 1.5f : 1))));
            }

            #endregion Rumble

            #region Shyvana

            if (champName == "Shyvana")
            {
                Spells.Add(
                    new MoveBuffData(
                        "ShyvanaW",
                        SpellSlot.W,
                        0,
                        4,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.3f, 0.35f, 0.4f, 0.45f, 0.5f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1])));
            }

            #endregion Shyvana

            #region Sivir

            if (champName == "Sivir")
            {
                Spells.Add(
                    new MoveBuffData(
                        "SivirR",
                        SpellSlot.R,
                        0,
                        5,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1 + new[] { 0.4f, 0.5f, 0.6f }[ObjectManager.Player.GetSpell(SpellSlot.R).Level - 1])));
            }

            #endregion Sivir

            #region Sona

            if (champName == "Sona")
            {
                Spells.Add(
                    new MoveBuffData(
                        "SonaE",
                        SpellSlot.E,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + (new[] { 0.1f, 0.11f, 0.12f, 0.13f, 0.14f }[
                               ObjectManager.Player.GetSpell(SpellSlot.E).Level - 1]
                              + ObjectManager.Player.TotalMagicalDamage / 100 * 0.06f))));
            }

            #endregion Sona

            #region Teemo

            if (champName == "Teemo")
            {
                Spells.Add(
                    new MoveBuffData(
                        "TeemoW",
                        SpellSlot.W,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.2f, 0.28f, 0.36f, 0.44f, 0.52f }[
                               ObjectManager.Player.GetSpell(SpellSlot.W).Level - 1])));
            }

            #endregion Teemo

            #region Udyr

            if (champName == "Udyr")
            {
                Spells.Add(
                    new MoveBuffData(
                        "UdyrE",
                        SpellSlot.E,
                        0,
                        3,
                        () =>
                        ObjectManager.Player.MoveSpeed
                        * (1
                           + new[] { 0.15f, 0.2f, 0.25f, 0.3f, 0.35f }[
                               ObjectManager.Player.GetSpell(SpellSlot.E).Level - 1])));
            }

            #endregion Udyr

            #region Zilean

            if (champName == "Zilean")
            {
                Spells.Add(new MoveBuffData("ZileanE", SpellSlot.E, 0, 3, () => ObjectManager.Player.MoveSpeed * 1.55f));
            }

            #endregion Zilean

            #endregion MoveSpeed

            #region Dash

            #region Aatrox

            if (champName == "Aatrox")
            {
                Spells.Add(new DashData("AatroxQ", SpellSlot.Q, 650, false, 600, 3050, 3) { Invert = true });
            }

            #endregion Aatrox

            #region Akali

            if (champName == "Akali")
            {
                Spells.Add(
                    new DashData("AkaliR", SpellSlot.R, 800, false, 100, 2461, 3)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion Akali

            #region Alistar

            if (champName == "Alistar")
            {
                Spells.Add(
                    new DashData("AlistarW", SpellSlot.W, 650, false, 100, 1900, 3)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion Alistar

            #region Caitlyn

            if (champName == "Caitlyn")
            {
                Spells.Add(new DashData("CaitlynE", SpellSlot.E, 390, true, 250, 1000, 3) { Invert = true });
            }

            #endregion

            #region Corki

            if (champName == "Corki")
            {
                Spells.Add(new DashData("CorkiW", SpellSlot.W, 600, false, 250, 1044, 3));
            }

            #endregion

            #region Fizz

            if (champName == "Fizz")
            {
                Spells.Add(
                    new DashData("FizzQ", SpellSlot.Q, 550, true, 100, 1400, 4)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion

            #region Gnar

            if (champName == "Gnar")
            {
                Spells.Add(new DashData("GnarE", SpellSlot.E, 475, false, 100, 900, 3) { CheckSpellName = "GnarE" });
            }

            #endregion

            #region Gragas

            if (champName == "Gragas")
            {
                Spells.Add(new DashData("GragasE", SpellSlot.E, 600, true, 100, 900, 3));
            }

            #endregion

            #region Graves

            if (champName == "Graves")
            {
                Spells.Add(new DashData("GravesE", SpellSlot.E, 425, true, 100, 1223, 3));
            }

            #endregion

            #region Irelia

            if (champName == "Irelia")
            {
                Spells.Add(
                    new DashData("IreliaQ", SpellSlot.Q, 650, false, 100, 2200, 3)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion

            #region Jax

            if (champName == "Jax")
            {
                Spells.Add(
                    new DashData("JaxQ", SpellSlot.Q, 700, false, 100, 1400, 3)
                        {
                            ValidTargets =
                                new[]
                                    {
                                        SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                        SpellValidTargets.AllyWards, SpellValidTargets.EnemyChampions,
                                        SpellValidTargets.EnemyMinions, SpellValidTargets.EnemyWards
                                    }
                        });
            }

            #endregion

            #region Leblanc

            if (champName == "Leblanc")
            {
                Spells.Add(
                    new DashData("LeBlancW", SpellSlot.W, 600, false, 100, 1600, 3) { CheckSpellName = "LeblancSlide" });
            }

            #endregion

            #region LeeSin

            if (champName == "LeeSin")
            {
                Spells.Add(
                    new DashData("LeeSinW", SpellSlot.W, 700, false, 100, 2000, 3)
                        {
                            ValidTargets =
                                new[]
                                    {
                                        SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                        SpellValidTargets.AllyWards
                                    },
                            CheckSpellName = "BlindMonkWOne"
                        });
            }

            #endregion

            #region Lucian

            if (champName == "Lucian")
            {
                Spells.Add(new DashData("LucianE", SpellSlot.E, 425, false, 100, 1350, 2));
            }

            #endregion

            #region Nidalee

            if (champName == "Nidalee")
            {
                Spells.Add(new DashData("NidaleeW", SpellSlot.W, 375, true, 250, 943, 3) { CheckSpellName = "Pounce" });
            }

            #endregion

            #region Pantheon

            if (champName == "Pantheon")
            {
                Spells.Add(
                    new DashData("PantheonW", SpellSlot.W, 600, false, 100, 1000, 3)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion

            #region Riven

            if (champName == "Riven")
            {
                Spells.Add(new DashData("RivenQ", SpellSlot.Q, 222, true, 100, 560, 3) { RequiresPreMove = true });

                Spells.Add(new DashData("RivenE", SpellSlot.E, 250, false, 100, 1200, 3));
            }

            #endregion

            #region Tristana

            if (champName == "Tristana")
            {
                Spells.Add(new DashData("TristanaW", SpellSlot.W, 900, false, 300, 1100, 5));
            }

            #endregion

            #region Tryndamare

            if (champName == "Tryndamere")
            {
                Spells.Add(new DashData("TryndamereE", SpellSlot.E, 650, false, 100, 900, 3));
            }

            #endregion

            #region Vayne

            if (champName == "Vayne")
            {
                Spells.Add(new DashData("VayneQ", SpellSlot.Q, 300, true, 100, 860, 2));
            }

            #endregion Vayne

            #region Wukong

            if (champName == "MonkeyKing")
            {
                Spells.Add(
                    new DashData("WukongE", SpellSlot.E, 650, false, 100, 1400, 3)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion

            #region Yasuo

            if (champName == "Yasuo")
            {
                Spells.Add(
                    new DashData("YasuoE", SpellSlot.E, 475, true, 100, 1200, 2)
                        { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion

            #endregion Dash

            #region Blink

            #region Ezreal

            if (champName == "Ezreal")
            {
                Spells.Add(new BlinkData("EzrealE", SpellSlot.E, 475, 250, 3));
            }

            #endregion Ezreal

            #region Kassadin

            if (champName == "Kassadin")
            {
                Spells.Add(new BlinkData("KassadinR", SpellSlot.R, 500, 250, 5));
            }

            #endregion Kassadin

            #region Katarina

            if (champName == "Katarina")
            {
                Spells.Add(
                    new BlinkData("KatarinaE", SpellSlot.E, 800, 100, 3)
                        {
                            ValidTargets =
                                new[]
                                    {
                                        SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                        SpellValidTargets.AllyWards, SpellValidTargets.EnemyChampions,
                                        SpellValidTargets.EnemyMinions, SpellValidTargets.EnemyWards
                                    }
                        });
            }

            #endregion Katarina

            #region Shaco

            if (champName == "Shaco")
            {
                Spells.Add(new BlinkData("ShacoQ", SpellSlot.Q, 400, 100, 3));
            }

            #endregion Shaco

            #endregion Blink

            #region Invulnerability

            #region Elise

            if (champName == "Elise")
            {
                Spells.Add(
                    new InvulnerabilityData("EliseE", SpellSlot.E, 100, 3)
                        { CheckSpellName = "EliseSpiderEInitial", SelfCast = true });
            }

            #endregion Elise

            #region Fizz

            if (champName == "Fizz")
            {
                Spells.Add(new InvulnerabilityData("FizzE", SpellSlot.E, 100, 3));
            }

            #endregion Fizz

            #region Maokai

            if (champName == "Maokai")
            {
                Spells.Add(
                    new InvulnerabilityData("MaokaiW", SpellSlot.W, 100, 3)
                        {
                            ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                            MaxRange = 525
                        });
            }

            #endregion Maokai

            #region MasterYi

            if (champName == "MasterYi")
            {
                Spells.Add(
                    new InvulnerabilityData("MasterYiQ", SpellSlot.Q, 100, 3)
                        {
                            ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions },
                            MaxRange = 600
                        });
            }

            #endregion MasterYi

            #region Vladimir

            if (champName == "Vladimir")
            {
                Spells.Add(new InvulnerabilityData("VladimirW", SpellSlot.W, 100, 3) { SelfCast = true });
            }

            #endregion Vladimir

            #endregion Invulnerability

            if (ObjectManager.Player.GetSpellSlot("SummonerFlash") != SpellSlot.Unknown)
            {
                Spells.Add(new BlinkData("Flash", ObjectManager.Player.GetSpellSlot("SummonerFlash"), 425, 100, 5));
            }

            Spells.Add(new EvadeSpellData("Zhonyas", 5) { IsItem = true });

            #region Shield

            #region Janna

            if (champName == "Janna")
            {
                Spells.Add(new ShieldData("JannaE", SpellSlot.E, 100, 1) { CanShieldAllies = true, MaxRange = 800 });
            }

            #endregion Janna

            #region Karma

            if (champName == "Karma")
            {
                Spells.Add(new ShieldData("KarmaE", SpellSlot.E, 100, 2) { CanShieldAllies = true, MaxRange = 800 });
            }

            #endregion Karma

            #region Morgana

            if (champName == "Morgana")
            {
                Spells.Add(new ShieldData("MorganaE", SpellSlot.E, 100, 3) { CanShieldAllies = true, MaxRange = 800 });
            }

            #endregion Morgana

            #endregion Shield
        }

        #endregion
    }
}
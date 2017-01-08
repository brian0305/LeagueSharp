﻿namespace vEvade.EvadeSpells
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

            /*
            #region Aatrox

            if (champName == "Aatrox")
            {
                Spells.Add(new DashData("Aatrox Q", SpellSlot.Q, 650, false, 600, 3050, 3) {Invert=true});
            }

            #endregion Aatrox

            #region Akali

            if (champName == "Akali")
            {
                Spells.Add(new DashData("Akali R", SpellSlot.R, 800, false, 100, 2461, 3) { ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions } });
            }

            #endregion Akali

            #region Alistar

            if (champName == "Alistar")
            {
                spell = new DashData("Alistar W", SpellSlot.W, 650, false, 100, 1900, 3);
                spell.ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions };
                Spells.Add(spell);
            }

            #endregion Alistar

            #region Caitlyn

            if (champName == "Caitlyn")
            {
                spell = new DashData("Caitlyn E", SpellSlot.E, 390, true, 250, 1000, 3);
                spell.Invert = true;
                Spells.Add(spell);
            }

            #endregion

            #region Corki

            if (champName == "Corki")
            {
                spell = new DashData("Corki W", SpellSlot.W, 600, false, 250, 1044, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Fizz

            if (champName == "Fizz")
            {
                spell = new DashData("Fizz Q", SpellSlot.Q, 550, true, 100, 1400, 4);
                spell.ValidTargets = new[] { SpellValidTargets.EnemyMinions, SpellValidTargets.EnemyChampions };
                Spells.Add(spell);
            }

            #endregion

            #region Gragas

            if (champName == "Gragas")
            {
                spell = new DashData("Gragas E", SpellSlot.E, 600, true, 250, 911, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Gnar

            if (champName == "Gnar")
            {
                spell = new DashData("Gnar E", SpellSlot.E, 50, false, 0, 900, 3);
                spell.CheckSpellName = "GnarE";
                Spells.Add(spell);
            }

            #endregion

            #region Graves

            if (champName == "Graves")
            {
                spell = new DashData("Graves E", SpellSlot.E, 425, true, 100, 1223, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Irelia

            if (champName == "Irelia")
            {
                spell = new DashData("Irelia Q", SpellSlot.Q, 650, false, 100, 2200, 3);
                spell.ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions };
                Spells.Add(spell);
            }

            #endregion

            #region Jax

            if (champName == "Jax")
            {
                spell = new DashData("Jax Q", SpellSlot.Q, 700, false, 100, 1400, 3);
                spell.ValidTargets = new[]
                                         {
                                             SpellValidTargets.EnemyWards, SpellValidTargets.AllyWards,
                                             SpellValidTargets.AllyMinions, SpellValidTargets.AllyChampions,
                                             SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions
                                         };
                Spells.Add(spell);
            }

            #endregion

            #region Leblanc

            if (champName == "Leblanc")
            {
                spell = new DashData("LeBlanc W1", SpellSlot.W, 600, false, 100, 1621, 3);
                spell.CheckSpellName = "LeblancSlide";
                Spells.Add(spell);
            }

            if (champName == "Leblanc")
            {
                spell = new DashData("LeBlanc RW", SpellSlot.R, 600, false, 100, 1621, 3);
                spell.CheckSpellName = "LeblancSlideM";
                Spells.Add(spell);
            }

            #endregion

            #region LeeSin

            if (champName == "LeeSin")
            {
                spell = new DashData("LeeSin W", SpellSlot.W, 700, false, 250, 2000, 3);
                spell.ValidTargets = new[]
                                         {
                                             SpellValidTargets.AllyChampions, SpellValidTargets.AllyMinions,
                                             SpellValidTargets.AllyWards
                                         };
                spell.CheckSpellName = "BlindMonkWOne";
                Spells.Add(spell);
            }

            #endregion

            #region Lucian

            if (champName == "Lucian")
            {
                spell = new DashData("Lucian E", SpellSlot.E, 425, false, 100, 1350, 2);
                Spells.Add(spell);
            }

            #endregion

            #region Nidalee

            if (champName == "Nidalee")
            {
                spell = new DashData("Nidalee W", SpellSlot.W, 375, true, 250, 943, 3);
                spell.CheckSpellName = "Pounce";
                Spells.Add(spell);
            }

            #endregion

            #region Pantheon

            if (champName == "Pantheon")
            {
                spell = new DashData("Pantheon W", SpellSlot.W, 600, false, 100, 1000, 3);
                spell.ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions };
                Spells.Add(spell);
            }

            #endregion

            #region Riven

            if (champName == "Riven")
            {
                spell = new DashData("Riven Q", SpellSlot.Q, 222, true, 250, 560, 3);
                spell.RequiresPreMove = true;
                Spells.Add(spell);

                spell = new DashData("Riven E", SpellSlot.E, 250, false, 250, 1200, 3);
                Spells.Add(spell);
            }

            #endregion

            #region Tristana

            if (champName == "Tristana")
            {
                spell = new DashData("Tristana W", SpellSlot.W, 900, true, 300, 800, 5);
                Spells.Add(spell);
            }

            #endregion

            #region Tryndamare

            if (champName == "Tryndamere")
            {
                spell = new DashData("Tryndamere E", SpellSlot.E, 650, true, 250, 900, 3);
                Spells.Add(spell);
            }

            #endregion
            */

            #region Vayne

            if (champName == "Vayne")
            {
                Spells.Add(new DashData("VayneQ", SpellSlot.Q, 300, true, 100, 860, 2));
            }

            #endregion Vayne

            /*
            #region Wukong

            if (champName == "MonkeyKing")
            {
                spell = new DashData("Wukong E", SpellSlot.E, 650, false, 100, 1400, 3);
                spell.ValidTargets = new[] { SpellValidTargets.EnemyChampions, SpellValidTargets.EnemyMinions };
                Spells.Add(spell);
            }

            #endregion
            */

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
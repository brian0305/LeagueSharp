namespace vEvade.Spells
{
    #region

    using System;
    using System.Collections.Generic;

    using LeagueSharp;
    using LeagueSharp.Common;

    #endregion

    public static class SpellDatabase
    {
        #region Static Fields

        public static List<SpellData> Spells = new List<SpellData>();

        #endregion

        #region Constructors and Destructors

        static SpellDatabase()
        {
            #region AllChampions

            Spells.Add(
                new SpellData
                    {
                        ChampName = "AllChampions", MenuName = "SnowBall", SpellName = "SummonerSnowball",
                        Slot = SpellSlot.Summoner1, Range = 1600, Delay = 0, Radius = 50, MissileSpeed = 1200,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsSummoner = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "AllChampions", MenuName = "Poro", SpellName = "SummonerPoroThrow",
                        Slot = SpellSlot.Summoner1, Range = 2500, Delay = 0, Radius = 50, MissileSpeed = 1200,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsSummoner = true
                    });

            #endregion AllChampions

            #region Aatrox

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Aatrox", MenuName = "AatroxQ", SpellName = "AatroxQ", DangerValue = 3, Range = 650,
                        Delay = 600, Radius = 285, MissileSpeed = 3050, Type = SpellType.Circle, IsDangerous = true,
                        AddHitbox = false, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Aatrox", MenuName = "AatroxE", SpellName = "AatroxE",
                        MissileName = "AatroxEConeMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1075, Radius = 60,
                        MissileSpeed = 1250, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            #endregion Aatrox

            #region Ahri

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ahri", MenuName = "AhriQ", SpellName = "AhriOrbofDeception",
                        MissileName = "AhriOrbMissile", DangerValue = 2, Range = 1000, Radius = 100, MissileSpeed = 2500,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true,
                        MissileAccel = -3200, MissileMinSpeed = 400, MissileMaxSpeed = 2500
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ahri", MenuName = "AhriQReturn", SpellName = "AhriOrbReturn",
                        MissileName = "AhriOrbReturn", DangerValue = 3, Range = 20000, Delay = 0, Radius = 100,
                        MissileSpeed = 60, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileToUnit = true, MissileAccel = 1900, MissileMinSpeed = 60, MissileMaxSpeed = 2600
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ahri", MenuName = "AhriE", SpellName = "AhriSeduce",
                        MissileName = "AhriSeduceMissile", DangerValue = 3, Slot = SpellSlot.E, Range = 1000, Radius = 60,
                        MissileSpeed = 1550,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Ahri

            #region Akali

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Akali", MenuName = "AkaliE", SpellName = "AkaliShadowSwipe", DangerValue = 2,
                        Slot = SpellSlot.E, Radius = 325, Type = SpellType.Circle, AddHitbox = false
                    });

            #endregion Akali

            #region Alistar

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Alistar", MenuName = "AlistarQ", SpellName = "Pulverize", DangerValue = 3,
                        Radius = 365, Type = SpellType.Circle, IsDangerous = true, AddHitbox = false
                    });

            #endregion Alistar

            #region Amumu

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Amumu", MenuName = "AmumuQ", SpellName = "BandageToss",
                        MissileName = "SadMummyBandageToss", DangerValue = 3, Range = 1100, Radius = 80,
                        MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Amumu", MenuName = "AmumuE", SpellName = "Tantrum", DangerValue = 2,
                        Slot = SpellSlot.E, Radius = 350, Type = SpellType.Circle, AddHitbox = false
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Amumu", MenuName = "AmumuR", SpellName = "CurseoftheSadMummy", DangerValue = 5,
                        Slot = SpellSlot.R, Radius = 550, Type = SpellType.Circle, IsDangerous = true, AddHitbox = false
                    });

            #endregion Amumu

            #region Anivia

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Anivia", MenuName = "AniviaQ", SpellName = "FlashFrostSpell",
                        MissileName = "FlashFrostSpell", DangerValue = 3, Range = 1100, Radius = 110, MissileSpeed = 850,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            #endregion Anivia

            #region Annie

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Annie", MenuName = "AnnieW", SpellName = "Incinerate", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 560, Radius = 50, Type = SpellType.Cone
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Annie", MenuName = "AnnieR", SpellName = "InfernalGuardian", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 600, Radius = 250, Type = SpellType.Circle, IsDangerous = true
                    });

            #endregion Annie

            #region Ashe

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ashe", MenuName = "AsheW", SpellName = "Volley", MissileName = "VolleyAttack",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 1150, Radius = 20, MissileSpeed = 1500,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, MultipleNumber = 9,
                        MultipleAngle = 4.62f * (float)Math.PI / 180, InfrontStart = 75
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ashe", MenuName = "AsheR", SpellName = "EnchantedCrystalArrow",
                        MissileName = "EnchantedCrystalArrow", DangerValue = 5, Slot = SpellSlot.R, Range = 25000,
                        Radius = 130, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Ashe

            #region Aurelion Sol

            Spells.Add(
                //Todo: Radius
                new SpellData
                    {
                        ChampName = "AurelionSol", MenuName = "AurelionSolQ", SpellName = "AurelionSolQ",
                        MissileName = "AurelionSolQMissile", DangerValue = 2, Range = 550, Radius = 210,
                        MissileSpeed = 850, CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true,
                        MissileAccel = -500, MissileMinSpeed = 600, MissileMaxSpeed = 1000
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "AurelionSol", MenuName = "AurelionSolR", SpellName = "AurelionSolR",
                        MissileName = "AurelionSolRBeamMissile", DangerValue = 3, Slot = SpellSlot.R, Range = 1500,
                        Delay = 350, Radius = 120, MissileSpeed = 4500,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            #endregion Aurelion Sol

            #region Azir

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Azir", MenuName = "AzirQ", SpellName = "AzirQWrapper",
                        MissileName = "AzirSoldierMissile", DangerValue = 2, Range = 20000, Delay = 0, Radius = 70,
                        MissileSpeed = 1600, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Azir", MenuName = "AzirR", SpellName = "AzirR", DangerValue = 3, Slot = SpellSlot.R,
                        Range = 750, Radius = 150, MissileSpeed = 1400,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, BehindStart = 300
                    });

            #endregion Azir

            #region Bard

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Bard", MenuName = "BardQ", SpellName = "BardQ", MissileName = "BardQMissile",
                        DangerValue = 3, Range = 950, Radius = 60, MissileSpeed = 1500,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Bard", MenuName = "BardR", SpellName = "BardR",
                        MissileName = "BardRMissileFixedTravelTime",
                        ExtraMissileNames =
                            new[]
                                {
                                    "BardRMissileRange1", "BardRMissileRange2", "BardRMissileRange3", "BardRMissileRange4",
                                    "BardRMissileRange5"
                                },
                        DangerValue = 2, Slot = SpellSlot.R, Range = 3400, Delay = 500,
                        Radius = 350, MissileSpeed = 2100, Type = SpellType.Circle, MissileMinSpeed = 500
                    });

            #endregion Bard

            #region Blitzcrank

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Blitzcrank", MenuName = "BlitzcrankQ", SpellName = "RocketGrab",
                        MissileName = "RocketGrabMissile", DangerValue = 4, Range = 1050, Radius = 70, MissileSpeed = 1800,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Blitzcrank", MenuName = "BlitzcrankR", SpellName = "StaticField", DangerValue = 2,
                        Slot = SpellSlot.R, Radius = 600, Type = SpellType.Circle, AddHitbox = false
                    });

            #endregion Blitzcrank

            #region Brand

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Brand", MenuName = "BrandQ", SpellName = "BrandQ", MissileName = "BrandQMissile",
                        DangerValue = 3, Range = 1100, Radius = 60, MissileSpeed = 1600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Brand", MenuName = "BrandW", SpellName = "BrandW", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 900, Delay = 850, Radius = 240, Type = SpellType.Circle,
                        AddHitbox = false
                    });

            #endregion Brand

            #region Braum

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Braum", MenuName = "BraumQ", SpellName = "BraumQ", MissileName = "BraumQMissile",
                        DangerValue = 3, Range = 1050, Radius = 60, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Braum", MenuName = "BraumR", SpellName = "BraumRWrapper",
                        MissileName = "BraumRMissile", DangerValue = 4, Slot = SpellSlot.R, Range = 1200, Delay = 550,
                        Radius = 115, MissileSpeed = 1400, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true, HasStartExplosion = true, RadiusEx = 330
                    });

            #endregion Braum

            #region Caitlyn

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Caitlyn", MenuName = "CaitlynQ", SpellName = "CaitlynPiltoverPeacemaker",
                        MissileName = "CaitlynPiltoverPeacemaker", DangerValue = 2, Range = 1300, Delay = 625, Radius = 60,
                        MissileSpeed = 2200, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Caitlyn", MenuName = "CaitlynQBehind", SpellName = "CaitlynQBehind",
                        MissileName = "CaitlynPiltoverPeacemaker2", DangerValue = 2, Range = 1300, Delay = 0, Radius = 90,
                        MissileSpeed = 2200, CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Caitlyn", MenuName = "CaitlynW", SpellName = "CaitlynW", TrapName = "caitlyntrap",
                        Slot = SpellSlot.W, Delay = 0, Radius = 75, Type = SpellType.Circle, IsDangerous = true,
                        ExtraDuration = 90000, DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Caitlyn", MenuName = "CaitlynE", SpellName = "CaitlynEntrapment",
                        MissileName = "CaitlynEntrapmentMissile", Slot = SpellSlot.E, Range = 800, Delay = 160,
                        Radius = 70, MissileSpeed = 1600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Caitlyn

            #region Cassiopeia

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Cassiopeia", MenuName = "CassiopeiaQ", SpellName = "CassiopeiaQ", DangerValue = 2,
                        Range = 850, Delay = 750, Radius = 160, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Cassiopeia", MenuName = "CassiopeiaW", SpellName = "CassiopeiaW",
                        MissileName = "CassiopeiaWMissile", DangerValue = 2, Slot = SpellSlot.W, Range = 20000,
                        Radius = 180, MissileSpeed = 3000, Type = SpellType.Circle, MissileOnly = true,
                        CanBeRemoved = false, ExtraDuration = 5000, AddHitbox = false
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Cassiopeia", MenuName = "CassiopeiaR", SpellName = "CassiopeiaR", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 790, Delay = 500, Radius = 80, Type = SpellType.Cone,
                        IsDangerous = true
                    });

            #endregion Cassiopeia

            #region Chogath

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Chogath", MenuName = "ChogathQ", SpellName = "Rupture", DangerValue = 3, Range = 950,
                        Delay = 1200, Radius = 250, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Chogath", MenuName = "ChogathW", SpellName = "FeralScream", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 650, Delay = 500, Radius = 55, Type = SpellType.Cone
                    });

            #endregion Chogath

            #region Corki

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Corki", MenuName = "CorkiQ", SpellName = "PhosphorusBomb",
                        MissileName = "PhosphorusBombMissile", ExtraMissileNames = new[] { "PhosphorusBombMissileMin" },
                        DangerValue = 2, Range = 825, Delay = 300, Radius = 250, MissileSpeed = 1000,
                        Type = SpellType.Circle, CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Corki", MenuName = "CorkiR", SpellName = "MissileBarrageMissile",
                        MissileName = "MissileBarrageMissile", DangerValue = 2, Slot = SpellSlot.R, Range = 1300,
                        Delay = 180, Radius = 40, MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Corki", MenuName = "CorkiREmpower", SpellName = "MissileBarrageMissile2",
                        MissileName = "MissileBarrageMissile2", DangerValue = 2, Slot = SpellSlot.R, Range = 1500,
                        Delay = 180, Radius = 40, MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Corki

            #region Darius

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Darius", MenuName = "DariusQ", SpellName = "DariusCleave", DangerValue = 3,
                        Delay = 750, Radius = 425, Type = SpellType.Ring, MissileToUnit = true, RadiusEx = 225,
                        DisabledByDefault = true, DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Darius", MenuName = "DariusE", SpellName = "DariusAxeGrabCone", DangerValue = 3,
                        Slot = SpellSlot.E, Range = 510, Radius = 50, Type = SpellType.Cone, IsDangerous = true
                    });

            #endregion Darius

            #region Diana

            Spells.Add(
                //Todo: Arc
                new SpellData
                    {
                        ChampName = "Diana", MenuName = "DianaQ", SpellName = "DianaArc", DangerValue = 3, Range = 900,
                        Radius = 195, MissileSpeed = 1400, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, IsDangerous = true
                    });

            #endregion Diana

            #region DrMundo

            Spells.Add(
                new SpellData
                    {
                        ChampName = "DrMundo", MenuName = "DrMundoQ", SpellName = "InfectedCleaverMissile",
                        MissileName = "InfectedCleaverMissile", DangerValue = 2, Range = 1050, Radius = 60,
                        MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion DrMundo

            #region Draven

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Draven", MenuName = "DravenE", SpellName = "DravenDoubleShot",
                        MissileName = "DravenDoubleShotMissile", DangerValue = 3, Slot = SpellSlot.E, Range = 1100,
                        Radius = 130, MissileSpeed = 1400, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Draven", MenuName = "DravenR", SpellName = "DravenRCast",
                        ExtraSpellNames = new[] { "DravenRDoublecast" }, MissileName = "DravenR", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 25000, Delay = 525, Radius = 160, MissileSpeed = 2000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        InfrontStart = 250
                    });

            #endregion Draven

            #region Ekko

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ekko", MenuName = "EkkoQ", SpellName = "EkkoQ", MissileName = "EkkoQMis",
                        DangerValue = 4, Range = 950, Radius = 60, MissileSpeed = 1650,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ekko", MenuName = "EkkoQReturn", SpellName = "EkkoQReturn",
                        MissileName = "EkkoQReturn", DangerValue = 3, Range = 20000, Delay = 0, Radius = 100,
                        MissileSpeed = 2300, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileToUnit = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ekko", MenuName = "EkkoW", SpellName = "EkkoW", MissileName = "EkkoWMis",
                        DangerValue = 3, Slot = SpellSlot.W, Range = 20000, Delay = 2300, Radius = 375,
                        Type = SpellType.Circle, CanBeRemoved = false, DisabledByDefault = true, ExtraDuration = 1600,
                        ExtraDelay = 1050
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ekko", MenuName = "EkkoR", SpellName = "EkkoR", DangerValue = 3, Slot = SpellSlot.R,
                        Range = 20000, Radius = 375, MissileSpeed = 1650, Type = SpellType.Circle
                    });

            #endregion Ekko

            #region Elise

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Elise", MenuName = "EliseEHuman", SpellName = "EliseHumanE",
                        MissileName = "EliseHumanE", DangerValue = 4, Slot = SpellSlot.E, Range = 1100, Radius = 55,
                        MissileSpeed = 1600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Elise

            #region Evelynn

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Evelynn", MenuName = "EvelynnR", SpellName = "EvelynnR", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 650, Radius = 350, Type = SpellType.Circle, IsDangerous = true
                    });

            #endregion Evelynn

            #region Ezreal

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ezreal", MenuName = "EzrealQ", SpellName = "EzrealMysticShot",
                        MissileName = "EzrealMysticShotMissile", DangerValue = 2, Range = 1200, Radius = 60,
                        MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ezreal", MenuName = "EzrealW", SpellName = "EzrealEssenceFlux",
                        MissileName = "EzrealEssenceFluxMissile", DangerValue = 2, Slot = SpellSlot.W, Range = 1050,
                        Radius = 80, MissileSpeed = 1600, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ezreal", MenuName = "EzrealR", SpellName = "EzrealTrueshotBarrage",
                        MissileName = "EzrealTrueshotBarrage", DangerValue = 3, Slot = SpellSlot.R, Range = 25000,
                        Delay = 1000, Radius = 160, MissileSpeed = 2000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            #endregion Ezreal

            #region Fiora

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Fiora", MenuName = "FioraW", SpellName = "FioraW", MissileName = "FioraWMissile",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 770, Delay = 500, Radius = 70, MissileSpeed = 3200,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, BehindStart = 50
                    });

            #endregion Fiora

            #region Fizz

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Fizz", MenuName = "FizzQ", SpellName = "FizzQ", DangerValue = 2, Range = 550,
                        Delay = 0, Radius = 60, MissileSpeed = 1400, FixedRange = true, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Fizz", MenuName = "FizzR", SpellName = "FizzR", MissileName = "FizzRMissile",
                        ToggleName = "Fizz_.+_R_OrbitFish", DangerValue = 5, Slot = SpellSlot.R, Range = 20000,
                        Radius = 80, MissileSpeed = 1300,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        IsDangerous = true, ExtraDuration = 2300, HasEndExplosion = true, RadiusEx = 240
                    });

            #endregion Fizz

            #region Galio

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Galio", MenuName = "GalioQ", SpellName = "GalioResoluteSmite",
                        MissileName = "GalioResoluteSmite", DangerValue = 2, Range = 930, Radius = 200,
                        MissileSpeed = 1300, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Galio", MenuName = "GalioE", SpellName = "GalioRighteousGust",
                        MissileName = "GalioRighteousGustMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1200,
                        Radius = 160, MissileSpeed = 1300, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Galio", MenuName = "GalioR", SpellName = "GalioIdolOfDurand", DangerValue = 5,
                        Slot = SpellSlot.R, Radius = 550, Type = SpellType.Circle, IsDangerous = true, AddHitbox = false,
                        DontCross = true, ExtraDuration = 2000
                    });

            #endregion Galio

            #region Gnar

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarQ", SpellName = "GnarQMissile", MissileName = "GnarQMissile",
                        DangerValue = 2, Range = 1125, Radius = 55, MissileSpeed = 2500,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true,
                        MissileAccel = -3000, MissileMinSpeed = 1400, MissileMaxSpeed = 2500
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarQReturn", SpellName = "GnarQMissileReturn",
                        MissileName = "GnarQMissileReturn", DangerValue = 2, Range = 3000, Delay = 0, Radius = 75,
                        MissileSpeed = 60, CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true,
                        MissileAccel = 800, MissileMinSpeed = 60, MissileMaxSpeed = 2600, Invert = true,
                        DisabledByDefault = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarQBig", SpellName = "GnarBigQMissile",
                        MissileName = "GnarBigQMissile", DangerValue = 2, Range = 1150, Delay = 500, Radius = 90,
                        MissileSpeed = 2100,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, HasEndExplosion = true,
                        RadiusEx = 210
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarWBig", SpellName = "GnarBigW", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 600, Delay = 600, Radius = 110, Type = SpellType.Line,
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarE", SpellName = "GnarE", DangerValue = 2, Slot = SpellSlot.E,
                        Range = 475, Delay = 0, Radius = 150, MissileSpeed = 900, Type = SpellType.Circle, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarEBig", SpellName = "GnarBigE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 475, Delay = 0, Radius = 350, MissileSpeed = 800,
                        Type = SpellType.Circle, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gnar", MenuName = "GnarR", SpellName = "GnarR", DangerValue = 5, Slot = SpellSlot.R,
                        Delay = 275, Radius = 500, Type = SpellType.Circle, IsDangerous = true, AddHitbox = false
                    });

            #endregion Gnar

            #region Gragas

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gragas", MenuName = "GragasQ", SpellName = "GragasQ", MissileName = "GragasQMissile",
                        ToggleName = "Gragas_.+_Q_(Enemy|Ally)", DangerValue = 2, Range = 850, Radius = 280,
                        MissileSpeed = 1000, Type = SpellType.Circle, ExtraDuration = 4300, DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gragas", MenuName = "GragasE", SpellName = "GragasE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 600, Delay = 0, Radius = 200, MissileSpeed = 900,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.Minions },
                        FixedRange = true, IsDash = true, ExtraRange = 300
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Gragas", MenuName = "GragasR", SpellName = "GragasR", MissileName = "GragasRBoom",
                        DangerValue = 5, Slot = SpellSlot.R, Range = 1050, Radius = 350, MissileSpeed = 1800,
                        Type = SpellType.Circle, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        IsDangerous = true
                    });

            #endregion Gragas

            #region Graves

            Spells.Add(
                //Todo: Split
                new SpellData
                    {
                        ChampName = "Graves", MenuName = "GravesQ", SpellName = "GravesQLineSpell",
                        MissileName = "GravesQLineMis", DangerValue = 2, Range = 808, Radius = 40, MissileSpeed = 3000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            Spells.Add(
                //Todo: Split
                new SpellData
                    {
                        ChampName = "Graves", MenuName = "GravesQReturn", SpellName = "GravesQReturn",
                        MissileName = "GravesQReturn", DangerValue = 2, Range = 808, Delay = 0, Radius = 100,
                        MissileSpeed = 1600, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Graves", MenuName = "GravesW", SpellName = "GravesSmokeGrenade",
                        MissileName = "GravesSmokeGrenadeBoom", ToggleName = "Graves_SmokeGrenade_.+_Team_(Green|Red)",
                        Slot = SpellSlot.W, Range = 950, Radius = 225, MissileSpeed = 1500, Type = SpellType.Circle,
                        ExtraDuration = 4250
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Graves", MenuName = "GravesR", SpellName = "GravesChargeShot",
                        MissileName = "GravesChargeShotShot", DangerValue = 5, Slot = SpellSlot.R, Range = 1000,
                        Radius = 100, MissileSpeed = 2100, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Graves", MenuName = "GravesRExplosion", SpellName = "GravesRExplosion",
                        MissileName = "GravesChargeShotFxMissile2", DangerValue = 5, Slot = SpellSlot.R, Range = 800,
                        Delay = 10, Radius = 45, Type = SpellType.Cone, IsDangerous = true, MissileDelayed = true
                    });

            #endregion Graves

            #region Hecarim

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Hecarim", MenuName = "HecarimRFear", SpellName = "HecarimUlt", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 1000, Delay = 0, Radius = 240, MissileSpeed = 1100,
                        Type = SpellType.Circle, IsDangerous = true, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Hecarim", MenuName = "HecarimR", SpellName = "HecarimRMissile",
                        MissileName = "HecarimUltMissile", DangerValue = 3, Slot = SpellSlot.R, Range = 1500, Delay = 0,
                        Radius = 40, MissileSpeed = 1100, FixedRange = true, MissileOnly = true
                    });

            #endregion Hecarim

            #region Heimerdinger

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerQTurretBlast",
                        SpellName = "HeimerdingerQTurretBlast", MissileName = "HeimerdingerTurretEnergyBlast",
                        DangerValue = 2, Range = 1000, Delay = 0, Radius = 50, MissileSpeed = 1650, FixedRange = true,
                        MissileAccel = -1000, MissileMinSpeed = 1200, MissileMaxSpeed = 1650, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerQTurretBigBlast",
                        SpellName = "HeimerdingerQTurretBigBlast", MissileName = "HeimerdingerTurretBigEnergyBlast",
                        DangerValue = 2, Range = 1000, Delay = 0, Radius = 75, MissileSpeed = 1650,
                        CollisionObjects = new[] { CollisionableObjects.Minions }, FixedRange = true, MissileAccel = -1000,
                        MissileMinSpeed = 1200, MissileMaxSpeed = 1650, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerW", SpellName = "HeimerdingerW",
                        MissileName = "HeimerdingerWAttack2", ExtraMissileNames = new[] { "HeimerdingerWAttack2Ult" },
                        DangerValue = 2, Slot = SpellSlot.W, Range = 1350, Delay = 0, Radius = 40, MissileSpeed = 750,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, MissileAccel = 4000,
                        MissileMinSpeed = 750, MissileMaxSpeed = 3000, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerE", SpellName = "HeimerdingerE",
                        MissileName = "HeimerdingerESpell", DangerValue = 2, Slot = SpellSlot.E, Range = 950, Radius = 100,
                        MissileSpeed = 1200, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerEUlt", SpellName = "HeimerdingerEUlt",
                        MissileName = "HeimerdingerESpell_ult", DangerValue = 2, Slot = SpellSlot.E, Range = 1000,
                        Radius = 150, MissileSpeed = 1200, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Heimerdinger", MenuName = "HeimerdingerEUltBounce",
                        SpellName = "HeimerdingerEUltBounce", MissileName = "HeimerdingerESpell_ult2",
                        ExtraMissileNames = new[] { "HeimerdingerESpell_ult3" }, DangerValue = 2, Slot = SpellSlot.E,
                        Range = 1000, Delay = 175, Radius = 150, MissileSpeed = 1400, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, DisabledByDefault = true,
                        MissileDelayed = true
                    });

            #endregion Heimerdinger

            #region Illaoi

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Illaoi", MenuName = "IllaoiQ", SpellName = "IllaoiQ", DangerValue = 2, Range = 850,
                        Delay = 750, Radius = 100, Type = SpellType.Line,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        BehindStart = 50
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Illaoi", MenuName = "Illaoi", SpellName = "IllaoiE", MissileName = "IllaoiEMis",
                        DangerValue = 3, Slot = SpellSlot.E, Range = 950, Delay = 265, Radius = 50, MissileSpeed = 1900,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Illaoi", MenuName = "Illaoi", SpellName = "IllaoiR", DangerValue = 3,
                        Slot = SpellSlot.R, Delay = 500, Radius = 450, Type = SpellType.Circle, IsDangerous = true,
                        AddHitbox = false
                    });

            #endregion Illaoi

            #region Irelia

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Irelia", MenuName = "Irelia", SpellName = "IreliaTranscendentBlades",
                        MissileName = "IreliaTranscendentBladesSpell", DangerValue = 2, Slot = SpellSlot.R, Range = 1200,
                        Delay = 0, Radius = 120, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, MissileOnly = true
                    });

            #endregion Irelia

            #region Ivern

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ivern", MenuName = "IvernQ", SpellName = "IvernQ", MissileName = "IvernQ",
                        DangerValue = 3, Range = 1100, Radius = 65, MissileSpeed = 1300,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Ivern

            #region Janna

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Janna", MenuName = "JannaQ", SpellName = "HowlingGale",
                        MissileName = "HowlingGaleSpell", DangerValue = 2, Range = 20000, Delay = 0, Radius = 120,
                        MissileSpeed = 900, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileOnly = true
                    });

            #endregion Janna

            #region JarvanIV

            Spells.Add(
                new SpellData
                    {
                        ChampName = "JarvanIV", MenuName = "JarvanIVQ", SpellName = "JarvanIVDragonStrike",
                        DangerValue = 3, Range = 770, Delay = 425, Radius = 70, Type = SpellType.Line, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "JarvanIV", MenuName = "JarvanIVQE", SpellName = "JarvanIVQE", DangerValue = 3,
                        Range = 910, Delay = 450, Radius = 120, MissileSpeed = 2600, IsDangerous = true, RadiusEx = 210,
                        IsDash = true, ExtraRange = 200
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "JarvanIV", MenuName = "JarvanIVE", SpellName = "JarvanIVDemacianStandard",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 850, Delay = 450, Radius = 175,
                        Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "JarvanIV", MenuName = "JarvanIVR", SpellName = "JarvanIVCataclysm", DangerValue = 3,
                        Slot = SpellSlot.R, Range = 650, Delay = 0, Radius = 340, MissileSpeed = 1850,
                        Type = SpellType.Circle
                    });

            #endregion JarvanIV

            #region Jayce

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jayce", MenuName = "JayceQ", SpellName = "JayceShockBlast",
                        MissileName = "JayceShockBlastMis", DangerValue = 2, Delay = 230, Range = 1050, Radius = 70,
                        MissileSpeed = 1450,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, HasEndExplosion = true,
                        RadiusEx = 275
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jayce", MenuName = "JayceQAccel", SpellName = "JayceQAccel",
                        MissileName = "JayceShockBlastWallMis", DangerValue = 2, Range = 2000, Delay = 0, Radius = 70,
                        MissileSpeed = 2350,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        HasEndExplosion = true, RadiusEx = 275
                    });

            #endregion Jayce

            #region Jhin

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jhin", MenuName = "JhinW", SpellName = "JhinW", DangerValue = 3, Slot = SpellSlot.W,
                        Range = 2550, Delay = 750, Radius = 40, Type = SpellType.Line,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jhin", MenuName = "JhinE", SpellName = "JhinTrap", TrapName = "jhintrap",
                        Slot = SpellSlot.E, Delay = 0, Radius = 135, Type = SpellType.Circle, ExtraDuration = 120000
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jhin", MenuName = "JhinR", SpellName = "JhinRShot", MissileName = "JhinRShotMis",
                        ExtraMissileNames = new[] { "JhinRShotMis4" }, DangerValue = 3, Slot = SpellSlot.R, Range = 3500,
                        Delay = 190, Radius = 80, MissileSpeed = 5000,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion

            #region Jinx

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jinx", MenuName = "JinxW", SpellName = "JinxWMissile", MissileName = "JinxWMissile",
                        DangerValue = 3, Slot = SpellSlot.W, Range = 1500, Delay = 600, Radius = 60, MissileSpeed = 3300,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jinx", MenuName = "JinxE", SpellName = "JinxTrap", TrapName = "jinxmine",
                        Slot = SpellSlot.E, Delay = 0, Radius = 65, Type = SpellType.Circle, IsDangerous = true,
                        ExtraDuration = 5000, DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Jinx", MenuName = "JinxR", SpellName = "JinxR", MissileName = "JinxR",
                        DangerValue = 5, Slot = SpellSlot.R, Range = 25000, Delay = 600, Radius = 140, MissileSpeed = 1700,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Jinx

            #region Kalista

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kalista", MenuName = "KalistaQ", SpellName = "KalistaMysticShot",
                        MissileName = "KalistaMysticShotMisTrue", DangerValue = 2, Range = 1200, Radius = 40,
                        MissileSpeed = 2400,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Kalista

            #region Karma

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Karma", MenuName = "KarmaQ", SpellName = "KarmaQ", MissileName = "KarmaQMissile",
                        DangerValue = 2, Range = 1050, Radius = 60, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Karma", MenuName = "KarmaQMantra", SpellName = "KarmaQMantra",
                        MissileName = "KarmaQMissileMantra", ToggleName = "Karma_.+_Q_impact_R_01", DangerValue = 2,
                        Range = 950, Delay = 0, Radius = 80, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, ExtraDuration = 1500,
                        HasEndExplosion = true, RadiusEx = 330
                    });

            #endregion Karma

            #region Karthus

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Karthus", MenuName = "KarthusQ", SpellName = "KarthusLayWasteA1",
                        ExtraSpellNames =
                            new[]
                                {
                                    "KarthusLayWasteA2", "KarthusLayWasteA3", "KarthusLayWasteDeadA1",
                                    "KarthusLayWasteDeadA2", "KarthusLayWasteDeadA3"
                                },
                        DangerValue = 2, Range = 875,
                        Delay = 950, Radius = 160, Type = SpellType.Circle, DontCheckForDuplicates = true
                    });

            #endregion Karthus

            #region Kassadin

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kassadin", MenuName = "KassadinE", SpellName = "ForcePulse", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 600, Radius = 80, Type = SpellType.Cone
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kassadin", MenuName = "KassadinR", SpellName = "RiftWalk", DangerValue = 2,
                        Slot = SpellSlot.R, Range = 500, Radius = 270, Type = SpellType.Circle
                    });

            #endregion Kassadin

            #region Kennen

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kennen", MenuName = "KennenQ", SpellName = "KennenShurikenHurlMissile1",
                        MissileName = "KennenShurikenHurlMissile1", DangerValue = 2, Range = 1050, Delay = 175,
                        Radius = 50, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Kennen

            #region Khazix

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Khazix", MenuName = "KhazixW", SpellName = "KhazixW",
                        ExtraSpellNames = new[] { "KhazixWLong" }, MissileName = "KhazixWMissile", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 1025, Radius = 70, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, MultipleNumber = 3,
                        MultipleAngle = 22 * (float)Math.PI / 180
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Khazix", MenuName = "KhazixE", SpellName = "KhazixE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 700, Delay = 0, Radius = 300, MissileSpeed = 1250,
                        Type = SpellType.Circle, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Khazix", MenuName = "KhazixEEvol", SpellName = "KhazixELong", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 900, Delay = 0, Radius = 300, MissileSpeed = 1200,
                        Type = SpellType.Circle, IsDash = true
                    });

            #endregion Khazix

            #region Kled

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kled", MenuName = "KledQ", SpellName = "KledQ", MissileName = "KledQMissile",
                        DangerValue = 2, Range = 800, Radius = 45, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kled", MenuName = "KledQDismount", SpellName = "KledRiderQ",
                        MissileName = "KledRiderQMissile", DangerValue = 2, Range = 700, Radius = 40, MissileSpeed = 3000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, MultipleNumber = 5,
                        MultipleAngle = 5 * (float)Math.PI / 180
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Kled", MenuName = "KledE", SpellName = "KledEDash", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 550, Delay = 0, Radius = 100, MissileSpeed = 970, FixedRange = true,
                        IsDangerous = true, IsDash = true
                    });

            #endregion Kled

            #region KogMaw

            Spells.Add(
                new SpellData
                    {
                        ChampName = "KogMaw", MenuName = "KogMawQ", SpellName = "KogMawQ", MissileName = "KogMawQ",
                        DangerValue = 2, Range = 1200, Radius = 70, MissileSpeed = 1650,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "KogMaw", MenuName = "KogMawE", SpellName = "KogMawVoidOozeMissile",
                        MissileName = "KogMawVoidOozeMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1300,
                        Radius = 120, MissileSpeed = 1400, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "KogMaw", MenuName = "KogMawR", SpellName = "KogMawLivingArtillery", DangerValue = 2,
                        Slot = SpellSlot.R, Range = 1200, Delay = 1150, Radius = 240, Type = SpellType.Circle,
                        DontCheckForDuplicates = true
                    });

            #endregion KogMaw

            #region Leblanc

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Leblanc", MenuName = "LeblancW", SpellName = "LeblancW", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 600, Delay = 0, Radius = 220, MissileSpeed = 1600,
                        Type = SpellType.Circle, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Leblanc", MenuName = "LeblancE", SpellName = "LeblancE",
                        MissileName = "LeblancEMissile", ExtraMissileNames = new[] { "LeblancRE" }, DangerValue = 3,
                        Slot = SpellSlot.E, Range = 950, Radius = 55, MissileSpeed = 1750,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Leblanc

            #region LeeSin

            Spells.Add(
                new SpellData
                    {
                        ChampName = "LeeSin", MenuName = "LeeSinQ", SpellName = "BlindMonkQOne",
                        MissileName = "BlindMonkQOne", DangerValue = 3, Range = 1100, Radius = 60, MissileSpeed = 1800,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "LeeSin", MenuName = "LeeSinE", SpellName = "BlindMonkEOne", DangerValue = 2,
                        Slot = SpellSlot.E, Radius = 430, Type = SpellType.Circle, AddHitbox = false
                    });

            #endregion LeeSin

            #region Leona

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Leona", MenuName = "LeonaE", SpellName = "LeonaZenithBlade",
                        MissileName = "LeonaZenithBladeMissile", DangerValue = 3, Slot = SpellSlot.E, Range = 900,
                        Radius = 70, MissileSpeed = 2000, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true, TakeClosestPath = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Leona", MenuName = "LeonaR", SpellName = "LeonaSolarFlare", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 1200, Delay = 930, Radius = 300, Type = SpellType.Circle,
                        IsDangerous = true
                    });

            #endregion Leona

            #region Lissandra

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lissandra", MenuName = "LissandraQ", SpellName = "LissandraQMissile",
                        MissileName = "LissandraQMissile", DangerValue = 2, Range = 700, Radius = 75, MissileSpeed = 2200,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lissandra", MenuName = "LissandraQExplosion", SpellName = "LissandraQShards",
                        MissileName = "LissandraQShards", DangerValue = 2, Range = 1650, Delay = 0, Radius = 90,
                        MissileSpeed = 2200, CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lissandra", MenuName = "LissandraW", SpellName = "LissandraW", DangerValue = 3,
                        Slot = SpellSlot.W, Delay = 10, Radius = 450, Type = SpellType.Circle, AddHitbox = false
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lissandra", MenuName = "LissandraE", SpellName = "LissandraEMissile",
                        MissileName = "LissandraEMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1025, Radius = 125,
                        MissileSpeed = 850, CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            #endregion Lissandra

            #region Lucian

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lucian", MenuName = "LucianQ", SpellName = "LucianQ", DangerValue = 2, Range = 900,
                        Delay = 350, Radius = 65, Type = SpellType.Line, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lucian", MenuName = "LucianW", SpellName = "LucianW", MissileName = "LucianWMissile",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 900, Delay = 325, Radius = 55, MissileSpeed = 1600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lucian", MenuName = "LucianR", SpellName = "LucianR",
                        MissileName = "LucianRMissileOffhand", ExtraMissileNames = new[] { "LucianRMissile" },
                        DangerValue = 2, Slot = SpellSlot.R, Range = 1200, Delay = 0, Radius = 110, MissileSpeed = 2800,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, MissileOnly = true,
                        DisabledByDefault = true
                    });

            #endregion Lucian

            #region Lulu

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lulu", MenuName = "LuluQ", SpellName = "LuluQ", MissileName = "LuluQMissile",
                        ExtraMissileNames = new[] { "LuluQMissileTwo" }, DangerValue = 2, Range = 950, Radius = 60,
                        MissileSpeed = 1450, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            #endregion Lulu

            #region Lux

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lux", MenuName = "LuxQ", SpellName = "LuxLightBinding",
                        MissileName = "LuxLightBindingMis", DangerValue = 3, Range = 1300, Radius = 70,
                        MissileSpeed = 1200, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lux", MenuName = "LuxE", SpellName = "LuxLightStrikeKugel",
                        MissileName = "LuxLightStrikeKugel", ToggleName = "Lux_.+_E_tar_aoe_(green|red)", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 1100, Radius = 330, MissileSpeed = 1300, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, ExtraDuration = 5100,
                        DontCross = true, DisabledByDefault = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Lux", MenuName = "LuxR", SpellName = "LuxMaliceCannon", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 3300, Delay = 1000, Radius = 150, Type = SpellType.Line,
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Lux

            #region Malphite

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Malphite", MenuName = "MalphiteE", SpellName = "Landslide", DangerValue = 2,
                        Slot = SpellSlot.E, Radius = 400, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Malphite", MenuName = "MalphiteR", SpellName = "UFSlash", DangerValue = 5,
                        Slot = SpellSlot.R, Range = 1000, Delay = 0, Radius = 270, MissileSpeed = 1600,
                        Type = SpellType.Circle, IsDangerous = true
                    });

            #endregion Malphite

            #region Malzahar

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Malzahar", MenuName = "MalzaharQ", SpellName = "MalzaharQ",
                        MissileName = "MalzaharQMissile", DangerValue = 2, Range = 750, Delay = 520, Radius = 85,
                        MissileSpeed = 1600, FixedRange = true
                    });

            #endregion Malzahar

            #region Maokai

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Maokai", MenuName = "MaokaiQ", SpellName = "MaokaiTrunkLine",
                        MissileName = "MaokaiTrunkLineMissile", DangerValue = 3, Range = 650, Delay = 400, Radius = 110,
                        MissileSpeed = 1800, FixedRange = true, HasStartExplosion = true, RadiusEx = 360
                    });

            #endregion Maokai

            #region Mordekaiser

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Mordekaiser", MenuName = "MordekaiserE", SpellName = "MordekaiserSyphonOfDestruction",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 640, Radius = 50, Type = SpellType.Cone
                    });

            #endregion Mordekaiser

            #region Morgana

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Morgana", MenuName = "MorganaQ", SpellName = "DarkBindingMissile",
                        MissileName = "DarkBindingMissile", DangerValue = 3, Range = 1300, Radius = 70,
                        MissileSpeed = 1200,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Morgana

            #region Nami

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Nami", MenuName = "NamiQ", SpellName = "NamiQ", MissileName = "NamiQMissile",
                        DangerValue = 3, Range = 850, Radius = 200, MissileSpeed = 2500, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, IsDangerous = true, ExtraDelay = 700
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Nami", MenuName = "NamiR", SpellName = "NamiRMissile", MissileName = "NamiRMissile",
                        DangerValue = 2, Slot = SpellSlot.R, Range = 2750, Delay = 500, Radius = 250, MissileSpeed = 850,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            #endregion Nami

            #region Nautilus

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Nautilus", MenuName = "NautilusQ", SpellName = "NautilusAnchorDragMissile",
                        MissileName = "NautilusAnchorDragMissile", DangerValue = 3, Range = 1130, Radius = 90,
                        MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Nautilus

            #region Nidalee

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Nidalee", MenuName = "NidaleeQ", SpellName = "JavelinToss",
                        MissileName = "JavelinToss", DangerValue = 3, Range = 1500, Radius = 40, MissileSpeed = 1300,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Nidalee

            #region Nocturne

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Nocturne", MenuName = "NocturneQ", SpellName = "NocturneDuskbringer",
                        MissileName = "NocturneDuskbringer", DangerValue = 2, Range = 1200, Radius = 60,
                        MissileSpeed = 1400, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            #endregion Nocturne

            #region Olaf

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Olaf", MenuName = "OlafQ", SpellName = "OlafAxeThrowCast",
                        MissileName = "OlafAxeThrow", DangerValue = 2, Range = 1000, Radius = 90, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, ExtraRange = 150
                    });

            #endregion Olaf

            #region Orianna

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Orianna", MenuName = "OriannaQ", SpellName = "OrianaIzunaCommand",
                        MissileName = "OrianaIzuna", DangerValue = 2, Range = 2000, Delay = 0, Radius = 80,
                        MissileSpeed = 1200, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        HasEndExplosion = true, RadiusEx = 230
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Orianna", MenuName = "OriannaW", SpellName = "OrianaDissonanceCommand",
                        DangerValue = 2, Slot = SpellSlot.W, Delay = 10, Radius = 255, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Orianna", MenuName = "OriannaR", SpellName = "OrianaDetonateCommand", DangerValue = 5,
                        Slot = SpellSlot.R, Delay = 500, Radius = 410, Type = SpellType.Circle, IsDangerous = true
                    });

            #endregion Orianna

            #region Pantheon

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Pantheon", MenuName = "PantheonE", SpellName = "PantheonE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 640, Delay = 400, Radius = 70, Type = SpellType.Cone,
                        ExtraDuration = 750
                    });

            #endregion Pantheon

            #region Poppy

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Poppy", MenuName = "PoppyQ", SpellName = "PoppyQ", DangerValue = 2, Range = 430,
                        Delay = 500, Radius = 100, Type = SpellType.Line, FixedRange = true, ExtraDuration = 900
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Poppy", MenuName = "PoppyRInstant", SpellName = "PoppyRSpellInstant", DangerValue = 4,
                        Slot = SpellSlot.R, Range = 450, Delay = 350, Radius = 100, Type = SpellType.Line,
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Poppy", MenuName = "PoppyRCharge", SpellName = "PoppyRSpell",
                        MissileName = "PoppyRMissile", DangerValue = 3, Slot = SpellSlot.R, Range = 1200, Delay = 350,
                        Radius = 100, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        IsDangerous = true
                    });

            #endregion

            #region Quinn

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Quinn", MenuName = "QuinnQ", SpellName = "QuinnQ", MissileName = "QuinnQ",
                        DangerValue = 2, Range = 1050, Radius = 60, MissileSpeed = 1550,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Quinn

            #region RekSai

            Spells.Add(
                new SpellData
                    {
                        ChampName = "RekSai", MenuName = "RekSaiQ", SpellName = "RekSaiQBurrowed",
                        MissileName = "RekSaiQBurrowedMis", DangerValue = 3, Range = 1500, Delay = 125, Radius = 65,
                        MissileSpeed = 1950,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "RekSai", MenuName = "RekSaiW", SpellName = "RekSaiW2", DangerValue = 3,
                        Slot = SpellSlot.W, Delay = 10, Radius = 200, Type = SpellType.Circle, IsDangerous = true
                    });

            #endregion RekSai

            #region Renekton

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Renekton", MenuName = "RenektonE", SpellName = "RenektonSliceAndDice",
                        ExtraSpellNames = new[] { "RenektonDice" }, DangerValue = 2, Slot = SpellSlot.E, Range = 450,
                        Delay = 0, Radius = 100, MissileSpeed = 1100, FixedRange = true, IsDash = true
                    });

            #endregion Renekton

            #region Rengar

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Rengar", MenuName = "RengarE", SpellName = "RengarE", MissileName = "RengarEMis",
                        DangerValue = 3, Slot = SpellSlot.E, Range = 1000, Radius = 70, MissileSpeed = 1500,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Rengar", MenuName = "RengarEEmp", SpellName = "RengarEEmp",
                        MissileName = "RengarEEmpMis", DangerValue = 3, Slot = SpellSlot.E, Range = 1000, Radius = 70,
                        MissileSpeed = 1500,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Rengar

            #region Riven

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Riven", MenuName = "RivenQ3", SpellName = "RivenQ3", DangerValue = 3, Delay = 350,
                        Radius = 260, Type = SpellType.Circle, IsDangerous = true, AddHitbox = false, IsDash = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Riven", MenuName = "RivenW", SpellName = "RivenMartyr", DangerValue = 3,
                        Slot = SpellSlot.W, Delay = 10, Radius = 250, Type = SpellType.Circle, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Riven", MenuName = "RivenR", SpellName = "RivenIzunaBlade",
                        MissileName = "RivenWindslashMissileCenter",
                        ExtraMissileNames = new[] { "RivenWindslashMissileLeft", "RivenWindslashMissileRight" },
                        DangerValue = 5, Slot = SpellSlot.R, Range = 1075, Radius = 100, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        AddHitbox = false, MultipleNumber = 3, MultipleAngle = 9 * (float)Math.PI / 180
                    });

            #endregion Riven

            #region Rumble

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Rumble", MenuName = "RumbleE", SpellName = "RumbleGrenade",
                        MissileName = "RumbleGrenadeMissile",
                        ExtraMissileNames = new[] { "RumbleGrenadeMissileDangerZone" }, DangerValue = 2,
                        Slot = SpellSlot.E, Range = 950, Radius = 60, MissileSpeed = 2000,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, DontCheckForDuplicates = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Rumble", MenuName = "RumbleR", SpellName = "RumbleCarpetBomb",
                        MissileName = "RumbleCarpetBombMissile", DangerValue = 4, Slot = SpellSlot.R, Range = 20000,
                        Delay = 400, Radius = 150, MissileSpeed = 1600, MissileOnly = true, MissileDelayed = true,
                        CanBeRemoved = false, AddHitbox = false
                    });

            #endregion Rumble

            #region Ryze

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ryze", MenuName = "RyzeQ", SpellName = "RyzeQ", MissileName = "RyzeQ",
                        DangerValue = 2, Range = 1000, Radius = 55, MissileSpeed = 1700,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            #endregion Ryze

            #region Sejuani

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sejuani", MenuName = "SejuaniQ", SpellName = "SejuaniArcticAssault", DangerValue = 3,
                        Range = 620, Delay = 0, Radius = 75, MissileSpeed = 1000,
                        CollisionObjects = new[] { CollisionableObjects.Heroes }, IsDangerous = true, IsDash = true,
                        ExtraRange = 200
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sejuani", MenuName = "SejuaniR", SpellName = "SejuaniGlacialPrisonCast",
                        MissileName = "SejuaniGlacialPrison", DangerValue = 3, Slot = SpellSlot.R, Range = 1100,
                        Radius = 110, MissileSpeed = 1600,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true, HasEndExplosion = true, RadiusEx = 500
                    });

            #endregion Sejuani

            #region Shen

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Shen", MenuName = "ShenE", SpellName = "ShenE", DangerValue = 3, Slot = SpellSlot.E,
                        Range = 600, Delay = 0, Radius = 50, MissileSpeed = 1300, IsDangerous = true, IsDash = true,
                        ExtraRange = 150
                    });

            #endregion Shen

            #region Shyvana

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Shyvana", MenuName = "ShyvanaE", SpellName = "ShyvanaFireball",
                        MissileName = "ShyvanaFireballMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 950,
                        Radius = 60, MissileSpeed = 1575, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Shyvana", MenuName = "ShyvanaEDragon", SpellName = "ShyvanaFireballDragon2",
                        MissileName = "ShyvanaFireballDragonMissile",
                        ExtraMissileNames = new[] { "ShyvanaFireballDragonMissileBig", "ShyvanaFireballDragonMissileMax" },
                        DangerValue = 3, Slot = SpellSlot.E, Range = 780, Delay = 350, Radius = 60, MissileSpeed = 1575,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        InfrontStart = 200, HasEndExplosion = true, RadiusEx = 350
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Shyvana", MenuName = "ShyvanaR", SpellName = "ShyvanaTransformCast", DangerValue = 3,
                        Slot = SpellSlot.R, Range = 950, Delay = 300, Radius = 150, MissileSpeed = 1100,
                        IsDangerous = true, IsDash = true, ExtraRange = 200
                    });

            #endregion Shyvana

            #region Sion

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sion", MenuName = "SionE", SpellName = "SionE", MissileName = "SionEMissile",
                        DangerValue = 3, Slot = SpellSlot.E, Range = 800, Radius = 80, MissileSpeed = 1800,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sion", MenuName = "SionEMinion", SpellName = "SionEMinion", DangerValue = 3,
                        Slot = SpellSlot.E, Range = 1400, Delay = 0, Radius = 60, MissileSpeed = 2100, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sion", MenuName = "SionR", SpellName = "SionR", DangerValue = 3, Slot = SpellSlot.R,
                        Range = 500, Delay = 0, Radius = 100, MissileSpeed = 500,
                        CollisionObjects = new[] { CollisionableObjects.Heroes }, FixedRange = true, IsDangerous = true
                    });

            #endregion Sion

            #region Sivir

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sivir", MenuName = "SivirQ", SpellName = "SivirQ", MissileName = "SivirQMissile",
                        DangerValue = 2, Range = 1250, Radius = 90, MissileSpeed = 1350,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sivir", MenuName = "SivirQReturn", SpellName = "SivirQReturn",
                        MissileName = "SivirQMissileReturn", DangerValue = 2, Range = 20000, Delay = 0, Radius = 100,
                        MissileSpeed = 1350, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileToUnit = true
                    });

            #endregion Sivir

            #region Skarner

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Skarner", MenuName = "SkarnerE", SpellName = "SkarnerFracture",
                        MissileName = "SkarnerFractureMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1000,
                        Radius = 70, MissileSpeed = 1500, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            #endregion Skarner

            #region Sona

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Sona", MenuName = "SonaR", SpellName = "SonaR", MissileName = "SonaR",
                        DangerValue = 5, Slot = SpellSlot.R, Range = 1000, Radius = 140, MissileSpeed = 2400,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            #endregion Sona

            #region Soraka

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Soraka", MenuName = "SorakaQ", SpellName = "SorakaQ", MissileName = "SorakaQMissile",
                        DangerValue = 2, Range = 800, Radius = 230, MissileSpeed = 1100, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Soraka", MenuName = "SorakaE", SpellName = "SorakaE", DangerValue = 3,
                        Slot = SpellSlot.E, Range = 920, Delay = 1770, Radius = 250, Type = SpellType.Circle
                    });

            #endregion Soraka

            #region Swain

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Swain", MenuName = "SwainW", SpellName = "SwainShadowGrasp", DangerValue = 3,
                        Slot = SpellSlot.W, Range = 900, Delay = 1100, Radius = 240, Type = SpellType.Circle,
                        IsDangerous = true
                    });

            #endregion Swain

            #region Syndra

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Syndra", MenuName = "SyndraQ", SpellName = "SyndraQ", MissileName = "SyndraQSpell",
                        DangerValue = 2, Range = 825, Delay = 650, Radius = 180, Type = SpellType.Circle,
                        MissileDelayed = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Syndra", MenuName = "SyndraW", SpellName = "SyndraWCast", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 950, Radius = 210, MissileSpeed = 1500, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Syndra", MenuName = "SyndraE", SpellName = "SyndraE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 650, Radius = 40, MissileSpeed = 2500, Type = SpellType.MissileCone,
                        DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Syndra", MenuName = "SyndraEMax", SpellName = "SyndraE5", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 650, Radius = 60, MissileSpeed = 2500, Type = SpellType.MissileCone,
                        DontCross = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Syndra", MenuName = "SyndraEQ", SpellName = "SyndraEQ", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 1300, Delay = 0, Radius = 55, MissileSpeed = 2000,
                        DontCheckForDuplicates = true
                    });

            #endregion Syndra

            #region TahmKench

            Spells.Add(
                new SpellData
                    {
                        ChampName = "TahmKench", MenuName = "TahmKenchQ", SpellName = "TahmKenchQ",
                        MissileName = "TahmKenchQMissile", DangerValue = 3, Range = 850, Radius = 70, MissileSpeed = 2800,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion TahmKench

            #region Taliyah

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Taliyah", MenuName = "TaliyahQ", SpellName = "TaliyahQ", MissileName = "TaliyahQMis",
                        DangerValue = 2, Range = 1000, Radius = 100, MissileSpeed = 3600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, MissileAccel = -5000,
                        MissileMinSpeed = 1500, MissileMaxSpeed = 3600, DisabledByDefault = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Taliyah", MenuName = "TaliyahW", SpellName = "TaliyahWVC", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 900, Delay = 800, Radius = 200, Type = SpellType.Circle,
                        IsDangerous = true
                    });

            #endregion Taliyah

            #region Talon

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Talon", MenuName = "TalonW", SpellName = "TalonW", MissileName = "TalonWMissileOne",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 850, Radius = 75, MissileSpeed = 2500,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        MultipleNumber = 3, MultipleAngle = 10.92f * (float)Math.PI / 180
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Talon", MenuName = "TalonWReturn", SpellName = "TalonWReturn",
                        MissileName = "TalonWMissileTwo", DangerValue = 2, Slot = SpellSlot.W, Range = 20000, Delay = 0,
                        Radius = 75, MissileSpeed = 3000, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileToUnit = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Talon", MenuName = "TalonR", SpellName = "TalonR", MissileName = "TalonRMisOne",
                        DangerValue = 2, Slot = SpellSlot.R, Range = 550, Delay = 0, Radius = 140, MissileSpeed = 2400,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Talon", MenuName = "TalonRReturn", SpellName = "TalonRReturn",
                        MissileName = "TalonRMisTwo", DangerValue = 2, Slot = SpellSlot.R, Range = 20000, Delay = 0,
                        Radius = 140, MissileSpeed = 4000, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        MissileToUnit = true, IsDangerous = true
                    });

            #endregion Talon

            #region Taric

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Taric", MenuName = "TaricE", SpellName = "TaricE", DangerValue = 3,
                        Slot = SpellSlot.E, Range = 650, Delay = 1000, Radius = 100, Type = SpellType.Line,
                        FixedRange = true, IsDangerous = true, MissileFromUnit = true
                    });

            #endregion

            #region Thresh

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Thresh", MenuName = "ThreshQ", SpellName = "ThreshQ", MissileName = "ThreshQMissile",
                        DangerValue = 3, Range = 1100, Delay = 500, Radius = 70, MissileSpeed = 1900,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Thresh", MenuName = "ThreshE", SpellName = "ThreshE", MissileName = "ThreshEMissile1",
                        DangerValue = 3, Slot = SpellSlot.E, Range = 1075, Delay = 0, Radius = 110, MissileSpeed = 2000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true,
                        MissileOnly = true
                    });

            #endregion Thresh

            #region Tristana

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Tristana", MenuName = "TristanaW", SpellName = "TristanaW", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 900, Delay = 300, Radius = 270, MissileSpeed = 1100,
                        Type = SpellType.Circle, IsDash = true
                    });

            #endregion Tristana

            #region Trundle

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Trundle", MenuName = "TrundleE", SpellName = "TrundleCircle", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 1000, Radius = 125, Type = SpellType.Circle
                    });

            #endregion Trundle

            #region Tryndamere

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Tryndamere", MenuName = "TryndamereE", SpellName = "TryndamereE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 650, Delay = 0, Radius = 160, MissileSpeed = 900, IsDash = true
                    });

            #endregion Tryndamere

            #region TwistedFate

            Spells.Add(
                new SpellData
                    {
                        ChampName = "TwistedFate", MenuName = "TwistedFateQ", SpellName = "WildCards",
                        MissileName = "SealFateMissile", DangerValue = 2, Range = 1450, Radius = 40, MissileSpeed = 1000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, MultipleNumber = 3,
                        MultipleAngle = 28 * (float)Math.PI / 180
                    });

            #endregion TwistedFate

            #region Twitch

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Twitch", MenuName = "TwitchW", SpellName = "TwitchVenomCask",
                        MissileName = "TwitchVenomCaskMissile", ToggleName = "twitch_w_indicator_(green|red)_team",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 950, Radius = 285, MissileSpeed = 1400,
                        Type = SpellType.Circle, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        ExtraDuration = 2850
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Twitch", MenuName = "TwitchR", SpellName = "TwitchSprayAndPrayAttack",
                        MissileName = "TwitchSprayAndPrayAttack", DangerValue = 2, Slot = SpellSlot.R, Range = 1100,
                        Delay = 0, Radius = 60, MissileSpeed = 4000,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, MissileOnly = true
                    });

            #endregion Twitch

            #region Urgot

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Urgot", MenuName = "UrgotQ", SpellName = "UrgotHeatseekingMissile",
                        MissileName = "UrgotHeatseekingLineMissile", DangerValue = 2, Range = 1000, Delay = 150,
                        Radius = 60, MissileSpeed = 1600,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Urgot", MenuName = "UrgotE", SpellName = "UrgotPlasmaGrenade",
                        MissileName = "UrgotPlasmaGrenadeBoom", DangerValue = 2, Slot = SpellSlot.E, Range = 900,
                        Radius = 250, MissileSpeed = 1500, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }
                    });

            #endregion Urgot

            #region Varus

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Varus", MenuName = "VarusQ", SpellName = "VarusQ", MissileName = "VarusQMissile",
                        DangerValue = 2, Range = 20000, Delay = 0, Radius = 70, MissileSpeed = 1900,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Varus", MenuName = "VarusE", SpellName = "VarusE", MissileName = "VarusEMissile",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 925, Radius = 260, MissileSpeed = 1500,
                        Type = SpellType.Circle, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        ExtraDelay = 550
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Varus", MenuName = "VarusR", SpellName = "VarusR", MissileName = "VarusRMissile",
                        DangerValue = 3, Slot = SpellSlot.R, Range = 1250, Radius = 120, MissileSpeed = 1950,
                        CollisionObjects = new[] { CollisionableObjects.Heroes, CollisionableObjects.YasuoWall },
                        FixedRange = true, IsDangerous = true
                    });

            #endregion Varus

            #region Veigar

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Veigar", MenuName = "VeigarQ", SpellName = "VeigarBalefulStrike",
                        MissileName = "VeigarBalefulStrikeMis", DangerValue = 2, Range = 950, Radius = 70,
                        MissileSpeed = 2200, CollisionObjects = new[] { CollisionableObjects.YasuoWall },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Veigar", MenuName = "VeigarW", SpellName = "VeigarDarkMatter", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 900, Delay = 1250, Radius = 225, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Veigar", MenuName = "VeigarE", SpellName = "VeigarEventHorizon", DangerValue = 3,
                        Slot = SpellSlot.E, Range = 700, Delay = 800, Radius = 390, Type = SpellType.Ring, RadiusEx = 300,
                        ExtraDuration = 3000, IsDangerous = true, DontAddExtraDuration = true, DontCross = true
                    });

            #endregion Veigar

            #region Velkoz

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Velkoz", MenuName = "VelkozQ", SpellName = "VelkozQ", MissileName = "VelkozQMissile",
                        DangerValue = 2, Range = 1100, Radius = 50, MissileSpeed = 1300,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Velkoz", MenuName = "VelkozQSplit", SpellName = "VelkozQSplit",
                        MissileName = "VelkozQMissileSplit", DangerValue = 2, Range = 1100, Delay = 0, Radius = 45,
                        MissileSpeed = 2100,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Velkoz", MenuName = "VelkozW", SpellName = "VelkozW", MissileName = "VelkozWMissile",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 1200, Radius = 88, MissileSpeed = 1700,
                        BehindStart = 100, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Velkoz", MenuName = "VelkozE", SpellName = "VelkozE", MissileName = "VelkozEMissile",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 800, Radius = 225, MissileSpeed = 1500,
                        Type = SpellType.Circle, AddHitbox = false, ExtraDelay = 550
                    });

            #endregion Velkoz

            #region Vi

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Vi", MenuName = "ViQ", SpellName = "ViQ", MissileName = "ViQMissile", DangerValue = 3,
                        Range = 750, Delay = 0, Radius = 90, MissileSpeed = 1500,
                        CollisionObjects = new[] { CollisionableObjects.Heroes }, MissileOnly = true, IsDangerous = true
                    });

            #endregion Vi

            #region Viktor

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Viktor", MenuName = "ViktorW", SpellName = "ViktorGravitonField", DangerValue = 3,
                        Slot = SpellSlot.W, Range = 700, Delay = 1500, Radius = 300, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Viktor", MenuName = "ViktorE", SpellName = "ViktorDeathRay",
                        MissileName = "ViktorDeathRayMissile", ExtraMissileNames = new[] { "ViktorEAugMissile" },
                        DangerValue = 2, Slot = SpellSlot.E, Range = 710, Delay = 0, Radius = 80, MissileSpeed = 1050,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, MissileOnly = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Viktor", MenuName = "ViktorEExplosion", SpellName = "ViktorEExplosion",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 710, Delay = 1000, Radius = 80, MissileSpeed = 1500,
                        FixedRange = true
                    });

            #endregion Viktor

            #region Vladimir

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Vladimir", MenuName = "VladimirR", SpellName = "VladimirHemoplague", DangerValue = 3,
                        Slot = SpellSlot.R, Range = 700, Delay = 10, Radius = 375, Type = SpellType.Circle
                    });

            #endregion Vladimir

            #region Wukong

            Spells.Add(
                new SpellData
                    {
                        ChampName = "MonkeyKing", MenuName = "MonkeyKingR", SpellName = "MonkeyKingSpinToWin",
                        DangerValue = 3, Slot = SpellSlot.R, Delay = 50, Radius = 320, Type = SpellType.Circle,
                        MissileToUnit = true, DisabledByDefault = true, DontCross = true
                    });

            #endregion Wukong

            #region Xerath

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Xerath", MenuName = "XerathQ", SpellName = "XerathArcanopulse2", DangerValue = 2,
                        Range = 20000, Delay = 530, Radius = 100, Type = SpellType.Line
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Xerath", MenuName = "XerathW", SpellName = "XerathArcaneBarrage2", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 1100, Delay = 780, Radius = 260, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Xerath", MenuName = "XerathE", SpellName = "XerathMageSpear",
                        MissileName = "XerathMageSpearMissile", DangerValue = 2, Slot = SpellSlot.E, Range = 1100,
                        Radius = 60, MissileSpeed = 1400,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionableObjects.Heroes, CollisionableObjects.Minions,
                                    CollisionableObjects.YasuoWall
                                },
                        FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Xerath", MenuName = "XerathR", SpellName = "XerathRMissileWrapper",
                        MissileName = "XerathLocusPulse", DangerValue = 3, Slot = SpellSlot.R, Range = 20000, Delay = 650,
                        Radius = 200, Type = SpellType.Circle, MissileDelayed = true, IsDangerous = true,
                        CanBeRemoved = false
                    });

            #endregion Xerath

            #region Yasuo

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Yasuo", MenuName = "YasuoQ", SpellName = "YasuoQ",
                        ExtraSpellNames = new[] { "YasuoQ2" }, DangerValue = 2, Range = 520, Delay = 400, Radius = 55,
                        Type = SpellType.Line, FixedRange = true, Invert = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Yasuo", MenuName = "YasuoQ3", SpellName = "YasuoQ3", MissileName = "YasuoQ3Mis",
                        DangerValue = 3, Range = 1100, Delay = 300, Radius = 90, MissileSpeed = 1200,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, Invert = true,
                        IsDangerous = true
                    });

            #endregion Yasuo

            #region Yorick

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Yorick", MenuName = "YorickW", SpellName = "YorickW", DangerValue = 2,
                        Slot = SpellSlot.W, Range = 600, Delay = 750, Radius = 250, Type = SpellType.Circle,
                        DisabledByDefault = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Yorick", MenuName = "YorickE", SpellName = "YorickE", DangerValue = 2,
                        Slot = SpellSlot.E, Range = 550, Delay = 50, Radius = 120, Type = SpellType.Line,
                        FixedRange = true
                    });

            #endregion Yorick

            #region Zac

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zac", MenuName = "ZacQ", SpellName = "ZacQ", DangerValue = 2, Range = 550,
                        Delay = 500, Radius = 120, Type = SpellType.Line, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zac", MenuName = "ZacE", SpellName = "ZacE2", DangerValue = 3, Slot = SpellSlot.E,
                        Range = 1800, Delay = 0, Radius = 250, MissileSpeed = 1350, Type = SpellType.Circle,
                        IsDangerous = true, IsDash = true
                    });

            #endregion Zac

            #region Zed

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zed", MenuName = "ZedQ", SpellName = "ZedQ", MissileName = "ZedQMissile",
                        DangerValue = 2, Range = 925, Radius = 50, MissileSpeed = 1700,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zed", MenuName = "ZedE", SpellName = "ZedE", DangerValue = 2, Slot = SpellSlot.E,
                        Delay = 10, Radius = 290
                    });

            #endregion Zed

            #region Ziggs

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ziggs", MenuName = "ZiggsQ", SpellName = "ZiggsQ", MissileName = "ZiggsQSpell",
                        DangerValue = 2, Range = 850, Radius = 125, MissileSpeed = 1700, Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ziggs", MenuName = "ZiggsQBounce", SpellName = "ZiggsQBounce",
                        MissileName = "ZiggsQSpell2", ExtraMissileNames = new[] { "ZiggsQSpell3" }, DangerValue = 2,
                        Range = 1100, Delay = 400, Radius = 125, MissileSpeed = 1600, Type = SpellType.Circle,
                        MissileDelayed = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ziggs", MenuName = "ZiggsW", SpellName = "ZiggsW", MissileName = "ZiggsW",
                        DangerValue = 2, Slot = SpellSlot.W, Range = 1000, Radius = 275, MissileSpeed = 1750,
                        Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ziggs", MenuName = "ZiggsE", SpellName = "ZiggsE", MissileName = "ZiggsE2",
                        DangerValue = 2, Slot = SpellSlot.E, Range = 900, Radius = 235, MissileSpeed = 1550,
                        Type = SpellType.Circle
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Ziggs", MenuName = "ZiggsR", SpellName = "ZiggsR", MissileName = "ZiggsRBoom",
                        DangerValue = 2, Slot = SpellSlot.R, Range = 5000, Delay = 400, Radius = 500, MissileSpeed = 1500,
                        Type = SpellType.Circle, ExtraDelay = 1100
                    });

            #endregion Ziggs

            #region Zilean

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zilean", MenuName = "ZileanQ", SpellName = "ZileanQ", MissileName = "ZileanQMissile",
                        ToggleName = "Zilean_.+_Q_TimeBombGround(Green|Red)", DangerValue = 2, Range = 900, Radius = 150,
                        MissileSpeed = 2000, Type = SpellType.Circle,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, ExtraDuration = 3100,
                        DontCross = true, ExtraDelay = 450
                    });

            #endregion Zilean

            #region Zyra

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zyra", MenuName = "ZyraQ", SpellName = "ZyraQ", DangerValue = 2, Range = 800,
                        Delay = 850, Radius = 140, Type = SpellType.Line, Perpendicular = true, RadiusEx = 450
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zyra", MenuName = "ZyraE", SpellName = "ZyraE", MissileName = "ZyraE",
                        DangerValue = 3, Slot = SpellSlot.E, Range = 1150, Radius = 70, MissileSpeed = 1150,
                        CollisionObjects = new[] { CollisionableObjects.YasuoWall }, FixedRange = true, IsDangerous = true
                    });

            Spells.Add(
                new SpellData
                    {
                        ChampName = "Zyra", MenuName = "ZyraR", SpellName = "ZyraR", DangerValue = 3, Slot = SpellSlot.R,
                        Range = 700, Delay = 2200, Radius = 520, Type = SpellType.Circle
                    });

            #endregion Zyra
        }

        #endregion
    }
}
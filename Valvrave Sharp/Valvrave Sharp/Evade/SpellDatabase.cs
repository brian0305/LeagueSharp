namespace Valvrave_Sharp.Evade
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using LeagueSharp;

    public static class SpellDatabase
    {
        #region Static Fields

        public static List<SpellData> Spells = new List<SpellData>();

        #endregion

        #region Constructors and Destructors

        static SpellDatabase()
        {
            #region Aatrox

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Aatrox", SpellName = "AatroxQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 600, Range = 650, Radius = 250, MissileSpeed = 2000,
                        AddHitbox = true, DangerValue = 3, IsDangerous = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Aatrox", SpellName = "AatroxE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1075, Radius = 35,
                        MissileSpeed = 1250, FixedRange = true, AddHitbox = true, DangerValue = 3,
                        MissileSpellName = "AatroxEConeMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Aatrox

            #region Ahri

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ahri", SpellName = "AhriOrbofDeception", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 100,
                        MissileSpeed = 2500, MissileAccel = -3200, MissileMaxSpeed = 2500, MissileMinSpeed = 400,
                        FixedRange = true, AddHitbox = true, DangerValue = 2, MissileSpellName = "AhriOrbMissile",
                        CanBeRemoved = true, ForceRemove = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ahri", SpellName = "AhriOrbReturn", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 100,
                        MissileSpeed = 60, MissileAccel = 1900, MissileMinSpeed = 60, MissileMaxSpeed = 2600,
                        FixedRange = true, AddHitbox = true, DangerValue = 2, MissileFollowsUnit = true,
                        CanBeRemoved = true, ForceRemove = true, MissileSpellName = "AhriOrbReturn",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ahri", SpellName = "AhriSeduce", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 60,
                        MissileSpeed = 1550, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "AhriSeduceMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Ahri

            #region Amumu

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Amumu", SpellName = "BandageToss", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 90,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "SadMummyBandageToss", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Amumu", SpellName = "CurseoftheSadMummy", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 0, Radius = 550,
                        MissileSpeed = int.MaxValue, FixedRange = true, DangerValue = 5, IsDangerous = true
                    });

            #endregion Amumu

            #region Anivia

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Anivia", SpellName = "FlashFrost", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 110,
                        MissileSpeed = 850, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "FlashFrostSpell", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Anivia

            #region Annie

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Annie", SpellName = "Incinerate", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCone, Delay = 250, Range = 825, Radius = 80,
                        MissileSpeed = int.MaxValue, DangerValue = 2
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Annie", SpellName = "InfernalGuardian", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 600, Radius = 251,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 5, IsDangerous = true
                    });

            #endregion Annie

            #region Ashe

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ashe", SpellName = "Volley", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1250, Radius = 60,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VolleyAttack", MultipleNumber = 9,
                        MultipleAngle = 4.62f * (float)Math.PI / 180, CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ashe", SpellName = "EnchantedCrystalArrow", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 20000, Radius = 130,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "EnchantedCrystalArrow", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Ashe

            #region Bard

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Bard", SpellName = "BardQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "BardQMissile", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Bard", SpellName = "BardR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 3400, Radius = 350, MissileSpeed = 2100,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "BardR"
                    });

            #endregion

            #region Blatzcrink

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Blitzcrank", SpellName = "RocketGrab", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 70,
                        MissileSpeed = 1800, FixedRange = true, AddHitbox = true, DangerValue = 4, IsDangerous = true,
                        MissileSpellName = "RocketGrabMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Blitzcrank", SpellName = "StaticField", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 0, Radius = 600,
                        MissileSpeed = int.MaxValue, FixedRange = true, DangerValue = 2
                    });

            #endregion Blatzcrink

            #region Brand

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Brand", SpellName = "BrandBlaze", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 60,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "BrandBlazeMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Brand", SpellName = "BrandFissure", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 850, Range = 900, Radius = 240,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2
                    });

            #endregion Brand

            #region Braum

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Braum", SpellName = "BraumQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 60,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "BraumQMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Braum", SpellName = "BraumRWrapper", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1200, Radius = 115,
                        MissileSpeed = 1400, FixedRange = true, AddHitbox = true, DangerValue = 4, IsDangerous = true,
                        MissileSpellName = "braumrmissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Braum

            #region Caitlyn

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Caitlyn", SpellName = "CaitlynPiltoverPeacemaker", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 625, Range = 1300, Radius = 90,
                        MissileSpeed = 2200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "CaitlynPiltoverPeacemaker",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Caitlyn", SpellName = "CaitlynEntrapment", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 125, Range = 1000, Radius = 80,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 1,
                        MissileSpellName = "CaitlynEntrapmentMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Caitlyn

            #region Cassiopeia

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Cassiopeia", SpellName = "CassiopeiaNoxiousBlast", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 750, Range = 850, Radius = 150,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "CassiopeiaNoxiousBlast"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Cassiopeia", SpellName = "CassiopeiaPetrifyingGaze", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCone, Delay = 600, Range = 825, Radius = 80,
                        MissileSpeed = int.MaxValue, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "CassiopeiaPetrifyingGaze"
                    });

            #endregion Cassiopeia

            #region Chogath

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Chogath", SpellName = "Rupture", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 1200, Range = 950, Radius = 250,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 3, MissileSpellName = "Rupture"
                    });

            #endregion Chogath

            #region Corki

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Corki", SpellName = "PhosphorusBomb", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 300, Range = 825, Radius = 250, MissileSpeed = 1000,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "PhosphorusBombMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Corki", SpellName = "MissileBarrage", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 200, Range = 1300, Radius = 40,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "MissileBarrageMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Corki", SpellName = "MissileBarrage2", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 200, Range = 1500, Radius = 40,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "MissileBarrageMissile2", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Corki

            #region Darius

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Darius", SpellName = "DariusCleave", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 750, Range = 0, Radius = 425,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 3,
                        MissileSpellName = "DariusCleave", FollowCaster = true, DisabledByDefault = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Darius", SpellName = "DariusAxeGrabCone", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCone, Delay = 250, Range = 550, Radius = 80,
                        MissileSpeed = int.MaxValue, FixedRange = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "DariusAxeGrabCone"
                    });

            #endregion Darius

            #region Diana

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Diana", SpellName = "DianaArc", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 895, Radius = 195, MissileSpeed = 1400,
                        AddHitbox = true, DangerValue = 3, IsDangerous = true, MissileSpellName = "DianaArcArc",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Diana", SpellName = "DianaArcArc", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotArc, Delay = 250, Range = 895, Radius = 195, MissileSpeed = 1400,
                        DontCross = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "DianaArcArc", TakeClosestPath = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Diana

            #region DrMundo

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "DrMundo", SpellName = "InfectedCleaverMissileCast", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 60,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3,
                        MissileSpellName = "InfectedCleaverMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion DrMundo

            #region Draven

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Draven", SpellName = "DravenDoubleShot", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 130,
                        MissileSpeed = 1400, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "DravenDoubleShotMissile", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Draven", SpellName = "DravenRCast", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 400, Range = 20000, Radius = 160,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "DravenR", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Draven

            #region Ekko

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ekko", SpellName = "EkkoQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 1650, FixedRange = true, AddHitbox = true, DangerValue = 4, IsDangerous = true,
                        MissileSpellName = "ekkoqmis", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ekko", SpellName = "EkkoW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 3750, Range = 1600, Radius = 375,
                        MissileSpeed = 1650, DangerValue = 3, MissileSpellName = "EkkoW", DisabledByDefault = true,
                        CanBeRemoved = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ekko", SpellName = "EkkoR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1600, Radius = 375, MissileSpeed = 1650,
                        FixedRange = true, AddHitbox = true, DangerValue = 3, MissileSpellName = "EkkoR",
                        CanBeRemoved = true, FromObjects = new[] { "Ekko_Base_R_TrailEnd.troy" }
                    });

            #endregion Ekko

            #region Elise

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Elise", SpellName = "EliseHumanE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 55,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 4, IsDangerous = true,
                        MissileSpellName = "EliseHumanE", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Elise

            #region Evelynn

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Evelynn", SpellName = "EvelynnR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 650, Radius = 350,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "EvelynnR"
                    });

            #endregion Evelynn

            #region Ezreal

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ezreal", SpellName = "EzrealMysticShot", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 60,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "EzrealMysticShotMissile",
                        ExtraMissileNames = new[] { "EzrealMysticShotPulseMissile" }, CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                },
                        Id = 229
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ezreal", SpellName = "EzrealEssenceFlux", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 80,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "EzrealEssenceFluxMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ezreal", SpellName = "EzrealTrueshotBarrage", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 1000, Range = 20000, Radius = 160,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "EzrealTrueshotBarrage",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }, Id = 245
                    });

            #endregion Ezreal

            #region Fiora

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Fiora", SpellName = "FioraW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 700, Range = 800, Radius = 70,
                        MissileSpeed = 3200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "FioraWMissile"
                    });

            #endregion Fiora

            #region Fizz

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Fizz", SpellName = "FizzMarinerDoom", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1300, Radius = 120,
                        MissileSpeed = 1350, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "FizzMarinerDoomMissile", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Fizz

            #region Galio

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Galio", SpellName = "GalioResoluteSmite", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 900, Radius = 200, MissileSpeed = 1300,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "GalioResoluteSmite",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Galio", SpellName = "GalioRighteousGust", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 120,
                        MissileSpeed = 1200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "GalioRighteousGust",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Galio", SpellName = "GalioIdolOfDurand", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 0, Radius = 550,
                        MissileSpeed = int.MaxValue, FixedRange = true, DangerValue = 5, IsDangerous = true
                    });

            #endregion Galio

            #region Gnar

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1125, Radius = 60,
                        MissileSpeed = 2500, MissileAccel = -3000, MissileMaxSpeed = 2500, MissileMinSpeed = 1400,
                        FixedRange = true, AddHitbox = true, DangerValue = 2, CanBeRemoved = true, ForceRemove = true,
                        MissileSpellName = "gnarqmissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarQReturn", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 2500, Radius = 75, MissileSpeed = 60,
                        MissileAccel = 800, MissileMaxSpeed = 2600, MissileMinSpeed = 60, FixedRange = true,
                        AddHitbox = true, DangerValue = 2, CanBeRemoved = true, ForceRemove = true,
                        MissileSpellName = "GnarQMissileReturn", DisabledByDefault = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarBigQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1150, Radius = 90,
                        MissileSpeed = 2100, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "GnarBigQMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarBigW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotLine, Delay = 600, Range = 600, Radius = 80,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "GnarBigW"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 473, Radius = 150, MissileSpeed = 903,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "GnarE"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarBigE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 475, Radius = 200, MissileSpeed = 1000,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "GnarBigE"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gnar", SpellName = "GnarR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 0, Radius = 500,
                        MissileSpeed = int.MaxValue, FixedRange = true, DangerValue = 5, IsDangerous = true
                    });

            #endregion

            #region Gragas

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gragas", SpellName = "GragasQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1100, Radius = 275, MissileSpeed = 1300,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "GragasQMissile", ExtraDuration = 4500,
                        ToggleParticleName = "Gragas_.+_Q_(Enemy|Ally)", DontCross = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gragas", SpellName = "GragasE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 950, Radius = 200,
                        MissileSpeed = 1200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "GragasE", CanBeRemoved = true, ExtraRange = 300,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.Minion }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Gragas", SpellName = "GragasR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1050, Radius = 375, MissileSpeed = 1800,
                        AddHitbox = true, DangerValue = 5, IsDangerous = true, MissileSpellName = "GragasRBoom",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Gragas

            #region Graves

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Graves", SpellName = "GravesClusterShot", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 50,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "GravesClusterShotAttack", MultipleNumber = 3,
                        MultipleAngle = 15 * (float)Math.PI / 180,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Graves", SpellName = "GravesChargeShot", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 100,
                        MissileSpeed = 2100, FixedRange = true, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "GravesChargeShotShot",
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Graves

            #region Heimerdinger

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Heimerdinger", SpellName = "Heimerdingerwm", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1500, Radius = 70,
                        MissileSpeed = 1800, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "HeimerdingerWAttack2",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Heimerdinger", SpellName = "HeimerdingerE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 925, Radius = 100, MissileSpeed = 1200,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "heimerdingerespell",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Heimerdinger

            #region Irelia

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Irelia", SpellName = "IreliaTranscendentBlades", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 1200, Radius = 65,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "IreliaTranscendentBlades",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Irelia

            #region Janna

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Janna", SpellName = "JannaQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1700, Radius = 120,
                        MissileSpeed = 900, AddHitbox = true, DangerValue = 2, MissileSpellName = "HowlingGaleSpell",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Janna

            #region JarvanIV

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "JarvanIV", SpellName = "JarvanIVDragonStrike", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 600, Range = 770, Radius = 70,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 3
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "JarvanIV", SpellName = "JarvanIVEQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 880, Radius = 70,
                        MissileSpeed = 1450, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "JarvanIV", SpellName = "JarvanIVDemacianStandard", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 860, Radius = 175,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "JarvanIVDemacianStandard"
                    });

            #endregion JarvanIV

            #region Jayce

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Jayce", SpellName = "jayceshockblast", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1300, Radius = 70,
                        MissileSpeed = 1450, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "JayceShockBlastMis", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Jayce", SpellName = "JayceQAccel", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1300, Radius = 70,
                        MissileSpeed = 2350, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "JayceShockBlastWallMis", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Jayce

            #region Jinx

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Jinx", SpellName = "JinxW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 600, Range = 1500, Radius = 60,
                        MissileSpeed = 3300, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "JinxWMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Jinx", SpellName = "JinxR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 600, Range = 20000, Radius = 140,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "JinxR", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Jinx

            #region Kalista

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Kalista", SpellName = "KalistaMysticShot", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 40,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "kalistamysticshotmis",
                        ExtraMissileNames = new[] { "kalistamysticshotmistrue" }, CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Kalista

            #region Karma

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Karma", SpellName = "KarmaQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 60,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KarmaQMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Karma", SpellName = "KarmaQMantra", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 80,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KarmaQMissileMantra", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Karma

            #region Karthus

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Karthus", SpellName = "KarthusLayWasteA2",
                        ExtraSpellNames =
                            new[]
                                {
                                    "karthuslaywastea3", "karthuslaywastea1", "karthuslaywastedeada1",
                                    "karthuslaywastedeada2", "karthuslaywastedeada3"
                                },
                        Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 625, Range = 875, Radius = 160,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2
                    });

            #endregion Karthus

            #region Kassadin

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Kassadin", SpellName = "RiftWalk", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 450, Radius = 270,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2, MissileSpellName = "RiftWalk"
                    });

            #endregion Kassadin

            #region Kennen

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Kennen", SpellName = "KennenShurikenHurlMissile1", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 125, Range = 1050, Radius = 50,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KennenShurikenHurlMissile1", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Kennen

            #region Khazix

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Khazix", SpellName = "KhazixW", ExtraSpellNames = new[] { "khazixwlong" },
                        Slot = SpellSlot.W, Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1025,
                        Radius = 73, MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KhazixWMissile", CanBeRemoved = true, MultipleNumber = 3,
                        MultipleAngle = 22f * (float)Math.PI / 180,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Khazix", SpellName = "KhazixE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 600, Radius = 300, MissileSpeed = 1500,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "KhazixE"
                    });

            #endregion Khazix

            #region KogMaw

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "KogMaw", SpellName = "KogMawQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 70,
                        MissileSpeed = 1650, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KogMawQMis", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "KogMaw", SpellName = "KogMawVoidOoze", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1360, Radius = 120,
                        MissileSpeed = 1400, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KogMawVoidOozeMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "KogMaw", SpellName = "KogMawLivingArtillery", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 1200, Range = 1800, Radius = 150,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "KogMawLivingArtillery"
                    });

            #endregion KogMaw

            #region Leblanc

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leblanc", SpellName = "LeblancSlide", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 600, Radius = 220, MissileSpeed = 1450,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "LeblancSlide"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leblanc", SpellName = "LeblancSlideM", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 600, Radius = 220, MissileSpeed = 1450,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "LeblancSlideM"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leblanc", SpellName = "LeblancSoulShackle", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 70,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "LeblancSoulShackle", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leblanc", SpellName = "LeblancSoulShackleM", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 70,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "LeblancSoulShackleM", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Leblanc

            #region LeeSin

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "LeeSin", SpellName = "BlindMonkQOne", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 65,
                        MissileSpeed = 1800, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "BlindMonkQOne", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion LeeSin

            #region Leona

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leona", SpellName = "LeonaZenithBlade", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 905, Radius = 70,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        TakeClosestPath = true, MissileSpellName = "LeonaZenithBladeMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Leona", SpellName = "LeonaSolarFlare", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 1000, Range = 1200, Radius = 300,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "LeonaSolarFlare"
                    });

            #endregion Leona

            #region Lissandra

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lissandra", SpellName = "LissandraQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 700, Radius = 75,
                        MissileSpeed = 2200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "LissandraQMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lissandra", SpellName = "LissandraQShards", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 700, Radius = 90,
                        MissileSpeed = 2200, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "lissandraqshards", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lissandra", SpellName = "LissandraE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1025, Radius = 125,
                        MissileSpeed = 850, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "LissandraEMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Lulu

            #region Lucian

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lucian", SpellName = "LucianQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 500, Range = 1300, Radius = 65,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "LucianQ"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lucian", SpellName = "LucianW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 55,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "lucianwmissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lucian", SpellName = "LucianRMis", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1400, Radius = 110,
                        MissileSpeed = 2800, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "lucianrmissileoffhand", ExtraMissileNames = new[] { "lucianrmissile" },
                        DontCheckForDuplicates = true, DisabledByDefault = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Lucian

            #region Lulu

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lulu", SpellName = "LuluQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 1450, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "LuluQMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lulu", SpellName = "LuluQPix", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 1450, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "LuluQMissileTwo", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Lulu

            #region Lux

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lux", SpellName = "LuxLightBinding", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1300, Radius = 70,
                        MissileSpeed = 1200, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "LuxLightBindingMis",
                        //CanBeRemoved = true,
                        CollisionObjects = new[]
                                               {
                                                   /*CollisionObjectTypes.Champions, CollisionObjectTypes.Minion,*/
                                                   CollisionObjectTypes.YasuoWall
                                               }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lux", SpellName = "LuxLightStrikeKugel", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1100, Radius = 340, MissileSpeed = 1300,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "LuxLightStrikeKugel", ExtraDuration = 5500,
                        ToggleParticleName = "Lux_.+_E_tar_aoe_", DontCross = true, CanBeRemoved = true,
                        DisabledByDefault = true, CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Lux", SpellName = "LuxMaliceCannon", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotLine, Delay = 1000, Range = 3500, Radius = 190,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 5,
                        IsDangerous = true, MissileSpellName = "LuxMaliceCannon"
                    });

            #endregion Lux

            #region Malphite

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Malphite", SpellName = "UFSlash", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 1000, Radius = 270, MissileSpeed = 1500,
                        AddHitbox = true, DangerValue = 5, IsDangerous = true, MissileSpellName = "UFSlash"
                    });

            #endregion Malphite

            #region Malzahar

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Malzahar", SpellName = "AlZaharCalloftheVoid", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 1000, Range = 900, Radius = 85,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2, DontCross = true,
                        MissileSpellName = "AlZaharCalloftheVoid"
                    });

            #endregion Malzahar

            #region Morgana

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Morgana", SpellName = "DarkBindingMissile", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1300, Radius = 80,
                        MissileSpeed = 1200, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "DarkBindingMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Morgana

            #region Nami

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Nami", SpellName = "NamiQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 950, Range = 875, Radius = 200,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "namiqmissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Nami", SpellName = "NamiR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 2750, Radius = 260,
                        MissileSpeed = 850, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "NamiRMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Nami

            #region Nautilus

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Nautilus", SpellName = "NautilusAnchorDrag", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1250, Radius = 90,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "NautilusAnchorDragMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Nautilus

            #region Nidalee

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Nidalee", SpellName = "JavelinToss", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1500, Radius = 40,
                        MissileSpeed = 1300, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "JavelinToss", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Nidalee

            #region Nocturne

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Nocturne", SpellName = "NocturneDuskbringer", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1125, Radius = 60,
                        MissileSpeed = 1400, DangerValue = 2, MissileSpellName = "NocturneDuskbringer",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Nocturne

            #region Olaf

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Olaf", SpellName = "OlafAxeThrowCast", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, ExtraRange = 150,
                        Radius = 105, MissileSpeed = 1600, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "olafaxethrow", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Olaf

            #region Orianna

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Orianna", SpellName = "OriannasQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 1500, Radius = 80,
                        MissileSpeed = 1200, AddHitbox = true, DangerValue = 2, MissileSpellName = "orianaizuna",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Orianna", SpellName = "OriannaQend", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 1500, Radius = 90, MissileSpeed = 1200,
                        AddHitbox = true, DangerValue = 2
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Orianna", SpellName = "OrianaDissonanceCommand", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 0, Radius = 255,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "OrianaDissonanceCommand", FromObject = "yomu_ring_"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Orianna", SpellName = "OriannasE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 1500, Radius = 85,
                        MissileSpeed = 1850, AddHitbox = true, DangerValue = 2, MissileSpellName = "orianaredact",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Orianna", SpellName = "OrianaDetonateCommand", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 700, Range = 0, Radius = 410,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 5,
                        IsDangerous = true, MissileSpellName = "OrianaDetonateCommand", FromObject = "yomu_ring_"
                    });

            #endregion Orianna

            #region Quinn

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Quinn", SpellName = "QuinnQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1050, Radius = 80,
                        MissileSpeed = 1550, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "QuinnQMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Quinn

            #region RekSai

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "RekSai", SpellName = "reksaiqburrowed", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1625, Radius = 60,
                        MissileSpeed = 1950, FixedRange = true, AddHitbox = true, DangerValue = 3,
                        MissileSpellName = "RekSaiQBurrowedMis",
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion RekSai

            #region Rengar

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Rengar", SpellName = "RengarE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 70,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "RengarEFinal", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Rengar

            #region Riven

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Riven", SpellName = "rivenizunablade", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 125,
                        MissileSpeed = 1600, DangerValue = 5, IsDangerous = true, MultipleNumber = 3,
                        MultipleAngle = 15 * (float)Math.PI / 180, MissileSpellName = "RivenLightsaberMissile",
                        ExtraMissileNames = new[] { "RivenLightsaberMissileSide" },
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Riven

            #region Rumble

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Rumble", SpellName = "RumbleGrenade", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "RumbleGrenade", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Rumble", SpellName = "RumbleCarpetBombM", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 400, MissileDelayed = true, Range = 1200,
                        Radius = 200, MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 4,
                        MissileSpellName = "RumbleCarpetBombMissile"
                    });

            #endregion Rumble

            #region Ryze

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ryze", SpellName = "RyzeQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 900, Radius = 55,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "RyzeQ", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ryze", SpellName = "ryzerq", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 900, Radius = 50,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "ryzerq", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion

            #region Sejuani

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sejuani", SpellName = "SejuaniArcticAssault", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 900, Radius = 70,
                        MissileSpeed = 1600, AddHitbox = true, DangerValue = 3, IsDangerous = true, ExtraRange = 200,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.Minion }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sejuani", SpellName = "SejuaniGlacialPrisonStart", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 110,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "sejuaniglacialprison", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Sejuani

            #region Shen

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Shen", SpellName = "ShenShadowDash", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 650, Radius = 50,
                        MissileSpeed = 1600, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "ShenShadowDash", ExtraRange = 200
                    });

            #endregion Shen

            #region Shyvana

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Shyvana", SpellName = "ShyvanaFireball", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 60,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "ShyvanaFireballMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Shyvana", SpellName = "ShyvanaFireballDragon", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 750, Radius = 70,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "ShyvanaFireballDragonFxMissile", MultipleNumber = 3,
                        MultipleAngle = 22f * (float)Math.PI / 180,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Shyvana", SpellName = "ShyvanaTransformCast", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 150,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "ShyvanaTransformCast", ExtraRange = 200
                    });

            #endregion Shyvana

            #region Sion

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sion", SpellName = "SionE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 800, Radius = 80,
                        MissileSpeed = 1800, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "SionEMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sion", SpellName = "SionR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 800, Radius = 120,
                        MissileSpeed = 1000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion }
                    });

            #endregion Sion

            #region Sivir

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sivir", SpellName = "SivirQReturn", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 1250, Radius = 100,
                        MissileSpeed = 1350, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "SivirQMissileReturn", MissileFollowsUnit = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sivir", SpellName = "SivirQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1250, Radius = 90,
                        MissileSpeed = 1350, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "SivirQMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Sivir

            #region Skarner

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Skarner", SpellName = "SkarnerFracture", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 70,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "SkarnerFractureMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Skarner

            #region Sona

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Sona", SpellName = "SonaR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 140,
                        MissileSpeed = 2400, FixedRange = true, AddHitbox = true, DangerValue = 5, IsDangerous = true,
                        MissileSpellName = "SonaR", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Sona

            #region Soraka

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Soraka", SpellName = "SorakaQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 950, Radius = 300, MissileSpeed = 1750,
                        AddHitbox = true, DangerValue = 2, CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Soraka

            #region Swain

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Swain", SpellName = "SwainShadowGrasp", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 1100, Range = 900, Radius = 180,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "SwainShadowGrasp"
                    });

            #endregion Swain

            #region Syndra

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Syndra", SpellName = "SyndraQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 600, Range = 800, Radius = 150,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2, MissileSpellName = "SyndraQ"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Syndra", SpellName = "syndrawcast", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 950, Radius = 210, MissileSpeed = 1450,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "syndrawcast"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Syndra", SpellName = "syndrae5", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 300, Range = 950, Radius = 90,
                        MissileSpeed = 1601, AddHitbox = true, DangerValue = 2, MissileSpellName = "syndrae5",
                        DisableFowDetection = true, CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Syndra", SpellName = "SyndraE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 300, Range = 950, Radius = 90,
                        MissileSpeed = 1601, AddHitbox = true, DangerValue = 2, DisableFowDetection = true,
                        MissileSpellName = "SyndraE", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Syndra

            #region Tahm Kench

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "TahmKench", SpellName = "TahmKenchQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 951, Radius = 90,
                        MissileSpeed = 2800, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "tahmkenchqmissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });

            #endregion Tahm Kench

            #region Talon

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Talon", SpellName = "TalonRake", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 800, Radius = 80,
                        MissileSpeed = 2300, FixedRange = true, AddHitbox = true, DangerValue = 2, IsDangerous = true,
                        MultipleNumber = 3, MultipleAngle = 20 * (float)Math.PI / 180,
                        MissileSpellName = "talonrakemissileone",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Talon", SpellName = "TalonRakeReturn", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 800, Radius = 80,
                        MissileSpeed = 1850, FixedRange = true, AddHitbox = true, DangerValue = 2, IsDangerous = true,
                        MultipleNumber = 3, MultipleAngle = 20 * (float)Math.PI / 180,
                        MissileSpellName = "talonrakemissiletwo",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Riven

            #region Thresh

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Thresh", SpellName = "ThreshQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1100, Radius = 70,
                        MissileSpeed = 1900, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "ThreshQMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Thresh", SpellName = "ThreshEFlay", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 125, Range = 1075, Radius = 110,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        Centered = true, MissileSpellName = "ThreshEMissile1",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Thresh

            #region Tristana

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Tristana", SpellName = "RocketJump", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 900, Radius = 270, MissileSpeed = 1500,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "RocketJump"
                    });

            #endregion Tristana

            #region Tryndamere

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Tryndamere", SpellName = "slashCast", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 0, Range = 660, Radius = 93,
                        MissileSpeed = 1300, AddHitbox = true, DangerValue = 2, MissileSpellName = "slashCast"
                    });

            #endregion Tryndamere

            #region TwistedFate

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "TwistedFate", SpellName = "WildCards", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1450, Radius = 40,
                        MissileSpeed = 1000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "SealFateMissile", MultipleNumber = 3,
                        MultipleAngle = 28 * (float)Math.PI / 180,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion TwistedFate

            #region Twitch

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Twitch", SpellName = "TwitchVenomCask", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 900, Radius = 275, MissileSpeed = 1400,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "TwitchVenomCaskMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Twitch

            #region Urgot

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Urgot", SpellName = "UrgotHeatseekingLineMissile", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 125, Range = 1000, Radius = 60,
                        MissileSpeed = 1600, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "UrgotHeatseekingLineMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Urgot", SpellName = "UrgotPlasmaGrenade", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1100, Radius = 210, MissileSpeed = 1500,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "UrgotPlasmaGrenadeBoom",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Urgot

            #region Varus

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Varus", SpellName = "VarusQMissilee", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1800, Radius = 70,
                        MissileSpeed = 1900, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VarusQMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Varus", SpellName = "VarusE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 1000, Range = 925, Radius = 235, MissileSpeed = 1500,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "VarusE",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Varus", SpellName = "VarusR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 120,
                        MissileSpeed = 1950, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "VarusRMissile", CanBeRemoved = true,
                        CollisionObjects = new[] { CollisionObjectTypes.Champion, CollisionObjectTypes.YasuoWall }
                    });

            #endregion Varus

            #region Veigar

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Veigar", SpellName = "VeigarBalefulStrike", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 950, Radius = 70,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VeigarBalefulStrikeMis",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Veigar", SpellName = "VeigarDarkMatter", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 1100, Range = 900, Radius = 225,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Veigar", SpellName = "VeigarEventHorizon", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotRing, Delay = 500, Range = 700, Radius = 80,
                        MissileSpeed = int.MaxValue, DangerValue = 3, IsDangerous = true, DontAddExtraDuration = true,
                        RingRadius = 350, ExtraDuration = 3300, DontCross = true
                    });

            #endregion Veigar

            #region Velkoz

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Velkoz", SpellName = "VelkozQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 50,
                        MissileSpeed = 1300, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VelkozQMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Velkoz", SpellName = "VelkozQSplit", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1100, Radius = 55,
                        MissileSpeed = 2100, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VelkozQMissileSplit", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Velkoz", SpellName = "VelkozW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1200, Radius = 88,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "VelkozWMissile", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Velkoz", SpellName = "VelkozE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 800, Radius = 225, MissileSpeed = 1500,
                        DangerValue = 2, MissileSpellName = "VelkozEMissile"
                    });

            #endregion Velkoz

            #region Vi

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Vi", SpellName = "Vi-q", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1000, Radius = 90,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "ViQMissile"
                    });

            #endregion Vi

            #region Viktor

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Viktor", SpellName = "Laser", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1500, Radius = 80,
                        MissileSpeed = 780, AddHitbox = true, DangerValue = 2, MissileSpellName = "ViktorDeathRayMissile",
                        ExtraMissileNames = new[] { "viktoreaugmissile" },
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Viktor

            #region Xerath

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Xerath", SpellName = "xeratharcanopulse2", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 600, Range = 1600, Radius = 100,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "xeratharcanopulse2"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Xerath", SpellName = "XerathArcaneBarrage2", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 700, Range = 1000, Radius = 200,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "XerathArcaneBarrage2"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Xerath", SpellName = "XerathMageSpear", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 200, Range = 1150, Radius = 60,
                        MissileSpeed = 1400, FixedRange = true, AddHitbox = true, DangerValue = 2, IsDangerous = true,
                        MissileSpellName = "XerathMageSpearMissile", CanBeRemoved = true,
                        CollisionObjects =
                            new[]
                                {
                                    CollisionObjectTypes.Champion, CollisionObjectTypes.Minion,
                                    CollisionObjectTypes.YasuoWall
                                }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Xerath", SpellName = "xerathrmissilewrapper", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 700, Range = 5600, Radius = 120,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "xerathrmissilewrapper"
                    });

            #endregion Xerath

            #region Yasuo 

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Yasuo", SpellName = "yasuoq", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 400, Range = 550, Radius = 20,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        IsDangerous = true, MissileSpellName = "yasuoq", Invert = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Yasuo", SpellName = "yasuoq2", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotLine, Delay = 400, Range = 550, Radius = 20,
                        MissileSpeed = int.MaxValue, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        IsDangerous = true, MissileSpellName = "yasuoq2", Invert = true
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Yasuo", SpellName = "yasuoq3w", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1150, Radius = 90,
                        MissileSpeed = 1500, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "yasuoq3w", CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Yasuo

            #region Zac

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zac", SpellName = "ZacQ", Slot = SpellSlot.Q, Type = SkillShotType.SkillshotLine,
                        Delay = 500, Range = 550, Radius = 120, MissileSpeed = int.MaxValue, FixedRange = true,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZacQ"
                    });

            #endregion Zac

            #region Zed

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zed", SpellName = "ZedQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 925, Radius = 50,
                        MissileSpeed = 1700, FixedRange = true, AddHitbox = true, DangerValue = 2,
                        MissileSpellName = "ZedQMissile",
                        FromObjects = new[] { "Zed_Base_W_tar.troy", "Zed_Base_W_cloneswap_buf.troy" },
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Zed

            #region Ziggs

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 850, Radius = 140, MissileSpeed = 1700,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsQSpell", DisableFowDetection = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsQBounce1", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 850, Radius = 140, MissileSpeed = 1700,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsQSpell2",
                        ExtraMissileNames = new[] { "ZiggsQSpell2" }, DisableFowDetection = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsQBounce2", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 850, Radius = 160, MissileSpeed = 1700,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsQSpell3",
                        ExtraMissileNames = new[] { "ZiggsQSpell3" }, DisableFowDetection = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsW", Slot = SpellSlot.W,
                        Type = SkillShotType.SkillshotCircle, Delay = 250, Range = 1000, Radius = 275, MissileSpeed = 1750,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsW", DisableFowDetection = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsE", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotCircle, Delay = 500, Range = 900, Radius = 235, MissileSpeed = 1750,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsE", DisableFowDetection = true,
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Ziggs", SpellName = "ZiggsR", Slot = SpellSlot.R,
                        Type = SkillShotType.SkillshotCircle, Delay = 0, Range = 5300, Radius = 500,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2, MissileSpellName = "ZiggsR",
                        DisableFowDetection = true
                    });

            #endregion Ziggs

            #region Zilean

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zilean", SpellName = "ZileanQ", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 300, Range = 900, Radius = 210, MissileSpeed = 2000,
                        AddHitbox = true, DangerValue = 2, MissileSpellName = "ZileanQMissile",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Zilean

            #region Zyra

            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zyra", SpellName = "ZyraQFissure", Slot = SpellSlot.Q,
                        Type = SkillShotType.SkillshotCircle, Delay = 850, Range = 800, Radius = 220,
                        MissileSpeed = int.MaxValue, AddHitbox = true, DangerValue = 2, MissileSpellName = "ZyraQFissure"
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zyra", SpellName = "ZyraGraspingRoots", Slot = SpellSlot.E,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 250, Range = 1150, Radius = 70,
                        MissileSpeed = 1150, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "ZyraGraspingRoots",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });
            Spells.Add(
                new SpellData
                    {
                        ChampionName = "Zyra", SpellName = "zyrapassivedeathmanager", Slot = SpellSlot.Unknown,
                        Type = SkillShotType.SkillshotMissileLine, Delay = 500, Range = 1474, Radius = 70,
                        MissileSpeed = 2000, FixedRange = true, AddHitbox = true, DangerValue = 3, IsDangerous = true,
                        MissileSpellName = "zyrapassivedeathmanager",
                        CollisionObjects = new[] { CollisionObjectTypes.YasuoWall }
                    });

            #endregion Zyra
        }

        #endregion

        #region Methods

        internal static SpellData GetByMissileName(string missileSpellName)
        {
            return
                Spells.FirstOrDefault(
                    i =>
                    (i.MissileSpellName != null
                     && string.Equals(i.MissileSpellName, missileSpellName, StringComparison.CurrentCultureIgnoreCase))
                    || i.ExtraMissileNames.Contains(missileSpellName.ToLower()));
        }

        internal static SpellData GetByName(string spellName)
        {
            return
                Spells.FirstOrDefault(
                    i =>
                    string.Equals(i.SpellName, spellName, StringComparison.CurrentCultureIgnoreCase)
                    || i.ExtraSpellNames.Contains(spellName.ToLower()));
        }

        #endregion
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModManager.Data.Enums
{
    /// <summary>
    /// Enumerates custom map - and
	/// debug spawner info locations.
    /// </summary>
    public enum MapLocation
    {
        A01_S12_MushroomCave,
        A01S01_CaveCamp,
        A01S01_EvilTribeCamp,
        A01S01_Harbor,
        A01S02,
        A01S03_Jeep,
        A01S04,
        A01S05_Puddle,
        A01S06_Cartel,
        A01S06_Cartel_Cave,
        A01S07_GrabbingHook,
        A01S07_RockToCartel,
        A01S07_StoneRings,
        A01S07_Village,
        A01S08_CrashedPlane,
        A01S09_Elevator,
        A01S09_GoldMine,
        A01S10_BambooBridge,
        A01S10_EvilTribeCamp,
        A01S11,
        A01S11_EvilTribeCamp,
        A01S11_SQPlace,
        A01S12_GHtoAirport,
        A01S12_Island,
        A01S12_WhaCaveCamp,
        A02S01_Airport,
        A02S01_Cenot,
        A02S02_Pond,
        A03S01_Camp,
        A03S01_CaveAyuhaska,
        A03S01_GrabbHookHigh,
        A03S01_OutofCenot,
        A03S01_StoneRing,
        A03S02_TutorialCamp,
        A03S03_TribeVillage,
        A04_S01_a_SacredPath_blocked,
        A04_S01_a_SacredPath_entrance,
        A04_S01_b_big_caves,
        A04_S01_b_big_enemy_camp,
        A04_S01_b_passage_to_A01_S07,
        A04_S01_b_passage_to_A01_S12,
        A04_S01_c_mangrove_border,
        A04_S01_c_passage_to_A03_S03,
        A04_S01_c_waterfall_caves,
        A04_S01_c_waterfalls_island,
        A04_S01_d_pve_boat,
        A04_S01_d_steamboat,
        A04_S01_e_giant_cave,
        A04_S01_e_muddy_gorges,
        A04_S01_f_hub_village,
        A04_S01_f_passage_to_A01_S04,
        A04_S01_f_shaman_passage,
        A04_S01_g_passage_to_A01_S06,
        A04_S01_g_POI_on_hills,
        A04_S02_b_sacred_ruins,
        A04_S02_b_stone_bridge,
        A04_S02_c_dead_bodies_water_cave,
        A04_S02_c_hot_river,
        A04_S02_d_lake_canyons,
        A04_S02_d_river_canyons_cave,
        A04_S02_e_big_swamps,
        A04_S02_e_river_canyons,
        A04S01_a,
        A04S01_b,
        A04S01_c,
        A04S01_c_Barrel1,
        A04S01_c_Destroy,
        A04S01_d,
        A04S01_e,
        A04S01_e_Barrel3,
        A04S01_f,
        A04S01_f_Barrel2,
        A04S01_g,
        A04S02_b,
        A04S02_c,
        A04S02_d,
        A04S02_e,
        Albino_01,
        Albino_02,
        Albino_03,
        Albino_04,
        Albino_05,
        Albino_06,
        Arena_Fishing,
        Arena_Hunting,
        Arena_Planting,
        Arena_Tribe,
        Arena_Tribe_Gatekeeper,
        AztecWarrior_01,
        AztecWarrior_02,
        AztecWarrior_03,
        AztecWarrior_04,
        AztecWarrior_05,
        AztecWarrior_06,
        AztecWarrior_07,
        AztecWarrior_08,
        BadWater_01,
        BadWater_02,
        BadWater_03,
        BadWater_04,
        BadWater_05,
        BadWater_06,
        BadWater_07,
        BigAICamp_01,
        BigAICamp_02,
        BigAICamp_03,
        Bike_01,
        Bike_02,
        Bike_03,
        Bike_04,
        Bike_05,
        Bike_06,
        Bike_07,
        Canoe_01,
        Canoe_02,
        Canoe_03,
        Canoe_04,
        Canoe_05,
        Canoe_06,
        Cartel_Antena,
        Caves_1,
        Caves_10,
        Caves_11,
        Caves_12,
        Caves_13,
        Caves_14,
        Caves_15,
        Caves_16,
        Caves_17,
        Caves_18,
        Caves_2,
        Caves_3,
        Caves_4,
        Caves_5,
        Caves_6,
        Caves_7,
        Caves_8,
        Caves_9,
        ChallangeSP_Combat_Battery,
        ChallangeSP_Combat_WT,
        ChallangeSP_FireCamp,
        ChallangeSP_MightyCamp,
        ChallangeSP_Raft,
        Chapel_01,
        Chapel_02,
        Chapel_03,
        Chapel_04,
        Chapel_05,
        Chapel_06,
        Chapel_07,
        Chapel_08,
        Chapel_09,
        Chapel_10,
        Debug,
        Drum_01,
        Drum_02,
        Drum_03,
        Drum_04,
        Drum_05,
        Drum_06,
        Drum_07,
        Drum_08,
        Fisherman_01_Spear,
        Fisherman_02_Spear,
        Fisherman_03_Bow,
        Fisherman_04_Bow,
        Fisherman_05_Bow,
        food_ration_01_mound,
        food_ration_02_mound,
        food_ration_03_mound,
        food_ration_04_mound,
        food_ration_05_mound,
        food_ration_06_liane,
        food_ration_07_mound,
        food_ration_08_mound,
        food_ration_09_liane,
        food_ration_10_mound,
        food_ration_11_liane,
        food_ration_12_liane,
        food_ration_13_mound,
        food_ration_14_mound,
        food_ration_15_liane,
        food_ration_16_mound,
        food_ration_17_liane,
        food_ration_18_mound,
        food_ration_19_liane,
        food_ration_20_liane,
        food_ration_21_mound,
        food_ration_22_liane,
        food_ration_23_mound,
        food_ration_24_liane,
        food_ration_25_mound,
        GoldenFish_01,
        GoldenFish_02,
        GoldenFish_03,
        GoldenFish_04,
        GoldenFish_05,
        GoldenFish_06,
        GoldenFish_07,
        GuardianMonkeys_01,
        GuardianMonkeys_02,
        GuardianMonkeys_03,
        GuardianMonkeys_04,
        GuardianMonkeys_05,
        GuardianMonkeys_06,
        GuardianMonkeys_07,
        HangedBody_01,
        HangedBody_02,
        HangedBody_03,
        HangedBody_04,
        HangedBody_05,
        HangedBody_06,
        HangedBody_07,
        HangedBody_08,
        HangedBody_09,
        HangedBody_10,
        HangedBody_11,
        HangedBody_12,
        HangedBody_13,
        Hub_Village_Outside,
        Kid_01,
        Kid_02,
        Kid_03,
        Kid_04,
        Kid_05,
        LegendaryQuest_AlbinoSpawner,
        LegendaryQuest_AlbinoSpawner_Outside,
        LegendaryQuest_AztecWarrior_HiltSpawner,
        LegendaryQuest_AztecWarrior_ObsidianScratch_1Spawner,
        LegendaryQuest_AztecWarrior_ObsidianScratch_2Spawner,
        LegendaryQuest_AztecWarrior_OutsideCaveSpawner,
        LegendaryQuest_AztecWarrior_SkullWoodSpawner,
        LegendaryQuest_BabyTapirSpawner1,
        LegendaryQuest_BabyTapirSpawner2,
        LegendaryQuest_BabyTapirSpawner3,
        LegendaryQuest_BabyTapirSpawner4,
        LegendaryQuest_BabyTapirSpawner5,
        LegendaryQuest_BabyTapirSpawner6,
        LegendaryQuest_BabyTapirSpawner7,
        LegendaryQuest_BabyTapirSpawner8,
        LegendaryQuest_BadWaterAltarSpawner,
        LegendaryQuest_BadWaterBoatSpawner,
        LegendaryQuest_BadWaterCaveSpawner,
        LegendaryQuest_Bike,
        LegendaryQuest_CanoeBoatSpawner,
        LegendaryQuest_GoldenFishTrapSpawner,
        LegendaryQuest_GuardianMonkeys_ClawsSpawner,
        LegendaryQuest_GuardianMonkeys_EyesSpawner,
        LegendaryQuest_GuardianMonkeys_TailSpawner,
        LegendaryQuest_Lovers_Charm01Spawner,
        LegendaryQuest_Lovers_Charm02Spawner,
        LegendaryQuest_Lovers_Charm03Spawner,
        LegendaryQuest_Lovers_Charm04Spawner,
        LegendaryQuest_Lovers_ShackSpawner,
        LegendaryQuest_PantherSpawner,
        LegendaryQuest_PantherSpawnerOutside,
        Lovers_01,
        Lovers_02,
        Lovers_03,
        Lovers_04,
        Lovers_05,
        Lovers_06,
        Lovers_07,
        Lovers_08,
        MantisSpawner,
        Panther_01,
        Panther_02,
        Panther_03,
        Panther_04,
        Panther_05,
        Panther_06,
        Panther_07,
        Patrol_01_01_C1_1,
        Patrol_02_01_C1_4,
        Patrol_02_02_C1_4,
        Patrol_03_01_C4_8,
        Patrol_04_01_C8_3,
        Patrol_04_02_C8_3,
        Patrol_05_01_C3_7,
        Patrol_05_02_C3_7,
        Patrol_06_01_C7_2,
        Patrol_06_02_C7_2,
        Patrol_07_01_C2_2,
        Patrol_08_01_C2_9,
        Patrol_08_02_C2_9,
        Patrol_09_01_C9_7,
        Patrol_09_02_C9_7,
        Patrol_10_01_C3_3,
        Patrol_10_02_C3_3,
        Patrol_10_03_C3_3,
        Patrol_11_01_C3_10,
        Patrol_12_01_C9_5,
        Patrol_12_02_C9_5,
        Patrol_13_01_C5_8,
        Patrol_13_02_C5_8,
        Patrol_14_01_C2_10,
        Patrol_14_02_C2_10,
        PoisonedWater01,
        PoisonedWater02,
        PoisonedWater03,
        PoisonedWater04,
        PoisonedWater05,
        PoisonedWater06,
        PoisonedWater07,
        PoisonedWater08,
        PoisonedWater09,
        PoisonedWater10,
        Przejscie_1_WHACamp,
        Przejscie_2_TutorialKloda,
        Przejscie_3_Predream2,
        Przejscie_4_zSOA,
        Przejscie_5_zSOA,
        Przejscie_6_zSOA,
        PT_A01S01_CaveCamp,
        PT_A01S01_Harbor,
        PT_A01S01_Jungle,
        PT_A01S01_TribeCamp,
        PT_A01S02_Jungle,
        PT_A01S03_Jeep,
        PT_A01S03_Jungle,
        PT_A01S05_Pond,
        PT_A01S05_River,
        PT_A01S06_Cartel,
        PT_A01S06_Jungle,
        PT_A01S07_Jungle,
        PT_A01S07_Jungle2,
        PT_A01S07_TribeVillage,
        PT_A01S08_Jungle,
        PT_A01S08_PlaneCrash,
        PT_A01S09_Elevator,
        PT_A01S09_GoldMine,
        PT_A01S10_TribeCamp,
        PT_A01S11_Jungle,
        PT_A01S11_JungleBamboo,
        PT_A01S12_RefugeeIsland,
        PT_A01S12_WHACamp,
        PT_A02S01_Airport,
        PT_A02S01_Cenote,
        PT_A02S01_Jungle,
        PT_A02S02_Cenote,
        PT_A02S02_Lake,
        PT_A03S01_Rozlewiska,
        PT_A03S01_WHACamp,
        PT_A03S02_Tutorial,
        PT_A03S03_Jungle,
        PT_A03S03_TribeVillage,
        PVE_Boat,
        PVE_Map_Crate,
        PVE_StartDebugSpawner,
        PVE2_BigAICamp_04,
        PVE2_BigAICamp_05,
        PVE2_BigAICamp_06,
        PVE2_food_ration_01,
        PVE2_food_ration_02,
        PVE2_food_ration_03,
        PVE2_food_ration_04,
        PVE2_food_ration_05,
        PVE2_food_ration_06,
        PVE2_food_ration_07,
        PVE2_food_ration_08,
        PVE2_food_ration_09,
        PVE2_food_ration_10,
        PVE2_food_ration_11,
        PVE2_food_ration_12,
        PVE2_food_ration_13,
        PVE2_food_ration_14,
        PVE2_food_ration_axe,
        PVE2_food_ration_hanging_01,
        PVE2_food_ration_hanging_02,
        PVE2_food_ration_hanging_03,
        PVE2_food_ration_hanging_04,
        PVE2_food_ration_hanging_05,
        PVE2_food_ration_hanging_06,
        PVE2_food_ration_hanging_07,
        PVE2_food_ration_hanging_08,
        PVE2_food_ration_hanging_09,
        PVE2_food_ration_hanging_10,
        PVE2_Kid_01,
        PVE2_Kid_02,
        PVE2_Kid_03,
        PVE2_Kid_04,
        PVE2_SmallAICamp_11,
        PVE2_SmallAICamp_12,
        PVE2_SmallAICamp_13,
        PVE2_SmallAICamp_14,
        PVE2_SmallAICamp_15,
        PVE2_SmallAICamp_16,
        PVE2_SmallAICamp_17,
        PVE2_SmallAICamp_18,
        PVE2_SmallAICamp_19,
        PVE2_SmallAICamp_20,
        PVE2_Wounded_01,
        PVE2_Wounded_01_cave,
        PVE2_Wounded_02,
        PVE2_Wounded_02_cave,
        PVE2_Wounded_03,
        PVE2_Wounded_03_cave,
        PVE2_Wounded_04,
        PVE2_Wounded_04_cave,
        PVE2_Wounded_05,
        PVE2_Wounded_05_cave,
        PVE3_BigAICamp_07,
        PVE3_BigAICamp_08,
        PVE3_BigAICamp_09,
        PVE3_BigAICamp_10,
        PVE3_BigAICamp_11,
        PVE3_ClimbingRope_A01_S07,
        PVE3_ClimbingRope_A01_S07_to_A04_S01b_01,
        PVE3_ClimbingRope_A01_S07_to_A04_S01b_02,
        PVE3_ClimbingRope_A01_S10,
        PVE3_ClimbingRope_A01_S10_Kid,
        PVE3_ClimbingRope_A01_S12_Mushroom,
        PVE3_ClimbingRope_A01_S12_to_A02_S01_01,
        PVE3_ClimbingRope_A01_S12_to_A02_S01_02,
        PVE3_ClimbingRope_A01_S12_to_A04_S01b_01,
        PVE3_ClimbingRope_A01_S12_to_A04_S01b_02,
        PVE3_ClimbingRope_A02_S01_to_A02_S02_01,
        PVE3_ClimbingRope_A02_S01_to_A02_S02_02,
        PVE3_ClimbingRope_A02_S01_to_A02_S02_03,
        PVE3_ClimbingRope_A02_S01_to_A02_S02_04,
        PVE3_ClimbingRope_A02_S02_to_A03_S01,
        PvE3_ClimbingRope_A03_S02,
        PVE3_ClimbingRope_A03_S03_to_A04_S01c,
        PVE3_ClimbingRope_A03_S03_to_Mushroom,
        PVE3_food_ration_hanging_01,
        PVE3_food_ration_hanging_02,
        PVE3_food_ration_hanging_03,
        PVE3_food_ration_hanging_04,
        PVE3_food_ration_hanging_05,
        PVE3_food_ration_hanging_06,
        PVE3_food_ration_hanging_07,
        PVE3_food_ration_hanging_08,
        PVE3_food_ration_hanging_09,
        PVE3_food_ration_hanging_10,
        PVE3_food_ration_hanging_11,
        PVE3_food_ration_hanging_12,
        PVE3_food_ration_hanging_13,
        PVE3_food_ration_hanging_14,
        PVE3_food_ration_mound_01,
        PVE3_food_ration_mound_02,
        PVE3_food_ration_mound_03,
        PVE3_food_ration_mound_04,
        PVE3_food_ration_mound_05,
        PVE3_food_ration_mound_06,
        PVE3_food_ration_mound_07,
        PVE3_food_ration_mound_08,
        PVE3_food_ration_mound_09,
        PVE3_food_ration_mound_10,
        PVE3_food_ration_mound_11,
        PVE3_food_ration_mound_12,
        PVE3_food_ration_mound_13,
        PVE3_food_ration_mound_14,
        PVE3_Patrol_01_C7_28,
        PVE3_Patrol_02_C7_31,
        PVE3_Patrol_03_C7_29,
        PVE3_Patrol_04_C8_26,
        PVE3_Patrol_05_C21_24,
        PVE3_Patrol_06_C22_23,
        PVE3_Patrol_07_C23_24,
        PVE3_Patrol_08_01_C27_28,
        PVE3_Patrol_08_02_C27_28,
        PVE3_Patrol_09_01_C27_30,
        PVE3_Patrol_09_02_C27_30,
        PVE3_Patrol_10_C28_29,
        PVE3_Patrol_11_01_C30_31,
        PVE3_Patrol_11_02_C30_31,
        PVE3_Patrol_12_01_C32_33,
        PVE3_Patrol_12_02_C32_33,
        PVE3_Patrol_13_01_C33_34,
        PVE3_Patrol_13_02_C33_34,
        PVE3_Patrol_14_01_C9_34,
        PVE3_Patrol_14_02_C9_34,
        PVE3_Patrol_15_01_C9_35,
        PVE3_Patrol_15_02_C9_35,
        PVE3_Patrol_16_01_C10_36,
        PVE3_Patrol_16_02_C10_36,
        PVE3_Patrol_17_01_C10_39,
        PVE3_Patrol_17_02_C10_39,
        PVE3_Patrol_18_01_C37_38,
        PVE3_Patrol_18_02_C37_38,
        PVE3_Patrol_19_01_C11_40,
        PVE3_Patrol_19_02_C11_40,
        PVE3_Patrol_20_01_C11_41,
        PVE3_Patrol_20_02_C11_41,
        PVE3_SmallAICamp_21,
        PVE3_SmallAICamp_22,
        PVE3_SmallAICamp_23,
        PVE3_SmallAICamp_24,
        PVE3_SmallAICamp_25,
        PVE3_SmallAICamp_26,
        PVE3_SmallAICamp_27,
        PVE3_SmallAICamp_28,
        PVE3_SmallAICamp_29,
        PVE3_SmallAICamp_30,
        PVE3_SmallAICamp_31,
        PVE3_SmallAICamp_32,
        PVE3_SmallAICamp_33,
        PVE3_SmallAICamp_34,
        PVE3_SmallAICamp_35,
        PVE3_SmallAICamp_36,
        PVE3_SmallAICamp_37,
        PVE3_SmallAICamp_38,
        PVE3_SmallAICamp_39,
        PVE3_SmallAICamp_40,
        PVE3_SmallAICamp_41,
        PVE3_SmallAICamp_42,
        QA_Test,
        SmallAICamp_01,
        SmallAICamp_01_outer,
        SmallAICamp_02,
        SmallAICamp_03,
        SmallAICamp_04,
        SmallAICamp_05,
        SmallAICamp_06,
        SmallAICamp_07,
        SmallAICamp_08,
        SmallAICamp_09,
        SmallAICamp_10,
        Spikes_Tree_Review_Spawner,
        Statue_01,
        Statue_02,
        Statue_03,
        Statue_04,
        Statue_05,
        Statue_06,
        Statue_07,
        Village_02_inside,
        Village_02_outside,
        Village_03_fishing_inside,
        Village_03_fishing_outside,
        Village_03_fishing_QuestGiver,
        Village_Yabahuaca_inside,
        Village_Yabahuaca_outside,
        WomanInCage_01,
        WomanInCage_02,
        WomanInCage_03,
        Wounded_01,
        Wounded_02,
        Wounded_03,
        Wounded_04,
        Wounded_05,
        Teleport_Start_Location,
        Bamboo_Bridge,
        Anaconda_Island,
        East_Native_Camp,
        Elevator_Cave,
        Planecrash_Cave,
        Native_Passage,
        Overturned_Jeep,
        Abandoned_Tribal_Village,
        West_Native_Camp,
        Pond,
        Puddle,
        Harbor,
        Drug_Facility,
        Bamboo_Camp,
        Scorpion_Cartel_Cave,
        Airport,
        Main_Tribal_Village,
        Omega_Camp,
        Tutorial_Camp,
        Story_Start_Oasis,
        Custom
    }
    /// <summary>
    /// Enumerates event identifiers
    /// </summary>
    public enum EventID
    {
        NoneEnabled = 0,
        ModsAndCheatsNotEnabled = 1,
        EnableDebugModeNotEnabled = 2,
        ModsAndCheatsEnabled = 4,
        EnableDebugModeEnabled = 16,
        AllEnabled = 32
    }
    /// <summary>
    /// Enumerates message types
    /// </summary>
    public enum MessageType
    {
        Info,
        Warning,
        Error
    }
    /// <summary>
    /// Enumerates ModAPI supported game identifiers.
    /// </summary>
    public enum GameID
    {
        EscapeThePacific,
        GreenHell,
        SonsOfTheForest,
        Subnautica,
        TheForest,
        TheForestDedicatedServer,
        TheForestVR
    }
}

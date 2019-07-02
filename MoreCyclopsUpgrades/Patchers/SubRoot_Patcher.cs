﻿namespace MoreCyclopsUpgrades.Patchers
{
    using Harmony;
    using Managers;

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("UpdateThermalReactorCharge")]
    internal class SubRoot_UpdateThermalReactorCharge_Patcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref SubRoot __instance)
        {
            bool requiresVanillaCharging = CyclopsManager.GetManager(__instance).Charge.RechargeCyclops();

            // If there is no mod taking over how thermal charging is done on the Cyclops,
            // then we will allow the original method to run so it provides the vanilla thermal charging.            
            return requiresVanillaCharging;
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("UpdatePowerRating")]
    internal class SubRoot_UpdatePowerRating_Patcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref SubRoot __instance)
        {
            // Performing this custom handling was necessary as UpdatePowerRating wouldn't work with the AuxUpgradeConsole
            CyclopsManager.GetManager(__instance).Engine.UpdatePowerRating();

            return false; // Completely override the method and do not continue with original execution
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("SetCyclopsUpgrades")]
    internal class SubRoot_SetCyclopsUpgrades_Patcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref SubRoot __instance)
        {
            LiveMixin cyclopsLife = __instance.live;

            if (cyclopsLife == null || !cyclopsLife.IsAlive())
                return true; // safety check

            CyclopsManager.GetManager(__instance).Upgrade.HandleUpgrades();

            // No need to execute original method anymore
            return false; // Completely override the method and do not continue with original execution
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("SetExtraDepth")]
    internal class SubRoot_SetExtraDepth_Patcher
    {
        [HarmonyPrefix]
        public static bool Prefix(ref SubRoot __instance)
        {
            // Providing this custom handler is necessary as SetExtraDepth wouldn't work with the AuxUpgradeConsole
            return false; // Now handled by UpgradeManager HandleUpgrades
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("OnPlayerEntered")]
    internal class SubRoot_OnPlayerEntered_BeQuiet
    {
        private static bool firstEventDone = false;

        [HarmonyPrefix]
        public static void Prefix(ref SubRoot __instance)
        {
            if (firstEventDone)
                return;

            __instance.voiceNotificationManager.ready = false;
        }

        [HarmonyPostfix]
        public static void Postfix(ref SubRoot __instance)
        {
            if (firstEventDone)
                return;

            __instance.voiceNotificationManager.ready = true;
            firstEventDone = true;
        }
    }

    [HarmonyPatch(typeof(SubRoot))]
    [HarmonyPatch("PowerDownCyclops")]
    internal class SubRoot_PowerDownCyclops_TurnOffSilentRunning
    {
        [HarmonyPostfix]
        public static void PostFix(ref SubRoot __instance)
        {
            // Turns this off for people who forget to turn off Silent Running when they power down the Cyclops
            __instance.silentRunning = false;
        }
    }
}

﻿namespace MoreCyclopsUpgrades
{
    using System.Reflection;

    /// <summary>
    /// Power Index 0: No efficiency modules equipped.
    /// Power Index 1: Standard PowerUpgradeModule equipped.
    /// Power Index 2: PowerUpgradeModuleMk2 equipped.
    /// Power Index 3: PowerUpgradeModuleMk3 equipped.
    /// </summary>    
    internal static class PowerIndexManager
    {
        private static readonly float[] EnginePowerRatings = new[]
        {
            1f, // Power Index 0: Base Value
            3f, // Power Index 1: 300% increase
            5f, // Power Index 2: 500% increase
            6f  // Power Index 3: 600% increase
        };

        private static readonly float[] SilentRunningPowerCosts = new[]
        {
            5f, // Power Index 0: Base Value
            5f, // Power Index 1: Base Value
            4f, // Power Index 2: 20% cost reduction
            3f  // Power Index 3: 40% cost reduction
        };

        private static readonly float[] SonarPowerCosts = new[]
        {
            10f, // Power Index 0: Base Value
            10f, // Power Index 1: Base Value
            8f,  // Power Index 2: 20% cost reduction
            6f   // Power Index 3: 40% cost reduction
        };

        private static readonly float[] ShieldPowerCosts = new[]
        {
            50f, // Power Index 0: Base Value
            50f, // Power Index 1: Base Value
            42f, // Power Index 2: 16% cost reduction
            35f  // Power Index 3: 30% cost reduction
        };

        internal static float GetCyclopsPowerRating(ref SubRoot __instance)
        {
            FieldInfo fieldInfo = typeof(SubRoot).GetField("currPowerRating", BindingFlags.NonPublic | BindingFlags.Instance);
            return (float)fieldInfo.GetValue(__instance);
        }

        internal static void SetCyclopsPowerRating(ref SubRoot __instance, float rating)
        {
            FieldInfo fieldInfo = typeof(SubRoot).GetField("currPowerRating", BindingFlags.NonPublic | BindingFlags.Instance);
            fieldInfo.SetValue(__instance, rating);
        }

        internal static void UpdatePowerIndex(ref SubRoot __instance, float originalRating)
        {
            Equipment modules = __instance.upgradeConsole.modules;

            int powerIndex = GetPowerIndex(modules);

            __instance.silentRunningPowerCost = SilentRunningPowerCosts[powerIndex];
            __instance.sonarPowerCost = SonarPowerCosts[powerIndex];
            __instance.shieldPowerCost = ShieldPowerCosts[powerIndex];

            float nextPowerRating = EnginePowerRatings[powerIndex];
            
            if (originalRating != nextPowerRating)
            {
                SetCyclopsPowerRating(ref __instance, nextPowerRating);
                // Inform the new power rating just like the original method would.
                string format = Language.main.GetFormat("PowerRatingNowFormat", nextPowerRating);
                ErrorMessage.AddMessage(format);
            }
        }

        private static int GetPowerIndex(Equipment modules)
        {
            int powerIndex = 0;

            if (modules.GetCount(TechType.PowerUpgradeModule) > 0)
            {
                powerIndex = 1;
            }

            if (modules.GetCount(PowerUpgradeMk2.Power2TechType) > 0)
            {
                powerIndex = 2;
            }

            if (modules.GetCount(PowerUpgradeMk3.Power3TechType) > 0)
            {
                powerIndex = 3;
            }

            return powerIndex;
        }
    }
}
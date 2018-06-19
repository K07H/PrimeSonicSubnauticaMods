namespace UpgradedVehicles
{
    using Harmony;

    [HarmonyPatch(typeof(Vehicle))]
    [HarmonyPatch("OnUpgradeModuleChange")]
    internal class Vehicle_OnUpgradeModuleChange_Patcher
    {
        public static void Postfix(Vehicle __instance)
        {
            VehicleUpgrader.UpgradeVehicle(__instance);
        }        
    }

    [HarmonyPatch(typeof(Vehicle))]
    [HarmonyPatch("GetStorageInSlot")]
    internal class Vehicle_GetStorageInSlot_Patcher
    {
        public static bool Prefix(ref Vehicle __instance, int slotID, TechType techType, ref ItemsContainer __result)
        {
            if (techType == TechType.VehicleStorageModule)
            {
                var deluxeStorage = __instance.GetComponent<SeaMothStorageDeluxe>();

                if (deluxeStorage == null)
                    return true;

                __result = deluxeStorage.GetStorageInSlot(slotID);
                return false;
            }

            return true;
        }
    }

}

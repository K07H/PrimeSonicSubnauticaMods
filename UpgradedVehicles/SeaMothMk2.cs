﻿namespace UpgradedVehicles
{
    using System.Collections.Generic;
    using SMLHelper;
    using SMLHelper.Patchers;
    using UnityEngine;

    internal class SeaMothMk2
    {
        public static TechType TechTypeID { get; private set; }
        public const string NameID = "SeaMothMk2";
        public const string FriendlyName = "Seamoth Mk2";
        public const string Description = "An upgraded SeaMoth ready to take you anywhere.";

        public static void Patch()
        //AssetBundle assetBundle)
        {
            TechTypeID = TechTypePatcher.AddTechType(NameID, FriendlyName, Description, unlockOnGameStart: true);

            CustomPrefabHandler.customPrefabs.Add(new CustomPrefab(NameID, $"WorldEntities/Tools/{NameID}", TechTypeID, GetGameObject));

            CustomSpriteHandler.customSprites.Add(new CustomSprite(TechTypeID, SpriteManager.Get(TechType.Seamoth)));
            //assetBundle.LoadAsset<Sprite>("ICON")));

            CraftTreePatcher.customNodes.Add(new CustomCraftNode(TechTypeID, CraftTree.Type.Constructor, $"Vehicles/{NameID}"));

            CraftDataPatcher.customTechData[TechTypeID] = GetRecipe();
        }

        private static TechDataHelper GetRecipe()
        {
            return new TechDataHelper()
            {
                _craftAmount = 1,
                _ingredients = new List<IngredientHelper>(new IngredientHelper[9]
                             {
                                 new IngredientHelper(TechType.PlasteelIngot, 1), // Stronger than titanium ingot
                                 new IngredientHelper(TechType.PowerCell, 1), // Same
                                 new IngredientHelper(TechType.EnameledGlass, 2), // Stronger than glass
                                 new IngredientHelper(TechType.Lead, 1), // Same
                                 new IngredientHelper(TechType.Benzene, 1), // More oily than lubricant
                                 new IngredientHelper(TechType.VehicleHullModule3, 1), // Minimum crush depth of 900 without upgrades
                                 new IngredientHelper(TechType.VehiclePowerUpgradeModule, 2), // Minimum of +2 to engine eficiency
                                 new IngredientHelper(TechType.VehicleArmorPlating, 2), // Minimum of +2 to armor                                 
                                 new IngredientHelper(TechType.VehicleStorageModule, 4), // All four storage modules always on
                             }),
                _techType = TechTypeID
            };
        }

        private static GameObject GetGameObject()
        {
            GameObject prefab = Resources.Load<GameObject>("WorldEntities/Tools/SeaMoth");
            GameObject obj = GameObject.Instantiate(prefab);

            obj.name = NameID;

            obj.GetComponent<PrefabIdentifier>().ClassId = NameID;
            obj.GetComponent<TechTag>().type = TechTypeID;

            var seamoth = obj.GetComponent<SeaMoth>();

            var life = seamoth.GetComponent<LiveMixin>();
            life.data.maxHealth = VehicleUpgrader.SeaMothMk2HP; // Double the normal health but still less than the ExoSuit's 600
            life.health = VehicleUpgrader.SeaMothMk2HP;

            var deluxeStorage = obj.AddComponent<SeaMothStorageDeluxe>();

            // Always on upgrades handled in OnUpgradeModuleChange patch

            return obj;
        }
    }
}

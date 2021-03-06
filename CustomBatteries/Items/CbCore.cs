﻿namespace CustomBatteries.Items
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Common;
    using CustomBatteries.API;
    using SMLHelper.V2.Assets;
    using SMLHelper.V2.Crafting;
    using SMLHelper.V2.Handlers;
    using SMLHelper.V2.Utility;
    using UnityEngine;
#if SUBNAUTICA
    using RecipeData = SMLHelper.V2.Crafting.TechData;
    using Sprite = Atlas.Sprite;
#endif

    internal abstract class CbCore : ModPrefab
    {
        internal const string BatteryCraftTab = "BatteryTab";
        internal const string PowCellCraftTab = "PowCellTab";
        internal const string ElecCraftTab = "Electronics";
        internal const string ResCraftTab = "Resources";

        private static readonly WorldEntitiesCache worldEntities = new WorldEntitiesCache();

        internal static readonly string[] BatteryCraftPath = new[] { ResCraftTab, BatteryCraftTab };
        internal static readonly string[] PowCellCraftPath = new[] { ResCraftTab, PowCellCraftTab };

        public static string ExecutingFolder { get; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static List<CbCore> BatteryItems { get; } = new List<CbCore>();

        internal static Dictionary<TechType, Texture2D> BatteryModels { get; } = new Dictionary<TechType, Texture2D>();

        public static List<CbCore> PowerCellItems { get; } = new List<CbCore>();

        internal static Dictionary<TechType, Texture2D> PowerCellModels { get; } = new Dictionary<TechType, Texture2D>();

        protected abstract TechType PrefabType { get; } // Should only ever be Battery or PowerCell
        protected abstract EquipmentType ChargerType { get; } // Should only ever be BatteryCharger or PowerCellCharger

        public TechType RequiredForUnlock { get; set; } = TechType.None;
        public bool UnlocksAtStart => this.RequiredForUnlock == TechType.None;

        public virtual RecipeData GetBlueprintRecipe()
        {
            var partsList = new List<Ingredient>();

            CreateIngredients(this.Parts, partsList);

            if (partsList.Count == 0)
                partsList.Add(new Ingredient(TechType.Titanium, 1));

            var batteryBlueprint = new RecipeData
            {
                craftAmount = 1,
                Ingredients = partsList
            };

            return batteryBlueprint;
        }

        public float PowerCapacity { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public string IconFileName { get; set; }

        public string PluginPackName { get; set; }

        public string PluginFolder { get; set; }

        public Sprite Sprite { get; set; }

        public IList<TechType> Parts { get; set; }

        public bool IsPatched { get; private set; }

        public bool UsingIonCellSkins { get; }

        public Texture2D CustomSkin { get; set; }

        public bool ExcludeFromChargers { get; set; }

        protected Action<GameObject> EnhanceGameObject { get; set; }

        protected CbCore(string classId, bool ionCellSkins)
            : base(classId, $"{classId}PreFab", TechType.None)
        {
            this.UsingIonCellSkins = ionCellSkins;
        }

        protected CbCore(CbItem packItem)
            : base(packItem.ID, $"{packItem.ID}PreFab", TechType.None)
        {
            this.UsingIonCellSkins = packItem.CustomSkin == null;

            if (packItem.CustomIcon != null)
                this.Sprite = packItem.CustomIcon;

            if (packItem.CustomSkin != null)
                this.CustomSkin = packItem.CustomSkin;

            this.ExcludeFromChargers = packItem.ExcludeFromChargers;

            this.EnhanceGameObject = packItem.EnhanceGameObject;
        }

        public override GameObject GetGameObject()
        {
            GameObject prefab = CraftData.GetPrefabForTechType(this.PrefabType);
            var obj = GameObject.Instantiate(prefab);

            Battery battery = obj.GetComponent<Battery>();
            battery._capacity = this.PowerCapacity;
            battery.name = $"{this.ClassID}BatteryCell";

            // Add the component that will readjust position.
            if (ChargerType == EquipmentType.PowerCellCharger)
                obj.AddComponent<CustomPowerCellPlaceTool>();
            else
                obj.AddComponent<CustomBatteryPlaceTool>();
            // Make item placeable.
            AddPlaceTool(obj);

            if (this.CustomSkin != null)
            {
                MeshRenderer meshRenderer = obj.GetComponentInChildren<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.material.mainTexture = this.CustomSkin;

                SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                    skinnedMeshRenderer.material.mainTexture = this.CustomSkin;
            }

            this.EnhanceGameObject?.Invoke(obj);

            return obj;
        }

        protected void CreateIngredients(IEnumerable<TechType> parts, List<Ingredient> partsList)
        {
            if (parts == null)
                return;

            foreach (TechType part in parts)
            {
                if (part == TechType.None)
                {
                    QuickLogger.Warning($"Parts list for '{this.ClassID}' contained an unidentified TechType");
                    continue;
                }

                Ingredient priorIngredient = partsList.Find(i => i.techType == part);

                if (priorIngredient != null)
#if SUBNAUTICA
                    priorIngredient.amount++;
#elif BELOWZERO
                    priorIngredient._amount++;
#endif
                else
                    partsList.Add(new Ingredient(part, 1));
            }
        }

        protected abstract void AddToList();

        protected abstract string[] StepsToFabricatorTab { get; }

        public void Patch()
        {
            if (this.IsPatched)
                return;

            this.TechType = TechTypeHandler.AddTechType(this.ClassID, this.FriendlyName, this.Description, this.UnlocksAtStart);

            if (this.CustomSkin != null)
            {
                if (this.ChargerType == EquipmentType.BatteryCharger && !BatteryModels.ContainsKey(this.TechType))
                {
                    BatteryModels.Add(this.TechType, this.CustomSkin);
                }
                else if (this.ChargerType == EquipmentType.PowerCellCharger && !PowerCellModels.ContainsKey(this.TechType))
                {
                    PowerCellModels.Add(this.TechType, this.CustomSkin);
                }
            }
            else if (this.UsingIonCellSkins)
            {
                if (this.ChargerType == EquipmentType.BatteryCharger)
                {
                    GameObject battery = worldEntities.IonBattery();
                    Texture2D texture = battery?.GetComponentInChildren<MeshRenderer>()?.material?.GetTexture(ShaderPropertyID._MainTex) as Texture2D;
                    if (texture != null)
                    {
                        BatteryModels.Add(this.TechType, texture);
                    }
                }
                else if (this.ChargerType == EquipmentType.PowerCellCharger)
                {
                    GameObject battery = worldEntities.IonPowerCell();
                    Texture2D texture = battery?.GetComponentInChildren<MeshRenderer>()?.material?.GetTexture(ShaderPropertyID._MainTex) as Texture2D;
                    if (texture != null)
                    {
                        BatteryModels.Add(this.TechType, texture);
                    }
                }
            }
            else
            {
                if (this.ChargerType == EquipmentType.BatteryCharger)
                {
                    GameObject battery = worldEntities.Battery();
                    Texture2D texture = battery?.GetComponentInChildren<MeshRenderer>()?.material?.GetTexture(ShaderPropertyID._MainTex) as Texture2D;
                    if (texture != null)
                    {
                        BatteryModels.Add(this.TechType, texture);
                    }
                }
                else if (this.ChargerType == EquipmentType.PowerCellCharger)
                {
                    GameObject battery = worldEntities.PowerCell();
                    Texture2D texture = battery?.GetComponentInChildren<MeshRenderer>()?.material?.GetTexture(ShaderPropertyID._MainTex) as Texture2D;
                    if (texture != null)
                    {
                        BatteryModels.Add(this.TechType, texture);
                    }
                }
            }

            if (!this.UnlocksAtStart)
                KnownTechHandler.SetAnalysisTechEntry(this.RequiredForUnlock, new TechType[] { this.TechType });

            if (this.Sprite == null)
            {
                string imageFilePath = IOUtilities.Combine(ExecutingFolder, this.PluginFolder, this.IconFileName);

                if (File.Exists(imageFilePath))
                    this.Sprite = ImageUtils.LoadSpriteFromFile(imageFilePath);
                else
                {
                    QuickLogger.Warning($"Did not find a matching image file at {imageFilePath}.{Environment.NewLine}Using default sprite instead.");
                    this.Sprite = SpriteManager.Get(this.PrefabType);
                }
            }

            SpriteHandler.RegisterSprite(this.TechType, this.Sprite);

            CraftDataHandler.SetTechData(this.TechType, GetBlueprintRecipe());

            CraftDataHandler.AddToGroup(TechGroup.Resources, TechCategory.Electronics, this.TechType);

            bool enablePlaceBatteriesFeature = false;
            QModManager.API.IQMod decorationsMod = QModManager.API.QModServices.Main.FindModById("DecorationsMod");
            if (decorationsMod != null && decorationsMod.Enable && decorationsMod.LoadedAssembly != null)
            {
                Type decorationsModConfig = decorationsMod.LoadedAssembly.GetType("DecorationsMod.ConfigSwitcher", false);
                if (decorationsModConfig != null)
                {
                    FieldInfo enablePlaceBatteriesField = decorationsModConfig.GetField("EnablePlaceBatteries", BindingFlags.Public | BindingFlags.Static);
                    if (enablePlaceBatteriesField != null)
                        enablePlaceBatteriesFeature = (bool)enablePlaceBatteriesField.GetValue(null);
                }
            }
            if (enablePlaceBatteriesFeature)
                CraftDataHandler.SetEquipmentType(this.TechType, EquipmentType.Hand);
            else
                CraftDataHandler.SetEquipmentType(this.TechType, this.ChargerType);
            
            CraftDataHandler.SetQuickSlotType(this.TechType, QuickSlotType.Selectable); // We can select the item.

            CraftTreeHandler.AddCraftingNode(CraftTree.Type.Fabricator, this.TechType, this.StepsToFabricatorTab);

            PrefabHandler.RegisterPrefab(this);

            AddToList();

            this.IsPatched = true;
        }

        private static void AddPlaceTool(GameObject customBattery)
        {
            PlaceTool placeTool = customBattery.AddComponent<PlaceTool>();
            placeTool.allowedInBase = true;
            placeTool.allowedOnBase = true;
            placeTool.allowedOnConstructable = true;
            placeTool.allowedOnGround = true;
            placeTool.allowedOnRigidBody = true;
            placeTool.allowedOutside = true;
#if BELOWZERO
            placeTool.allowedUnderwater = true;
#endif
            placeTool.allowedOnCeiling = false;
            placeTool.allowedOnWalls = false;
            placeTool.reloadMode = PlayerTool.ReloadMode.None;
            placeTool.socket = PlayerTool.Socket.RightHand;
            placeTool.rotationEnabled = true;
            placeTool.drawTime = 0.5f;
            placeTool.dropTime = 1f;
            placeTool.holsterTime = 0.35f;
            // Associate collider
            Collider mainCollider = customBattery.GetComponent<Collider>() ?? customBattery.GetComponentInChildren<Collider>();
            if (mainCollider != null)
                placeTool.mainCollider = mainCollider;
            // Associate pickupable
            placeTool.pickupable = customBattery.GetComponent<Pickupable>();
        }
    }
}

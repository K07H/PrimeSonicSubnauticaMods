﻿namespace CustomCraft2SML.Serialization.Entries
{
    using Common;
    using Common.EasyMarkup;
    using CustomCraft2SML.Interfaces;
    using CustomCraft2SML.Interfaces.InternalUse;
    using CustomCraft2SML.PublicAPI;
    using SMLHelper.V2.Handlers;
    using SMLHelper.V2.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using IOPath = System.IO.Path;

    internal class CustomCraftingTab : EmPropertyCollection, ICraftingTab, ICustomCraft
    {
        public string[] TutorialText => CustomCraftingTabTutorial;

        internal static readonly string[] CustomCraftingTabTutorial = new[]
        {
            "CustomCraftingTab: Add your own custom tabs into the fabricator crafting trees. ",
            "    An absolute must for organizing large numbers of crafts.",
           $"    {TabIdKey}: This uniquely identifies the tab.",
           $"        If you want to use a custom sprite for your tab, the file must be named exactly as the {TabIdKey}",
           $"        This option will take priority over the {SpriteItemIdKey}.",
           $"    {DisplayNameKey}: The tab name you will see in-game.",
           $"    {SpriteItemIdKey}: Alternative way to set the tab sprite, by re-using the sprite of an existing in-game item.",
           $"        This option will be used only if a png file matching the {TabIdKey} isn't found in the Assets folder.",
           $"    {ParentTabPathKey}: This defines where your tab begins on the crafting tree.",
            "        You can have as many custom tabs as you want, and even include custom tabs inside other custom tabs.",
            "        Just make sure you add your custom tabs to the file in the correct order, from inside to outside.",
            "        If a custom tab goes inside another custom tab, then the parent tab must be placed above the child tab.",
            "        You can find a full list of all original crafting paths for all the standard fabricators in the OriginalRecipes folder.",
        };

        public const string TypeName = "CustomTab";
        protected const string TabIdKey = "TabID";
        protected const string DisplayNameKey = "DisplayName";
        protected const string SpriteItemIdKey = "SpriteItemID";
        protected const string ParentTabPathKey = "ParentTabPath";

        protected readonly EmProperty<string> emTabID;
        protected readonly EmProperty<string> emDisplayName;
        protected readonly EmProperty<TechType> emSpriteID;
        protected readonly EmProperty<string> emParentTabPath;

        protected CraftingPath craftingPath;

        protected static ICollection<EmProperty> CustomCraftingTabProperties => new List<EmProperty>(4)
        {
            new EmProperty<string>(TabIdKey),
            new EmProperty<string>(DisplayNameKey),
            new EmProperty<TechType>(SpriteItemIdKey) { Optional = true },
            new EmProperty<string>(ParentTabPathKey),
        };

        public CustomCraftingTab() : this(TypeName, CustomCraftingTabProperties)
        {
        }

        protected CustomCraftingTab(string key, ICollection<EmProperty> definitions) : base(key, definitions)
        {
            emTabID = (EmProperty<string>)Properties[TabIdKey];
            emDisplayName = (EmProperty<string>)Properties[DisplayNameKey];
            emSpriteID = (EmProperty<TechType>)Properties[SpriteItemIdKey];
            emParentTabPath = (EmProperty<string>)Properties[ParentTabPathKey];

            base.OnValueExtractedEvent += ParsePath;
        }

        public OriginFile Origin { get; set; }

        internal CustomCraftingTab(string path) : this()
        {
            this.ParentTabPath = path;
            ParsePath();
        }

        protected void ParsePath()
        {
            try
            {
                craftingPath = new CraftingPath(this.ParentTabPath.TrimEnd(CraftingPath.Separator));
            }
            catch
            {
                craftingPath = null;
            }
        }

        public string ID => this.TabID;

        public string TabID
        {
            get => emTabID.Value;
            set => emTabID.Value = value;
        }

        public string DisplayName
        {
            get => emDisplayName.Value;
            set => emDisplayName.Value = value;
        }

        public virtual CraftTree.Type FabricatorType
        {
            get
            {
                if (craftingPath is null)
                    return CraftTree.Type.None;

                return craftingPath.Scheme;
            }
        }

        public TechType SpriteItemID
        {
            get => emSpriteID.Value;
            set => emSpriteID.Value = value;
        }

        public string ParentTabPath
        {
            get => emParentTabPath.Value;
            set => emParentTabPath.Value = value;
        }

        public string[] StepsToTab
        {
            get
            {
                if (craftingPath is null)
                    return null;

                return craftingPath.Steps;
            }
        }

        public string FullPath => $"{this.ParentTabPath}{"/"}{this.TabID}";

        internal override EmProperty Copy()
        {
            return new CustomCraftingTab(this.Key, this.CopyDefinitions);
        }

        public bool PassesPreValidation()
        {
            return craftingPath != null & ValidFabricator();
        }

        protected virtual bool ValidFabricator()
        {
            if (this.FabricatorType > CraftTree.Type.Rocket)
            {
                QuickLogger.Error($"Error on crafting tab '{this.TabID}'. This API in intended only for use with standard, non-modded CraftTree.Types.");
                return false;
            }

            if (this.FabricatorType == CraftTree.Type.None)
            {
                QuickLogger.Error($"Error on crafting tab '{this.TabID}'. {ParentTabPathKey} must identify a fabricator for the custom tab.");
                return false;
            }

            return true;
        }

        public virtual bool SendToSMLHelper()
        {
            try
            {
                HandleCraftingTab();

                return true;
            }
            catch (Exception ex)
            {
                QuickLogger.Error($"Exception thrown while handling {this.Key} '{this.TabID}' from {this.Origin}", ex);
                return false;
            }
        }

        protected void HandleCraftingTab()
        {
            Atlas.Sprite sprite = GetCraftingTabSprite();

            if (this.StepsToTab == null)
            {
                CraftTreeHandler.AddTabNode(this.FabricatorType, this.TabID, this.DisplayName, sprite);
            }
            else
            {
                CraftTreeHandler.AddTabNode(this.FabricatorType, this.TabID, this.DisplayName, sprite, this.StepsToTab);
            }
        }

        protected Atlas.Sprite GetCraftingTabSprite()
        {
            string imagePath = IOPath.Combine(FileLocations.AssetsFolder, this.TabID + @".png");

            if (File.Exists(imagePath))
            {
                QuickLogger.Debug($"Custom sprite found in Assets folder for {this.Key} '{this.TabID}' from {this.Origin}");
                return ImageUtils.LoadSpriteFromFile(imagePath);
            }

            if (this.SpriteItemID != TechType.None)
            {
                QuickLogger.Debug($"SpriteItemID used for {this.Key} '{this.TabID}' from {this.Origin}");
                return SpriteManager.Get(this.SpriteItemID);
            }

            QuickLogger.Warning($"No sprite loaded for {this.Key} '{this.TabID}' from {this.Origin}");
            return SpriteManager.Get(TechType.None);
        }
    }
}
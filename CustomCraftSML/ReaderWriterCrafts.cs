﻿namespace CustomCraft2SML
{
    using System.Collections.Generic;
    using System.IO;
    using Common;
    using Common.EasyMarkup;
    using CustomCraft2SML.Interfaces;
    using CustomCraft2SML.PublicAPI;
    using CustomCraft2SML.Serialization;
    using UnityEngine.Assertions;

    internal static partial class FileReaderWriter
    {
        private const string WorkingFolder = FolderRoot + "WorkingFiles/";
        private const string CustomSizesFile = WorkingFolder + "CustomSizes.txt";
        private const string ModifiedRecipesFile = WorkingFolder + "ModifiedRecipes.txt";
        private const string AddedRecipiesFile = WorkingFolder + "AddedRecipes.txt";
        private const string CustomBioFuelsFile = WorkingFolder + "CustomBioFuels.txt";

        //  Initial storage for the serialization - key is string as we have not resolved the TechType at this point
        private static List<AddedRecipe> addedRecipes = new List<AddedRecipe>();
        private static List<AliasRecipe> aliasRecipes = new List<AliasRecipe>();
        private static List<ModifiedRecipe> modifiedRecipes = new List<ModifiedRecipe>();
        private static List<CustomSize> customSizes = new List<CustomSize>();
        private static List<CustomBioFuel> customBioFuels = new List<CustomBioFuel>();

        //  Crafting tabs to not use TechType for key - store these by name
        private static readonly IDictionary<string, CustomCraftingTab> customTabs = new Dictionary<string, CustomCraftingTab>();

        //  After the prepass - we have resolved the TechType and filtered out duplicates.
        private static IDictionary<TechType, AddedRecipe> uniqueAddedRecipes = new Dictionary<TechType, AddedRecipe>();
        private static IDictionary<TechType, AliasRecipe> uniqueAliasRecipes = new Dictionary<TechType, AliasRecipe>();
        private static IDictionary<TechType, ModifiedRecipe> uniqueModifiedRecipes = new Dictionary<TechType, ModifiedRecipe>();
        private static IDictionary<TechType, CustomSize> uniqueCustomSizes = new Dictionary<TechType, CustomSize>();
        private static IDictionary<TechType, CustomBioFuel> uniqueCustomBioFuels = new Dictionary<TechType, CustomBioFuel>();

        private static void HandleWorkingFiles()
        {
            ICollection<string> workingFiles = new List<string>(Directory.GetFiles(WorkingFolder));

            foreach (string file in workingFiles)
                DeserializeFile(file);

            PrePassSMLHelper(addedRecipes, ref uniqueAddedRecipes);
            PrePassSMLHelper(aliasRecipes, ref uniqueAliasRecipes);
            PrePassSMLHelper(modifiedRecipes, ref uniqueModifiedRecipes);
            PrePassSMLHelper(customSizes, ref uniqueCustomSizes);
            PrePassSMLHelper(customBioFuels, ref uniqueCustomBioFuels);

            SendToSMLHelper(customTabs);
            SendToSMLHelper(uniqueAddedRecipes);
            SendToSMLHelper(uniqueAliasRecipes);
            SendToSMLHelper(uniqueModifiedRecipes);
            SendToSMLHelper(uniqueCustomSizes);
            SendToSMLHelper(uniqueCustomBioFuels);
        }

        private static void CreateEmptyFile<T>(string filePath) where T : EmProperty, ITutorialText, new()
        {
            T emptyList = new T();

            List<string> tutorialText = emptyList.TutorialText;

            tutorialText.Add(emptyList.PrettyPrint());

            File.WriteAllLines(filePath, tutorialText.ToArray());
        }

        private static void DeserializeFile(string workingFilePath)
        {
            QuickLogger.Message($"Reading file: {workingFilePath}");

            string serializedData = File.ReadAllText(workingFilePath);

            if (string.IsNullOrEmpty(serializedData))
            {
                QuickLogger.Warning($"File contained no text");
                return;
            }

            if (EmProperty.CheckKey(serializedData, out string key))
            {
                int check = -2;
                switch (key)
                {
                    case "AddedRecipes":
                        check = ParseEntries<AddedRecipe, AddedRecipeList>(serializedData, ref addedRecipes);
                        break;

                    case "AliasRecipes":
                        check = ParseEntries<AliasRecipe, AliasRecipeList>(serializedData, ref aliasRecipes);
                        break;

                    case "ModifiedRecipes":
                        check = ParseEntries<ModifiedRecipe, ModifiedRecipeList>(serializedData, ref modifiedRecipes);
                        break;

                    case "CustomSizes":
                        check = ParseEntries<CustomSize, CustomSizeList>(serializedData, ref customSizes);
                        break;

                    case "CustomBioFuels":
                        check = ParseEntries<CustomBioFuel, CustomBioFuelList>(serializedData, ref customBioFuels);
                        break;

                    case "CustomCraftingTabs":
                        check = ParseEntries<CustomCraftingTab, CustomCraftingTabList>(serializedData, customTabs);
                        break;

                    default:
                        QuickLogger.Error($"Invalid primary key '{key}' detected in file");
                        return;
                }

                switch (check)
                {
                    case -1:
                        QuickLogger.Error($"Unable to parse file");
                        break;
                    case 0:
                        QuickLogger.Message($"File was parsed but no entries were found");
                        break;
                    default:
                        QuickLogger.Message($"{check} entries parsed from file");
                        break;
                }
            }
            else
            {
                QuickLogger.Warning("Could not identify primary key in file");
            }
        }

        private static int ParseEntries<T, T2>(string serializedData, ref List<T> parsedItems)
            where T : EmPropertyCollection, ITechTyped
            where T2 : EmPropertyCollectionList<T>, new()
        {
            T2 list = new T2();

            Assert.AreEqual(typeof(T), list.ItemType);

            bool successfullyParsed = list.Deserialize(serializedData);

            if (!successfullyParsed)
                return -1; // Error case

            if (list.Count == 0)
                return 0; // No entries

            int count = 0;
            foreach (T item in list)
            {
                parsedItems.Add(item);
                count++;
            }

            return count; // Return the number of unique entries added in this list
        }

        private static int ParseEntries<T, T2>(string serializedData, IDictionary<string, T> parsedItems)
            where T : EmPropertyCollection, ICraftingTab
            where T2 : EmPropertyCollectionList<T>, new()
        {
            T2 list = new T2();

            Assert.AreEqual(typeof(T), list.ItemType);

            bool successfullyParsed = list.Deserialize(serializedData);

            if (!successfullyParsed)
                return -1; // Error case

            if (list.Count == 0)
                return 0; // No entries

            int unique = 0;
            foreach (T item in list)
            {
                if (parsedItems.ContainsKey(item.TabID))
                {
                    QuickLogger.Warning($"Duplicate entry for '{item.TabID}' in '{list.Key}' was already added by another working file. Kept first one. Discarded duplicate.");
                }
                else
                {
                    parsedItems.Add(item.TabID, item);
                    unique++;
                }
            }

            return unique++; // Return the number of unique entries added in this list
        }

        private static void PrePassSMLHelper<T>(List<T> entries, ref IDictionary<TechType, T> uniqueEntries)
            where T : ITechTyped
        {
            int successCount = 0;
            //  Use the ToSet function as a copy constructor - this way we can iterate across the
            //      temp structure, but change the permanent one in the case of duplicates
            foreach (var item in entries)
            {
                TechType rv = CustomCraft.PrePass(item);
                if (uniqueEntries.ContainsKey(rv))
                {
                    QuickLogger.Warning($"Duplicate entry for '{rv}' was already added by another working file. Kept first one. Discarded duplicate.");
                }
                else
                {
                    uniqueEntries.Add(rv, item);
                }
            }
        }

        private static void SendToSMLHelper<T>(IDictionary<TechType, T> uniqueEntries)
        where T : ITechTyped
        {
            int successCount = 0;
            foreach (T item in uniqueEntries.Values)
            {
                bool result = CustomCraft.AddEntry(item);

                if (result) successCount++;
            }

            Logger.Log($"{successCount} of {uniqueEntries.Count} {typeof(T).Name} entries were patched.");
        }

        private static void SendToSMLHelper<T>(IDictionary<string, T> uniqueEntries)
            where T : ICraftingTab
        {
            foreach (T item in uniqueEntries.Values)
            {
                CustomCraft.CustomCraftingTab(item);
            }

            Logger.Log($"Custom Crafting Tabs were patched.");
        }
    }
}

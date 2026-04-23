using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
// CraftingSystem
// Recipe database, recipe lookup, and crafting validation helpers. (Этот скрипт отвечает за: recipe database, recipe lookup, and crafting validation helpers.)
// ============================================================
public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance;

    [Serializable]
    public class RecipeCost
    {
        public string itemId;
        public int amount;
    }

    [Serializable]
    public class Recipe
    {
        public string recipeId;
        public string displayName;
        public string resultItemId;
        public int resultAmount = 1;
        public List<RecipeCost> costs = new List<RecipeCost>();
    }

// Inspector fields below are tuned in Unity Inspector. (Поля ниже обычно настраиваются в Unity Inspector.)
    [Header("Recipes")]
    public List<Recipe> recipes = new List<Recipe>();

    private Dictionary<string, Recipe> recipeMap = new Dictionary<string, Recipe>();

    // Initialize references and singleton setup. (Initialize references and singleton setup)
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildRecipeMap();

        if (recipes.Count == 0)
        {
            CreateDefaultRecipes();
            BuildRecipeMap();
        }
    }

    // Build fast recipe lookup map. (Build fast рецепт lookup map)
    private void BuildRecipeMap()
    {
        recipeMap.Clear();

        for (int i = 0; i < recipes.Count; i++)
        {
            Recipe recipe = recipes[i];

            if (recipe == null || string.IsNullOrWhiteSpace(recipe.recipeId))
                continue;

            if (!recipeMap.ContainsKey(recipe.recipeId))
                recipeMap.Add(recipe.recipeId, recipe);
        }
    }

    // Create fallback default recipes. (Create fallback default рецептs)
    private void CreateDefaultRecipes()
    {
        recipes.Clear();

        Recipe furnaceRecipe = new Recipe
        {
            recipeId = "craft_furnace",
            displayName = "Furnace",
            resultItemId = "Furnace",
            resultAmount = 1,
            costs = new List<RecipeCost>
            {
                new RecipeCost { itemId = "Stone", amount = 10 },
                new RecipeCost { itemId = "IronIngot", amount = 1 }
            }
        };
        recipes.Add(furnaceRecipe);

        Recipe axeRecipe = new Recipe
        {
            recipeId = "craft_axe",
            displayName = "Axe",
            resultItemId = "Axe",
            resultAmount = 1,
            costs = new List<RecipeCost>
            {
                new RecipeCost { itemId = "Plank", amount = 2 },
                new RecipeCost { itemId = "Stone", amount = 2 },
                new RecipeCost { itemId = "IronIngot", amount = 1 }
            }
        };
        recipes.Add(axeRecipe);

        Recipe hammerRecipe = new Recipe
        {
            recipeId = "craft_hammer",
            displayName = "Hammer",
            resultItemId = "Hammer",
            resultAmount = 1,
            costs = new List<RecipeCost>
            {
                new RecipeCost { itemId = "Plank", amount = 1 },
                new RecipeCost { itemId = "Stone", amount = 2 }
            }
        };
        recipes.Add(hammerRecipe);

        Recipe crowbarRecipe = new Recipe
        {
            recipeId = "craft_crowbar",
            displayName = "Crowbar",
            resultItemId = "Crowbar",
            resultAmount = 1,
            costs = new List<RecipeCost>
            {
                new RecipeCost { itemId = "IronIngot", amount = 3 }
            }
        };
        recipes.Add(crowbarRecipe);
    }

    // Get Recipe. (Get Recipe)
    public Recipe GetRecipe(string recipeId)
    {
        if (string.IsNullOrWhiteSpace(recipeId))
            return null;

        recipeMap.TryGetValue(recipeId, out Recipe recipe);
        return recipe;
    }

    // Get All Recipes. (Get All Recipes)
    public List<Recipe> GetAllRecipes()
    {
        return recipes;
    }

    // Check whether recipe requirements are met. (Check whether рецепт requirements are met)
    public bool CanCraft(string recipeId, HotbarManager inventory)
    {
        Recipe recipe = GetRecipe(recipeId);
        if (recipe == null || inventory == null)
            return false;

        for (int i = 0; i < recipe.costs.Count; i++)
        {
            RecipeCost cost = recipe.costs[i];
            if (inventory.GetItemCount(cost.itemId) < cost.amount)
                return false;
        }

        return true;
    }

    // Consume ingredients and create crafted result. (Consume ingredients and create crafted result)
    public bool Craft(string recipeId, HotbarManager inventory)
    {
        Recipe recipe = GetRecipe(recipeId);
        if (recipe == null || inventory == null)
            return false;

        if (!CanCraft(recipeId, inventory))
            return false;

        for (int i = 0; i < recipe.costs.Count; i++)
        {
            RecipeCost cost = recipe.costs[i];
            bool removed = inventory.RemoveItems(cost.itemId, cost.amount);

            if (!removed)
            {
                Debug.LogWarning("[CraftingSystem] Failed to remove required items: " + cost.itemId);
                return false;
            }
        }

        HotbarItemDefinition craftedDefinition = inventory.FindDefinition(recipe.resultItemId);

        for (int i = 0; i < recipe.resultAmount; i++)
        {
            Sprite icon = craftedDefinition != null ? craftedDefinition.icon : null;
            GameObject prefab = craftedDefinition != null ? craftedDefinition.prefab : null;

            bool added = inventory.AddItem(recipe.resultItemId, icon, prefab);

            if (!added)
            {
                Debug.LogWarning("[CraftingSystem] Could not add crafted item to hotbar: " + recipe.resultItemId);
                return false;
            }
        }

        return true;
    }
}

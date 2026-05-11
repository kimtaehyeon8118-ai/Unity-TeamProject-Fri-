using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GameProgression
{
    private const string UnlockedIngredientsKey = "Taehyeon2NE.UnlockedIngredients";
    private const string PendingIngredientsKey = "Taehyeon2NE.PendingIngredients";
    private const string NightClearCountKey = "Taehyeon2NE.NightClearCount";

    private static readonly string[] StartingIngredients =
    {
        "된장",
        "두부",
        "버섯",
        "애호박"
    };

    private static readonly string[] NightRewardSequence =
    {
        "김치",
        "돼지고기",
        "대파",
        "순두부",
        "고춧가루",
        "계란"
    };

    public static IReadOnlyList<string> GetUnlockedIngredients()
    {
        EnsureInitialized();
        return LoadList(UnlockedIngredientsKey);
    }

    public static bool IsIngredientUnlocked(string ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName))
            return false;

        EnsureInitialized();
        return LoadList(UnlockedIngredientsKey).Contains(ingredientName);
    }

    public static string GrantNightReward()
    {
        EnsureInitialized();

        List<string> unlockedIngredients = LoadList(UnlockedIngredientsKey).ToList();
        List<string> pendingIngredients = LoadList(PendingIngredientsKey).ToList();

        string reward = NightRewardSequence.FirstOrDefault(candidate => !unlockedIngredients.Contains(candidate));
        PlayerPrefs.SetInt(NightClearCountKey, PlayerPrefs.GetInt(NightClearCountKey, 0) + 1);

        if (string.IsNullOrEmpty(reward))
        {
            PlayerPrefs.Save();
            return string.Empty;
        }

        unlockedIngredients.Add(reward);
        if (!pendingIngredients.Contains(reward))
            pendingIngredients.Add(reward);

        SaveList(UnlockedIngredientsKey, unlockedIngredients);
        SaveList(PendingIngredientsKey, pendingIngredients);
        PlayerPrefs.Save();
        return reward;
    }

    public static string[] ConsumePendingIngredients()
    {
        EnsureInitialized();

        string[] pendingIngredients = LoadList(PendingIngredientsKey);
        SaveList(PendingIngredientsKey, Array.Empty<string>());
        PlayerPrefs.Save();
        return pendingIngredients;
    }

    public static void ResetProgress()
    {
        SaveList(UnlockedIngredientsKey, StartingIngredients);
        SaveList(PendingIngredientsKey, Array.Empty<string>());
        PlayerPrefs.SetInt(NightClearCountKey, 0);
        PlayerPrefs.Save();
    }

    private static void EnsureInitialized()
    {
        bool changed = false;

        if (!PlayerPrefs.HasKey(UnlockedIngredientsKey))
        {
            SaveList(UnlockedIngredientsKey, StartingIngredients);
            changed = true;
        }

        if (!PlayerPrefs.HasKey(PendingIngredientsKey))
        {
            SaveList(PendingIngredientsKey, Array.Empty<string>());
            changed = true;
        }

        if (!PlayerPrefs.HasKey(NightClearCountKey))
        {
            PlayerPrefs.SetInt(NightClearCountKey, 0);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }
    }

    private static string[] LoadList(string key)
    {
        string raw = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        return raw
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToArray();
    }

    private static void SaveList(string key, IEnumerable<string> values)
    {
        string serialized = string.Join("|", values.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
        PlayerPrefs.SetString(key, serialized);
    }
}

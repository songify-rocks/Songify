using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General;

/// <summary>Applies UI culture from the language combo (Settings and setup wizard).</summary>
internal static class LocalizationHelper
{
    public static Dictionary<string, string> GetLanguages()
    {
        ResourceDictionary resources = Application.Current?.Resources;
        string Loc(string key, string fallback) => resources?[key] as string ?? fallback;

        return new Dictionary<string, string>
        {
            { "en", Loc("language_en", "English") },
            { "nl", Loc("language_nl", "Dutch") },
            { "de-DE", Loc("language_de", "German") },
            { "ru-RU", Loc("language_ru", "Russian") },
            { "es", Loc("language_es", "Spanish") },
            { "fr", Loc("language_fr", "French") },
            { "pl-PL", Loc("language_pl", "Polish") },
            { "pt-PT", Loc("language_pt", "Portuguese") },
            { "it-IT", Loc("language_it", "Italian") },
            { "pt-BR", Loc("language_pt_br", "Brazilian Portuguese") },
            { "be-BY", Loc("language_be", "Belarusian") }
        };
    }

    public static void Apply(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode) || Application.Current == null)
            return;

        CultureInfo newCulture = new(cultureCode);
        Thread.CurrentThread.CurrentUICulture = newCulture;

        ResourceDictionary newLocalizationDict = App.ResxToDictionaryHelper.CreateResourceDictionary(newCulture);
        Collection<ResourceDictionary> dictionaries = Application.Current.Resources.MergedDictionaries;
        ResourceDictionary localizationDict =
            dictionaries.FirstOrDefault(dict => dict.Contains("window_settings_system_language"));

        if (localizationDict != null)
        {
            int index = dictionaries.IndexOf(localizationDict);
            dictionaries.Remove(localizationDict);
            dictionaries.Insert(index, newLocalizationDict);
        }
        else
        {
            dictionaries.Add(newLocalizationDict);
        }

        Settings.Language = cultureCode;
    }
}

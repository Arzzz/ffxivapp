﻿// FFXIVAPP.Plugin.Log
// LocaleHelper.cs
//  
// Created by Ryan Wilson.
// Copyright © 2007-2013 Ryan Wilson - All Rights Reserved

#region Usings

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using FFXIVAPP.Plugin.Log.Localization;

#endregion

namespace FFXIVAPP.Plugin.Log.Helpers
{
    internal static class LocaleHelper
    {
        private static readonly string[] Supported = new[]
        {
            "en"
        };

        /// <summary>
        /// </summary>
        /// <param name="cultureInfo"> </param>
        public static Dictionary<string, string> Update(CultureInfo cultureInfo)
        {
            var culture = cultureInfo.TwoLetterISOLanguageName;
            ResourceDictionary dictionary;
            if (Supported.Contains(culture))
            {
                switch (culture)
                {
                    case "ja":
                        dictionary = Japanese.Context();
                        break;
                    case "de":
                        dictionary = German.Context();
                        break;
                    case "fr":
                        dictionary = French.Context();
                        break;
                    default:
                        dictionary = English.Context();
                        break;
                }
            }
            else
            {
                dictionary = English.Context();
            }
            return dictionary.Cast<DictionaryEntry>()
                             .ToDictionary(item => (string) item.Key, item => (string) item.Value);
        }
    }
}

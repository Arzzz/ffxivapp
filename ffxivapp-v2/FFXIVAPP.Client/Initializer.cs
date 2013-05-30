﻿// FFXIVAPP.Client
// Initializer.cs
//  
// Created by Ryan Wilson.
// Copyright © 2007-2013 Ryan Wilson - All Rights Reserved

#region Usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using FFXIVAPP.Client.Delegates;
using FFXIVAPP.Client.Memory;
using FFXIVAPP.Client.Properties;
using FFXIVAPP.Client.Views;
using FFXIVAPP.Common.Utilities;
using HtmlAgilityPack;
using NLog;
using Newtonsoft.Json.Linq;

#endregion

namespace FFXIVAPP.Client
{
    internal static class Initializer
    {
        #region Declarations

        private static ChatWorker _chatWorker;

        #endregion

        /// <summary>
        /// </summary>
        public static void SetupCurrentUICulture()
        {
            var cultureInfo = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var currentCulture = new CultureInfo(cultureInfo);
            Common.Constants.CultureInfo = Settings.Default.CultureSet ? Settings.Default.Culture : currentCulture;
        }

        /// <summary>
        /// </summary>
        public static void LoadChatCodes()
        {
            if (Constants.XChatCodes != null)
            {
                foreach (var xElement in Constants.XChatCodes.Descendants()
                                                  .Elements("Code"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xDescription = (string) xElement.Element("Description");
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xDescription))
                    {
                        continue;
                    }
                    Common.Constants.ChatCodes.Add(xKey, xDescription);
                }
                Common.Constants.ChatCodesXml = Constants.XChatCodes.ToString();
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedChatCodes : {0} KeyValuePairs", Common.Constants.ChatCodes.Count));
            }
        }

        /// <summary>
        /// </summary>
        public static void LoadAutoTranslate()
        {
            if (Constants.XAutoTranslate != null)
            {
                foreach (var xElement in Constants.XAutoTranslate.Descendants()
                                                  .Elements("Code"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xValue = (string) xElement.Element(Settings.Default.GameLanguage);
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xValue))
                    {
                        continue;
                    }
                    Common.Constants.AutoTranslate.Add(xKey, xValue);
                }
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedAutoTranslate : {0} KeyValuePairs", Common.Constants.AutoTranslate.Count));
            }
        }

        /// <summary>
        /// </summary>
        public static void LoadColors()
        {
            if (Constants.XColors != null)
            {
                foreach (var xElement in Constants.XColors.Descendants()
                                                  .Elements("Color"))
                {
                    var xKey = (string) xElement.Attribute("Key");
                    var xValue = (string) xElement.Element("Value");
                    var xDescription = (string) xElement.Element("Description");
                    if (String.IsNullOrWhiteSpace(xKey) || String.IsNullOrWhiteSpace(xValue))
                    {
                        continue;
                    }
                    if (Common.Constants.ChatCodes.ContainsKey(xKey))
                    {
                        if (xDescription.ToLower()
                                        .Contains("unknown") || String.IsNullOrWhiteSpace(xDescription))
                        {
                            xDescription = Common.Constants.ChatCodes[xKey];
                        }
                    }
                    Common.Constants.Colors.Add(xKey, new[]
                    {
                        xValue, xDescription
                    });
                }
                Logging.Log(LogManager.GetCurrentClassLogger(), String.Format("LoadedColors : {0} KeyValuePairs", Common.Constants.AutoTranslate.Count));
            }
        }

        public static void SetGlobals()
        {
            Common.Constants.CharacterName = Settings.Default.CharacterName;
            Common.Constants.GameLanguage = Settings.Default.GameLanguage;
            Common.Constants.ServerName = Settings.Default.ServerName;
            Common.Constants.ServerNumber = Settings.Default.ServerNumber;
            Common.Constants.EnableNLog = Settings.Default.EnableNLog;
        }

        public static void SetCharacter()
        {
            var name = String.Format("{0} {1}", Settings.Default.FirstName, Settings.Default.LastName);
            Settings.Default.CharacterName = name.Trim();
        }

        public static void CheckUpdates()
        {
            Func<bool> updateCheck = delegate
            {
                var current = Assembly.GetExecutingAssembly()
                                      .GetName()
                                      .Version.ToString();
                AppViewModel.Instance.CurrentVersion = current;
                var request = (HttpWebRequest) WebRequest.Create(String.Format("http://ffxiv-app.com/Json/CurrentVersion"));
                request.UserAgent = "Mozilla/5.0 (Macintosh; U; Intel Mac OS X 10_6_3; en-US) AppleWebKit/533.4 (KHTML, like Gecko) Chrome/5.0.375.70 Safari/533.4";
                request.Headers.Add("Accept-Language", "en;q=0.8");
                request.ContentType = "application/json; charset=utf-8";
                var response = (HttpWebResponse) request.GetResponse();
                var responseText = "";
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    try
                    {
                        responseText = streamReader.ReadToEnd();
                    }
                    catch (Exception ex)
                    {
                    }
                }
                if (response.StatusCode != HttpStatusCode.OK || String.IsNullOrWhiteSpace(responseText))
                {
                    AppViewModel.Instance.HasNewVersion = false;
                    AppViewModel.Instance.LatestVersion = "Unknown";
                }
                else
                {
                    var jsonResult = JObject.Parse(responseText);
                    var latest = jsonResult["Version"].ToString();
                    var updateNotes = jsonResult["Notes"].ToList();
                    try
                    {
                        foreach (var note in updateNotes.Select(updateNote => updateNote.Value<string>()))
                        {
                            AppViewModel.Instance.UpdateNotes.Add(note);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    AppViewModel.Instance.LatestVersion = latest;
                    switch (latest)
                    {
                        case "Unknown":
                            AppViewModel.Instance.HasNewVersion = false;
                            break;
                        default:
                            var lver = latest.Split('.');
                            var cver = current.Split('.');
                            int lmajor = 0, lminor = 0, lbuild = 0, lrevision = 0;
                            int cmajor = 0, cminor = 0, cbuild = 0, crevision = 0;
                            try
                            {
                                lmajor = Int32.Parse(lver[0]);
                                lminor = Int32.Parse(lver[1]);
                                lbuild = Int32.Parse(lver[2]);
                                lrevision = Int32.Parse(lver[3]);
                                cmajor = Int32.Parse(cver[0]);
                                cminor = Int32.Parse(cver[1]);
                                cbuild = Int32.Parse(cver[2]);
                                crevision = Int32.Parse(cver[3]);
                            }
                            catch (Exception ex)
                            {
                                AppViewModel.Instance.HasNewVersion = false;
                                Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
                            }
                            if (lmajor <= cmajor)
                            {
                                if (lminor <= cminor)
                                {
                                    if (lbuild == cbuild)
                                    {
                                        AppViewModel.Instance.HasNewVersion = lrevision > crevision;
                                    }
                                    AppViewModel.Instance.HasNewVersion = lbuild > cbuild;
                                }
                                AppViewModel.Instance.HasNewVersion = true;
                            }
                            break;
                    }
                }
                return true;
            };
            updateCheck.BeginInvoke(null, null);
        }

        public static void SetSignatures()
        {
            var signatures = AppViewModel.Instance.Signatures;
            signatures.Clear();
            signatures.Add(new Signature
            {
                Key = "CHATLOG",
                Value = "4000000006000000000000000001021202020300000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000????????????????00000000",
                Offset = 92
            });
            //signatures.Add(new Signature
            //{
            //    Key = "CHARMAP",
            //    Value = "FFFFFFFF??000000000000000000000000000000????????00000000DB0FC93F6F12833A00??????",
            //    Offset = 40
            //});
            //signatures.Add(new Signature
            //{
            //    Key = "NPCMAP",
            //    Value = "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF000080BF000000",
            //    Offset = 120
            //});
            //signatures.Add(new Signature
            //{
            //    Key = "TARGET",
            //    Value = "FFFFFFFF????????00000000????0000000000000000000000000000????????000000000000000000000000DB0FC93F6F12833A",
            //    Offset = 248
            //});
            //signatures.Add(new Signature
            //{
            //    Key = "RECAST",
            //    Value = "0300000002000000010122200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000DB0FC93F6F12833A0000000000000000????????000000000000000000000000",
            //    Offset = 408
            //});
            //signatures.Add(new Signature
            //{
            //    Key = "RECAST_WS",
            //    Value = "0300000002000000010122200000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000DB0FC93F6F12833A0000000000000000????????000000000000000000000000",
            //    Offset = 1228
            //});
            //signatures.Add(new Signature
            //{
            //    Key = "AGRO",
            //    Value = "FFFFFFFF??000000000000000000000000000000????????00000000DB0FC93F6F12833A00????????00????",
            //    Offset = 3176
            //});
        }

        /// <summary>
        /// </summary>
        /// <returns> </returns>
        private static int GetPID()
        {
            if (Common.Constants.IsOpen && Common.Constants.ProcessID > 0)
            {
                try
                {
                    Process.GetProcessById(Common.Constants.ProcessID);
                    return Common.Constants.ProcessID;
                }
                catch (ArgumentException ex)
                {
                    Common.Constants.IsOpen = false;
                    Logging.Log(LogManager.GetCurrentClassLogger(), "", ex);
                }
            }
            Common.Constants.ProcessIDs = Process.GetProcessesByName("ffxiv");
            if (Common.Constants.ProcessIDs.Length == 0)
            {
                Common.Constants.IsOpen = false;
                return -1;
            }
            Common.Constants.IsOpen = true;
            foreach (var process in Common.Constants.ProcessIDs)
            {
                MainView.View.PIDSelect.Items.Add(process.Id);
            }
            MainView.View.PIDSelect.SelectedIndex = 0;
            PID(Common.Constants.ProcessIDs.First()
                      .Id);
            return Common.Constants.ProcessIDs.First()
                         .Id;
        }

        /// <summary>
        /// </summary>
        public static void SetPID()
        {
            StopLogging();
            if (MainView.View.PIDSelect.Text == "")
            {
                return;
            }
            PID(Convert.ToInt32(MainView.View.PIDSelect.Text));
            StartLogging();
        }

        /// <summary>
        /// </summary>
        public static void ResetPID()
        {
            Common.Constants.ProcessID = -1;
        }

        /// <summary>
        /// </summary>
        /// <param name="pid"> </param>
        private static void PID(int pid)
        {
            Common.Constants.ProcessID = pid;
            Common.Constants.ProcessHandle = Common.Constants.ProcessIDs[MainView.View.PIDSelect.SelectedIndex].MainWindowHandle;
        }

        /// <summary>
        /// </summary>
        public static void StartLogging()
        {
            var id = MainView.View.PIDSelect.Text == "" ? GetPID() : Common.Constants.ProcessID;
            Common.Constants.IsOpen = true;
            if (id < 0)
            {
                Common.Constants.IsOpen = false;
                return;
            }
            var process = Process.GetProcessById(id);
            var offsets = new SigFinder(process, AppViewModel.Instance.Signatures);
            _chatWorker = new ChatWorker(process, offsets);
            _chatWorker.StartLogging();
            _chatWorker.OnNewline += ChatWorkerDelegate.OnNewLine;
        }

        /// <summary>
        /// </summary>
        public static void StopLogging()
        {
            if (_chatWorker == null)
            {
                return;
            }
            _chatWorker.OnNewline -= ChatWorkerDelegate.OnNewLine;
            _chatWorker.StopLogging();
            _chatWorker.Dispose();
        }
    }
}

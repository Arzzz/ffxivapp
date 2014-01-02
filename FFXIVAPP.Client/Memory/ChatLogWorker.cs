﻿// FFXIVAPP.Client
// ChatLogWorker.cs
// 
// © 2013 Ryan Wilson

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Timers;
using FFXIVAPP.Client.Helpers;
using NLog;
using SmartAssembly.Attributes;

namespace FFXIVAPP.Client.Memory
{
    [DoNotObfuscate]
    internal class ChatLogWorker : INotifyPropertyChanged, IDisposable
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Property Bindings

        private uint ChatPointerMap { get; set; }

        #endregion

        #region Declarations

        private static readonly Logger Tracer = Logger;
        private readonly List<int> _indexes = new List<int>();
        private readonly Timer _scanTimer;
        private readonly BackgroundWorker _scanner = new BackgroundWorker();
        private int _chatLimit = 1000;

        private Structures.ChatLog _chatLogPointers;
        private bool _isScanning;
        private int _previousArrayIndex;
        private int _previousOffset;

        public bool FirstRun = true;

        #endregion

        public ChatLogWorker()
        {
            _scanTimer = new Timer(10);
            _scanTimer.Elapsed += ScanTimerElapsed;
        }

        #region Timer Controls

        /// <summary>
        /// </summary>
        public void StartScanning()
        {
            _scanTimer.Enabled = true;
        }

        /// <summary>
        /// </summary>
        public void StopScanning()
        {
            _scanTimer.Enabled = false;
        }

        #endregion

        #region Threads

        /// <summary>
        /// </summary>
        /// <param name="sender"> </param>
        /// <param name="e"> </param>
        private void ScanTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isScanning)
            {
                return;
            }
            _isScanning = true;
            Func<bool> scannerWorker = delegate
            {
                if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("GAMEMAIN"))
                {
                    if (MemoryHandler.Instance.SigScanner.Locations.ContainsKey("CHATLOG"))
                    {
                        ChatPointerMap = MemoryHandler.Instance.SigScanner.Locations["CHATLOG"];
                        if (ChatPointerMap <= 20)
                        {
                            return false;
                        }
                        var buffered = new List<List<byte>>();
                        try
                        {
                            _indexes.Clear();
                            _chatLogPointers = MemoryHandler.Instance.GetStructure<Structures.ChatLog>(ChatPointerMap);
                            EnsureArrayIndexes();
                            var currentArrayIndex = (_chatLogPointers.OffsetArrayPos - _chatLogPointers.OffsetArrayStart) / 4;
                            if (FirstRun)
                            {
                                FirstRun = false;
                                _previousOffset = _indexes[(int) currentArrayIndex - 1];
                                _previousArrayIndex = (int) currentArrayIndex - 1;
                            }
                            else
                            {
                                if (currentArrayIndex < _previousArrayIndex)
                                {
                                    buffered.AddRange(ResolveEntries(_previousArrayIndex, _chatLimit));
                                    _previousOffset = 0;
                                    _previousArrayIndex = 0;
                                }
                                if (_previousArrayIndex < currentArrayIndex)
                                {
                                    buffered.AddRange(ResolveEntries(_previousArrayIndex, (int)currentArrayIndex));
                                }
                                _previousArrayIndex = (int)currentArrayIndex;
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                        foreach (var bytes in buffered)
                        {
                            try
                            {
                                var chatLogEntry = ChatEntry.Process(bytes.ToArray());
                                if (Regex.IsMatch(chatLogEntry.Combined, @"[\w\d]{4}::?.+"))
                                {
                                    AppContextHelper.Instance.RaiseNewChatLogEntry(chatLogEntry);
                                }
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            ChatPointerMap = MemoryHandler.Instance.GetUInt32(MemoryHandler.Instance.SigScanner.Locations["GAMEMAIN"]) + 20;
                            MemoryHandler.Instance.SigScanner.Locations.Add("CHATLOG", ChatPointerMap);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
                _isScanning = false;
                return true;
            };
            scannerWorker.BeginInvoke(delegate { }, scannerWorker);
        }

        private void EnsureArrayIndexes()
        {
            _indexes.Clear();
            for (var i = 0; i < _chatLimit; i++)
            {
                _indexes.Add((int) MemoryHandler.Instance.GetUInt32((uint) (_chatLogPointers.OffsetArrayStart + (i * 4))));
            }
        }

        private IEnumerable<List<byte>> ResolveEntries(int offset, int length)
        {
            var entries = new List<List<byte>>();
            for (var i = offset; i < length; i++)
            {
                EnsureArrayIndexes();
                var currentOffset = _indexes[i];
                entries.Add(ResolveEntry(_previousOffset, currentOffset));
                _previousOffset = currentOffset;
            }
            return entries;
        }

        private List<byte> ResolveEntry(int offset, int length)
        {
            return new List<byte>(MemoryHandler.Instance.GetByteArray((uint) (_chatLogPointers.LogStart + offset), length - offset));
        }

        #endregion

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            _scanTimer.Elapsed -= ScanTimerElapsed;
        }

        #endregion
    }
}

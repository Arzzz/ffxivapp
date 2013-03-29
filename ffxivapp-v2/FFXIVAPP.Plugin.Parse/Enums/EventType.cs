// FFXIVAPP.Plugin.Parse
// EventType.cs
//  
// Created by Ryan Wilson.
// Copyright � 2007-2012 Ryan Wilson - All Rights Reserved

namespace FFXIVAPP.Plugin.Parse.Enums
{
    public enum EventType
    {
        Unknown = 0x00000000,
        Damage = 0x00002000,
        Failed = 0x00004000,
        Actions = 0x00008000,
        Items = 0x00010000,
        Cure = 0x00020000,
        Benficial = 0x00040000,
        Detrimental = 0x00080000,
        System = 0x00100000,
        Battle = 0x00200000,
        Synthesis = 0x00400000,
        Gathering = 0x00800000,
        Error = 0x01000000,
        Echo = 0x02000000,
        Dialogue = 0x04000000,
        Loot = 0x08000000,
        Progression = 0x10000000
    }
}

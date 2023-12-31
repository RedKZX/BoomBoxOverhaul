using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BepInEx;
using BepInEx.Logging;
using Plugin.Patches;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TMPro;


namespace BoomboxOverhaul

{
    [BepInPlugin("Henrehs.BoomBoxOverhaul", "BoomBoxOverhaul", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;

        private Harmony _harmony;

        public static Key volumeUpKey = (Key)14;

        public static Key volumeDownKey = (Key)13;

        public static string volumeHoverTip = "Volume up:   [+]\nVolume down: [-]";

        public static float volumeIncrement = 0.1f;

        public static float maxDisplayedVolume = 1.5f;

        public static float defaultVolume = 1f;

        private void Awake()
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0011: Expected O, but got Unknown
            _harmony = new Harmony("BoomBoxOverhaul");
            _harmony.PatchAll();
            ((Plugin)this).Logger.LogInfo((object)("BoomBox Upgraded!"));

            instance = this;
        }

        public static void Log(string message)
        {
            instance.Logger.LogInfo(message);
        }

        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "Henrehs.BoomBoxOverhaul";

            public const string PLUGIN_NAME = "BooomBoxOverhaul";

            public const string PLUGIN_VERSION = "1.0.0";
        }
    }
}

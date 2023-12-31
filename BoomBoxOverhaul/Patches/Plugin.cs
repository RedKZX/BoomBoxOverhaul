using BepInEx.Configuration;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;
using Object = UnityEngine.Object;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;




namespace Plugin.Patches
{
    [HarmonyPatch(typeof(BoomboxItem))]
    public class Plugin

    {
        public static Key volumeUpKey = (Key)14;

        public static Key volumeDownKey = (Key)13;

        public static string volumeHoverTip = "Volume up:   [+]\nVolume down: [-]";

        public static float volumeIncrement = 0.1f;

        public static float maxDisplayedVolume = 1.5f;

        public static float defaultVolume = 1f;


        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void PatchBattery(ref Item ___itemProperties)
        {
            ___itemProperties.requiresBattery = false;
        }

        [HarmonyPatch("PocketItem")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ManualLogSource val = BepInEx.Logging.Logger.CreateLogSource("Henrehs.BoomBoxOverhaul");
            int num = -1;
            int num2 = -1;
            List<CodeInstruction> list = new List<CodeInstruction>(instructions);
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    if (!(list[i].opcode == OpCodes.Call))
                    {
                        continue;
                    }
                    string text = ((object)list[i]).ToString();
                    if (!text.Contains("BoomboxItem::StartMusic"))
                    {
                        continue;
                    }
                    for (int num3 = i; num3 > 0; num3--)
                    {
                        if (list[num3].opcode == OpCodes.Ldarg_0)
                        {
                            num = num3;
                        }
                    }
                    num2 = i;
                }
                catch (Exception)
                {
                }
            }
            if (num2 > -1 && num > -1)
            {
                list.RemoveRange(num, num2 - num + 1);
            }
            return list.AsEnumerable();

            //Volume Changer
        }

        [HarmonyPatch]
        internal class ChangeVolumePatcher
        {
            private static AudioPlayerItemType boomboxItems = new AudioPlayerItemType("Boomboxes", "Grab boombox: [E]");

            private static AudioPlayerPlaceableType recordPlayers = new AudioPlayerPlaceableType("Record players", "Record player: [E]");

            private static AudioPlayerPlaceableType televisions = new AudioPlayerPlaceableType("Televisions", "Television: [E]");

            [HarmonyPatch(typeof(PlayerControllerB), "Update")]
            [HarmonyPrefix]
            public static void GetObjectLookingAt(PlayerControllerB __instance)
            {
                boomboxItems.lookingAtDeviceType = false;
                recordPlayers.lookingAtDeviceType = false;
                televisions.lookingAtDeviceType = false;
                if (!((NetworkBehaviour)__instance).IsOwner || !__instance.isPlayerControlled || __instance.inTerminalMenu || __instance.isTypingChat || __instance.isPlayerDead)
                {
                    return;
                }
                InteractTrigger hoveringOverTrigger = __instance.hoveringOverTrigger;
                object obj;
                if (hoveringOverTrigger == null)
                {
                    obj = null;
                }
                else
                {
                    Transform parent = ((Component)hoveringOverTrigger).transform.parent;
                    obj = ((parent != null) ? ((Component)parent).gameObject : null);
                }
                GameObject val = (GameObject)obj;
                if ((Object)(object)val != (Object)null)
                {
                    if (((Object)val).name.Contains("RecordPlayer"))
                    {
                        recordPlayers.lookingAtDeviceType = true;
                    }
                    else if (((Object)val).name.Contains("Television"))
                    {
                        televisions.lookingAtDeviceType = true;
                    }
                }
                if (!recordPlayers.lookingAtDeviceType && !televisions.lookingAtDeviceType)
                {
                    if ((Object)(object)__instance.cursorTip != (Object)null && ((TMP_Text)__instance.cursorTip).text.Contains(boomboxItems.originalHoverTip))
                    {
                        boomboxItems.lookingAtDeviceType = true;
                    }
                    else if ((Object)(object)__instance.currentlyHeldObjectServer != (Object)null && __instance.currentlyHeldObjectServer is BoomboxItem)
                    {
                        boomboxItems.lookingAtDeviceType = true;
                    }
                }
            }

            [HarmonyPatch(typeof(PlayerControllerB), "Update")]
            [HarmonyPostfix]
            public static void GetVolumeInput(PlayerControllerB __instance)
            {
                if (!((NetworkBehaviour)__instance).IsOwner || !__instance.isPlayerControlled || __instance.inTerminalMenu || __instance.isTypingChat || __instance.isPlayerDead || (!boomboxItems.lookingAtDeviceType && !recordPlayers.lookingAtDeviceType && !televisions.lookingAtDeviceType))
                {
                    return;
                }
                float num = 0f;
                if (((ButtonControl)Keyboard.current.minusKey).wasPressedThisFrame)
                {
                    num = 0f - Plugin.volumeIncrement;
                }
                if (((ButtonControl)Keyboard.current.equalsKey).wasPressedThisFrame)
                {
                    num = Plugin.volumeIncrement;
                }
                if (num != 0f)
                {
                    
                    AudioPlayerTypeBase audioPlayerTypeBase = null;
                    if (boomboxItems.lookingAtDeviceType)
                    {
                        audioPlayerTypeBase = boomboxItems;
                    }
                    else if (recordPlayers.lookingAtDeviceType)
                    {
                        audioPlayerTypeBase = recordPlayers;
                    }
                    else if (televisions.lookingAtDeviceType)
                    {
                        audioPlayerTypeBase = televisions;
                    }
                    if (audioPlayerTypeBase != null)
                    {
                        audioPlayerTypeBase.currentVolume = Mathf.Clamp(audioPlayerTypeBase.currentVolume + num, 0f, Plugin.maxDisplayedVolume);
                        audioPlayerTypeBase.UpdateVolumes();
                        audioPlayerTypeBase.UpdateTooltips();
                    }
                }
            }

            [HarmonyPatch(typeof(BoomboxItem), "Start")]
            [HarmonyPostfix]
            public static void SetBoomboxHoverTip(BoomboxItem __instance)
            {
                boomboxItems.audioSources.Add(__instance.boomboxAudio);
                boomboxItems.items.Add((GrabbableObject)(object)__instance);
                ((GrabbableObject)__instance).itemProperties.canBeGrabbedBeforeGameStart = true;
                if (boomboxItems.defaultVolume == 0f)
                {
                    boomboxItems.defaultVolume = __instance.boomboxAudio.volume;
                }
                if (boomboxItems.controlTooltips == null)
                {
                    boomboxItems.controlTooltips = new List<string>(((GrabbableObject)__instance).itemProperties.toolTips);
                    boomboxItems.controlTooltips.Add("");
                }
                boomboxItems.UpdateTooltips();
                boomboxItems.UpdateVolumes();
            }

            [HarmonyPatch(typeof(AutoParentToShip), "Awake")]
            [HarmonyPostfix]
            public static void SetRecordPlayerHoverTip(AutoParentToShip __instance)
            {
                if (((Object)__instance).name.Contains("RecordPlayerContainer"))
                {
                    AudioSource val = ((Component)__instance).GetComponentInChildren<AnimatedObjectTrigger>()?.thisAudioSource;
                    if ((Object)(object)val != (Object)null)
                    {
                        recordPlayers.audioSources.Add(val);
                    }
                    InteractTrigger componentInChildren = ((Component)__instance).GetComponentInChildren<InteractTrigger>();
                    if (recordPlayers.defaultVolume == 0f)
                    {
                        recordPlayers.defaultVolume = val.volume;
                        
                    }
                    if ((Object)(object)componentInChildren == (Object)null)
                    {
                        
                        return;
                    }
                    recordPlayers.triggers.Add(componentInChildren);
                    recordPlayers.UpdateTooltips();
                    recordPlayers.UpdateVolumes();
                }
            }

            [HarmonyPatch(typeof(TVScript), "__initializeVariables")]
            [HarmonyPostfix]
            public static void SetTelevisionHoverTip(TVScript __instance)
            {
                televisions.audioSources.Add(__instance.tvSFX);
                Transform parent = ((Component)__instance).transform.parent;
                InteractTrigger val = ((parent != null) ? ((Component)parent).GetComponentInChildren<InteractTrigger>() : null);
                if (televisions.defaultVolume == 0f)
                {
                    televisions.defaultVolume = __instance.tvSFX.volume;
                    
                }
                if ((Object)(object)val == (Object)null)
                {
                    
                    return;
                }
                televisions.triggers.Add(val);
                televisions.UpdateTooltips();
                televisions.UpdateVolumes();
            }
        }
        internal abstract class AudioPlayerTypeBase
        {
            public string name;

            public List<AudioSource> audioSources;

            public string originalHoverTip;

            public float defaultVolume;

            public float currentVolume;

            public bool lookingAtDeviceType;

            public Type objectType;

            protected AudioPlayerTypeBase(Type objectType)
            {
                this.objectType = objectType;
            }

            public AudioPlayerTypeBase(string name, string originalHoverTip = "", float defaultVolume = 0f)
            {
                this.name = name;
                audioSources = new List<AudioSource>();
                this.originalHoverTip = originalHoverTip;
                this.defaultVolume = defaultVolume;
                currentVolume = Plugin.defaultVolume;
                lookingAtDeviceType = false;
            }

            public void UpdateVolumes()
            {
                for (int i = 0; i < audioSources.Count; i++)
                {
                    if ((Object)(object)audioSources[i] != (Object)null)
                    {
                        audioSources[i].volume = currentVolume / Plugin.maxDisplayedVolume;
                    }
                }
            }

            public abstract void UpdateTooltips();
        }
        internal class AudioPlayerItemType : AudioPlayerTypeBase
        {
            public List<GrabbableObject> items;

            public List<string> controlTooltips;

            public AudioPlayerItemType(string name, string originalHoverTip = "", float defaultVolume = 0f)
                : base(name, originalHoverTip, defaultVolume)
            {
                items = new List<GrabbableObject>();
            }

            public override void UpdateTooltips()
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if ((Object)(object)items[i] != (Object)null)
                    {
                        items[i].customGrabTooltip = $"{originalHoverTip}\n{Mathf.RoundToInt(currentVolume * 10f) * 10}% volume\n{Plugin.volumeHoverTip}";
                        controlTooltips[controlTooltips.Count - 1] = items[i].customGrabTooltip.Replace(originalHoverTip + "\n", "");
                        items[i].itemProperties.toolTips = controlTooltips.ToArray();
                    }
                }
                PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
                if ((Object)(object)localPlayerController != (Object)null && (Object)(object)localPlayerController.currentlyHeldObjectServer != (Object)null && items.Contains(localPlayerController.currentlyHeldObjectServer))
                {
                    localPlayerController.currentlyHeldObjectServer.EquipItem();
                }
            }
        }
        internal class AudioPlayerPlaceableType : AudioPlayerTypeBase
        {
            public List<InteractTrigger> triggers;

            public AudioPlayerPlaceableType(string name, string originalHoverTip = "", float defaultVolume = 0f)
                : base(name, originalHoverTip, defaultVolume)
            {
                triggers = new List<InteractTrigger>();
            }

            public override void UpdateTooltips()
            {
                for (int i = 0; i < triggers.Count; i++)
                {
                    if ((Object)(object)triggers[i] != (Object)null)
                    {
                        triggers[i].hoverTip = $"{originalHoverTip}\n{Mathf.RoundToInt(currentVolume * 10f) * 10}% volume\n{Plugin.volumeHoverTip}";
                    }
                }
            }
        }
    }
}



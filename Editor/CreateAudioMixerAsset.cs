using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;
// Not enough API and function to create AudioMixer asset from code, we will just create it manually and use it as a template for generating new mixers with the same settings. The generator will clone the template and add groups/params as needed.
// public class CreateAudioMixerAsset
// {
//     [MenuItem("Assets/Create Default Audio Mixer")]
//     public static void CreateNewAudioMixer()
//     {
//         // Define the path for the new Audio Mixer asset
//         string assetPath = "Assets/Plugins/BattleTurn/Generated/AudioManager/NewAudioMixer.mixer";
//         assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

//         // Use reflection to get the internal AudioMixerController type
//         Type audioMixerControllerType = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
//         if (audioMixerControllerType == null)
//         {
//             Debug.LogError("Could not find AudioMixerController type using reflection.");
//             return;
//         }
//         else
//         {
//             Debug.Log($"Successfully found {audioMixerControllerType} type using reflection.");
//         }

//         var method = audioMixerControllerType.GetMethod(
//             "CreateMixerControllerAtPath",
//             BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
//         );

//         if (method == null)
//         {
//             Debug.LogError("❌ Method not found");
//             return;
//         }

//         var mixer = method.Invoke(null, new object[] { assetPath });

//         // foreach (var m in audioMixerControllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
//         // {
//         //     Debug.Log(m.Name);
//         // }

//         var type = mixer.GetType();

//         AudioMixer mixerObj = mixer as AudioMixer;

//         var groups = mixerObj.FindMatchingGroups("Master");

//         if (groups.Length == 0)
//         {
//             Debug.LogError("❌ Cannot find Master group");
//             return;
//         }

//         AudioMixerGroup master = groups[0];

//         Debug.Log("✅ Found Master: " + master.name);
//         var mixerType = mixerObj.GetType();
//         var createGroup = mixerType.GetMethod(
//     "CreateNewGroup",
//     BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
// );

//         foreach (var p in master.GetType().GetProperties(
//             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
//         {
//             Debug.Log("PROP: " + p.Name);
//         }

//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();

//         Debug.Log("✅ Done");
//     }

//     static void AddGroup(object mixer, string groupName)
//     {
//         var type = mixer.GetType();

//         // Lấy Master group
//         var findMaster = type.GetMethod(
//             "FindMixingGroup",
//             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
//         );

//         // Method tạo group
//         var createGroup = type.GetMethod(
//             "CreateNewGroup",
//             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
//         );

//         if (findMaster == null || createGroup == null)
//         {
//             Debug.LogError("❌ Cannot find group methods");
//             return;
//         }

//         var master = findMaster.Invoke(mixer, new object[] { "Master" });

//         if (master == null)
//         {
//             Debug.LogError("❌ Master group not found");
//             return;
//         }

//         // 🚀 Tạo group con
//         createGroup.Invoke(mixer, new object[] { groupName, master });

//         Debug.Log($"✅ Created group: {groupName}");
//     }
// }

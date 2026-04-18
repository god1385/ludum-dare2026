#if UNITY_EDITOR
using LudumDare2026.Core.GameFlow;
using UnityEditor;
using UnityEngine;

namespace LudumDare2026.Utilities.Editor
{
    public static class SampleGameContentMenu
    {
        private const string Folder = "Assets/GameContent";

        [MenuItem("Game/Generate Sample Cipher Content")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "GameContent");

            var message = ScriptableObject.CreateInstance<CipherMessageData>();
            var messageObject = new SerializedObject(message);
            messageObject.FindProperty("_title").stringValue = "[URGENT] Decode the payload";
            messageObject.FindProperty("_encryptedBody").stringValue =
                "Uijt jt b dbftbs djqifs. Botxfs sfrvjsfe.";
            var tags = messageObject.FindProperty("_tags");
            tags.arraySize = 3;
            tags.GetArrayElementAtIndex(0).stringValue = "cipher";
            tags.GetArrayElementAtIndex(1).stringValue = "deadline";
            tags.GetArrayElementAtIndex(2).stringValue = "management";
            messageObject.FindProperty("_expectedAnswer").stringValue = "This is a caesar cipher. Answer required.";
            messageObject.FindProperty("_terminalFeed").stringValue =
                "User/User> job #4412: decode payload\n"
                + "Uijt jt b dbftbs djqifs. Botxfs sfrvjsfe.\n"
                + "User/User> _";
            messageObject.ApplyModifiedPropertiesWithoutUndo();

            var messagePath = $"{Folder}/CipherMessage_Sample.asset";
            AssetDatabase.CreateAsset(message, messagePath);

            var config = ScriptableObject.CreateInstance<GameContentConfig>();
            var configObject = new SerializedObject(config);
            var messages = configObject.FindProperty("_messages");
            messages.arraySize = 1;
            messages.GetArrayElementAtIndex(0).objectReferenceValue = message;
            configObject.ApplyModifiedPropertiesWithoutUndo();

            var configPath = $"{Folder}/GameContentConfig_Sample.asset";
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(config);
        }
    }
}
#endif

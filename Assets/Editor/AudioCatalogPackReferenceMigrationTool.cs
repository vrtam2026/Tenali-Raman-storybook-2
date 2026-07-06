using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// One-time migration tool.
///
/// The audio catalog used to store a typed Addressable ADDRESS STRING per entry
/// (e.g. "audio/English/S1_P-4-5"). It now stores a direct drag-and-drop reference
/// to the actual ARPageAudioPack asset instead — no address text involved at all.
///
/// Existing catalog entries created before this change still have their
/// languageName/pageId filled in correctly, but their pack reference is empty,
/// because the old data was stored under a field that no longer exists.
///
/// This tool fixes that: for every entry with a missing pack reference, it finds
/// the matching ARPageAudioPack asset using the same folder/naming convention the
/// setup tools already use, and assigns it — restoring every entry with zero
/// manual work and zero risk of mistyping anything.
///
/// Menu: Tools → AR Storybook → Migrate Audio Catalog To Pack References
/// Safe to run more than once — entries that already have a pack reference are
/// left untouched.
/// </summary>
public static class AudioCatalogPackReferenceMigrationTool
{
    private const string CatalogAssetPath = "Assets/code/AudioLanguageCatalog.asset";
    private const string PackOutputFolder = "Assets/code/AudioPacks";

    [MenuItem("Tools/AR Storybook/Migrate Audio Catalog To Pack References")]
    public static void Run()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<ARAddressableAudioCatalog>(CatalogAssetPath);
        if (catalog == null)
        {
            Debug.LogError($"[Catalog Migration] AudioLanguageCatalog not found at: {CatalogAssetPath}");
            return;
        }

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty entries = so.FindProperty("entries");

        int fixedCount = 0;
        int alreadyOkCount = 0;
        int missingPackCount = 0;

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry    = entries.GetArrayElementAtIndex(i);
            SerializedProperty language = entry.FindPropertyRelative("languageName");
            SerializedProperty pageId   = entry.FindPropertyRelative("pageId");
            SerializedProperty audioPack = entry.FindPropertyRelative("audioPack");
            SerializedProperty guidProp  = audioPack.FindPropertyRelative("m_AssetGUID");

            if (!string.IsNullOrEmpty(guidProp.stringValue))
            {
                alreadyOkCount++;
                continue;
            }

            string packPath = $"{PackOutputFolder}/{language.stringValue}/Page_{pageId.stringValue}_{language.stringValue}.asset";
            if (!File.Exists(packPath))
            {
                Debug.LogWarning($"[Catalog Migration] No pack file found for {language.stringValue}/{pageId.stringValue} " +
                                 $"at expected path: {packPath}");
                missingPackCount++;
                continue;
            }

            string guid = AssetDatabase.AssetPathToGUID(packPath);
            if (string.IsNullOrEmpty(guid))
            {
                missingPackCount++;
                continue;
            }

            guidProp.stringValue = guid;
            fixedCount++;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Catalog Migration] Done — Fixed:{fixedCount}  Already OK:{alreadyOkCount}  Missing pack file:{missingPackCount}");
        EditorUtility.DisplayDialog("Audio Catalog Migration Complete",
            $"Fixed (pack reference restored): {fixedCount}\n" +
            $"Already correct: {alreadyOkCount}\n" +
            $"Could not find matching pack file: {missingPackCount}\n\n" +
            "You can safely run this again — it only fixes entries that are missing a reference.",
            "OK");
    }
}

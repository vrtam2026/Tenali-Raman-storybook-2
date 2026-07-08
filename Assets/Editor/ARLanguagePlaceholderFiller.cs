using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Fills in placeholder audio for ONE language, for every page that doesn't have
/// real audio for it yet -- including pages with no pack at all yet, not just
/// packs that already exist with empty slots. Works for ANY language.
///
/// Run from: click a Language asset in the Project window → its Inspector's
/// "Set Up &lt;Language&gt; Audio" button calls this automatically after trying
/// to connect real recordings first.
/// </summary>
public static class ARLanguagePlaceholderFiller
{
    private const string PackOutputFolder = "Assets/code/AudioPacks";
    private const string CatalogAssetPath = "Assets/code/AudioLanguageCatalog.asset";

    /// <summary>Standalone entry point -- confirms with the user, then fills gaps.</summary>
    public static void Run(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            EditorUtility.DisplayDialog("Error", "No language given.", "OK");
            return;
        }

        bool proceed = EditorUtility.DisplayDialog($"Fill {language} Placeholder Audio",
            $"This puts a placeholder sound into every {language} page that has NO audio yet.\n\n" +
            $"Pages that already have real {language} audio are never touched.",
            "Fill It", "Cancel");
        if (!proceed) return;

        int filled = FillGaps(language, out int skipped);
        EditorUtility.DisplayDialog("Done",
            $"Filled: {filled} {language} pages\nSkipped: {skipped} (already had real audio)",
            "OK");
    }

    /// <summary>
    /// Does the actual work with no confirmation dialog -- used when a caller (like the
    /// merged "Set Up Language Audio" button) has already confirmed with the user once.
    /// Returns how many pages were filled; `skipped` reports how many already had audio.
    /// </summary>
    public static int FillGaps(string language, out int skipped)
    {
        skipped = 0;
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var catalog = AssetDatabase.LoadAssetAtPath<ARAddressableAudioCatalog>(CatalogAssetPath);
        if (settings == null || catalog == null) return 0;

        AudioClip placeholder = FindPlaceholderClip();
        if (placeholder == null) return 0;

        var pages = PageIdentityUtility.GetAllPages()
            .Where(p => !p.addressableKey.ToLowerInvariant().Contains("quiz") && !string.IsNullOrEmpty(p.pageId))
            .ToList();

        int filled = 0;
        bool catalogDirty = false;

        foreach (var page in pages)
        {
            if (catalog.HasEntry(language, page.pageId))
            {
                skipped++;
                continue;
            }

            string packFolder = $"{PackOutputFolder}/{language}";
            AudioAddressableSetupTool.EnsureFolder(packFolder);
            string packPath = $"{packFolder}/Page_{page.pageId}_{language}.asset";

            var pack = AssetDatabase.LoadAssetAtPath<ARPageAudioPack>(packPath);
            if (pack == null)
            {
                pack = ScriptableObject.CreateInstance<ARPageAudioPack>();
                pack.languageName = language;
                pack.pageId = page.pageId;
                pack.bgmClips = new List<ARPageAudioPack.AudioSegment>();
                AssetDatabase.CreateAsset(pack, packPath);
            }

            pack.voiceClips = new List<ARPageAudioPack.AudioSegment>
            {
                new ARPageAudioPack.AudioSegment { clip = placeholder, volume = 1f, loop = false }
            };
            EditorUtility.SetDirty(pack);

            AddressableAssetGroup group = AudioAddressableSetupTool.GetOrCreateGroup(settings, $"Audio_{language}");
            AudioAddressableSetupTool.RegisterAddressable(settings, group, packPath, $"audio/{language}/{page.pageId}");

            catalog.AddEntry(language, page.pageId, new AssetReferenceT<ARPageAudioPack>(AssetDatabase.AssetPathToGUID(packPath)));
            catalogDirty = true;
            filled++;
        }

        if (catalogDirty) EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[{language} Filler] Done — {filled} filled, {skipped} skipped. Clip: {placeholder.name}");
        return filled;
    }

    private static AudioClip FindPlaceholderClip()
    {
        string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip");
        if (clipGuids.Length == 0)
        {
            Debug.LogWarning("[Audio Filler] No AudioClip assets found in the project to use as a placeholder.");
            return null;
        }

        // Prefer something called "bgm" or "loop" so it's clearly a placeholder
        foreach (var guid in clipGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLower();
            if (lower.Contains("bgm") || lower.Contains("loop") || lower.Contains("background"))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) return clip;
            }
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(clipGuids[0]));
    }
}

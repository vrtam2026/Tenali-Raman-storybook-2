using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Maps (language + pageId) to the actual ARPageAudioPack asset — drag the audio
/// pack file directly into the entry, no address text ever needs to be typed.
/// Safe to keep local (small size). One entry per language per page.
/// </summary>
[CreateAssetMenu(menuName = "AR/Addressable Audio Catalog", fileName = "ARAddressableAudioCatalog")]
public class ARAddressableAudioCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string languageName;
        public string pageId;
        [Tooltip("Drag the ARPageAudioPack asset for this page/language here directly.")]
        public AssetReferenceT<ARPageAudioPack> audioPack;
    }

    [SerializeField] private List<Entry> entries = new();

    public bool TryGetAudioPack(string language, string pageId, out AssetReferenceT<ARPageAudioPack> audioPack)
    {
        audioPack = null;
        if (string.IsNullOrWhiteSpace(language) || string.IsNullOrWhiteSpace(pageId)) return false;

        string langKey = Normalize(language);
        string pageKey = Normalize(pageId);

        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (Normalize(entry.languageName) == langKey && Normalize(entry.pageId) == pageKey)
            {
                audioPack = entry.audioPack;
                return audioPack != null && audioPack.RuntimeKeyIsValid();
            }
        }
        return false;
    }

    public bool HasEntry(string language, string pageId) =>
        TryGetAudioPack(language, pageId, out _);

    public IReadOnlyList<Entry> GetAllEntries() => entries;

    public void AddEntry(string language, string pageId, AssetReferenceT<ARPageAudioPack> audioPack)
    {
        entries.Add(new Entry { languageName = language, pageId = pageId, audioPack = audioPack });
    }

    private static string Normalize(string s) => (s ?? "").Trim().ToLowerInvariant();
}

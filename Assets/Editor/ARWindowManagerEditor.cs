using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for ARWindowManager.
/// Auto-populates the full page list the first time you select ARWindowManager in the Inspector.
/// You can also re-run it any time via the "Re-Setup All Pages" button.
///
/// NOTE: The page list below is EMPTY on purpose. Fill in your own story's pages
/// in PopulatePages() below, following the P("addressableKey", "pageId") pattern.
/// addressableKey must match the CustomARHandler.addressableKey on your page prefab.
/// pageId must match the pageId used in your own audio packs/catalog.
/// Quiz pages (if any) use an empty pageId: P("YourQuizPrefabKey", "")
/// </summary>
[CustomEditor(typeof(ARWindowManager))]
public class ARWindowManagerEditor : Editor
{
    void OnEnable()
    {
        var mgr = (ARWindowManager)target;
        if (mgr.pages == null || mgr.pages.Count == 0)
        {
            PopulatePages(mgr);
            EditorUtility.SetDirty(mgr);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Re-Setup All Pages", GUILayout.Height(36)))
        {
            PopulatePages((ARWindowManager)target);
            EditorUtility.SetDirty(target);
            Debug.Log("[AR-WINDOW] Pages list re-populated.");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox(
            $"Total pages: {((ARWindowManager)target).pages?.Count ?? 0}  " +
            $"(including quiz pages with no audio)\n" +
            "Position in list = page index used by the window calculation.",
            MessageType.Info);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Fill this in with YOUR OWN story's pages, in page order.
    // Format: P("addressableKey", "pageId")
    // Quiz pages (no audio) use an empty pageId: P("YourQuizKey", "")
    // ─────────────────────────────────────────────────────────────────────────

    public static void PopulatePages(ARWindowManager mgr)
    {
        mgr.pages = new List<ARWindowManager.PageEntry>
        {
            // Example — replace with your own pages:
            // P("Story_2_page_intro", "S1_P-intro"),
            // P("Story_2_page_4-5",   "S1_P-4-5"),
            // P("Story_2_page_6-quiz", ""),   // quiz — no audio
        };
    }

    static ARWindowManager.PageEntry P(string addressableKey, string pageId) =>
        new ARWindowManager.PageEntry { addressableKey = addressableKey, pageId = pageId };
}

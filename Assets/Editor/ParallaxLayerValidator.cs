using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

// Tools -> AR Storybook -> Check Layer Setup
// Read-only check across every page prefab for the 3 layer/video bugs found in the
// Sept 2026 repair pass: a layer object present but missing from ParallexWithAnimation's
// Layers list (never gets positioned), two+ layers sharing a render queue (they'll
// flicker/collide), and a "Play these: All at the same time" video slot whose items
// actually have different Start-after values (so they don't really sync).
// Reports only -- does not change anything. Run it after adding/editing any page.
public static class ParallaxLayerValidator
{
    [MenuItem("Tools/AR Storybook/Check Layer Setup")]
    public static void CheckLayerSetup()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });
        int checkedCount = 0, flaggedPages = 0;
        var report = new StringBuilder();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            var parallax = asset.GetComponentInChildren<ParallexWithAnimation>(true);
            if (parallax == null) continue;
            checkedCount++;

            var issues = new List<string>();

            // 1. A layer object with its own renderer, sitting next to known layers,
            //    but never dragged into the Layers list -- it never gets positioned.
            if (parallax.layers.Count > 0 && parallax.layers[0] != null)
            {
                var parent = parallax.layers[0].parent;
                if (parent != null)
                {
                    for (int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);
                        if (child.GetComponent<MeshRenderer>() == null) continue;
                        if (parallax.layers.Contains(child)) continue;
                        issues.Add($"'{child.name}' has a renderer but is NOT in the Layers list (won't be positioned)");
                    }
                }
            }

            // 2. Empty/missing slots in the Layers list itself.
            for (int i = 0; i < parallax.layers.Count; i++)
                if (parallax.layers[i] == null) issues.Add($"Layers[{i}] is empty (missing reference)");

            // 3. Two or more transparent layers sharing the exact same render queue --
            //    they'll draw-order-flicker as the camera moves.
            var queueGroups = new Dictionary<int, List<string>>();
            foreach (var l in parallax.layers)
            {
                if (l == null) continue;
                var mr = l.GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;
                int q = mr.sharedMaterial.renderQueue;
                if (q < 2500) continue; // opaque -- not at risk
                if (!queueGroups.TryGetValue(q, out var list)) queueGroups[q] = list = new List<string>();
                list.Add(l.name);
            }
            foreach (var kv in queueGroups)
                if (kv.Value.Count > 1)
                    issues.Add($"Layers share render queue {kv.Key}, will flicker/collide: {string.Join(", ", kv.Value)}");

            // 4. "Play these: All at the same time" but the items' own Start-after
            //    values actually differ -- the dropdown doesn't do anything by itself.
            var node = asset.GetComponentInChildren<ARTrackedPageNode>(true);
            if (node != null)
            {
                var so = new SerializedObject(node);
                var partsProp = so.FindProperty("storyParts");
                if (partsProp != null)
                {
                    for (int p = 0; p < partsProp.arraySize; p++)
                    {
                        var part = partsProp.GetArrayElementAtIndex(p);
                        var playOrder = part.FindPropertyRelative("mainPlayOrder");
                        var mainVideos = part.FindPropertyRelative("mainVideos");
                        if (mainVideos == null || mainVideos.arraySize < 2) continue;

                        var delays = new List<float>();
                        for (int i = 0; i < mainVideos.arraySize; i++)
                            delays.Add(mainVideos.GetArrayElementAtIndex(i).FindPropertyRelative("startDelay").floatValue);

                        bool allSame = true;
                        for (int i = 1; i < delays.Count; i++)
                            if (Mathf.Abs(delays[i] - delays[0]) > 0.001f) allSame = false;

                        string label = playOrder != null ? playOrder.enumDisplayNames[playOrder.enumValueIndex] : "?";
                        if (label == "All At Same Time" && !allSame)
                            issues.Add($"Part[{p}]: set to 'All At Same Time' but Start-after values differ ({string.Join(",", delays)}) -- won't actually sync");
                    }
                }
            }

            if (issues.Count > 0)
            {
                flaggedPages++;
                report.AppendLine();
                report.AppendLine(Path.GetFileNameWithoutExtension(path) + ":");
                foreach (var iss in issues) report.AppendLine("  - " + iss);
            }
        }

        string summary = $"Checked {checkedCount} pages. {flaggedPages} have issues.";
        Debug.Log(summary + report);
        EditorUtility.DisplayDialog("Layer Setup Check", summary + (flaggedPages > 0 ? "\nSee Console for details." : " No problems found."), "OK");
    }
}

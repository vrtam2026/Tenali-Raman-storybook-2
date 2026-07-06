using UnityEngine;

public class PageTarget : MonoBehaviour
{
    public string pageKey;  // set in Inspector like "53-54"
    ARWindowManager _manager;

    void Start()
    {
        _manager = FindFirstObjectByType<ARWindowManager>();
    }

    public void OnTargetFound()
    {
        if (_manager == null) return;

        _manager.OnPageDetected(pageKey);
    }
}
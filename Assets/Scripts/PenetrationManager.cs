using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class PenetrationManager : MonoBehaviour
{
    [Header("Scope")]
    public Transform root;
    public LayerMask layerMask = ~0;
    public bool includeInactive = false;

    [Header("Sampling")]
    [Tooltip("统计间隔（秒）。0 = 每帧统计（更耗性能）。")]
    [Range(0f, 1f)] public float checkInterval = 0.1f;
    [Tooltip("邻域粗筛的边界扩展（米），避免全量两两比对。")]
    [Range(0f, 0.2f)] public float neighborSkin = 0.02f;

    [Header("Penetration Settings")]
    [Tooltip("穿模判定阈值（米），小于该值视为忽略")]
    public float penetrationThreshold = 0.01f;

    [SerializeField, Tooltip("最近一次统计结果：overlapPairs / testedPairs")]
    private float penetrationRate = 0f;
    [SerializeField] private int testedPairs = 0;
    [SerializeField] private int overlapPairs = 0;
    [SerializeField] private int partsCount = 0;

    public float CurrentRate { get { return penetrationRate; } }
    public int CurrentTestedPairs { get { return testedPairs; } }
    public int CurrentOverlapPairs { get { return overlapPairs; } }
    public int CurrentPartsCount { get { return partsCount; } }

    private readonly List<Collider> _parts = new List<Collider>();
    private readonly Collider[] _neighbors = new Collider[64];
    private float _acc;

    void Start()
    {
        Debug.Log("[PenetrationManager] Start()");
        RefreshParts();
        ResetStats();
    }

    void Update()
    {
        if (_parts.Count == 0)
        {
            Debug.Log("[PenetrationManager] _parts.Count == 0, 尝试再次刷新");
            RefreshParts();
        }

        _acc += Time.deltaTime;
        if (checkInterval <= 0f || _acc >= checkInterval)
        {
            _acc = 0f;
            SampleOnce();
        }
    }

    public void RefreshParts()
    {
        _parts.Clear();
        Transform scope = root ? root : transform;
        Collider[] cols = scope.GetComponentsInChildren<Collider>(includeInactive);

        Debug.Log("[PenetrationManager] RefreshParts: 找到 Collider 数量 = " + cols.Length);

        foreach (Collider c in cols)
        {
            if (c == null) continue;
            if (!c.enabled) continue;
            if (((1 << c.gameObject.layer) & layerMask.value) == 0) continue;

            Debug.Log("  收录 Collider: " + c.name + " (Layer=" + LayerMask.LayerToName(c.gameObject.layer) + ")");
            _parts.Add(c);
        }
        partsCount = _parts.Count;

        Debug.Log("[PenetrationManager] 总收录 Collider 数量 = " + partsCount);
    }

    public void ResetStats()
    {
        testedPairs = 0;
        overlapPairs = 0;
        penetrationRate = 0f;
        Debug.Log("[PenetrationManager] ResetStats()");
    }

    private void SampleOnce()
    {
        if (_parts.Count <= 1)
        {
            testedPairs = 0;
            overlapPairs = 0;
            penetrationRate = 0f;
            Debug.LogWarning("[PenetrationManager] 有效零件不足, _parts.Count = " + _parts.Count);
            return;
        }

        testedPairs = 0;
        overlapPairs = 0;

        for (int i = 0; i < _parts.Count; i++)
        {
            Collider a = _parts[i];
            if (a == null) continue;

            Bounds bnd = a.bounds;
            Vector3 ext = bnd.extents + Vector3.one * neighborSkin;
            int cnt = Physics.OverlapBoxNonAlloc(bnd.center, ext, _neighbors,
                                                 a.transform.rotation, layerMask,
                                                 QueryTriggerInteraction.Ignore);

            for (int k = 0; k < cnt; k++)
            {
                Collider b = _neighbors[k];
                if (b == null || b == a) continue;

                int j = _parts.IndexOf(b);
                if (j <= i) continue;

                testedPairs++;

                Vector3 dir;
                float dist;
                bool overlap = Physics.ComputePenetration(
                        a, a.transform.position, a.transform.rotation,
                        b, b.transform.position, b.transform.rotation,
                        out dir, out dist);

                if (overlap && dist >= penetrationThreshold)
                {
                    overlapPairs++;
                    Debug.Log("[PenetrationManager] 检测到穿模: " + a.name + " <-> " + b.name + " 深度=" + dist);
                }
                else
                {
                    Debug.Log("[PenetrationManager] 忽略或没有穿模: " + a.name + " <-> " + b.name + " 深度=" + dist);
                }
            }
        }

        penetrationRate = testedPairs == 0 ? 0f : (float)overlapPairs / (float)testedPairs;
        Debug.Log("[PenetrationManager] 本次统计: 测试对数=" + testedPairs + ", 穿模对数=" + overlapPairs + ", 穿模率=" + penetrationRate);
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.red;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = Texture2D.whiteTexture;
        GUILayout.BeginArea(new Rect(10, 10, 250, 130), boxStyle);

        GUILayout.Label("穿模统计", style);
        GUILayout.Label("零件数: " + partsCount, style);
        GUILayout.Label("穿模对数: " + overlapPairs, style);
        GUILayout.Label("穿模率: " + (penetrationRate * 100f).ToString("F2") + " %", style);
        GUILayout.Label("阈值: " + penetrationThreshold.ToString("F3") + " m", style);

        GUILayout.EndArea();
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ExplosionManager : MonoBehaviour
{
    public static ExplosionManager Instance;

    [Header("UI / Controls")]
    public bool useUnclampedExplosion;
    public Slider explosionSlider;

    [Header("Hierarchy")]
    public Transform explosionParent;

    [ContextMenuItem("Auto Collect Pieces", "AutoCollectPieces")]
    public ExplosionPiece[] explosionPieces;

    [Space]
    [Header("Initial Transform Override (Optional)")]
    public bool useNewTransformations;
    public Vector3 positionOffset;
    public Vector3 eulerRotation;
    public Vector3 localScale = Vector3.one;

    // —— 新增：可选爆炸中心 ——
    [Header("Explosion Center (NEW)")]
    public Transform explosionCenter; // 指定则以此为爆炸中心；为空则回退父物体

    // —— 新增：播放与节奏 ——
    [Header("Playback (NEW)")]
    public bool autoPlay = false;     // 空格开关
    public bool pingPong = true;      // 往返
    public float playSpeed = 0.5f;    // t 每秒推进速度（0~1）

    [Header("Easing / Curve (NEW)")]
    public AnimationCurve accelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Range / Style (NEW)")]
    public float globalDistanceMul = 1f; // 全局距离倍增
    public float perPieceDelay = 0f;     // 每件延迟（秒）
    public bool delayByDistance = false; // 按距中心远近分批
    public Vector3 delayCenter = Vector3.zero; // 本地坐标

    [Header("Noise (NEW)")]
    public float noiseAmplitude = 0f;    // 抖动幅度（米）
    public int noiseSeed = 1234;

    bool resetting;
    float _dir = 1f;
    System.Random _rng;

    void Awake()
    {
        Instance = this;

        // 编辑器外隐藏 slider（保持原逻辑）
        if (Application.platform != RuntimePlatform.WindowsEditor &&
            Application.platform != RuntimePlatform.OSXEditor)
        {
            if (explosionSlider != null)
                explosionSlider.gameObject.SetActive(false);
        }

        if (explosionSlider != null)
            explosionSlider.onValueChanged.AddListener(delegate { MoveExplosion(); });
    }

    void Start()
    {
        if (useNewTransformations && explosionParent != null)
        {
            explosionParent.position = positionOffset;
            explosionParent.localEulerAngles = eulerRotation;
            explosionParent.localScale = localScale;
        }

        if ((explosionPieces == null || explosionPieces.Length == 0) && explosionParent != null)
            AutoCollectPieces();

        if (_rng == null) _rng = new System.Random(noiseSeed);

        MoveExplosion();
    }

    void Update()
    {
        // 原 Esc 逻辑：退相机/重置
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (ExplosionCamera.hasMoved)
                ExplosionCamera.Instance.MoveCameraBack();
            else if (!resetting)
                StartCoroutine(ResetTransform());
        }

        // —— 热键 —— 
        if (Input.GetKeyDown(KeyCode.Space)) autoPlay = !autoPlay;
        if (Input.GetKeyDown(KeyCode.Equals)) playSpeed *= 1.1f;        // '='
        if (Input.GetKeyDown(KeyCode.Minus)) playSpeed /= 1.1f;        // '-'
        if (Input.GetKeyDown(KeyCode.LeftBracket)) globalDistanceMul *= 0.9f; // '['
        if (Input.GetKeyDown(KeyCode.RightBracket)) globalDistanceMul *= 1.1f; // ']'
        if (Input.GetKeyDown(KeyCode.R)) UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        // —— 自动播放推进 slider —— 
        if (autoPlay && explosionSlider != null && !ExplosionCamera.isMoving)
        {
            float v = explosionSlider.value + playSpeed * _dir * Time.deltaTime;
            if (pingPong)
            {
                if (v > explosionSlider.maxValue) { v = explosionSlider.maxValue; _dir = -_dir; }
                if (v < explosionSlider.minValue) { v = explosionSlider.minValue; _dir = -_dir; }
            }
            else
            {
                v = Mathf.Clamp(v, explosionSlider.minValue, explosionSlider.maxValue);
            }
            explosionSlider.value = v; // 触发 MoveExplosion()
        }
    }

    IEnumerator ResetTransform()
    {
        resetting = true;

        if (explosionParent != null)
        {
            if (useNewTransformations)
            {
                Quaternion target = Quaternion.Euler(eulerRotation);
                while (Quaternion.Angle(target, explosionParent.rotation) > 3f)
                {
                    explosionParent.rotation = Quaternion.RotateTowards(explosionParent.rotation, target, 2f);
                    yield return new WaitForEndOfFrame();
                }
                explosionParent.rotation = target;
            }
            else
            {
                while (explosionParent.eulerAngles.sqrMagnitude > 10f)
                {
                    explosionParent.rotation = Quaternion.RotateTowards(explosionParent.rotation, Quaternion.identity, 1f);
                    yield return new WaitForEndOfFrame();
                }
                explosionParent.rotation = Quaternion.identity;
            }
        }

        yield return StartCoroutine(ResetSlider());
        resetting = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    IEnumerator ResetSlider()
    {
        if (explosionSlider == null) yield break;

        float step = (explosionSlider.maxValue - explosionSlider.minValue) / 100f;

        if (explosionSlider.value > 0)
        {
            while (explosionSlider.value > 0)
            {
                explosionSlider.value -= step;
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            while (explosionSlider.value < 0)
            {
                explosionSlider.value += step;
                yield return new WaitForEndOfFrame();
            }
        }
        explosionSlider.value = 0;
    }

    /// 爆炸插值（增强版）
    public void MoveExplosion()
    {
        if (explosionParent == null || explosionPieces == null) return;

        float t0 = explosionSlider != null ? explosionSlider.value : 0f;

        // 按距离排序（用于延迟分批）
        if (delayByDistance && explosionPieces != null && explosionPieces.Length > 1)
        {
            Array.Sort(explosionPieces, delegate (ExplosionPiece a, ExplosionPiece b)
            {
                if (a == null || a.Piece == null) return -1;
                if (b == null || b.Piece == null) return 1;
                float da = (a.Piece.localPosition - delayCenter).sqrMagnitude;
                float db = (b.Piece.localPosition - delayCenter).sqrMagnitude;
                return da.CompareTo(db);
            });
        }

        for (int i = 0; i < explosionPieces.Length; i++)
        {
            ExplosionPiece piece = explosionPieces[i];
            if (piece == null || piece.Piece == null) continue;

            // 分批延迟
            float delay = perPieceDelay > 0f ? (i * perPieceDelay) : 0f;
            float tLocal = Mathf.Clamp01(t0 - delay);

            // 曲线缓动
            float eased = accelCurve != null ? accelCurve.Evaluate(tLocal) : tLocal;

            // 目标位（叠加全局与单件倍增）
            Vector3 end = piece.EndPoint * (globalDistanceMul * piece.pieceDistanceMul);

            // 抖动
            Vector3 jitter = Vector3.zero;
            if (noiseAmplitude > 0f)
            {
                float n1 = (float)_rng.NextDouble() * 2f - 1f;
                float n2 = (float)_rng.NextDouble() * 2f - 1f;
                float n3 = (float)_rng.NextDouble() * 2f - 1f;
                jitter = new Vector3(n1, n2, n3) * noiseAmplitude * piece.pieceNoiseMul * eased;
            }

            if (useUnclampedExplosion)
                piece.Piece.localPosition = Vector3.LerpUnclamped(piece.StartPoint, end, eased) + jitter;
            else
                piece.Piece.localPosition = Vector3.Lerp(piece.StartPoint, end, eased) + jitter;
        }
    }

    [ContextMenu("Auto Collect Pieces")]
    public void AutoCollectPieces()
    {
        if (!explosionParent)
        {
            Debug.LogWarning("[ExplosionManager] 请先指定 Explosion Parent！");
            return;
        }

        explosionPieces = explosionParent.GetComponentsInChildren<ExplosionPiece>(true);
        Debug.Log("[ExplosionManager] 已收集 " + explosionPieces.Length + " 个 ExplosionPiece。");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying &&
            explosionParent != null &&
            (explosionPieces == null || explosionPieces.Length == 0))
        {
            AutoCollectPieces();
        }
    }
#endif

    // —— 右上角显示：播放速度 + 爆炸半径(distance) —— 
    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.green;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = Texture2D.whiteTexture;

        float avgRadius = ComputeAverageExplosionRadius(); // 米

        // 右上角小面板
        GUILayout.BeginArea(new Rect(Screen.width - 240, 10, 230, 80), boxStyle);
        GUILayout.Label("参数显示", style);
        GUILayout.Label("播放速度: " + playSpeed.ToString("F2"), style);
        GUILayout.Label("爆炸半径(distance): " + avgRadius.ToString("F2") + " m", style);
        GUILayout.EndArea();
    }

    // 计算“爆炸半径”：平均(|EndPoint|) × globalDistanceMul
    float ComputeAverageExplosionRadius()
    {
        if (explosionPieces == null || explosionPieces.Length == 0) return 0f;

        float sum = 0f;
        int count = 0;

        for (int i = 0; i < explosionPieces.Length; i++)
        {
            ExplosionPiece p = explosionPieces[i];
            if (p == null || p.Piece == null) continue;

            float len = p.EndPoint.magnitude;
            sum += len;
            count++;
        }

        if (count == 0) return 0f;
        float avgBase = sum / count;
        return avgBase * globalDistanceMul;
    }
}

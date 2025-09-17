using UnityEngine;

public class ExplosionPiece : MonoBehaviour
{
    public bool referOrigin = true;
    public float distance = 5f;

    // —— 单件可调 —— 
    [Header("Per-Piece Tuning (NEW)")]
    public float pieceDistanceMul = 1f; // 单件距离倍增
    public float pieceNoiseMul = 1f;    // 单件抖动倍增

    public Transform Piece { private set; get; }
    public Vector3 StartPoint { private set; get; }
    public Vector3 EndPoint { private set; get; }

    float lastTapTime = -1;
    const float DOUBLE_TAP_TIME = .3f;

    void Awake()
    {
        Piece = transform;
        StartPoint = transform.position;

        if (referOrigin)
        {
            // 优先使用 ExplosionManager.explosionCenter
            Vector3 dirLocal;

            if (ExplosionManager.Instance != null && ExplosionManager.Instance.explosionCenter != null)
            {
                // 用“中心→本件”的方向；转换到父节点的局部方向，确保 EndPoint 与 localPosition 同空间
                Vector3 worldDir = (transform.position - ExplosionManager.Instance.explosionCenter.position).normalized;
                dirLocal = transform.parent != null
                    ? transform.parent.InverseTransformDirection(worldDir)
                    : worldDir; // 无父节点则世界方向直接用
            }
            else
            {
                // 回退到：以父物体为中心
                dirLocal = transform.localPosition.normalized;
            }

            EndPoint = dirLocal.normalized * (distance * pieceDistanceMul);
        }
        else
        {
            // 保持你原有的“取第一个子物体的位置”作为目标（如需严格局部，可改为 InverseTransformPoint）
            EndPoint = transform.GetChild(0).position;
        }
    }

    private void OnMouseUpAsButton()
    {
        if (lastTapTime > 0 && Time.timeSinceLevelLoad - lastTapTime < DOUBLE_TAP_TIME)
            ExplosionCamera.Instance.MoveCameraTo(transform);
        lastTapTime = Time.timeSinceLevelLoad;
    }
}

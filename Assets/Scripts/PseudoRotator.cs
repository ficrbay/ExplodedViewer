using UnityEngine;

public class PseudoRotator : MonoBehaviour
{
    public static Transform target;
    public float dampening = 10;

    void Start()
    {
        target = transform;
    }

    void LateUpdate()
    {
        // 触屏（原逻辑保留）
        if (Input.touchCount == 1)
        {
            if (ExplosionCamera.isMoving) return;
            Vector2 touchDelta = Input.GetTouch(0).deltaPosition * (1f / dampening);
            if (ExplosionCamera.hasMoved)
                target.Rotate(-touchDelta.y, touchDelta.x, 0, Space.World);
            else
                target.Rotate(touchDelta.y, -touchDelta.x, 0, Space.World);
        }
        // —— 新增：桌面鼠标左键拖拽 —— 
        else if (Input.GetMouseButton(0))
        {
            if (ExplosionCamera.isMoving) return;
            float dx = Input.GetAxis("Mouse X") * (1f / dampening) * 10f;
            float dy = Input.GetAxis("Mouse Y") * (1f / dampening) * 10f;
            if (ExplosionCamera.hasMoved)
                target.Rotate(-dy, dx, 0, Space.World);
            else
                target.Rotate(dy, -dx, 0, Space.World);
        }
    }
}

using UnityEngine;

public class PseudoScaler : MonoBehaviour
{
    public float dampening = 20;

    Touch touch1, touch2;
    Vector2[] lastScaleValue;
    bool wasScalingLastFrame;

    void Update()
    {
        // 触屏双指缩放（原逻辑保留）
        if (Input.touchCount == 2)
        {
            if (ExplosionCamera.hasMoved) return;

            touch1 = Input.GetTouch(0);
            touch2 = Input.GetTouch(1);

            if (touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
            {
                Vector2[] newScaleValue = { touch1.position, touch2.position };
                if (!wasScalingLastFrame)
                {
                    lastScaleValue = newScaleValue;
                    wasScalingLastFrame = true;
                }
                else
                {
                    float newDistance = Vector2.Distance(newScaleValue[0], newScaleValue[1]);
                    float oldDistance = Vector2.Distance(lastScaleValue[0], lastScaleValue[1]);
                    float offset = (newDistance - oldDistance) / dampening;

                    if (ExplosionManager.Instance != null && ExplosionManager.Instance.explosionSlider != null)
                        ExplosionManager.Instance.explosionSlider.value += offset * Time.deltaTime;

                    lastScaleValue = newScaleValue;
                }
            }
            if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
                wasScalingLastFrame = false;

            return;
        }

        // —— 新增：桌面鼠标滚轮 -> Slider —— 
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f &&
            ExplosionManager.Instance != null &&
            ExplosionManager.Instance.explosionSlider != null)
        {
            ExplosionManager.Instance.explosionSlider.value += scroll * (1f / dampening);
        }
    }
}

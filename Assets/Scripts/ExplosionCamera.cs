using System.Collections;
using UnityEngine;

public class ExplosionCamera : MonoBehaviour
{
    public static ExplosionCamera Instance;
    public static bool hasMoved;
    public static bool isMoving;

    Vector3 origPosition;

    // —— 新增：可调速度 ——
    public float moveSpeed = 2f;   // m/s
    public float rotSpeed = 180f;  // deg/s

    private void Awake()
    {
        if (!Instance) Instance = this;
        origPosition = transform.position;
    }

    public void MoveCameraTo(Transform target)
    {
        if (!isMoving) StartCoroutine(MoveCamera(target));
    }

    public void MoveCameraBack()
    {
        if (!isMoving) StartCoroutine(MoveCamera(origPosition));
    }

    IEnumerator MoveCamera(Transform target)
    {
        Vector3 targetPosition = target.position;
        isMoving = true;

        while ((transform.position - targetPosition).sqrMagnitude > 1e-6f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        hasMoved = targetPosition != origPosition;
        PseudoRotator.target = targetPosition == origPosition ? ExplosionManager.Instance.explosionParent : transform;
        isMoving = false;
    }

    IEnumerator MoveCamera(Vector3 targetPosition)
    {
        isMoving = true;

        // 回正旋转
        while (Quaternion.Angle(transform.rotation, Quaternion.identity) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.identity, rotSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }
        transform.rotation = Quaternion.identity;

        // 平移
        while ((transform.position - targetPosition).sqrMagnitude > 1e-6f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return new WaitForEndOfFrame();
        }

        PseudoRotator.target = targetPosition == origPosition ? ExplosionManager.Instance.explosionParent : transform;
        hasMoved = targetPosition != origPosition;
        isMoving = false;
    }
}

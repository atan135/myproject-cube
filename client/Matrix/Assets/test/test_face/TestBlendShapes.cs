using UnityEngine;
using System.Collections;

public class SimpleFaceController : MonoBehaviour
{
    private SkinnedMeshRenderer meshRenderer;

    [Header("设置")]
    public string blinkShapeName = "eyeBlinkLeft"; // 也可以同时控制左右眼
    public float blinkSpeed = 15f;    // 眨眼速度
    public float minWaitTime = 1f;    // 最小间隔
    public float maxWaitTime = 5f;    // 最大间隔

    void Start()
    {
        meshRenderer = GetComponent<SkinnedMeshRenderer>();
        StartCoroutine(BlinkRoutine());
    }
    
    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            // 1. 随机等待一段时间
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));

            // 2. 闭眼
            yield return LerpShape(blinkShapeName, 100f, blinkSpeed);
            // 3. 睁眼
            yield return LerpShape(blinkShapeName, 0f, blinkSpeed);
        }
    }

    IEnumerator LerpShape(string shapeName, float target, float speed)
    {
        int index = meshRenderer.sharedMesh.GetBlendShapeIndex(shapeName);
        if (index == -1) yield break;

        float current = meshRenderer.GetBlendShapeWeight(index);
        while (Mathf.Abs(current - target) > 0.1f)
        {
            current = Mathf.MoveTowards(current, target, speed * Time.deltaTime * 100f);
            meshRenderer.SetBlendShapeWeight(index, current);

            // 技巧：如果你想让双眼同步，可以同时设置左右眼的Index
            // meshRenderer.SetBlendShapeWeight(indexRight, current);

            yield return null;
        }
    }
}

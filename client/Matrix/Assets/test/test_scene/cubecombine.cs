using UnityEngine;
using System.Collections.Generic;

public class SkeletonCombiner : MonoBehaviour
{
    public string targetLayer = "MapIcon";

    void Awake()
    {
        // 1. 收集所有属于 MapIcon 层的 MeshFilter
        List<CombineInstance> combineList = new List<CombineInstance>();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        Material skeletonMaterial = null;

        foreach (var renderer in renderers)
        {
            if (renderer.gameObject.layer == LayerMask.NameToLayer(targetLayer))
            {
                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf == null) continue;

                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                // 关键：将子 Cube 的坐标、旋转、缩放全部烘焙进矩阵
                ci.transform = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                combineList.Add(ci);

                if (skeletonMaterial == null) skeletonMaterial = renderer.sharedMaterial;

                // 彻底禁用原始子物体，释放 CPU Transform 更新压力
                renderer.gameObject.SetActive(false);
            }
        }

        if (combineList.Count == 0) return;

        // 2. 创建合并后的新物体
        GameObject combinedObj = new GameObject("Combined_Skeleton");
        combinedObj.transform.SetParent(this.transform, false);
        combinedObj.layer = LayerMask.NameToLayer(targetLayer);

        MeshFilter newMf = combinedObj.AddComponent<MeshFilter>();
        newMf.mesh = new Mesh();
        newMf.mesh.name = "MergedSkeletonMesh";
        newMf.mesh.CombineMeshes(combineList.ToArray(), true, true);

        MeshRenderer newMr = combinedObj.AddComponent<MeshRenderer>();
        newMr.sharedMaterial = skeletonMaterial;
        
        // 确保新物体也不投射阴影
        newMr.castShadows = false;
        newMr.receiveShadows = false;
    }
}

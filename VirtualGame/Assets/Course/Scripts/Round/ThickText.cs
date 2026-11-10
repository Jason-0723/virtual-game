using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 把字描粗一點：辰宇落雁體是細體，小字在螢幕上會太細看不清楚
// 做法：把每個字的網格往右、往下各多畫一份（顏色跟著文字本身，換顏色也不會出錯）
[RequireComponent(typeof(Text))]
public class ThickText : BaseMeshEffect
{
    static readonly List<UIVertex> verts = new List<UIVertex>();

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;
        float a = GetComponent<Text>().fontSize * 0.035f;
        verts.Clear();
        vh.GetUIVertexStream(verts);
        int n = verts.Count;
        foreach (Vector3 o in new[] { new Vector3(a, 0), new Vector3(0, -a), new Vector3(a, -a) })
            for (int i = 0; i < n; i++) { UIVertex v = verts[i]; v.position += o; verts.Add(v); }
        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }

    // 只有手寫字型需要加粗
    public static void Apply(Text t)
    {
        if (t.font && t.font.name.Contains("ChenYu") && !t.GetComponent<ThickText>()) t.gameObject.AddComponent<ThickText>();
    }
}

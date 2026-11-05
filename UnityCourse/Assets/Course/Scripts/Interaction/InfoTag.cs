using System.Collections.Generic;
using UnityEngine;

// 第 8 週：掛在「可以被點選的物件」上，存名稱與描述，並負責高亮
public class InfoTag : MonoBehaviour
{
    public string displayName = "物件名稱";
    [TextArea(2, 5)]
    public string description = "在這裡寫物件的描述";

    public bool IsSelected { get; private set; }

    // 記住每個材質原本的顏色，取消選取時還原
    private readonly Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();

    public void SetHighlight(bool on, Color highlightColor)
    {
        IsSelected = on;
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials)   // r.materials 會複製一份材質，不會改到原始檔
            {
                string prop = m.HasProperty("_BaseColor") ? "_BaseColor"     // URP Lit
                            : m.HasProperty("_Color")     ? "_Color"         // Built-in / 其他
                            : null;
                if (prop == null) continue;

                if (on)
                {
                    if (!originalColors.ContainsKey(m)) originalColors[m] = m.GetColor(prop);
                    m.SetColor(prop, highlightColor);
                }
                else if (originalColors.TryGetValue(m, out Color c))
                {
                    m.SetColor(prop, c);
                }
            }
        }
    }
}

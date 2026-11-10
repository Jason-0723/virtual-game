using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 術語翻譯：左邊是工程師常講的詞，右邊是白話解釋（打亂）；先點詞，再點它的意思
// 給完全不懂程式的玩家：玩完就聽得懂同事在講什麼
public class JargonGame : MonitorGame
{
    public override string Title => "工程師術語翻譯";

    static readonly string[,] Pairs =
    {
        { "Bug", "程式裡的錯誤" },
        { "伺服器", "24 小時不關機、負責回應大家的電腦" },
        { "資料庫", "存所有訂單和會員資料的地方" },
        { "上線", "把新版本放出去給所有客人用" },
        { "雲端", "租別人機房裡的電腦來用" },
        { "API", "兩個程式互相傳話的窗口" },
        { "快取", "常用的東西先放手邊，拿比較快" },
        { "當機", "系統卡住、沒有反應" },
        { "備份", "另外存一份，壞了可以救回來" },
        { "防火牆", "擋掉可疑連線的門口警衛" },
    };

    List<int> picked;
    readonly Dictionary<int, Button> terms = new Dictionary<int, Button>(), meanings = new Dictionary<int, Button>();
    int selected, matched, mistakes;
    Text hint;

    protected override void Setup()
    {
        int n = Pairs.GetLength(0);
        picked = new List<int>();
        for (int i = 0; i < n; i++) picked.Add(i);
        CampaignDirector.Shuffle(picked, new System.Random());
        picked = picked.GetRange(0, Mathf.Min(6, 4 + difficulty));
        terms.Clear(); meanings.Clear();
        selected = -1; matched = mistakes = 0;
        Label("先點左邊的術語，再點右邊它的意思", 22, Ink, new Vector2(0, 175), new Vector2(880, 34));
        hint = Label("", 20, Ink, new Vector2(0, -172), new Vector2(880, 30));
        var right = new List<int>(picked);
        CampaignDirector.Shuffle(right, new System.Random());
        for (int i = 0; i < picked.Count; i++)
        {
            int t = picked[i], m = right[i];
            terms[t] = Btn(Pairs[t, 0], Paper, Ink, new Vector2(-310, 125 - i * 52), new Vector2(220, 46), () => Term(t));
            meanings[m] = Btn(Pairs[m, 1], Paper, Ink, new Vector2(120, 125 - i * 52), new Vector2(560, 46), () => Meaning(m));
            meanings[m].GetComponentInChildren<Text>().fontSize = 21;
        }
    }

    void Term(int t)
    {
        if (!Running || !terms[t].interactable) return;
        if (selected >= 0) terms[selected].GetComponent<Image>().color = Paper;
        selected = t;
        terms[t].GetComponent<Image>().color = Gold;
    }

    void Meaning(int m)
    {
        if (!Running || selected < 0 || !meanings[m].interactable) return;
        if (m != selected) { mistakes++; hint.text = $"「{Pairs[selected, 0]}」不是這個意思，再想想"; return; }
        Color done = new Color32(0xb8, 0xec, 0xc9, 255);
        foreach (Button b in new[] { terms[m], meanings[m] }) { b.interactable = false; b.GetComponent<Image>().color = done; }
        hint.text = $"對！{Pairs[m, 0]}＝{Pairs[m, 1]}";
        selected = -1;
        if (++matched >= picked.Count) Finish(mistakes == 0 ? 3 : mistakes <= 2 ? 2 : 1, $"全部配對完成，配錯 {mistakes} 次");
    }

    protected override string Goal() => $"配對 {matched} / {picked.Count}　配錯 {mistakes} 次";
    protected override void Tick() { }
    protected override void TimeUp() => Finish(matched * 2 >= picked.Count ? 1 : 0, $"時間到：配對了 {matched} / {picked.Count}");

    public override void AutoPlay()
    {
        if (selected < 0) { foreach (var kv in terms) if (kv.Value.interactable) { Term(kv.Key); return; } }
        else Meaning(selected);
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// 半動態事件的規則檢查：權重抽籤、技術債讓 bug 事件變多、events.json 內容一致
public class EventScheduleTest
{
    [Serializable] class EventList { public int[] slots; public DayManager.GameEvent[] main, pool; }
    static EventList Load() => JsonUtility.FromJson<EventList>(Resources.Load<TextAsset>("events").text);

    [Test]
    public void WeightedPickFollowsWeights()
    {
        var w = new List<float> { 1, 0, 3 };
        Assert.AreEqual(0, DayManager.WeightedPick(w, 0.0f));
        Assert.AreEqual(0, DayManager.WeightedPick(w, 0.2f));    // 0.8 < 1
        Assert.AreEqual(2, DayManager.WeightedPick(w, 0.5f));    // 2.0 落在第三格
        Assert.AreEqual(2, DayManager.WeightedPick(w, 1.0f));    // 邊界
        Assert.AreEqual(-1, DayManager.WeightedPick(new List<float> { 0, 0 }, 0.5f));
    }

    [Test]
    public void TechDebtMakesBugsLikelier()
    {
        EventList d = Load();
        var afternoon = Array.FindAll(d.pool, e => e.minAt >= 720);
        float BugShare(int debt)
        {
            float bug = 0, all = 0;
            foreach (var e in afternoon)
            {
                float w = DayManager.Weight(e, debt, 30, 50);
                all += w;
                if (e.bug) bug += w;
            }
            return bug / all;
        }
        Debug.Log($"[Schedule] 下午 bug 事件機率：技術債 0 → {BugShare(0):P0}，35 → {BugShare(35):P0}，70 → {BugShare(70):P0}");
        Assert.Less(BugShare(0), 0.1f);
        Assert.Greater(BugShare(35), 0.5f);
        Assert.Greater(BugShare(70), BugShare(35));
    }

    [Test]
    public void EventsJsonIsConsistent()
    {
        EventList d = Load();
        var ids = new HashSet<string>();
        var all = new List<DayManager.GameEvent>(d.main);
        all.AddRange(d.pool);
        foreach (var e in all)
            foreach (var o in e.options)
            {
                Assert.IsFalse(string.IsNullOrEmpty(o.id), e.title + " 有選項沒 id");
                Assert.IsTrue(ids.Add(o.id), "選項 id 重複：" + o.id);
            }
        foreach (var e in all)
            if (e.causedBy != null)
                foreach (string c in e.causedBy) Assert.IsTrue(ids.Contains(c), e.title + " 的 causedBy 找不到：" + c);
        foreach (var e in d.main) Assert.Greater(e.at, 0, e.title + " 沒有 at");
        Assert.LessOrEqual(all.Find(e => e.title == "早上的站立會議").at, 540);
    }
}

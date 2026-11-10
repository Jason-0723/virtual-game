using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

// events.json 的規則檢查：權重抽籤、技術債讓 bug 事件變多、id 一致、死罪選項的位置、待辦和摸魚內容齊全
public class EventScheduleTest
{
    static DayManager.EventList Load() => JsonUtility.FromJson<DayManager.EventList>(Resources.Load<TextAsset>("events").text);

    static List<DayManager.GameEvent> AllEvents(DayManager.EventList d)
    {
        var all = new List<DayManager.GameEvent>(d.pool);
        foreach (var day in d.days) all.AddRange(day.main);
        return all;
    }

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
        DayManager.EventList d = Load();
        var wednesday = Array.FindAll(d.pool, e => e.days == null || e.days.Length == 0 || Array.IndexOf(e.days, 2) >= 0);
        float BugShare(int debt)
        {
            float bug = 0, all = 0;
            foreach (var e in wednesday)
            {
                float w = DayManager.Weight(e, debt, 30, 50);
                all += w;
                if (e.debtWeight > 0) bug += w;
            }
            return bug / all;
        }
        Debug.Log($"[Schedule] 週三抽到「技術債 bug」的機率：技術債 0 → {BugShare(0):P0}，35 → {BugShare(35):P0}，70 → {BugShare(70):P0}");
        Assert.Less(BugShare(0), 0.1f);
        Assert.Greater(BugShare(70), BugShare(35));
        Assert.Greater(BugShare(35), BugShare(0) * 4);
    }

    [Test]
    public void EventsJsonIsConsistent()
    {
        DayManager.EventList d = Load();
        Assert.AreEqual(3, d.days.Length, "應該有週一、週三、週五三天");
        var ids = new HashSet<string>();
        var all = AllEvents(d);
        foreach (var e in all)
            foreach (var o in e.options)
            {
                Assert.IsFalse(string.IsNullOrEmpty(o.id), e.title + " 有選項沒 id");
                Assert.IsTrue(ids.Add(o.id), "選項 id 重複：" + o.id);
                if (!string.IsNullOrEmpty(o.startTask))
                    Assert.IsNotNull(Array.Find(d.tasks, t => t.id == o.startTask), o.id + " 的 startTask 找不到：" + o.startTask);
            }
        foreach (var e in all)
        {
            if (e.causedBy != null)
                foreach (string c in e.causedBy) Assert.IsTrue(ids.Contains(c), e.title + " 的 causedBy 找不到：" + c);
            // 死罪選項放在中間：不會是最安全的第一個，也不是自動試玩「永遠選最後一個」會踩到的位置
            for (int i = 0; i < e.options.Length; i++)
                if (!string.IsNullOrEmpty(e.options[i].gameOver))
                    Assert.IsTrue(i > 0 && i < e.options.Length - 1, e.title + " 的死罪選項要放在中間");
        }
        int deadly = 0;
        foreach (var e in all) foreach (var o in e.options) if (!string.IsNullOrEmpty(o.gameOver)) deadly++;
        Assert.GreaterOrEqual(deadly, 10, "死罪選項不到 10 個");
        Assert.GreaterOrEqual(d.slack.Length, 12, "摸魚內容不到 12 則");
        Assert.GreaterOrEqual(Array.FindAll(d.tasks, t => t.random).Length, 4, "隨機待辦太少");
        foreach (var t in d.tasks) Assert.IsTrue(t.chars > 0 || !string.IsNullOrEmpty(t.place), t.id + " 既不用打字也不用走去哪");
    }
}

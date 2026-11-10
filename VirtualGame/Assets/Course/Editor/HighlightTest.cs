using NUnit.Framework;

// 打字畫面的語法上色：註解和字串裡的字不能再被拆開上色
public class HighlightTest
{
    [Test]
    public void ColorsTokensLikeVsCode()
    {
        string h = TypingTerminal.Highlight("public int x = 1; // public 不能再變藍");
        StringAssert.StartsWith("<color=#569cd6>public</color>", h);
        StringAssert.Contains("<color=#6a9955>// public 不能再變藍</color>", h);
        StringAssert.Contains("<color=#ce9178>\"Desk\"</color>", TypingTerminal.Highlight("place == \"Desk\""));
        StringAssert.Contains("<color=#4ec9b0>DayManager</color>", TypingTerminal.Highlight("DayManager.Instance"));
        Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(TypingTerminal.Highlight("\"打到一半的字串"), "<color").Count);
    }
}

using AutoClick.utils;
using Newtonsoft.Json.Linq;

namespace AutoClick.model.impl;

public class ClickAction(MatchResult matchResult, JObject action) : IAction
{
    public JObject Action { get; set; } = action;
    public MatchResult MatchResult { get; set; } = matchResult;

    public void DoAction()
    {
        Console.WriteLine($"Click action at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        MouseUtil.Click();
    }
}
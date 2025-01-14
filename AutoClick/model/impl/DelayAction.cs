using AutoClick.utils;
using Newtonsoft.Json.Linq;

namespace AutoClick.model.impl;

public class DelayAction(MatchResult result, JObject action) : IAction
{
    public MatchResult MatchResult { get; set; } = result;
    public JObject Action { get; set; } = action;

    public void DoAction()
    {
        var delay = SafeConvertUtil.ToInt(Action["Delay"]);
        Console.WriteLine($"Delay action: {delay} at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Thread.Sleep(delay);
    }
}
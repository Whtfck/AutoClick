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
        LogUtil.Info($"Delay action: {delay} ms");
        Thread.Sleep(delay);
    }
}
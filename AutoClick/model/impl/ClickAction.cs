using AutoClick.utils;
using Newtonsoft.Json.Linq;

namespace AutoClick.model.impl;

public class ClickAction(MatchResult matchResult, JObject action) : IAction
{
    public JObject Action { get; set; } = action;
    public MatchResult MatchResult { get; set; } = matchResult;

    public void DoAction()
    {
        MouseUtil.Click();
    }
}
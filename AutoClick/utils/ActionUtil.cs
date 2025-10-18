using AutoClick.model;
using AutoClick.model.impl;
using Newtonsoft.Json.Linq;

namespace AutoClick.utils;

public class ActionUtil
{
    public static void DoActions(RECT rect, JArray actions, MatchResult result)
    {
        foreach (var actObj in actions)
        {
            IAction action = GetAction(rect, SafeConvertUtil.ToJObject(actObj), result);
            action.DoAction();
        }
    }

    public static IAction GetAction(RECT rect, JObject action, MatchResult result)
    {
        action["Rect"] = JObject.FromObject(rect);
        string type = SafeConvertUtil.ObjToString(action["Type"]);
        switch (type)
        {
            case "move":
                return new MoveAction(result, action);
            case "click":
                return new ClickAction(result, action);
            case "delay":
                return new DelayAction(result, action);
        }

        LogUtil.Warning($"Unsupported action type: {type}");
        return new EmptyAction(result, action);
    }
}
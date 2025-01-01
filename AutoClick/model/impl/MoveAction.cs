using System.Drawing;
using AutoClick.utils;
using Newtonsoft.Json.Linq;

namespace AutoClick.model.impl;

public class MoveAction(MatchResult matchResult, JObject action) : IAction
{
    public JObject Action { get; set; } = action;
    public MatchResult MatchResult { get; set; } = matchResult;

    public void DoAction()
    {
        JObject offset = SafeConvertUtil.ToJObject(Action["Offset"]);
        // 目标程序RECT
        RECT rect = SafeConvertUtil.ToRect(Action["Rect"]);
        
        Point absPoint = CaptureUtil.GetAbsPoint(rect, MatchResult);
        
        absPoint.X += SafeConvertUtil.ToInt(offset["X"]);
        absPoint.Y += SafeConvertUtil.ToInt(offset["Y"]);
        
        MouseUtil.Move(absPoint);
    }
}
using Newtonsoft.Json.Linq;

namespace AutoClick.utils;

public static class SafeConvertUtil
{
    public static string ObjToString(object? obj)
    {
        return obj as string ?? (obj?.ToString() ?? string.Empty);
    }

    public static JArray ToJArray(object? obj)
    {
        return obj as JArray ?? [];
    }

    public static JObject ToJObject(object? obj)
    {
        return obj as JObject ?? ((obj is string s) ? JObject.Parse(s) : new JObject());
    }

    public static int ToInt(object? obj)
    {
        return obj is JValue jValue ? jValue.ToObject<int>() : 0;
    }

    public static double ToDouble(object? obj)
    {
        return obj is JValue jValue ? jValue.ToObject<double>() : 0;
    }

    public static RECT ToRect(object? obj)
    {
        return ToJObject(obj).ToObject<RECT>();
    }
}
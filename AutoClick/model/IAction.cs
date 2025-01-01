using Newtonsoft.Json.Linq;

namespace AutoClick.model;

public interface IAction
{
    public JObject Action { get; set; }
    public MatchResult MatchResult { get; set; }
    void DoAction();
}
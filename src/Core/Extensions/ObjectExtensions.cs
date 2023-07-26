using System.Text.Json.Serialization;
using System.Text.Json;

namespace MaksIT.Core.Extensions {
  public static class ObjectExtensions {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string ToJson<T>(this T? obj) => obj.ToJson(null);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="converters"></param>
    /// <returns></returns>
    public static string ToJson<T>(this T? obj, List<JsonConverter>? converters) {
      if (obj == null)
        return "{}";

      var options = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
      };

      converters?.ForEach(x => options.Converters.Add(x));

      return JsonSerializer.Serialize(obj, options);
    }
  }
}

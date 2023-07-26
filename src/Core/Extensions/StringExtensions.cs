using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaksIT.Core.Extensions {
  public static class StringExtensions {
    /// <summary>
    /// Converts JSON string to object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="s"></param>
    /// <returns></returns>
    public static T? ToObject<T>(this string? s) => ToObjectCore<T>(s, null);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="s"></param>
    /// <param name="converters"></param>
    /// <returns></returns>
    public static T? ToObject<T>(this string? s, List<JsonConverter> converters) => ToObjectCore<T>(s, converters);

    private static T? ToObjectCore<T>(string? s, List<JsonConverter>? converters) {
      var options = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true
      };

      converters?.ForEach(x => options.Converters.Add(x));

      return s != null
        ? JsonSerializer.Deserialize<T>(s, options)
        : default;
    }
  }
}

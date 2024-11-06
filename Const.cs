using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparrow.UPnP;

public abstract class Const {

   public static readonly JsonSerializerOptions JsonOptions = new() {
      IgnoreReadOnlyFields = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = true
   };

   public static readonly JsonSerializerOptions JsonMaskedOptions = new() {
      IgnoreReadOnlyFields = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      WriteIndented = true,
      Converters = { new MaskingJsonConverter<object>() }
   };

   private class MaskingJsonConverter<T> : JsonConverter<T>
   {
      public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
         return JsonSerializer.Deserialize<T>(ref reader, options);
      }

      public static bool IsEndWithAny(string propName, params string[] keywords) {
         return keywords.Any(keyword => propName.EndsWith(keyword, StringComparison.OrdinalIgnoreCase));
      }

      public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
         writer.WriteStartObject();
         foreach (var property in typeof(T).GetProperties()) {
            var propName = property.Name;
            var propValue = property.GetValue(value)?.ToString();

            writer.WritePropertyName(propName);
            JsonSerializer.Serialize(writer, propValue, options);
         }
         writer.WriteEndObject();
      }
   }
}
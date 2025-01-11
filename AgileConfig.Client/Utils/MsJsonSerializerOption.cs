using System.Text.Json;

namespace AgileConfig.Client.Utils
{
    public static class MsJsonSerializerOption
    {
        public static readonly JsonSerializerOptions Default = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}

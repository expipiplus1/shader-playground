using System.Collections.Generic;
using System.Text.Json;

namespace ShaderPlayground.Core.Util
{
    internal sealed class JsonTable
    {
        public JsonTableRow Header { get; set; }

        public List<JsonTableRow> Rows { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }

    internal sealed class JsonTableRow
    {
        public string[] Data { get; set; }
    }
}

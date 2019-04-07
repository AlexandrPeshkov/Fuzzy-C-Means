using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fuzzy_C_Means
{
    public static class D3Writer
    {
        public static void WriteD3Json(NodeD3 head, string fileName = "d3.json")
        {
            var data = JsonConvert.SerializeObject(head, Formatting.Indented);

            System.IO.File.WriteAllText(fileName, data);
        }

        public class NodeD3
        {
            [JsonProperty("n")]
            public string Name { get; set; }

            [JsonProperty("d")]
            public double Value { get; set; }

            [JsonProperty("c")]
            public List<NodeD3> Children { get; set; }
        }
    }
}

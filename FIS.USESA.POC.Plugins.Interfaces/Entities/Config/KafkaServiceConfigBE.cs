using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace FIS.USESA.POC.Plugins.Interfaces.Entities.Config
{
    /// <summary>
    /// This class describes the Kafka Config
    /// </summary>
    public class KafkaServiceConfigBE
    {
        [JsonPropertyName(@"bootstrapServers")]
        public string BootstrapServers { get; set; }
        [JsonPropertyName(@"schemaRegistry")]
        public string SchemaRegistry { get; set; }
    }
}

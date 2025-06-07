namespace COMMON.Configuration
{
    public class KafkaConfig
    {
        public string? BootstrapServers { get; set; }

        public string? SchemaRegistryUrl { get; set; }
        public string? ServiceName { get; set; } // sevrice name
    }
}

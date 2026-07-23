namespace FlowFlex.Application.Contracts.Options
{
    public class KafkaOptions
    {
        public const string SectionName = "Kafka";

        public List<string> BootstrapServers { get; set; } = new();

        public int SecurityProtocol { get; set; }

        public Dictionary<string, string> Topics { get; set; } = new();
    }
}

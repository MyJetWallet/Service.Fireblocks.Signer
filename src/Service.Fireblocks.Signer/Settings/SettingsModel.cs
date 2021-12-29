using MyJetWallet.Sdk.Service;
using MyYamlParser;

namespace Service.Fireblocks.Signer.Settings
{
    public class SettingsModel
    {
        [YamlProperty("FireblocksSigner.SeqServiceUrl")]
        public string SeqServiceUrl { get; set; }

        [YamlProperty("FireblocksSigner.ZipkinUrl")]
        public string ZipkinUrl { get; set; }

        [YamlProperty("FireblocksSigner.ElkLogs")]
        public LogElkSettings ElkLogs { get; set; }

        [YamlProperty("FireblocksSigner.BaseUrl")]
        public string FireblocksBaseUrl { get; set; }

        [YamlProperty("FireblocksSigner.MyNoSqlWriterUrl")]
        public string MyNoSqlWriterUrl { get; internal set; }

        [YamlProperty("FireblocksSigner.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; internal set; }
    }
}

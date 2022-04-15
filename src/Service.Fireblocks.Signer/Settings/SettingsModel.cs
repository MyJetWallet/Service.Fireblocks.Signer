using MyJetWallet.Sdk.Service;
using MyYamlParser;
using System;

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
        public string MyNoSqlWriterUrl { get; set; }

        [YamlProperty("FireblocksSigner.MyNoSqlReaderHostPort")]
        public string MyNoSqlReaderHostPort { get; set; }

        [YamlProperty("FireblocksSigner.ApiKeyId")]
        public string ApiKeyId { get; set; }

        [YamlProperty("FireblocksSigner.SignaturePublicApiKeyId")]
        public string SignaturePublicApiKeyId { get; set; }

        [YamlProperty("FireblocksSigner.CheckTransactionSignature")]
        public bool CheckTransactionSignature { get; set; }

        [YamlProperty("FireblocksSigner.SignatureValidFor")]
        public TimeSpan SignatureValidFor { get; set; }
    }
}

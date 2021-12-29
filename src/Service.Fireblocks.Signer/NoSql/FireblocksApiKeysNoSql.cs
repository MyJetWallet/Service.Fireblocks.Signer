namespace Service.Fireblocks.Signer.NoSql
{
    public class FireblocksApiKeysNoSql : MyNoSqlServer.Abstractions.MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-fireblocks-keys";

        public static string GeneratePartitionKey() => "Key";

        public static string GenerateRowKey() => "Signer";

        public string ApiKey { get; set; }

        public string PrivateKey { get; set; }

        public static FireblocksApiKeysNoSql Create(string apiKey, string privateKey)
        {
            return new FireblocksApiKeysNoSql()
            {
                ApiKey = apiKey,
                PrivateKey = privateKey,
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
            };
        }
    }
}

using System;
using System.Text;

namespace Service.Fireblocks.Signer.Settings
{
    public class EnvSettingsModel
    {
        public string ENCRYPTION_KEY { get; set; }

        public string GetEncryptionKey()
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ENCRYPTION_KEY.Trim()));
        }

    }
}

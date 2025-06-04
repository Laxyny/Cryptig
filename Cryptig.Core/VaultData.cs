using System.Collections.Generic;

namespace Cryptig.Core
{
    public class VaultData
    {
        /// <summary>
        /// Base32 encoded secret used for generating TOTP codes. If null or empty,
        /// two-factor authentication is considered disabled.
        /// </summary>
        public string? TwoFactorSecret { get; set; }

        public List<VaultEntry> Entries { get; set; } = new();
    }
}

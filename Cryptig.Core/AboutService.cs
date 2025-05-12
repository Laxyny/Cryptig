namespace Cryptig.Core
{
    public static class AboutService
    {
        public static string AppName => "Cryptig";
        public static string Version => "0.1.0";
        public static string Author => "Kevin GREGOIRE";
        public static string License => "MPL-2.0 License";
        public static string GitHub => "https://github.com/Laxyny/Cryptig";
        public static string Description =>
            "Cryptig is a local, open-source and secure password manager.\n" +
            "Encryption: Argon2id + AES-GCM.\n" +
            "Fully offline, no external connection required.";
    }
}
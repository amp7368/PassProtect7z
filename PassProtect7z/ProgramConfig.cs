using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace PassProtect7z {
    class ProgramConfig {
        private static ProgramConfig instance;
        private static string SaveFile = "";

        public string[] IgnoreEncryptedPasswords = Array.Empty<string>();
        public bool? ShouldEncryptStandaloneFiles = null;
        public bool AlwaysEncryptAgain = false;
        public DateTime SkipEncryptionTimestamp = DateTime.Now;
        public bool IsThisRootFolder = false;
        public bool ShouldVerify7zContents = true;
        public string? LastPasswordHash;
        public bool RandomizeFileOrder = true;

        public static void Load(string file) {
            FileInfo config = new(file);
            if (!config.Exists) {
                instance = new ProgramConfig();
            } else {
                instance = JsonConvert.DeserializeObject<ProgramConfig>(File.ReadAllText(file)) ??
                    throw new Exception($"Error Parsing {FileUtils.CONFIG}");
            }
            SaveFile = file;
            Save();
        }
        public static ProgramConfig get() {
            return instance;
        }
        public bool isFileCreationAfter(string file) {
            return SkipEncryptionTimestamp < File.GetCreationTime(file);
        }
        public string SkipEncryptionTimestampFormat() {
            return SkipEncryptionTimestamp.ToString();
        }

        public bool PasswordMatches(string newPassword) {
            string newHash = HashSha256(newPassword);
            if (this.LastPasswordHash == null) {
                this.LastPasswordHash = newHash;
                Save();
            }
            return this.LastPasswordHash == newHash;
        }


        internal static string HashSha256(string plaintext) {
            byte[] data = Encoding.UTF8.GetBytes(plaintext);
            byte[] hashBytes = SHA256.Create().ComputeHash(data);
            return Convert.ToBase64String(hashBytes);
        }
        internal static void Save() {
            string obj = JsonConvert.SerializeObject(instance, Formatting.Indented);
            File.WriteAllText(SaveFile, obj);
        }

    }

}

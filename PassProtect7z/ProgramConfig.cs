using Newtonsoft.Json;

namespace PassProtect7z {
    class ProgramConfig {
        private static ProgramConfig instance;

        public bool? ShouldEncryptStandaloneFiles = null;
        public bool SkipIntegrityCheck = false;
        public DateTime SkipEncryptionTimestamp = DateTime.Now;
        public bool IsThisRootFolder = false;
        public static void Load(string file) {
            FileInfo config = new(file);
            if (!config.Exists) {
                instance = new ProgramConfig();
            } else {
                instance = JsonConvert.DeserializeObject<ProgramConfig>(File.ReadAllText(file)) ?? throw new Exception($"Error Parsing {FileUtils.CONFIG}");
            }
            string obj = JsonConvert.SerializeObject(instance, Formatting.Indented);
            File.WriteAllText(file, obj);
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
    }

}

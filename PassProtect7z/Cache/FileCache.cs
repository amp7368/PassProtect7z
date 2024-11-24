using Force.Crc32;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace PassProtect7z.Cache {
    public class FileCache {
        private static SortedDictionary<string, FileCache> AllFiles = new();

        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public string? HashSha256 { get; set; }
        public string HashCrc32 { get; set; }


        [JsonConstructor]
        public FileCache() {
            this.Name = "";
            this.HashCrc32 = "";
        }

        private void Load() {
        }

        public static void LoadAll() {
            FileInfo file = new(FileUtils.CACHE);
            if (file.Exists) {
                AllFiles = JsonConvert.DeserializeObject<SortedDictionary<string, FileCache>>(File.ReadAllText(FileUtils.CACHE)) ??
                        throw new JsonException($"Error Parsing {FileUtils.CACHE}");
            }
            foreach (FileCache cache in AllFiles.Values) {
                cache.Load();
            }
            Save();
        }
        public static FileCache? GetFileCache(string relativePath) {
            if (AllFiles.ContainsKey(relativePath))
                return AllFiles[relativePath];
            return null;
        }
        public static bool IsCached(string absolutePath, string relativePath) {
            FileCache? cache = GetFileCache(relativePath);
            if (cache == null) return false;
            FileInfo file = new(absolutePath);
            return cache.Matches(file);
        }

        private bool Matches(FileInfo file) {
            if (file.Length != this.Length) return false;
            if (file.CreationTimeUtc != this.CreationTimeUtc) return false;

            if (this.HashCrc32 == "" && this.HashSha256 == CreateSha256Hash(file)) {
                this.HashSha256 = null;
                this.HashCrc32 = CreateCRCHash(file);
                Save();
                return true;
            }
            if (this.HashCrc32 != CreateCRCHash(file)) return false;
            return true;
        }

        internal static string CreateSha256Hash(FileInfo file) {
            byte[] hashBytes = SHA256.Create().ComputeHash(file.OpenRead());
            return Convert.ToBase64String(hashBytes);
        }

        internal static string CreateCRCHash(FileInfo file) {
            using FileStream fileStream = file.OpenRead();

            byte[] buffer = new byte[4096];
            int bytesRead;
            uint hash = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                hash = Crc32Algorithm.Append(hash, buffer, 0, bytesRead);

            byte[] hashBytes = BitConverter.GetBytes(hash);
            return Convert.ToHexString(hashBytes);
        }

        internal static void Save(string absolutePath, string relativePath) {
            FileInfo file = new(absolutePath);

            AllFiles[relativePath] = new() {
                Name = relativePath,
                Length = file.Length,
                CreationTimeUtc = file.CreationTimeUtc,
                HashCrc32 = CreateCRCHash(file)
            };

            Save();
        }

        internal static void Save() {
            JsonSerializerSettings settings = new() {
                NullValueHandling = NullValueHandling.Ignore,
            };
            string obj = JsonConvert.SerializeObject(AllFiles, Formatting.Indented, settings);

            File.WriteAllText(FileUtils.CACHE, obj);
        }

    }

}

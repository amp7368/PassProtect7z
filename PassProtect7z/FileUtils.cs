namespace PassProtect7z {
    internal class FileUtils {
        private static readonly string ROOT = Program.ROOT_DIR;
        public static readonly string CONFIG = Path.Combine(ROOT, Program.CONFIG_FILE_NAME);
        public static readonly string ORIGINAL_DIR = Path.Combine(ROOT, "Original");
        public static readonly string ENCRYPTED_DIR = Path.Combine(ROOT, "Encrypted");
        public static readonly string DO_ENCRYPTION_DIR = Path.Combine(ROOT, "DoEncryption");
        public static readonly string SKIPPED_DIR = Path.Combine(ROOT, "Skipped");
        public static readonly string WORK_DIR = Path.Combine(ROOT, "Work");
        public static readonly string WORK_EXTRACT_DIR = Path.Combine(WORK_DIR, "Extract");
        public static readonly string WORK_EXTRACT_DIR2 = WORK_EXTRACT_DIR + "-2";
        static FileUtils() {
            try {
                if (Directory.Exists(WORK_DIR)) {
                    Console.WriteLine($"Deleting work folder: {WORK_DIR}");
                    CleanupFolder(WORK_DIR);
                }
                if (Directory.Exists(DO_ENCRYPTION_DIR)) {
                    Console.WriteLine($"Deleting DoEncryption folder: {DO_ENCRYPTION_DIR}");
                    CleanupFolder(DO_ENCRYPTION_DIR);
                }
            } catch (UnauthorizedAccessException e) {
                throw new Exception($"Verify that {WORK_DIR} can be deleted, and retry", e);
            }
            Directory.CreateDirectory(ORIGINAL_DIR);
            Directory.CreateDirectory(ENCRYPTED_DIR);
            Directory.CreateDirectory(DO_ENCRYPTION_DIR);
            Directory.CreateDirectory(SKIPPED_DIR);
            Directory.CreateDirectory(WORK_DIR);
            Directory.CreateDirectory(WORK_EXTRACT_DIR);
        }
        public static void ParentMkDirs(string file) {
            string parent = Path.GetFullPath("..", file);
            Directory.CreateDirectory(parent);
        }

        public static string RootPath(string Dir, string ArchivePath) {
            return Path.GetFullPath(Path.Combine(Dir, ArchivePath));
        }
        public static string OriginalArchivePath(string ArchivePath) {
            return RootPath(FileUtils.ORIGINAL_DIR, ArchivePath);
        }
        public static string EncryptedArchivePath(string ArchivePath) {
            return RootPath(FileUtils.ENCRYPTED_DIR, ArchivePath);
        }
        public static string DoEncryptionArchivePath(string ArchivePath) {
            return RootPath(FileUtils.DO_ENCRYPTION_DIR, ArchivePath);
        }
        public static string WorkArchivePath(string ArchivePath) {
            return RootPath(FileUtils.WORK_DIR, ArchivePath);
        }
        public static string SkippedArchivePath(string ArchivePath) {
            return RootPath(FileUtils.SKIPPED_DIR, ArchivePath);
        }
        public static bool CheckFilesEqual(string path1, string path2) {
            FileInfo file1 = new(path1);
            FileInfo file2 = new(path2);
            if (!file1.Exists || !file2.Exists) { return false; }
            if (file1.Length != file2.Length) { return false; }
            FileStream read1 = file1.OpenRead();
            FileStream read2 = file2.OpenRead();
            int length = 1024;
            byte[] buffer1 = new byte[length];
            byte[] buffer2 = new byte[length];
            while (true) {
                int bytesRead1 = read1.Read(buffer1, 0, length);
                int bytesRead2 = read2.Read(buffer2, 0, length);
                if (bytesRead1 != bytesRead2) return false;
                if (bytesRead1 <= 0) return true;

                for (int i = 0; i < bytesRead1; i++) {
                    if (buffer1[i] != buffer2[i]) return false;
                }
            }
        }
        public static void CleanupFolder(string path) {
            DirectoryInfo folder = new(path) {
                Attributes = FileAttributes.Normal
            };

            FileSystemInfo[] files = folder.GetFileSystemInfos("", SearchOption.AllDirectories);
            foreach (FileSystemInfo file in files) {
                file.Attributes = FileAttributes.Normal;
            }

            folder.Delete(true);
        }
        public static void CopyAlreadyEncrypted(string original, string outFile) {
            // copy to EncryptedArchivePath
            bool alreadyCopied = FileUtils.CheckFilesEqual(original, outFile);
            if (alreadyCopied) {
                Console.WriteLine($"{outFile} already exists. Skipping.");
                return;
            }
            FileUtils.ParentMkDirs(outFile);
            if (File.Exists(outFile)) {
                File.SetAttributes(outFile, FileAttributes.Normal);
                Console.WriteLine($"{outFile} exists, but is not the same. Renaming old.");
                File.Move(outFile, outFile + ".old");
            }
            File.SetAttributes(original, FileAttributes.Normal);
            Console.WriteLine($"Copying {original} to {outFile}.");
            File.Copy(original, outFile, true);
            return;
        }

    }
}


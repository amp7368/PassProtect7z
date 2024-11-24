namespace PassProtect7z {
    class ReEncryptArchive {
        private readonly string archivePath;
        private readonly string rootPath;
        public ReEncryptArchive(string RootPath, string ArchivePath) {
            rootPath = RootPath;
            archivePath = ArchivePath;
        }


        public void Run() {
            Encrypt encrypt = new(FileUtils.WORK_EXTRACT_DIR, archivePath, false);
            if (encrypt.ShouldSkipEncryption(rootPath)) return;
            if (Commands.Decrypt(rootPath, archivePath, FileUtils.WORK_EXTRACT_DIR)) {
                encrypt.EncryptFolder();
                FileUtils.CleanupFolder(FileUtils.WORK_EXTRACT_DIR);
                return;
            }
            FileUtils.CleanupFolder(FileUtils.WORK_EXTRACT_DIR);

            Console.WriteLine($"Error decrypting {archivePath}");
            if (Commands.CheckArchiveIntegrity(Path.Combine(rootPath, archivePath))) {
                Console.WriteLine($"{archivePath} has an known password. Copying.");
                CopyAlreadyEncrypted(FileUtils.ENCRYPTED_DIR);
            } else {
                Console.WriteLine($"{archivePath} has an unknown password or corrupted. Copying.");
                CopyAlreadyEncrypted(FileUtils.SKIPPED_DIR);
            }
        }


        private void CopyAlreadyEncrypted(string destination) {
            string original = Path.Combine(rootPath, archivePath);
            string outFile = Path.Combine(destination, archivePath);
            FileUtils.CopyAlreadyEncrypted(original, outFile);
        }
    }
}

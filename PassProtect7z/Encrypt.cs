using System.Diagnostics;

namespace PassProtect7z {
    internal class Encrypt {
        private readonly string srcPath;
        private readonly bool isFile;
        private readonly string relativePath;
        private readonly string encryptedFolderPath;
        private readonly string doEncryptionPath;

        public Encrypt(string SrcPath, string RelativePath, bool IsFile) {
            srcPath = SrcPath;
            relativePath = RelativePath;
            isFile = IsFile;
            encryptedFolderPath = FileUtils.EncryptedArchivePath(relativePath);
            doEncryptionPath = FileUtils.DoEncryptionArchivePath(relativePath);
        }

        public void EncryptFolder() {
            ProcessStartInfo encryptingPInfo = new("7z", $"a \"{doEncryptionPath}\" * -p{Program.Password} -mhe");
            encryptingPInfo.RedirectStandardOutput = true;
            encryptingPInfo.WorkingDirectory = srcPath;

            Console.WriteLine($"Encrypting to {doEncryptionPath}");
            using Process encrypting = Process.Start(encryptingPInfo) ?? throw new Exception(encryptingPInfo.ToString()); ;
            encrypting.WaitForExit();

            Console.WriteLine($"Moving {doEncryptionPath} to {encryptedFolderPath}");
            FileUtils.ParentMkDirs(encryptedFolderPath);
            File.Move(doEncryptionPath, encryptedFolderPath, true);
        }

        public bool ShouldSkipEncryption(string? originalArchiveRoot) {
            if (ProgramConfig.get().AlwaysEncryptAgain) return false;

            string encryptedPath;

            string? originalPath = null;
            if (originalArchiveRoot != null) {
                originalPath = Path.GetFullPath(relativePath, originalArchiveRoot);
            }
            if (isFile) {
                string relativeOutPath = Path.ChangeExtension(relativePath, ".7z");
                encryptedPath = FileUtils.EncryptedArchivePath(relativeOutPath);

            } else {
                encryptedPath = encryptedFolderPath;
            }
            if (!File.Exists(encryptedPath)) return false;

            if (!Commands.CheckArchiveIntegrity(encryptedPath)) {
                if (ProgramConfig.get().IgnoreEncryptedPasswords.Contains(encryptedPath)) {
                    return true;
                }
                throw new Exception($"{encryptedPath} already exists with an unknown password or is corrupted");
            }

            bool shouldSkip = ProgramConfig.get().isFileCreationAfter(encryptedPath);

            if (originalPath != null && ProgramConfig.get().ShouldVerify7zContents) {
                shouldSkip = shouldSkip && Commands.CheckArchiveMatches(originalPath, encryptedPath);
            }

            if (shouldSkip) {
                Console.WriteLine($"{encryptedPath} already exists... Archive contents match.");
                return true;
            }
            return false;
        }

        public void EncryptFile() {
            string absoluteInPath = Path.GetFullPath(relativePath, srcPath);
            string absoluteInFolder = Path.GetFullPath("..", absoluteInPath);
            string singleFilePath = new FileInfo(absoluteInPath).Name;
            string relativeOutPath = Path.ChangeExtension(relativePath, ".7z");
            string doEncryptionPath = FileUtils.DoEncryptionArchivePath(relativeOutPath);
            string encryptedPath = FileUtils.EncryptedArchivePath(relativeOutPath);

            ProcessStartInfo encryptingPInfo = new("7z", $"a \"{doEncryptionPath}\" \"{singleFilePath}\" -p{Program.Password} -mhe");
            encryptingPInfo.RedirectStandardOutput = true;
            encryptingPInfo.WorkingDirectory = absoluteInFolder;

            Console.WriteLine($"Encrypting to {doEncryptionPath}");
            using Process encrypting = Process.Start(encryptingPInfo) ?? throw new Exception(encryptingPInfo.ToString()); ;
            encrypting.WaitForExit();

            Console.WriteLine($"Moving {doEncryptionPath} to {encryptedPath}");
            FileUtils.ParentMkDirs(encryptedPath);
            File.Move(doEncryptionPath, encryptedPath, true);
        }
    }
}

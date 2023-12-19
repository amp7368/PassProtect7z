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

        public bool ShouldSkipEncryption() {
            string encryptedPath;
            if (isFile) {
                string relativeOutPath = Path.ChangeExtension(relativePath, ".7z");
                encryptedPath = FileUtils.EncryptedArchivePath(relativeOutPath);
            } else {
                encryptedPath = encryptedFolderPath;
            }
            if (!File.Exists(encryptedPath)) return false;

            bool shouldSkip = ProgramConfig.get().isFileCreationAfter(encryptedPath);
            if (shouldSkip && !ProgramConfig.get().SkipIntegrityCheck) {
                shouldSkip = Commands.CheckArchiveIntegrity(encryptedPath);
            }

            if (shouldSkip) {
                Console.WriteLine($"{encryptedPath} already exists... Is after {ProgramConfig.get().SkipEncryptionTimestampFormat()} & has integrity.");
                return true;
            }
            Console.WriteLine($"{encryptedPath} already exists with an unknown password or is corrupted");
            File.Delete(encryptedPath);
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

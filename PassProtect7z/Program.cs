namespace PassProtect7z {
    class Program {
        public static string Password;
        public static string ROOT_DIR;
        public static string CONFIG_FILE_NAME = "config.json";
        static void Main(string[] args) {
            try {
                Run(args);
                Console.WriteLine("Done!!!");
                Console.ReadKey();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.Write("Press a key to continue.");
                Console.ReadKey();
                throw;
            }
        }

        static void PrepEnv() {
            Console.WriteLine("Enter Password: ");
            Console.Write(">>> ");
            bool testing = false;
            Password = testing ? "pass" : Console.ReadLine();

            ProgramConfig.Load(CONFIG_FILE_NAME);

            bool isRootFolder = ProgramConfig.get().IsThisRootFolder;
            if (isRootFolder) {
                ROOT_DIR = Path.GetFullPath(".");
            } else {
                Console.WriteLine("Enter Path (Must contain ./Original subdir) ): ");
                Console.Write(">>> ");
                ROOT_DIR = testing ? "B:/xyz" : Console.ReadLine();
            }

            try {
                if (!Directory.Exists(FileUtils.ORIGINAL_DIR)) {
                    throw new Exception($"{FileUtils.ORIGINAL_DIR} does not exist!");
                }
            } catch (Exception ex) { throw ex.GetBaseException(); }
            if (!isRootFolder)
                ProgramConfig.Load(FileUtils.CONFIG);
        }
        static void Cleanup() {
            Console.WriteLine("Cleaning up work folders");
            FileUtils.CleanupFolder(FileUtils.DO_ENCRYPTION_DIR);
            FileUtils.CleanupFolder(FileUtils.WORK_DIR);
            if (Directory.GetFiles(FileUtils.SKIPPED_DIR).Length == 0) {
                FileUtils.CleanupFolder(FileUtils.SKIPPED_DIR);
            }
        }
        static void Run(string[] args) {
            PrepEnv();
            string[] originalFiles = Directory.GetFiles(FileUtils.ORIGINAL_DIR, "", SearchOption.AllDirectories);
            foreach (string originalFile in originalFiles) {
                try {
                    FileStream read = File.OpenRead(originalFile);
                    read.Close();
                } catch (Exception _) {
                    Console.WriteLine($"Cannot read {originalFile}. Skipping.");
                }
                string relativePath = Path.GetRelativePath(FileUtils.ORIGINAL_DIR, originalFile);
                if (originalFile.EndsWith(".7z")) {
                    new ReEncryptArchive(FileUtils.ORIGINAL_DIR, relativePath).Run();
                } else {
                    bool? shouldEncryptPre = ProgramConfig.get().ShouldEncryptStandaloneFiles;
                    bool shouldEncrypt = shouldEncryptPre ?? true;
                    if (!shouldEncryptPre.HasValue) {
                        Console.WriteLine($"Encrypt {originalFile}? (y/n)");
                        Console.Write(">>> ");
                        shouldEncrypt = Console.ReadLine().Equals("y");
                    }
                    if (shouldEncrypt) {
                        Encrypt encrypt = new(FileUtils.ORIGINAL_DIR, relativePath, true);
                        if (!encrypt.ShouldSkipEncryption())
                            encrypt.EncryptFile();
                    } else {
                        string skippedFile = FileUtils.SkippedArchivePath(relativePath);
                        Console.WriteLine($"Copying {originalFile} to {skippedFile}");
                        FileUtils.ParentMkDirs(skippedFile);
                        File.Copy(originalFile, skippedFile, true);
                    }
                }
            }
            Cleanup();
        }


    }
}
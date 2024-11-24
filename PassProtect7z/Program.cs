using PassProtect7z.Cache;
using System.Text;

namespace PassProtect7z {
    class Program {
        public static string Password = "";
        public static string ROOT_DIR;
        public static string CONFIG_FILE_NAME = "config.json";
        private static List<string> errors = new();

        static void Main(string[] args) {
            try {
                Run(args);
                Console.WriteLine("Done!!!");
            } catch (Exception ex) {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            } finally {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                for (int i = 0; i < errors.Count; i++) {
                    Console.WriteLine($"{i + 1}. {errors[i]}");
                }
            }
            Console.WriteLine();
            Console.WriteLine("Press a enter to continue.");
            Console.ReadLine();
        }
        static void VerifyPassword() {
            int remainingTries = 5;
            while (remainingTries != 0) {
                Password = ReadPassword();
                if (Password.Length == 0) {
                    Console.WriteLine("Password is empty. Try again...");
                    continue;
                }

                if (ProgramConfig.get().PasswordMatches(Password)) {
                    Console.WriteLine("Passwords match! Starting now.");
                    return;
                }
                remainingTries--;

                Console.Error.WriteLine("Passwords do not match!\n" +
                        "If this was an intentional password change, remove $LastPasswordHash in config.json");
                Console.WriteLine("Remaining tries: " + remainingTries);
            }
            throw new Exception("Too many tries");

        }
        static string ReadPassword() {
            Console.WriteLine("Enter Password: ");

            StringBuilder password = new();
            ConsoleKeyInfo read = Console.ReadKey(true);
            while (read.Key != ConsoleKey.Enter) {
                password.Append(read.KeyChar);
                read = Console.ReadKey(true);
            }

            return password.ToString();
        }
        static void PrepEnv() {
            ProgramConfig.Load(CONFIG_FILE_NAME);

            bool isRootFolder = ProgramConfig.get().IsThisRootFolder;
            if (isRootFolder) {
                ROOT_DIR = Path.GetFullPath(".");
            } else {
                Console.WriteLine("Enter Path (Must contain ./Original subdir) ): ");
                Console.Write(">>> ");
                ROOT_DIR = Console.ReadLine();
            }

            try {
                if (!Directory.Exists(FileUtils.ORIGINAL_DIR)) {
                    throw new Exception($"{FileUtils.ORIGINAL_DIR} does not exist!");
                }
            } catch (Exception ex) { throw ex.GetBaseException(); }
            if (!isRootFolder)
                ProgramConfig.Load(FileUtils.CONFIG);

            VerifyPassword();

            FileCache.LoadAll();
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
            if (ProgramConfig.get().RandomizeFileOrder) {
                new Random().Shuffle(originalFiles);
            }

            foreach (string originalFile in originalFiles) {
                try {
                    TryFile(originalFile);
                } catch (Exception e) {
                    string error = $"Error with {originalFile}: {e.Message}";
                    Console.Error.WriteLine(error);
                    errors.Add(error);
                }
            }

            Cleanup();
        }
        static void TryFile(string originalFile) {
            try {
                FileStream read = File.OpenRead(originalFile);
                read.Close();
            } catch {
                throw new IOException($"Cannot read {originalFile}. Skipping.");
            }
            string relativePath = Path.GetRelativePath(FileUtils.ORIGINAL_DIR, originalFile);


            Console.Write($"Starting {originalFile} ... ");
            Console.Out.FlushAsync();

            bool cached = FileCache.IsCached(originalFile, relativePath);
            if (cached) {
                Console.WriteLine($"Hit cache.");
                return;
            }



            if (originalFile.EndsWith(".7z", StringComparison.InvariantCulture)) {
                try {
                    new ReEncryptArchive(FileUtils.ORIGINAL_DIR, relativePath).Run();
                } catch (Exception e) {
                    throw new IOException($"Cannot reencrypt - {e.Message}.");
                }
                FileCache.Save(originalFile, relativePath);
                return;
            }

            bool? shouldEncryptPre = ProgramConfig.get().ShouldEncryptStandaloneFiles;
            bool shouldEncrypt = shouldEncryptPre ?? true;

            if (!shouldEncryptPre.HasValue) {
                Console.WriteLine($"Encrypt {originalFile}? (y/n)");
                Console.Write(">>> ");
                shouldEncrypt = Console.ReadLine().Equals("y");
            }
            if (shouldEncrypt) {
                Encrypt encrypt = new(FileUtils.ORIGINAL_DIR, relativePath, true);
                if (encrypt.ShouldSkipEncryption(null))
                    return;

                try {
                    encrypt.EncryptFile();
                } catch (Exception e) {
                    throw new IOException($"Cannot encrypt - {e.Message}.");
                }
                FileCache.Save(originalFile, relativePath);
            } else {
                string skippedFile = FileUtils.SkippedArchivePath(relativePath);
                Console.WriteLine($"Copying {originalFile} to {skippedFile}");
                FileUtils.ParentMkDirs(skippedFile);
                File.Copy(originalFile, skippedFile, true);
            }
        }


    }
}
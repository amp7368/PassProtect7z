using System.Diagnostics;

namespace PassProtect7z {
    internal class Commands {

        public static bool CheckArchiveIntegrity(string fullPath) {
            Console.WriteLine($"Checking {fullPath} for present password. And verifing integrity");

            ProcessStartInfo checkPasswordPInfo = new($"7z", $"t \"{fullPath}\" -p{Program.Password}") {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetFullPath("..", fullPath)
            };

            using Process checkPassword = Process.Start(checkPasswordPInfo) ?? throw new Exception(checkPasswordPInfo.ToString());
            checkPassword.StandardInput.WriteLine(); // in case there's a password

            checkPassword.WaitForExit();
            return checkPassword.ExitCode == 0;
        }
        public static bool Decrypt(string rootPath, string archivePath, string outputDir) {
            ProcessStartInfo decryptingPInfo = new("7z", $"x \"{archivePath}\" -o{outputDir} -y") {
                WorkingDirectory = rootPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Console.WriteLine($"Decrypting {archivePath} to {outputDir}");
            using Process decrypting = Process.Start(decryptingPInfo) ?? throw new Exception(decryptingPInfo.ToString()); ;
            decrypting.WaitForExit(1000);
            decrypting.StandardInput.WriteLine(); // in case there's a password

            decrypting.WaitForExit();
            return decrypting.ExitCode == 0;
        }
    }
}

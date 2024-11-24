using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace PassProtect7z {
    internal class Commands {

        private static byte[] PipeToFile(string file) {
            ProcessStartInfo checkMatchesPInfo = new($"7z", $"l -slt {file} -p{Program.Password}") {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using Process process = Process.Start(checkMatchesPInfo) ?? throw new Exception(checkMatchesPInfo.ToString());

            string crcString = "CRC = ";
            List<uint> lines = new();
            while (!process.StandardOutput.EndOfStream) {
                string? line = process.StandardOutput.ReadLine();
                if (line == null)
                    continue;
                if (!line.StartsWith(crcString, StringComparison.Ordinal))
                    continue;

                string rest = line[crcString.Length..];
                if (rest.Length == 0) continue;

                uint lineCRC = uint.Parse(rest, NumberStyles.HexNumber);
                lines.Add(lineCRC);
            }
            process.WaitForExit();

            lines.Sort();

            byte[] buffer = new byte[lines.Count * 4];
            for (int i = 0; i < lines.Count; i++) {
                uint line = lines[i];
                BitConverter.GetBytes(line).CopyTo(buffer, i * 4);
            }
            SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(buffer.ToArray());
        }
        public static bool CheckArchiveMatches(string archive1, string archive2) {
            Task<byte[]> hash1 = Task.Run(() => PipeToFile(archive1));
            Task<byte[]> hash2 = Task.Run(() => PipeToFile(archive2));

            Task.WaitAll(new[] { hash1, hash2 });

            return hash1.Result.SequenceEqual(hash2.Result);
        }
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

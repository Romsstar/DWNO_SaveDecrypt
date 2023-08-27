using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DWNO_SaveDecrypt
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string filePath;
            var key = Encoding.UTF8.GetBytes("923ld8bofl[a^z-0gi4kyng0bkela3jT");
            var iv = Encoding.UTF8.GetBytes("keiv92lgpz0glske");
            byte[] input;
            byte[] output;
            string steamID = null;
            var checksumPos = 0x1A;
            var SteamIdPos = 0x9;

            if (args.Length == 0 || args[0] == "-help")
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("DWNOSave.exe -input <input file path> (-encrypt | -decrypt) -output <output file path>");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  -input     The path to the input file to encrypt or decrypt.");
                Console.WriteLine("  -encrypt   Encrypt the input file.");
                Console.WriteLine("  -decrypt   Decrypt the input file.");
                Console.WriteLine("  -help      Display this usage information.");
                Console.WriteLine("  -output    The path to the output file to create.");
                Console.WriteLine();
                Console.Read();
                return;
            }

            if (args.Length != 0)
            {
                // Input file path is provided as command-line argument
                input = File.ReadAllBytes(args[0]);
                filePath = Path.GetFileName(args[0]);

                var canDecrypt = CanDecrypt(input, key, iv);
                if (canDecrypt)
                {
                    // Decrypt the input file
                    output = Decrypt(input, key, iv);
                    File.WriteAllBytes(filePath, output);
                    Console.WriteLine("Savefile was decrypted!");
                    Console.Read();
                }
                else
                {
                    Console.Write("Resign the Savegame to another Steam ID? (y/n): ");
                    var resignChoice = Console.ReadLine();

                    if (resignChoice.Trim().ToLower() == "y")
                    {
                        Console.WriteLine("Folder with Steam ID " + GetSteamIDFolder() +
                                          " was found. Use it as the new Steam ID?  (y/n):");
                        var userIDChoice = Console.ReadLine();
                        if (userIDChoice.Trim().ToLower() == "y")
                        {
                            ResignSavegameWithNewSteamID(input, GetSteamIDFolder(), SteamIdPos);
                        }
                        else
                        {
                            ResignSavegameWithNewSteamID(input, steamID, SteamIdPos);
                        }
                    }

                    // Encrypt the input file
                    ClearChecksum(input, checksumPos);
                    GetChecksum(input);
                    WriteChecksum(input, checksumPos);
                    output = Encrypt(input, key, iv);
                    File.WriteAllBytes(filePath, output);
                    Console.WriteLine("Savefile was encrypted!");
                    Console.Read();
                }
            }
            else if (args[0] == "-input" && (args[2] == "-encrypt" || args[2] == "-decrypt") && args[3] == "-output")
            {
                var inputPath = args[1];
                input = File.ReadAllBytes(inputPath);
                var canDecrypt = CanDecrypt(input, key, iv);
                var outputPath = args[4];
                if (args[2] == "-encrypt" || args[2] == "-e")
                {
                    // Encrypt the input file
                    output = Encrypt(input, key, iv);
                    File.WriteAllBytes(outputPath, output);
                }

                if (args[2] == "-decrypt" || args[2] == "-d")
                {
                    // Decrypt the input file
                    output = Decrypt(input, key, iv);
                    File.WriteAllBytes(outputPath, output);
                }
            }
        }

        public static bool CanDecrypt(byte[] data, byte[] key, byte[] iv)
        {
            try
            {
                var result = Decrypt(data, key, iv);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aesManaged = new AesManaged())
            {
                aesManaged.Key = key;
                aesManaged.IV = iv;

                var encryptor = aesManaged.CreateEncryptor();
                var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                return encryptedData;
            }
        }

        public static string GetSteamIDFolder()
        {
            var savegamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "Bandai Namco Entertainment", "Digimon World Next Order");
            if (Directory.Exists(savegamePath))
            {
                var subdirectories = Directory.GetDirectories(savegamePath);

                if (subdirectories.Length > 0)
                {
                    var steamIDFolder = subdirectories[0];
                    return Path.GetFileName(steamIDFolder);
                }
            }

            return string.Empty;
        }

        public static void ResignSavegameWithNewSteamID(byte[] data, string newSteamID, int steamIDPos)
        {
            do
            {
                Console.Write("Enter the new Steam ID: ");
                newSteamID = Console.ReadLine();

                var newSteamIDBytes = Encoding.UTF8.GetBytes(newSteamID);

                if (newSteamIDBytes.Length == 0x11) // Check if SteamID length is correct
                {
                    using (var output = new MemoryStream(data))
                    {
                        using (var binaryWriter = new BinaryWriter(output))
                        {
                            binaryWriter.Seek(steamIDPos, SeekOrigin.Begin);
                            binaryWriter.Write(newSteamIDBytes);
                        }
                    }

                    break; // Exit the loop if the SteamID is of correct length
                }
                {
                    Console.WriteLine("Please enter a valid SteamID.");
                }
            } while (true);
        }

        public static void ClearChecksum(byte[] data, int checksumPos)
        {
            if (checksumPos < data.Length && data[checksumPos] != 0)
                using (var output = new MemoryStream(data))
                {
                    using (var binaryWriter = new BinaryWriter(output))
                    {
                        binaryWriter.Seek(checksumPos, SeekOrigin.Begin);
                        binaryWriter.Write(0);
                    }
                }
        }

        public static uint WriteChecksum(byte[] data, int checksumPos)
        {
            var checksum = GetChecksum(data);
            using (var output = new MemoryStream(data))
            {
                using (var binaryWriter = new BinaryWriter(output))
                {
                    binaryWriter.Seek(checksumPos, SeekOrigin.Begin);
                    binaryWriter.Write(checksum);
                    return (uint)(checksumPos - binaryWriter.BaseStream.Position);
                }
            }
        }

        public static int GetChecksum(byte[] _data)
        {
            var num = 0;
            for (var i = 0; i < _data.Length; i++) num += _data[i];
            return num;
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            byte[] result;
            var aesManaged = new AesManaged();
            aesManaged.Key = key;
            aesManaged.IV = iv;

            var decryptor = aesManaged.CreateDecryptor();
            result = decryptor.TransformFinalBlock(data, 0, data.Length);
            return result;
        }
    }
}
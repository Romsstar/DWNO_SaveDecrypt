using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DWNO_SaveDecrypt
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath;
            byte[] key = Encoding.UTF8.GetBytes("923ld8bofl[a^z-0gi4kyng0bkela3jT");
            byte[] iv = Encoding.UTF8.GetBytes("keiv92lgpz0glske");
            byte[] input;
            byte[] output;
            int checksumPos=0x1A;

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


                bool canDecrypt = CanDecrypt(input, key, iv);
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
                string inputPath = args[1];
                input = File.ReadAllBytes(inputPath);
                bool canDecrypt = CanDecrypt(input, key, iv);
                string outputPath = args[4];
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
                byte[] result = Decrypt(data, key, iv);
                return true;
            }
            catch (CryptographicException)
            {
                // If an exception is thrown during the decryption process, the data cannot be decrypted
                return false;
            }
        }
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (AesManaged aesManaged = new AesManaged())
            {
                aesManaged.Key = key;
                aesManaged.IV = iv;

                ICryptoTransform encryptor = aesManaged.CreateEncryptor();
                byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

                return encryptedData;
            }
        }
        public static void ClearChecksum(byte[] data, int checksumPos)
        {
            if (checksumPos < data.Length && data[checksumPos] != 0)
            {
                using (MemoryStream output = new MemoryStream(data))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(output))
                    {
                        binaryWriter.Seek(checksumPos, SeekOrigin.Begin);
                        binaryWriter.Write(0);
                    }
                }
            }
        }

        public static uint WriteChecksum(byte[] data, int checksumPos)
        {
            int checksum = GetChecksum(data);
            using (MemoryStream output = new MemoryStream(data))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(output))
                {
                    binaryWriter.Seek(checksumPos, SeekOrigin.Begin);
                    binaryWriter.Write(checksum);
                    return (uint)(checksumPos - binaryWriter.BaseStream.Position);
                }
            }
        }


        public static int GetChecksum(byte[] _data)
        {
            int num = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                num += _data[i];
            }
            return num;
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            byte[] result;
            AesManaged aesManaged = new AesManaged();
            aesManaged.Key = key;
            aesManaged.IV = iv;

            ICryptoTransform decryptor = aesManaged.CreateDecryptor();
            result = decryptor.TransformFinalBlock(data, 0, data.Length);
            return result;
        }
    }
}








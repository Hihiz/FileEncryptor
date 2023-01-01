using FileEncryptor.WPF.Services.Interfaces;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace FileEncryptor.WPF.Services
{
    internal class Rfc2898Encryptor : IEncryptor
    {
        private static readonly byte[] __Salt =
        {
            0x26, 0xdc, 0xff, 0x00,
            0xad, 0xed, 0x7a, 0xee,
            0xc5, 0xfe, 0x07, 0xaf,
            0x4d, 0x08, 0x22, 0x3c
        };

        private static ICryptoTransform GetEncryptor(string password, byte[] Slat = null)
        {
            var pdb = new Rfc2898DeriveBytes(password, Slat ?? __Salt);
            var algorithm = Rijndael.Create();
            algorithm.Key = pdb.GetBytes(32);
            algorithm.IV = pdb.GetBytes(16);
            return algorithm.CreateEncryptor();
        }

        private static ICryptoTransform GetDecryptor(string password, byte[] Slat = null)
        {
            var pdb = new Rfc2898DeriveBytes(password, Slat ?? __Salt);
            var algorithm = Rijndael.Create();
            algorithm.Key = pdb.GetBytes(32);
            algorithm.IV = pdb.GetBytes(16);
            return algorithm.CreateDecryptor();
        }

        public void Encrypt(string SourcePath, string DestinationPath, string Password, int BufferLength = 104200)
        {
            var encryptor = GetEncryptor(Password);

            using var destinationEncrypted = File.Create(DestinationPath, BufferLength);
            using var destination = new CryptoStream(destinationEncrypted, encryptor, CryptoStreamMode.Write);
            using var source = File.OpenRead(SourcePath);
            int reader;

            byte[] buffer = new byte[BufferLength];

            do
            {
                reader = source.Read(buffer, 0, BufferLength);
                destination.Write(buffer, 0, reader);

            } while (reader > 0);

            destination.FlushFinalBlock();

        }

        public bool Decrypt(string SourcePath, string DestinationPath, string Password, int BufferLength = 104200)
        {
            var decryptor = GetDecryptor(Password);

            using var destinationDecrypted = File.Create(DestinationPath, BufferLength);
            using var destination = new CryptoStream(destinationDecrypted, decryptor, CryptoStreamMode.Write);
            using var encryptedSource = File.OpenRead(SourcePath);

            byte[] buffer = new byte[BufferLength];
            int reader;

            do
            {
                Thread.Sleep(1);
                reader = encryptedSource.Read(buffer, 0, BufferLength);
                destination.Write(buffer, 0, reader);

            } while (reader > 0);

            try
            {
                destination.FlushFinalBlock();
            }
            catch (CryptographicException)
            {
                return false;
            }

            return true;
        }
    }
}

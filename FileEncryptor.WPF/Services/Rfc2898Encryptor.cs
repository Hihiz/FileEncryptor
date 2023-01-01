using FileEncryptor.WPF.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

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
                Thread.Sleep(1);
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

        public async Task EncryptAsync(string SourcePath, string DestinationPath, string Password, int BufferLength = 104200, IProgress<double> Progress = null, CancellationToken Cancel = default)
        {
            if (!File.Exists(SourcePath)) throw new FileNotFoundException("Файл-источник для процесса шифрования не найден", SourcePath);
            if (BufferLength <= 0) throw new ArgumentOutOfRangeException(nameof(BufferLength), BufferLength, "Размер буфера чтения должен быть больше 0");

            Cancel.ThrowIfCancellationRequested();

            var encryptor = GetEncryptor(Password/*, Encoding.UTF8.GetBytes(SourcePath)*/);

            try
            {
                await using var destinationEncrypted = File.Create(DestinationPath, BufferLength);
                await using var destination = new CryptoStream(destinationEncrypted, encryptor, CryptoStreamMode.Write);
                await using var source = File.OpenRead(SourcePath);

                var fileLength = source.Length;

                int reader;
                byte[] buffer = new byte[BufferLength];
                var lastPercent = 0.0;

                do
                {
                    reader = await source.ReadAsync(buffer, 0, BufferLength, Cancel).ConfigureAwait(false);
                    // дополнительные действия по завершению асинхронной операции
                    await destination.WriteAsync(buffer, 0, reader, Cancel).ConfigureAwait(false);

                    var position = source.Position;
                    var percent = (double)position / fileLength;
                    if (percent - lastPercent >= 0.001)
                    {
                        Progress?.Report(percent);
                        lastPercent = percent;
                    }

                    Thread.Sleep(1);

                    if (Cancel.IsCancellationRequested)
                    {
                        // очистка состояния операции
                        Cancel.ThrowIfCancellationRequested();
                    }

                } while (reader > 0);

                destination.FlushFinalBlock();

                Progress?.Report(1);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == Cancel)
            {
                //File.Delete(DestinationPath);
                Progress?.Report(0);
            }
            catch (Exception error)
            {
                Debug.WriteLine("Error in EncryptAsync:\r\n{0}", error);
                throw;
            }
        }

        public async Task<bool> DecryptAsync(string SourcePath, string DestinationPath, string Password, int BufferLength = 104200, IProgress<double> Progress = null, CancellationToken Cancel = default)
        {
            if (!File.Exists(SourcePath)) throw new FileNotFoundException("Файл-источник для процесса дешифрования не найден", SourcePath);
            if (BufferLength <= 0) throw new ArgumentOutOfRangeException(nameof(BufferLength), BufferLength, "Размер буфера чтения должен быть больше 0");

            Cancel.ThrowIfCancellationRequested();

            var decryptor = GetDecryptor(Password);

            try
            {
                await using var destinationDecrypted = File.Create(DestinationPath, BufferLength);
                await using var destination = new CryptoStream(destinationDecrypted, decryptor, CryptoStreamMode.Write);
                await using var encryptedSource = File.OpenRead(SourcePath);

                var fileLength = encryptedSource.Length;

                var buffer = new byte[BufferLength];
                int readed;
                var lastPercent = 0.0;

                do
                {
                    readed = await encryptedSource.ReadAsync(buffer, 0, BufferLength, Cancel).ConfigureAwait(false);

                    await destination.WriteAsync(buffer, 0, readed, Cancel).ConfigureAwait(false);

                    var position = encryptedSource.Position;

                    var percent = (double)position / fileLength;
                    if (percent - lastPercent >= 0.001)
                    {
                        Progress?.Report(percent);
                        lastPercent = percent;
                    }

                    Cancel.ThrowIfCancellationRequested();
                }
                while (readed > 0);

                try
                {
                    destination.FlushFinalBlock();
                }
                catch (CryptographicException)
                {
                    //return Task.FromResult(false);
                    return false;
                }

                Progress?.Report(1);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == Cancel)
            {
                //File.Delete(DestinationPath);
                Progress?.Report(0);
                throw;
            }

            //return Task.FromResult(true);
            return true;
        }


    }
}

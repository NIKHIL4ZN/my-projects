using SmartX.Core.DTOs;
using SmartX.Core.Entities;
using SmartX.Core.Exceptions;
using SmartX.Core.Interfaces;
using SmartX.Infrastructure.Data;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace SmartX.Infrastructure.Services
{
    public sealed class EncryptedSensorAttachmentService
    : ISensorAttachmentService
    {
        private const int BufferSize = 81920;

        private static readonly HashSet<string>
            AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            ".jpg",
            ".jpeg",
            ".png",
            ".pdf",
            ".txt",
            ".log",
            ".json",
            ".csv",
            ".yaml",
            ".yml"
            };

        private static readonly HashSet<string>
            AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "image/jpeg",
            "image/png",
            "application/pdf",
            "application/json",
            "application/octet-stream",
            "text/plain",
            "text/csv",
            "application/x-yaml",
            "text/yaml"
            };

        private readonly SmartXDbContext _dbContext;
        private readonly string _storageRoot;
        private readonly long _maximumFileSizeBytes;
        private readonly byte[] _encryptionKey;

        public EncryptedSensorAttachmentService(
            SmartXDbContext dbContext,
            string storageRoot,
            long maximumFileSizeBytes,
            string base64EncryptionKey)
        {
            _dbContext = dbContext;

            _storageRoot =
                Path.GetFullPath(storageRoot);

            _maximumFileSizeBytes =
                maximumFileSizeBytes;

            try
            {
                _encryptionKey =
                    Convert.FromBase64String(
                        base64EncryptionKey);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "The attachment encryption key is not " +
                    "valid Base64.",
                    exception);
            }

            if (_encryptionKey.Length != 32)
            {
                throw new InvalidOperationException(
                    "The attachment encryption key must " +
                    "contain exactly 32 bytes.");
            }

            if (_maximumFileSizeBytes <= 0)
            {
                throw new InvalidOperationException(
                    "The maximum attachment size must be positive.");
            }

            Directory.CreateDirectory(_storageRoot);
        }

        public async Task<SensorAttachmentResponse> UploadAsync(
            Guid sensorId,
            Stream content,
            string originalFileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            var sensorExists =
                await _dbContext.Sensors.AnyAsync(
                    sensor => sensor.Id == sensorId,
                    cancellationToken);

            if (!sensorExists)
            {
                throw new SensorNotFoundException(sensorId);
            }

            var safeFileName =
                ValidateFile(
                    originalFileName,
                    contentType,
                    fileSizeBytes);

            var storedFileName =
                $"{Guid.NewGuid():N}.enc";

            var fullPath = GetSafeFullPath(
                storedFileName);

            long actualBytesWritten;

            try
            {
                actualBytesWritten =
                    await EncryptToFileAsync(
                        content,
                        fullPath,
                        cancellationToken);
            }
            catch
            {
                DeleteFileIfPresent(fullPath);
                throw;
            }

            var attachment = new SensorAttachment
            {
                SensorId = sensorId,
                OriginalFileName = safeFileName,
                StoredFileName = storedFileName,
                ContentType = contentType,
                FileSizeBytes = actualBytesWritten,
                RelativePath = storedFileName,
                IsEncrypted = true
            };

            _dbContext.SensorAttachments.Add(attachment);

            try
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                DeleteFileIfPresent(fullPath);
                throw;
            }

            return ToResponse(attachment);
        }

        public async Task<IReadOnlyList<
            SensorAttachmentResponse>> GetForSensorAsync(
            Guid sensorId,
            CancellationToken cancellationToken = default)
        {
            var sensorExists =
                await _dbContext.Sensors
                    .AsNoTracking()
                    .AnyAsync(
                        sensor => sensor.Id == sensorId,
                        cancellationToken);

            if (!sensorExists)
            {
                throw new SensorNotFoundException(sensorId);
            }

            return await _dbContext.SensorAttachments
                .AsNoTracking()
                .Where(item => item.SensorId == sensorId)
                .OrderByDescending(item => item.UploadedAtUtc)
                .Select(item => new SensorAttachmentResponse
                {
                    Id = item.Id,
                    SensorId = item.SensorId,
                    OriginalFileName =
                        item.OriginalFileName,
                    ContentType = item.ContentType,
                    FileSizeBytes =
                        item.FileSizeBytes,
                    UploadedAtUtc =
                        item.UploadedAtUtc,
                    IsEncrypted = item.IsEncrypted
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SensorAttachmentDownload?> DownloadAsync(
            Guid sensorId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
        {
            var attachment =
                await _dbContext.SensorAttachments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item =>
                            item.Id == attachmentId
                            && item.SensorId == sensorId,
                        cancellationToken);

            if (attachment is null)
            {
                return null;
            }

            var fullPath = GetSafeFullPath(
                attachment.RelativePath);

            if (!File.Exists(fullPath))
            {
                return null;
            }

            var encryptedStream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);

            try
            {
                var initialisationVector =
                    new byte[16];

                await encryptedStream.ReadExactlyAsync(
                    initialisationVector,
                    cancellationToken);

                using var aes = Aes.Create();

                aes.Key = _encryptionKey;
                aes.IV = initialisationVector;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var decryptor =
                    aes.CreateDecryptor();

                var decryptedStream =
                    new CryptoStream(
                        encryptedStream,
                        decryptor,
                        CryptoStreamMode.Read,
                        leaveOpen: false);

                return new SensorAttachmentDownload
                {
                    Content = decryptedStream,
                    FileName =
                        attachment.OriginalFileName,
                    ContentType =
                        attachment.ContentType
                };
            }
            catch
            {
                await encryptedStream.DisposeAsync();
                throw;
            }
        }

        private string ValidateFile(
            string originalFileName,
            string contentType,
            long fileSizeBytes)
        {
            if (fileSizeBytes <= 0)
            {
                throw new InvalidAttachmentException(
                    "The uploaded file is empty.");
            }

            if (fileSizeBytes > _maximumFileSizeBytes)
            {
                throw new InvalidAttachmentException(
                    $"The file exceeds the maximum size of " +
                    $"{_maximumFileSizeBytes} bytes.");
            }

            var safeFileName =
                Path.GetFileName(originalFileName);

            if (string.IsNullOrWhiteSpace(safeFileName))
            {
                throw new InvalidAttachmentException(
                    "The attachment requires a valid filename.");
            }

            var extension =
                Path.GetExtension(safeFileName);

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidAttachmentException(
                    $"Files with the '{extension}' extension " +
                    "are not permitted.");
            }

            if (string.IsNullOrWhiteSpace(contentType)
                || !AllowedContentTypes.Contains(contentType))
            {
                throw new InvalidAttachmentException(
                    $"Files with content type '{contentType}' " +
                    "are not permitted.");
            }

            return safeFileName;
        }

        private async Task<long> EncryptToFileAsync(
            Stream input,
            string fullPath,
            CancellationToken cancellationToken)
        {
            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan);

            using var aes = Aes.Create();

            aes.Key = _encryptionKey;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            await output.WriteAsync(
                aes.IV,
                cancellationToken);

            await using var encryptionStream =
                new CryptoStream(
                    output,
                    aes.CreateEncryptor(),
                    CryptoStreamMode.Write,
                    leaveOpen: true);

            var buffer =
                ArrayPool<byte>.Shared.Rent(BufferSize);

            long totalBytes = 0;

            try
            {
                int bytesRead;

                while ((bytesRead =
                    await input.ReadAsync(
                        buffer.AsMemory(0, BufferSize),
                        cancellationToken)) > 0)
                {
                    totalBytes += bytesRead;

                    if (totalBytes >
                        _maximumFileSizeBytes)
                    {
                        throw new InvalidAttachmentException(
                            "The uploaded stream exceeded the " +
                            "maximum permitted file size.");
                    }

                    await encryptionStream.WriteAsync(
                        buffer.AsMemory(0, bytesRead),
                        cancellationToken);
                }

                await encryptionStream.FlushFinalBlockAsync(
                    cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(
                    buffer,
                    clearArray: true);
            }

            return totalBytes;
        }

        private string GetSafeFullPath(
            string relativePath)
        {
            var fullPath = Path.GetFullPath(
                Path.Combine(
                    _storageRoot,
                    relativePath));

            var requiredPrefix =
                _storageRoot.EndsWith(
                    Path.DirectorySeparatorChar)
                    ? _storageRoot
                    : _storageRoot
                        + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidAttachmentException(
                    "The attachment path is invalid.");
            }

            return fullPath;
        }

        private static void DeleteFileIfPresent(
            string fullPath)
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private static SensorAttachmentResponse ToResponse(
            SensorAttachment attachment)
        {
            return new SensorAttachmentResponse
            {
                Id = attachment.Id,
                SensorId = attachment.SensorId,
                OriginalFileName =
                    attachment.OriginalFileName,
                ContentType = attachment.ContentType,
                FileSizeBytes =
                    attachment.FileSizeBytes,
                UploadedAtUtc =
                    attachment.UploadedAtUtc,
                IsEncrypted =
                    attachment.IsEncrypted
            };
        }
    }
}

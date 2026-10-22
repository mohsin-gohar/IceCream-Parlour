using System.Security.Cryptography;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Centralised, safe image upload handling: extension whitelist, content-type
    /// check, magic-byte validation, size limit and collision-free file names.
    /// </summary>
    public static class FileUploadHelper
    {
        public const long DefaultMaxBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/gif", "image/webp",
                // Some browsers report generic types for drag-and-drop uploads.
                "application/octet-stream"
            };

        public sealed class ValidationException : Exception
        {
            public ValidationException(string message) : base(message) { }
        }

        public static async Task<string> SaveImageAsync(
            IFormFile? file,
            IWebHostEnvironment environment,
            string subFolder,
            long maxBytes = DefaultMaxBytes,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ValidationException("Please choose an image file.");
            }

            if (file.Length > maxBytes)
            {
                throw new ValidationException($"Image must be smaller than {maxBytes / (1024 * 1024)} MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                throw new ValidationException("Only JPG, PNG, GIF and WEBP images are allowed.");
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                throw new ValidationException("The uploaded file is not a valid image.");
            }

            await using (var headerStream = file.OpenReadStream())
            {
                var header = new byte[12];
                var read = await headerStream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);
                if (!LooksLikeImage(header, read))
                {
                    throw new ValidationException("The uploaded file content is not a valid image.");
                }
            }

            var folder = subFolder.Trim('/');
            var folderPath = Path.Combine(environment.WebRootPath, folder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folderPath);

            var safeExtension = extension.ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{safeExtension}";
            var fullPath = Path.Combine(folderPath, fileName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew);
            await file.CopyToAsync(stream, cancellationToken);

            return $"/{folder}/{fileName}";
        }

        /// <summary>
        /// Deletes an uploaded file, but only when the resolved path stays inside wwwroot.
        /// </summary>
        public static void DeleteIfExists(IWebHostEnvironment environment, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var root = Path.GetFullPath(environment.WebRootPath);
            var candidate = Path.GetFullPath(Path.Combine(root, relativePath.TrimStart('/', '\\')));

            if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return; // path traversal attempt - ignore
            }

            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }

        private static bool LooksLikeImage(byte[] header, int length)
        {
            if (length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return true; // JPEG
            }

            if (length >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
                header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
            {
                return true; // PNG
            }

            if (length >= 6 &&
                header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
            {
                return true; // GIF
            }

            return length >= 12 &&
                   header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                   header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50; // WEBP
        }
    }
}

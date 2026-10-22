using System.Text;

namespace Ice_Cream_Parlour_Eproject.Helpers
{
    /// <summary>
    /// Centralised, validated image storage for uploaded files.
    /// All user supplied file names are ignored - a new GUID based name is
    /// generated so path traversal / double extension attacks are impossible.
    /// </summary>
    public static class FileStorage
    {
        public const long MaxImageSizeBytes = 2 * 1024 * 1024; // 2 MB

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/pjpeg", "image/png", "image/gif", "image/webp"
            };

        public static bool TryValidateImage(IFormFile? file, out string error)
        {
            error = string.Empty;

            if (file == null || file.Length == 0)
            {
                error = "Please select an image file.";
                return false;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                error = "Image size must be less than 2 MB.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            {
                error = "Only image files (jpg, jpeg, png, gif, webp) are allowed.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                error = "The selected file is not a valid image.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates and stores an image below wwwroot. Returns the public relative
        /// path ("/images/...") on success, or null with an error message.
        /// </summary>
        public static async Task<(string? Path, string? Error)> SaveImageAsync(
            IFormFile? file,
            IWebHostEnvironment environment,
            string folderName)
        {
            if (!TryValidateImage(file, out var error))
            {
                return (null, error);
            }

            var folder = "images/" + folderName.Trim('/', '\\') + "/";
            var folderPath = Path.Combine(environment.WebRootPath, folder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folderPath);

            var extension = Path.GetExtension(file!.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + extension;
            var fullPath = Path.Combine(folderPath, fileName);

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            return ("/" + folder + fileName, null);
        }

        /// <summary>
        /// Deletes a previously stored image. Only files below wwwroot/images are
        /// ever touched, so a tampered database value cannot delete arbitrary files.
        /// </summary>
        public static void DeleteImage(IWebHostEnvironment environment, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var relative = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relative));

            var imagesRoot = Path.GetFullPath(Path.Combine(environment.WebRootPath, "images"))
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(imagesRoot, StringComparison.OrdinalIgnoreCase))
            {
                return; // outside the managed images folder - never delete
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        /// <summary>Builds a safe, unique name for downloadable export files.</summary>
        public static string BuildExportFileName(string prefix, string extension)
        {
            var safePrefix = new string(prefix.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (string.IsNullOrEmpty(safePrefix)) safePrefix = "export";

            var builder = new StringBuilder(safePrefix);
            builder.Append('-').Append(DateTime.Now.ToString("yyyyMMdd-HHmmss")).Append(extension);
            return builder.ToString();
        }
    }
}

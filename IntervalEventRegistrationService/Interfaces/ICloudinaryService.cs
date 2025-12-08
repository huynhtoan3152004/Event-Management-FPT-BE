using Microsoft.AspNetCore.Http;

namespace IntervalEventRegistrationService.Interfaces;

public interface ICloudinaryService
{
    /// <summary>
    /// Upload file lên Cloudinary
    /// </summary>
    /// <param name="file">File cần upload</param>
    /// <param name="folder">Folder lưu trên Cloudinary (vd: "speakers", "events")</param>
    /// <param name="publicId">Public ID custom (không bắt buộc)</param>
    /// <returns>URL của file sau khi upload</returns>
    Task<string> UploadAsync(IFormFile file, string folder, string? publicId = null);

    /// <summary>
    /// Upload file từ URL lên Cloudinary
    /// </summary>
    /// <param name="fileUrl">URL của file</param>
    /// <param name="folder">Folder lưu trên Cloudinary</param>
    /// <returns>URL của file sau khi upload</returns>
    Task<string> UploadFromUrlAsync(string fileUrl, string folder);

    /// <summary>
    /// Xóa file từ Cloudinary
    /// </summary>
    /// <param name="publicId">Public ID của file cần xóa</param>
    /// <returns>True nếu xóa thành công</returns>
    Task<bool> DeleteAsync(string publicId);

    /// <summary>
    /// Tối ưu hóa URL ảnh (resize, quality, etc)
    /// </summary>
    /// <param name="publicId">Public ID của file</param>
    /// <param name="width">Chiều rộng (tùy chọn)</param>
    /// <param name="height">Chiều cao (tùy chọn)</param>
    /// <param name="quality">Chất lượng (default: auto)</param>
    /// <returns>URL tối ưu hóa</returns>
    string GetOptimizedUrl(string publicId, int? width = null, int? height = null, string quality = "auto");
}

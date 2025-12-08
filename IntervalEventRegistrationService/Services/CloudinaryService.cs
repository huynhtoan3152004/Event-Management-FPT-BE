using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using IntervalEventRegistrationService.Configuration;
using IntervalEventRegistrationService.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace IntervalEventRegistrationService.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;

    public CloudinaryService(IOptions<CloudinarySettings> options)
    {
        _settings = options.Value;
        
        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);
        
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadAsync(IFormFile file, string folder, string? publicId = null)
    {
        if (file.Length == 0)
            throw new ArgumentException("File không được rỗng");

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = $"interval-event-registration/{folder}",
                    PublicId = publicId ?? Guid.NewGuid().ToString(),
                    Overwrite = true
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                    throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

                return uploadResult.SecureUrl.ToString();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi upload file: {ex.Message}", ex);
        }
    }

    public async Task<string> UploadFromUrlAsync(string fileUrl, string folder)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("URL không được rỗng");

        try
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileUrl),
                Folder = $"interval-event-registration/{folder}",
                PublicId = Guid.NewGuid().ToString(),
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi upload file từ URL: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("Public ID không được rỗng");

        try
        {
            var deleteParams = new DeletionParams(publicId);
            var deleteResult = await _cloudinary.DestroyAsync(deleteParams);

            return deleteResult.Result == "ok";
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi xóa file: {ex.Message}", ex);
        }
    }

    public string GetOptimizedUrl(string publicId, int? width = null, int? height = null, string quality = "auto")
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException("Public ID không được rỗng");

        try
        {
            // Xây dựng URL tối ưu hóa từ Cloudinary
            var transformation = new Transformation()
                .Quality(quality)
                .FetchFormat("auto");

            if (width.HasValue)
                transformation = transformation.Width(width.Value);
            
            if (height.HasValue)
                transformation = transformation.Height(height.Value);

            var url = _cloudinary.Api.UrlImgUp
                .Transform(transformation)
                .BuildUrl(publicId);

            return url;
        }
        catch (Exception ex)
        {
            throw new Exception($"Lỗi khi tạo optimized URL: {ex.Message}", ex);
        }
    }
}

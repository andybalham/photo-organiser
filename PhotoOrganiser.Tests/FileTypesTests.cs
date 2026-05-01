using PhotoOrganiser.Helpers;

namespace PhotoOrganiser.Tests;

public class FileTypesTests
{
    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".bmp")]
    [InlineData(".tiff")]
    [InlineData(".tif")]
    [InlineData(".webp")]
    [InlineData(".heic")]
    [InlineData(".heif")]
    [InlineData(".raw")]
    [InlineData(".cr2")]
    [InlineData(".cr3")]
    [InlineData(".nef")]
    [InlineData(".arw")]
    [InlineData(".orf")]
    [InlineData(".rw2")]
    [InlineData(".dng")]
    [InlineData(".pef")]
    [InlineData(".srw")]
    [InlineData(".raf")]
    public void IsSupported_ImageExtensions_ReturnsTrue(string ext)
    {
        Assert.True(FileTypes.IsSupported(ext));
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".mov")]
    [InlineData(".avi")]
    [InlineData(".mkv")]
    [InlineData(".wmv")]
    [InlineData(".m4v")]
    [InlineData(".3gp")]
    [InlineData(".flv")]
    [InlineData(".mpg")]
    [InlineData(".mpeg")]
    [InlineData(".mts")]
    [InlineData(".m2ts")]
    public void IsSupported_VideoExtensions_ReturnsTrue(string ext)
    {
        Assert.True(FileTypes.IsSupported(ext));
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".docx")]
    [InlineData(".pdf")]
    [InlineData(".zip")]
    [InlineData(".exe")]
    [InlineData("")]
    public void IsSupported_UnsupportedExtensions_ReturnsFalse(string ext)
    {
        Assert.False(FileTypes.IsSupported(ext));
    }

    [Theory]
    [InlineData(".JPG")]
    [InlineData(".JPEG")]
    [InlineData(".PNG")]
    [InlineData(".MP4")]
    [InlineData(".MOV")]
    public void IsSupported_UppercaseExtensions_ReturnsTrue(string ext)
    {
        Assert.True(FileTypes.IsSupported(ext));
    }

    [Fact]
    public void Images_ContainsAllRequiredExtensions()
    {
        string[] required = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif", ".webp",
            ".heic", ".heif", ".raw", ".cr2", ".cr3", ".nef", ".arw", ".orf",
            ".rw2", ".dng", ".pef", ".srw", ".raf"];
        foreach (var ext in required)
            Assert.Contains(ext, FileTypes.Images);
    }

    [Fact]
    public void Videos_ContainsAllRequiredExtensions()
    {
        string[] required = [".mp4", ".mov", ".avi", ".mkv", ".wmv", ".m4v", ".3gp", ".flv",
            ".mpg", ".mpeg", ".mts", ".m2ts"];
        foreach (var ext in required)
            Assert.Contains(ext, FileTypes.Videos);
    }
}

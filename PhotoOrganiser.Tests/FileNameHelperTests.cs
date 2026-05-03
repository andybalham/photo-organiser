using PhotoOrganiser.Helpers;

namespace PhotoOrganiser.Tests;

public class FileNameHelperTests
{
    [Fact]
    public void GetUniqueDestinationPath_NoCollision_ReturnsOriginal()
    {
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\photo.jpg", _ => false);
        Assert.Equal(@"C:\dest\photo.jpg", result);
    }

    [Fact]
    public void GetUniqueDestinationPath_OneCollision_AppendsSuffix1()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dest\photo.jpg" };
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\photo.jpg", existing.Contains);
        Assert.Equal(@"C:\dest\photo_1.jpg", result);
    }

    [Fact]
    public void GetUniqueDestinationPath_MultipleCollisions_IncrementsUntilFree()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\dest\photo.jpg",
            @"C:\dest\photo_1.jpg",
            @"C:\dest\photo_2.jpg",
        };
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\photo.jpg", existing.Contains);
        Assert.Equal(@"C:\dest\photo_3.jpg", result);
    }

    [Fact]
    public void GetUniqueDestinationPath_PreservesExtension()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dest\clip.mp4" };
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\clip.mp4", existing.Contains);
        Assert.Equal(@"C:\dest\clip_1.mp4", result);
    }

    [Fact]
    public void GetUniqueDestinationPath_NoExtension_AppendsSuffixCorrectly()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dest\noext" };
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\noext", existing.Contains);
        Assert.Equal(@"C:\dest\noext_1", result);
    }

    [Fact]
    public void SanitiseFolderName_CleanName_Unchanged()
    {
        Assert.Equal("Summer Holiday", FileNameHelper.SanitiseFolderName("Summer Holiday"));
    }

    [Fact]
    public void SanitiseFolderName_ReplacesColon()
    {
        Assert.Equal("Andy-s Trip", FileNameHelper.SanitiseFolderName("Andy:s Trip"));
    }

    [Fact]
    public void SanitiseFolderName_ReplacesAllIllegalChars()
    {
        Assert.Equal("a-b-c-d-e-f-g-h-i", FileNameHelper.SanitiseFolderName("a:b*c?d\"e<f>g|h\0i"));
    }

    [Fact]
    public void SanitiseFolderName_EmptyString_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FileNameHelper.SanitiseFolderName(string.Empty));
    }

    [Fact]
    public void SanitiseFolderName_PreservesForwardSlashAndBackslash()
    {
        // slashes are excluded from replacement — Path.Combine handles them
        var result = FileNameHelper.SanitiseFolderName("a/b");
        Assert.Equal("a/b", result);
    }

    [Fact]
    public void GetUniqueDestinationPath_PreservesDirectory()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dest\2024\01 January\photo.jpg" };
        var result = FileNameHelper.GetUniqueDestinationPath(@"C:\dest\2024\01 January\photo.jpg", existing.Contains);
        Assert.Equal(@"C:\dest\2024\01 January\photo_1.jpg", result);
    }
}

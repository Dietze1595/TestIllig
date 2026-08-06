using Xunit;

namespace Illig_AI_Platform.Tests.UI;

public class FileDropzoneDesignTests
{
    [Fact]
    public void FileDropzone_UsesFullSurfaceInputAndCustomCallToAction()
    {
        var componentPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Components",
            "FileDropzone.razor"));

        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "Components",
            "FileDropzone.razor.css"));

        var jsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "wwwroot",
            "fileDropzone.js"));

        var indexPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "Illig-AI-Platform.Client",
            "wwwroot",
            "index.html"));

        var componentSource = File.ReadAllText(componentPath);
        var cssSource = File.ReadAllText(cssPath);
        var jsSource = File.ReadAllText(jsPath);
        var indexSource = File.ReadAllText(indexPath);

        Assert.Contains("@inject IJSRuntime JS", componentSource);
        Assert.Contains("InputFile @ref=\"_inputFile\"", componentSource);
        Assert.Contains("disabled=\"@Busy\" hidden", componentSource);
        Assert.Contains("JS.InvokeVoidAsync(\"suFileDropzone.init\"", componentSource);
        Assert.Contains("su-dropzone__action", componentSource);
        Assert.Contains("su-dropzone__eyebrow", componentSource);
        Assert.Contains("clip: rect(0, 0, 0, 0);", cssSource);
        Assert.Contains(".su-dropzone--dragging", cssSource);
        Assert.Contains(".su-dropzone--busy", cssSource);
        Assert.Contains(".su-dropzone__input::file-selector-button", cssSource);
        Assert.Contains("display: none;", cssSource);
        Assert.Contains(".su-dropzone:hover", cssSource);
        Assert.Contains(".su-dropzone__action", cssSource);
        Assert.Contains("preventDefault()", jsSource);
        Assert.Contains("dataTransfer.files", jsSource);
        Assert.Contains("dispatchEvent(new Event(\"change\"", jsSource);
        Assert.Contains("fileDropzone.js", indexSource);
    }
}

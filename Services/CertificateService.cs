using MailSender.Interfaces;
using MailSender.Models;
using SkiaSharp;

namespace MailSender.Services;

public class CertificateService : ICertificateService
{
    private readonly IWebHostEnvironment _environment;

    public CertificateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // ============================================================
    // Generate Certificate
    // ============================================================
    // Matches:
    // ICertificateService.GenerateCertificateAsync(
    //     string templatePath,
    //     string studentName,
    //     CertificateSettings settings)
    // ============================================================

    public async Task<string> GenerateCertificateAsync(
        string templatePath,
        string studentName,
        CertificateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException(
                "Certificate template path is required.",
                nameof(templatePath));

        if (!File.Exists(templatePath))
            throw new FileNotFoundException(
                "Certificate template was not found.",
                templatePath);

        var certificateDirectory = Path.Combine(
            _environment.WebRootPath,
            "certificates");

        Directory.CreateDirectory(certificateDirectory);

        var safeName = MakeSafeFileName(studentName);

        var outputPath = Path.Combine(
            certificateDirectory,
            $"{safeName}_{Guid.NewGuid():N}.png");

        await GenerateImageAsync(
            templatePath,
            outputPath,
            studentName,
            settings);

        return outputPath;
    }

    // ============================================================
    // Generate Preview
    // ============================================================
    // Matches:
    // ICertificateService.GeneratePreviewAsync(
    //     string templatePath,
    //     string studentName,
    //     CertificateSettings settings)
    // ============================================================

    public async Task<string> GeneratePreviewAsync(
        string templatePath,
        string studentName,
        CertificateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(templatePath))
            throw new ArgumentException(
                "Certificate template path is required.",
                nameof(templatePath));

        if (!File.Exists(templatePath))
            throw new FileNotFoundException(
                "Certificate template was not found.",
                templatePath);

        await using var inputStream =
            File.OpenRead(templatePath);

        using var bitmap =
            SKBitmap.Decode(inputStream);

        if (bitmap == null)
            throw new InvalidOperationException(
                "Could not decode the certificate image.");

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
            throw new InvalidOperationException(
                "Certificate image has invalid dimensions.");

        // IMPORTANT:
        // We DO NOT clear the canvas.
        //
        // The certificate image already exists inside
        // the bitmap. Clearing it would make the preview
        // transparent/black.
        using var canvas = new SKCanvas(bitmap);

        DrawStudentName(
            canvas,
            studentName,
            bitmap.Width,
            bitmap.Height,
            settings);

        canvas.Flush();

        using var image =
            SKImage.FromBitmap(bitmap);

        using var data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);

        if (data == null)
            throw new InvalidOperationException(
                "Could not generate certificate preview.");

        return $"data:image/png;base64,{Convert.ToBase64String(data.ToArray())}";
    }

    // ============================================================
    // Generate Image
    // ============================================================

    private async Task GenerateImageAsync(
        string templatePath,
        string outputPath,
        string studentName,
        CertificateSettings settings)
    {
        await using var inputStream =
            File.OpenRead(templatePath);

        using var bitmap =
            SKBitmap.Decode(inputStream);

        if (bitmap == null)
            throw new InvalidOperationException(
                "Could not decode the certificate image.");

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
            throw new InvalidOperationException(
                "Certificate image has invalid dimensions.");

        // IMPORTANT:
        // Do not call canvas.Clear().
        using var canvas = new SKCanvas(bitmap);

        DrawStudentName(
            canvas,
            studentName,
            bitmap.Width,
            bitmap.Height,
            settings);

        canvas.Flush();

        using var image =
            SKImage.FromBitmap(bitmap);

        using var data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);

        if (data == null)
            throw new InvalidOperationException(
                "Could not encode certificate image.");

        await File.WriteAllBytesAsync(
            outputPath,
            data.ToArray());
    }

    // ============================================================
    // Draw Student Name
    // ============================================================

    private static void DrawStudentName(
        SKCanvas canvas,
        string studentName,
        int imageWidth,
        int imageHeight,
        CertificateSettings settings)
    {
        if (string.IsNullOrWhiteSpace(studentName))
            studentName = "Student Name";

        // -----------------------------
        // Font Size
        // -----------------------------

        var fontSize =
            settings.FontSize > 0
                ? settings.FontSize
                : 48;

        // -----------------------------
        // Font Family
        // -----------------------------

        var fontFamily =
            string.IsNullOrWhiteSpace(settings.FontFamily)
                ? "Arial"
                : settings.FontFamily;

        using var typeface =
            SKTypeface.FromFamilyName(
                fontFamily,
                SKFontStyle.Normal);

        // -----------------------------
        // Paint
        // -----------------------------

        using var paint = new SKPaint
        {
            Typeface = typeface ?? SKTypeface.Default,

            TextSize = fontSize,

            IsAntialias = true,

            SubpixelText = true,

            Style = SKPaintStyle.Fill,

            Color = ParseColor(settings.Color)
        };

        // -----------------------------
        // X Position
        // -----------------------------

        var x = settings.X;

        if (x <= 0)
        {
            x = (int)(imageWidth / 2f);
        }

        // -----------------------------
        // Y Position
        // -----------------------------

        var y = settings.Y;

        if (y <= 0)
        {
            y = (int)(imageHeight / 2f);
        }

        // -----------------------------
        // Alignment
        // -----------------------------

        var alignment =
            settings.Alignment?
                .Trim()
                .ToLowerInvariant();

        switch (alignment)
        {
            case "center":
                paint.TextAlign =
                    SKTextAlign.Center;
                break;

            case "right":
                paint.TextAlign =
                    SKTextAlign.Right;
                break;

            default:
                paint.TextAlign =
                    SKTextAlign.Left;
                break;
        }

        // -----------------------------
        // Draw Name
        // -----------------------------

        canvas.DrawText(
            studentName,
            x,
            y,
            paint);
    }

    // ============================================================
    // Parse Color
    // ============================================================

    private static SKColor ParseColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return SKColors.Black;

        try
        {
            if (SKColor.TryParse(
                    color,
                    out var parsedColor))
            {
                return parsedColor;
            }
        }
        catch
        {
            // Use black if color is invalid.
        }

        return SKColors.Black;
    }

    // ============================================================
    // Safe File Name
    // ============================================================

    private static string MakeSafeFileName(
        string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "student";

        foreach (var invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            fileName =
                fileName.Replace(
                    invalidCharacter,
                    '_');
        }

        fileName = fileName.Trim();

        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "student";

        return fileName;
    }
}

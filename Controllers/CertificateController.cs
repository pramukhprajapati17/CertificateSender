
using MailSender.Interfaces;
using MailSender.Models;
using MailSender.Services;
using Microsoft.AspNetCore.Mvc;

namespace MailSender.Controllers;

public class CertificateController : Controller
{
    private readonly IExcelService _excelService;
    private readonly ICertificateService _certificateService;
    private readonly IEmailService _emailService;
    private readonly IWorkspaceService _workspaceService;
    private readonly SendProgressService _sendProgressService;

    public CertificateController(
        IExcelService excelService,
        ICertificateService certificateService,
        IEmailService emailService,
        IWorkspaceService workspaceService,
        SendProgressService sendProgressService)
    {
        _excelService = excelService;
        _certificateService = certificateService;
        _emailService = emailService;
        _workspaceService = workspaceService;
        _sendProgressService = sendProgressService;
    }


    // =========================================================
    // SETUP PAGE
    // =========================================================

    [HttpGet]
    public IActionResult Setup()
    {
        WorkspaceState state =
            _workspaceService.GetState();

        ViewBag.State = state;

        return View(state.CertificateSettings);
    }


    // =========================================================
    // UPLOAD CERTIFICATE + EXCEL
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(
        IFormFile? certificateImage,
        IFormFile? excelFile)
    {
        if (certificateImage == null ||
            certificateImage.Length == 0)
        {
            TempData["Error"] =
                "Please select a certificate image.";

            return RedirectToAction(nameof(Setup));
        }

        if (excelFile == null ||
            excelFile.Length == 0)
        {
            TempData["Error"] =
                "Please select an Excel file.";

            return RedirectToAction(nameof(Setup));
        }

        try
        {
            string uploadDirectory =
                _workspaceService.GetUploadDirectory();

            Directory.CreateDirectory(uploadDirectory);


            // =====================================================
            // 1. VALIDATE CERTIFICATE IMAGE
            // =====================================================

            string certificateExtension =
                Path.GetExtension(
                    certificateImage.FileName)
                .ToLowerInvariant();

            string[] allowedImageExtensions =
            {
                ".png",
                ".jpg",
                ".jpeg"
            };

            if (!allowedImageExtensions.Contains(
                    certificateExtension))
            {
                TempData["Error"] =
                    "Only PNG, JPG and JPEG certificate images are allowed.";

                return RedirectToAction(nameof(Setup));
            }


            // =====================================================
            // 2. REMOVE OLD CERTIFICATE TEMPLATES
            // =====================================================

            foreach (string oldFile in Directory.GetFiles(
                uploadDirectory,
                "certificate_template.*"))
            {
                TryDeleteFile(oldFile);
            }


            // =====================================================
            // 3. SAVE CERTIFICATE TEMPLATE
            // =====================================================

            string certificatePath =
                Path.Combine(
                    uploadDirectory,
                    "certificate_template" +
                    certificateExtension);

            await using (
                FileStream imageStream =
                    new FileStream(
                        certificatePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
            {
                await certificateImage.CopyToAsync(
                    imageStream);
            }


            // =====================================================
            // 4. VALIDATE EXCEL EXTENSION
            // =====================================================

            string excelExtension =
                Path.GetExtension(
                    excelFile.FileName)
                .ToLowerInvariant();

            if (excelExtension != ".xlsx")
            {
                TempData["Error"] =
                    "Please upload an .xlsx Excel file.";

                return RedirectToAction(nameof(Setup));
            }


            // =====================================================
            // 5. CREATE UNIQUE WORKING EXCEL
            // =====================================================
            //
            // This is the ONLY Excel file used by the application.
            //
            // It will be updated directly after sending.
            //
            // Example:
            //
            // students_abc123.xlsx
            //
            // =====================================================

            string excelFileName =
                $"students_{Guid.NewGuid():N}.xlsx";

            string excelPath =
                Path.Combine(
                    uploadDirectory,
                    excelFileName);


            // =====================================================
            // 6. COPY EXCEL
            // =====================================================

            await using (
                FileStream excelStream =
                    new FileStream(
                        excelPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await excelFile.CopyToAsync(
                    excelStream);
            }


            // =====================================================
            // 7. VALIDATE EXCEL
            // =====================================================

            List<Student> students;

            try
            {
                students =
                    await _excelService.ReadStudentsAsync(
                        excelPath);
            }
            catch (Exception ex)
            {
                TryDeleteFile(excelPath);

                TempData["Error"] =
                    $"Excel validation failed: {ex.Message}";

                return RedirectToAction(nameof(Setup));
            }


            // =====================================================
            // 8. CHECK STUDENT DATA
            // =====================================================

            if (students.Count == 0)
            {
                TryDeleteFile(excelPath);

                TempData["Error"] =
                    "The Excel file does not contain any students.";

                return RedirectToAction(nameof(Setup));
            }


            // =====================================================
            // 9. SAVE WORKSPACE STATE
            // =====================================================

            WorkspaceState state =
                _workspaceService.GetState();

            state.TemplatePath =
                certificatePath;

            state.ExcelPath =
                excelPath;

            // Important:
            // We no longer create/store a ResultExcelPath.

            state.ResultExcelPath = null;

            if (state.CertificateSettings == null)
            {
                state.CertificateSettings =
                    new CertificateSettings();
            }

            _workspaceService.SaveState(state);


            // =====================================================
            // 10. SUCCESS
            // =====================================================

            TempData["Success"] =
                $"Upload successful. {students.Count} students found.";

            return RedirectToAction(nameof(Students));
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                $"Upload failed: {ex.Message}";

            return RedirectToAction(nameof(Setup));
        }
    }


    // =========================================================
    // SAVE CERTIFICATE SETTINGS
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SaveSettings(
        CertificateSettings model)
    {
        WorkspaceState state =
            _workspaceService.GetState();

        if (model.X < 0)
        {
            model.X = 0;
        }

        if (model.Y < 0)
        {
            model.Y = 0;
        }

        if (model.FontSize <= 0)
        {
            model.FontSize = 48;
        }

        if (string.IsNullOrWhiteSpace(
                model.FontFamily))
        {
            model.FontFamily = "Arial";
        }

        if (string.IsNullOrWhiteSpace(
                model.Color))
        {
            model.Color = "#000000";
        }

        if (string.IsNullOrWhiteSpace(
                model.Alignment))
        {
            model.Alignment = "Center";
        }

        state.CertificateSettings =
            model;

        _workspaceService.SaveState(state);

        TempData["Success"] =
            "Certificate settings saved successfully.";

        return RedirectToAction(
            nameof(Setup));
    }


    // =========================================================
    // PREVIEW PAGE
    // =========================================================

    [HttpGet]
    public IActionResult Preview()
    {
        WorkspaceState state =
            _workspaceService.GetState();

        if (string.IsNullOrWhiteSpace(
                state.TemplatePath) ||
            !System.IO.File.Exists(
                state.TemplatePath))
        {
            TempData["Error"] =
                "Please upload a certificate template first.";

            return RedirectToAction(
                nameof(Setup));
        }

        return View(
            state.CertificateSettings);
    }


    // =========================================================
    // GENERATE PREVIEW
    // =========================================================
    //
    // Returns JSON:
    //
    // {
    //     success: true,
    //     image: "data:image/png;base64,..."
    // }
    //
    // =========================================================

    [HttpPost]
    public async Task<IActionResult> Preview(
        string studentName,
        CertificateSettings settings)
    {
        try
        {
            WorkspaceState state =
                _workspaceService.GetState();

            if (string.IsNullOrWhiteSpace(
                    state.TemplatePath))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Please upload a certificate template first."
                });
            }

            if (!System.IO.File.Exists(
                    state.TemplatePath))
            {
                return Json(new
                {
                    success = false,
                    message =
                        "Certificate template was not found."
                });
            }

            if (string.IsNullOrWhiteSpace(
                    studentName))
            {
                studentName = "Student Name";
            }

            // If the submitted settings are empty,
            // use saved workspace settings.
            if (settings == null)
            {
                settings =
                    state.CertificateSettings
                    ?? new CertificateSettings();
            }

            string preview =
                await _certificateService
                    .GeneratePreviewAsync(
                        state.TemplatePath,
                        studentName,
                        settings);

            return Json(new
            {
                success = true,
                image = preview
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = ex.Message
            });
        }
    }


    // =========================================================
    // STUDENT LIST
    // =========================================================

    [HttpGet]
    public async Task<IActionResult> Students()
    {
        WorkspaceState state =
            _workspaceService.GetState();

        if (string.IsNullOrWhiteSpace(
                state.ExcelPath) ||
            !System.IO.File.Exists(
                state.ExcelPath))
        {
            TempData["Error"] =
                "Please upload the student Excel file first.";

            return RedirectToAction(
                nameof(Setup));
        }

        try
        {
            List<Student> students =
                await _excelService
                    .ReadStudentsAsync(
                        state.ExcelPath);

            return View(students);
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                $"Unable to read Excel: {ex.Message}";

            return RedirectToAction(
                nameof(Setup));
        }
    }


    // =========================================================
    // SEND ALL CERTIFICATES
    // =========================================================
    //
    // IMPORTANT:
    //
    // The SAME Excel file is updated.
    //
    // NO certificate_result_*.xlsx is created.
    //
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAll()
    {
        var state = _workspaceService.GetState();

        if (string.IsNullOrWhiteSpace(state.TemplatePath) ||
            !System.IO.File.Exists(state.TemplatePath))
        {
            TempData["Error"] = "Certificate template not found.";
            return RedirectToAction(nameof(Students));
        }

        if (string.IsNullOrWhiteSpace(state.ExcelPath) ||
            !System.IO.File.Exists(state.ExcelPath))
        {
            TempData["Error"] = "Excel file not found.";
            return RedirectToAction(nameof(Students));
        }

        try
        {
            var students =
                await _excelService.ReadStudentsAsync(state.ExcelPath);

            // Only process students that are not already successful
            var pendingStudents = students
                .Where(x =>
                    !string.Equals(
                        x.Status,
                        "Success",
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pendingStudents.Count == 0)
            {
                TempData["Success"] =
                    "All students have already been sent successfully.";

                return RedirectToAction(nameof(Students));
            }

            // Start progress
            _sendProgressService.Start(pendingStudents.Count);

            foreach (var student in pendingStudents)
            {
                // ============================================
                // PAUSE CHECK
                // ============================================

                while (_sendProgressService.IsPaused)
                {
                    await Task.Delay(500);
                }


                // ============================================
                // SET CURRENT STUDENT
                // ============================================

                _sendProgressService.SetCurrentStudent(
                    student.RowNumber,
                    student.Name,
                    student.Email);


                // ============================================
                // VALIDATE NAME
                // ============================================

                if (string.IsNullOrWhiteSpace(student.Name))
                {
                    student.Status = "Failed";
                    student.SentAt = null;
                    student.Error = "Student name is empty.";

                    await _excelService.UpdateResultExcelAsync(
                        state.ExcelPath,
                        state.ExcelPath,
                        students);

                    _sendProgressService.SetFailed(
                        "Student name is empty.");

                    continue;
                }


                // ============================================
                // VALIDATE EMAIL
                // ============================================

                if (!IsValidEmail(student.Email))
                {
                    student.Status = "Failed";
                    student.SentAt = null;
                    student.Error = "Invalid email address.";

                    await _excelService.UpdateResultExcelAsync(
                        state.ExcelPath,
                        state.ExcelPath,
                        students);

                    _sendProgressService.SetFailed(
                        "Invalid email address.");

                    continue;
                }


                // ============================================
                // SEND CERTIFICATE
                // ============================================

                try
                {
                    // Generate certificate
                    var certificatePath =
                        await _certificateService
                            .GenerateCertificateAsync(
                                state.TemplatePath,
                                student.Name,
                                state.CertificateSettings);


                    // Send email
                    await _emailService.SendCertificateAsync(
                        student.Email,
                        student.Name,
                        certificatePath);


                    // ========================================
                    // SUCCESS
                    // ========================================

                    student.Status = "Success";
                    student.SentAt = DateTime.Now;
                    student.Error = string.Empty;


                    // Save into SAME Excel
                    await _excelService.UpdateResultExcelAsync(
                        state.ExcelPath,
                        state.ExcelPath,
                        students);


                    _sendProgressService.SetSuccess();
                }
                catch (Exception ex)
                {
                    // ========================================
                    // FAILED
                    // ========================================

                    student.Status = "Failed";
                    student.SentAt = null;
                    student.Error = ex.Message;


                    try
                    {
                        // Save failure into SAME Excel
                        await _excelService.UpdateResultExcelAsync(
                            state.ExcelPath,
                            state.ExcelPath,
                            students);
                    }
                    catch
                    {
                        // Keep original sending error
                    }


                    _sendProgressService.SetFailed(
                        ex.Message);
                }
            }


            // ============================================
            // FINISH
            // ============================================

            _sendProgressService.Finish();

            TempData["Success"] =
                "Send All process completed.";

        }
        catch (Exception ex)
        {
            _sendProgressService.Finish();

            TempData["Error"] =
                $"Send All failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Students));
    }


    // ====================================================
    // GET SEND PROGRESS
    // ====================================================

    [HttpGet]
    public IActionResult SendProgress()
    {
        return Json(
            _sendProgressService.GetStatus());
    }


    // ====================================================
    // PAUSE SEND ALL
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PauseSending()
    {
        _sendProgressService.Pause();

        return Json(new
        {
            success = true,
            message = "Sending paused."
        });
    }


    // ====================================================
    // RESUME SEND ALL
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResumeSending()
    {
        _sendProgressService.Resume();

        return Json(new
        {
            success = true,
            message = "Sending resumed."
        });
    }


    // =========================================================
    // DOWNLOAD THE SAME EXCEL
    // =========================================================
    //
    // This downloads the uploaded Excel after its statuses
    // have been updated.
    //
    // =========================================================

    [HttpGet]
    public IActionResult DownloadResult()
    {
        WorkspaceState state =
            _workspaceService.GetState();

        if (string.IsNullOrWhiteSpace(
                state.ExcelPath) ||
            !System.IO.File.Exists(
                state.ExcelPath))
        {
            TempData["Error"] =
                "Student Excel file is not available.";

            return RedirectToAction(
                nameof(Students));
        }

        return PhysicalFile(
            state.ExcelPath,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Path.GetFileName(
                state.ExcelPath));
    }


    // =========================================================
    // RESET WORKSPACE
    // =========================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        _workspaceService.Reset();

        TempData["Success"] =
            "Workspace has been reset successfully.";

        return RedirectToAction(
            nameof(Setup));
    }


    // =========================================================
    // EMAIL VALIDATION
    // =========================================================

    private static bool IsValidEmail(
        string email)
    {
        if (string.IsNullOrWhiteSpace(
                email))
        {
            return false;
        }

        try
        {
            var mailAddress =
                new System.Net.Mail.MailAddress(
                    email);

            return mailAddress.Address.Equals(
                email.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }


    // =========================================================
    // SAFE FILE DELETE
    // =========================================================

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (System.IO.File.Exists(
                    filePath))
            {
                System.IO.File.Delete(
                    filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendIndividual(int rowNumber)
    {
        var state = _workspaceService.GetState();

        // Check certificate template
        if (string.IsNullOrWhiteSpace(state.TemplatePath) ||
            !System.IO.File.Exists(state.TemplatePath))
        {
            TempData["Error"] = "Certificate template not found.";
            return RedirectToAction(nameof(Students));
        }

        // Check Excel file
        if (string.IsNullOrWhiteSpace(state.ExcelPath) ||
            !System.IO.File.Exists(state.ExcelPath))
        {
            TempData["Error"] = "Excel file not found.";
            return RedirectToAction(nameof(Students));
        }

        try
        {
            // Read students from the SAME uploaded Excel file
            var students = await _excelService.ReadStudentsAsync(
                state.ExcelPath);

            // Find selected student
            var student = students.FirstOrDefault(
                x => x.RowNumber == rowNumber);

            if (student == null)
            {
                TempData["Error"] = "Student not found.";
                return RedirectToAction(nameof(Students));
            }

            // Do not send again if already successful
            if (string.Equals(
                    student.Status,
                    "Success",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    $"{student.Name} has already been sent successfully.";

                return RedirectToAction(nameof(Students));
            }

            // Validate student name
            if (string.IsNullOrWhiteSpace(student.Name))
            {
                student.Status = "Failed";
                student.SentAt = null;
                student.Error = "Student name is empty.";

                await _excelService.UpdateResultExcelAsync(
                    state.ExcelPath,
                    state.ExcelPath,
                    students);

                TempData["Error"] = "Student name is empty.";

                return RedirectToAction(nameof(Students));
            }

            // Validate email
            if (!IsValidEmail(student.Email))
            {
                student.Status = "Failed";
                student.SentAt = null;
                student.Error = "Invalid email address.";

                await _excelService.UpdateResultExcelAsync(
                    state.ExcelPath,
                    state.ExcelPath,
                    students);

                TempData["Error"] =
                    $"Invalid email address for {student.Name}.";

                return RedirectToAction(nameof(Students));
            }

            try
            {
                // ============================================
                // 1. GENERATE PERSONALIZED CERTIFICATE
                // ============================================

                var certificatePath =
                    await _certificateService.GenerateCertificateAsync(
                        state.TemplatePath,
                        student.Name,
                        state.CertificateSettings);

                // ============================================
                // 2. SEND EMAIL
                // ============================================

                await _emailService.SendCertificateAsync(
                    student.Email,
                    student.Name,
                    certificatePath);

                // ============================================
                // 3. UPDATE STUDENT STATUS
                // ============================================

                student.Status = "Success";
                student.SentAt = DateTime.Now;
                student.Error = string.Empty;

                // ============================================
                // 4. UPDATE SAME EXCEL FILE
                // ============================================

                await _excelService.UpdateResultExcelAsync(
                    state.ExcelPath,
                    state.ExcelPath,
                    students);

                TempData["Success"] =
                    $"Certificate sent successfully to {student.Name}.";
            }
            catch (Exception ex)
            {
                // Email/certificate generation failed
                student.Status = "Failed";
                student.SentAt = null;
                student.Error = ex.Message;

                try
                {
                    // Save failed status into SAME Excel
                    await _excelService.UpdateResultExcelAsync(
                        state.ExcelPath,
                        state.ExcelPath,
                        students);

                    TempData["Error"] =
                        $"Failed to send certificate to " +
                        $"{student.Name}: {ex.Message}";
                }
                catch (Exception excelEx)
                {
                    TempData["Error"] =
                        $"Failed to send certificate to " +
                        $"{student.Name}: {ex.Message}. " +
                        $"Excel update also failed: {excelEx.Message}";
                }
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] =
                $"Unable to process student: {ex.Message}";
        }

        return RedirectToAction(nameof(Students));
    }

}

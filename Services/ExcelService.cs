using ClosedXML.Excel;
using MailSender.Interfaces;
using MailSender.Models;

namespace MailSender.Services;

public class ExcelService : IExcelService
{
    public async Task<List<Student>> ReadStudentsAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(
                "Excel file path is required.",
                nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "Excel file was not found.",
                filePath);

        var students = new List<Student>();

        // IMPORTANT:
        // XLWorkbook must be disposed before this method returns.
        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new InvalidOperationException(
                    "The Excel file does not contain a worksheet.");

            var firstRow = worksheet.FirstRowUsed();

            if (firstRow == null)
                throw new InvalidOperationException(
                    "The Excel file is empty.");

            var headerRow = firstRow.RowNumber();

            var lastColumn = worksheet.LastColumnUsed();

            if (lastColumn == null)
                throw new InvalidOperationException(
                    "The Excel file does not contain columns.");

            int nameColumn = -1;
            int emailColumn = -1;
            int statusColumn = -1;
            int sentAtColumn = -1;
            int errorColumn = -1;

            // --------------------------------------------
            // Find columns
            // --------------------------------------------

            for (int column = 1;
                 column <= lastColumn.ColumnNumber();
                 column++)
            {
                var header =
                    worksheet.Cell(
                        headerRow,
                        column)
                    .GetString()
                    .Trim()
                    .ToLowerInvariant();

                switch (header)
                {
                    case "name":
                    case "student name":
                    case "studentname":
                    case "full name":

                        nameColumn = column;
                        break;

                    case "email":
                    case "email address":
                    case "e-mail":

                        emailColumn = column;
                        break;

                    case "status":

                        statusColumn = column;
                        break;

                    case "sent at":
                    case "sentat":
                    case "sent date":

                        sentAtColumn = column;
                        break;

                    case "error":
                    case "error message":

                        errorColumn = column;
                        break;
                }
            }

            if (nameColumn == -1)
                throw new InvalidOperationException(
                    "Excel file must contain a Name column.");

            if (emailColumn == -1)
                throw new InvalidOperationException(
                    "Excel file must contain an Email column.");

            var lastRow =
                worksheet.LastRowUsed()?.RowNumber()
                ?? headerRow;

            for (int row = headerRow + 1;
                 row <= lastRow;
                 row++)
            {
                var name =
                    worksheet.Cell(
                        row,
                        nameColumn)
                    .GetString()
                    .Trim();

                var email =
                    worksheet.Cell(
                        row,
                        emailColumn)
                    .GetString()
                    .Trim();

                // Ignore completely empty rows.
                if (string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var student = new Student
                {
                    RowNumber = row,
                    Name = name,
                    Email = email,

                    Status =
                        statusColumn > 0
                            ? worksheet.Cell(
                                row,
                                statusColumn)
                              .GetString()
                              .Trim()
                            : "Pending",

                    SentAt =
                        sentAtColumn > 0 &&
                        worksheet.Cell(
                            row,
                            sentAtColumn)
                        .TryGetValue<DateTime>(
                            out var sentAt)
                            ? sentAt
                            : null,

                    Error =
                        errorColumn > 0
                            ? worksheet.Cell(
                                row,
                                errorColumn)
                              .GetString()
                              .Trim()
                            : null
                };

                students.Add(student);
            }
        }

        // Give the file handle time to be released.
        await Task.CompletedTask;

        return students;
    }


    // ============================================================
    // UPDATE THE SAME EXCEL FILE
    // ============================================================

    public async Task UpdateResultExcelAsync(
        string sourceFilePath,
        string outputFilePath,
        List<Student> students)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException(
                "Excel file path is required.",
                nameof(sourceFilePath));

        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException(
                "Excel file was not found.",
                sourceFilePath);

        if (students == null)
            throw new ArgumentNullException(
                nameof(students));

        // IMPORTANT:
        // Do not keep the workbook open.
        using (var workbook = new XLWorkbook(sourceFilePath))
        {
            var worksheet =
                workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                throw new InvalidOperationException(
                    "The Excel file does not contain a worksheet.");

            var headerRow =
                worksheet.FirstRowUsed()?.RowNumber() ?? 1;

            var lastColumn =
                worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            if (lastColumn == 0)
                throw new InvalidOperationException(
                    "The Excel file does not contain columns.");

            int statusColumn = FindColumn(
                worksheet,
                headerRow,
                lastColumn,
                "status");

            int sentAtColumn = FindColumn(
                worksheet,
                headerRow,
                lastColumn,
                "sent at");

            int errorColumn = FindColumn(
                worksheet,
                headerRow,
                lastColumn,
                "error");


            // --------------------------------------------
            // Create missing columns
            // --------------------------------------------

            if (statusColumn == -1)
            {
                lastColumn++;

                statusColumn = lastColumn;

                worksheet.Cell(
                    headerRow,
                    statusColumn)
                    .Value = "Status";
            }

            if (sentAtColumn == -1)
            {
                lastColumn++;

                sentAtColumn = lastColumn;

                worksheet.Cell(
                    headerRow,
                    sentAtColumn)
                    .Value = "Sent At";
            }

            if (errorColumn == -1)
            {
                lastColumn++;

                errorColumn = lastColumn;

                worksheet.Cell(
                    headerRow,
                    errorColumn)
                    .Value = "Error";
            }


            // --------------------------------------------
            // Update student rows
            // --------------------------------------------

            foreach (var student in students)
            {
                if (student.RowNumber <= headerRow)
                    continue;

                worksheet.Cell(
                    student.RowNumber,
                    statusColumn)
                    .Value =
                    student.Status ?? "Pending";

                var sentAtCell =
                    worksheet.Cell(
                        student.RowNumber,
                        sentAtColumn);

                if (student.SentAt.HasValue)
                {
                    sentAtCell.Value =
                        student.SentAt.Value;

                    sentAtCell.Style.DateFormat.Format =
                        "dd-MM-yyyy HH:mm:ss";
                }
                else
                {
                    sentAtCell.Clear();
                }

                worksheet.Cell(
                    student.RowNumber,
                    errorColumn)
                    .Value =
                    student.Error ?? string.Empty;
            }


            // --------------------------------------------
            // Formatting
            // --------------------------------------------

            worksheet
                .Row(headerRow)
                .Style
                .Font
                .Bold = true;

            worksheet.Columns().AdjustToContents();


            // --------------------------------------------
            // SAVE TO OUTPUT FILE
            // --------------------------------------------

            workbook.SaveAs(outputFilePath);
        }

        // Workbook is now completely disposed.
        await Task.CompletedTask;
    }


    // ============================================================
    // Get Student Key
    // ============================================================

    public string GetStudentKey(Student student)
    {
        return $"{student.Name}|{student.Email}"
            .ToLowerInvariant()
            .Trim();
    }


    // ============================================================
    // Find Column
    // ============================================================

    private static int FindColumn(
        IXLWorksheet worksheet,
        int headerRow,
        int lastColumn,
        string columnName)
    {
        for (int column = 1;
             column <= lastColumn;
             column++)
        {
            var header =
                worksheet.Cell(
                    headerRow,
                    column)
                .GetString()
                .Trim()
                .ToLowerInvariant();

            if (columnName == "status" &&
                header == "status")
            {
                return column;
            }

            if (columnName == "sent at" &&
                (header == "sent at" ||
                 header == "sentat" ||
                 header == "sent date"))
            {
                return column;
            }

            if (columnName == "error" &&
                (header == "error" ||
                 header == "error message"))
            {
                return column;
            }
        }

        return -1;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailSender.Models;

namespace MailSender.Interfaces
{
    public interface IExcelService
    {
        Task<List<Student>> ReadStudentsAsync(
        string filePath);

        Task UpdateResultExcelAsync(
            string sourceFilePath,
            string outputFilePath,
            List<Student> students);

        string GetStudentKey(Student student);

    }
}
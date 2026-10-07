using System.Text.Json;

using MailSender.Interfaces;
using MailSender.Models;

namespace CertificateMailer.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWebHostEnvironment _environment;

    private readonly object _lock =
        new object();

    public WorkspaceService(
        IWebHostEnvironment environment)
    {
        _environment =
            environment;

        Directory.CreateDirectory(
            GetDataDirectory());

        Directory.CreateDirectory(
            GetUploadDirectory());

        Directory.CreateDirectory(
            GetCertificateDirectory());
    }

    public string GetDataDirectory()
    {
        return Path.Combine(
            _environment.ContentRootPath,
            "App_Data");
    }

    public string GetUploadDirectory()
    {
        return Path.Combine(
            _environment.WebRootPath,
            "uploads");
    }

    public string GetCertificateDirectory()
    {
        return Path.Combine(
            _environment.WebRootPath,
            "certificates");
    }

    private string GetStateFile()
    {
        return Path.Combine(
            GetDataDirectory(),
            "state.json");
    }

    public WorkspaceState GetState()
    {
        lock (_lock)
        {
            string stateFile =
                GetStateFile();

            if (!File.Exists(stateFile))
            {
                return new WorkspaceState();
            }

            string json =
                File.ReadAllText(
                    stateFile);

            return JsonSerializer
                .Deserialize<WorkspaceState>(
                    json)
                ?? new WorkspaceState();
        }
    }

    public void SaveState(
        WorkspaceState state)
    {
        lock (_lock)
        {
            string json =
                JsonSerializer.Serialize(
                    state,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            File.WriteAllText(
                GetStateFile(),
                json);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            string stateFile =
                GetStateFile();

            if (File.Exists(stateFile))
            {
                File.Delete(stateFile);
            }

            string uploadDirectory =
                GetUploadDirectory();

            if (Directory.Exists(
                    uploadDirectory))
            {
                foreach (
                    string file
                    in Directory.GetFiles(
                        uploadDirectory))
                {
                    File.Delete(file);
                }
            }

            string certificateDirectory =
                GetCertificateDirectory();

            if (Directory.Exists(
                    certificateDirectory))
            {
                foreach (
                    string file
                    in Directory.GetFiles(
                        certificateDirectory,
                        "*",
                        SearchOption.AllDirectories))
                {
                    File.Delete(file);
                }
            }
        }
    }
}

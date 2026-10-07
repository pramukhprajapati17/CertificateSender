using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailSender.Models;

namespace MailSender.Interfaces
{
    public interface IWorkspaceService
    {
        WorkspaceState GetState();

        void SaveState(WorkspaceState state);

        string GetUploadDirectory();

        string GetCertificateDirectory();

        string GetDataDirectory();

        void Reset();

    }
}
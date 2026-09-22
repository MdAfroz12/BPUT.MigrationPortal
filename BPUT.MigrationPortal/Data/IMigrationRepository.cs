// Data/IMigrationRepository.cs
using BPUT.MigrationPortal.Models;

namespace BPUT.MigrationPortal.Data
{
    public interface IMigrationRepository
    {
        Task<MigrationCertificateVM?> GetStudentByRegNoAsync(string regNo);
        Task SaveIssueDateAsync(string regNo, DateTime issueDate);
        Task<string> GenerateAndSaveCertificateNoAsync(string regNo);
        Task ConfirmMigrationIssuedAsync(string regNo);
    }
}
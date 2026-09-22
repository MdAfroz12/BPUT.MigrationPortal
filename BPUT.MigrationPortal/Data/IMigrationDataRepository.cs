using BPUT.MigrationPortal.Models;

namespace BPUT.MigrationPortal.Data
{
    public interface IMigrationDataRepository
    {
        Task<MigrationRecordVM?> GetFromAdmissionAsync(string regNo);
        Task<MigrationRecordVM?> GetFromDegreeAsync(string regNo);
        Task AddRecordAsync(MigrationRecordVM record, DateTime receivedDate);
        Task<List<MigrationRecordVM>> GetAllRecordsAsync();
        Task<MigrationRecordVM?> GetRecordByRegNoAsync(string regNo);
        Task IssueMigrationAsync(string regNo, DateTime issueDate, string certificateNo);
        Task<string> GetNextCertificateNoAsync();
        Task<List<CollegeOptionVM>> GetDistinctCollegesAsync();
        Task<List<MigrationRecordVM>> GetReportAsync(DateTime fromDate, DateTime toDate, string collegeCode);
        Task SaveDuplicateIssueDateAsync(string regNo, DateTime duplicateDate);
    }
}
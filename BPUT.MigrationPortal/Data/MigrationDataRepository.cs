using Dapper;
using Microsoft.Data.SqlClient;
using BPUT.MigrationPortal.Models;

namespace BPUT.MigrationPortal.Data
{
    public class MigrationDataRepository : IMigrationDataRepository
    {
        private readonly string _connectionString = "";

        public MigrationDataRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("BPUTLive") ?? "";
        }

        public async Task<MigrationRecordVM?> GetFromAdmissionAsync(string regNo)
        {
            const string sql = @"
                SELECT
                    S.FRM_ROLLNO   AS RegNo,
                    S.FRM_NAME     AS StudentName,
                    S.FRM_INSTID   AS CollegeCode,
                    C.Inst_Title   AS CollegeName
                FROM [BPUTLIVE].[dbo].[Student_Info] S
                INNER JOIN [BPUTLIVE].[dbo].[College] C
                    ON S.FRM_INSTID = C.Inst_ID
                WHERE S.FRM_ROLLNO = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<MigrationRecordVM>(
                sql, new { RegNo = regNo });
        }

        public async Task<MigrationRecordVM?> GetFromDegreeAsync(string regNo)
        {
            const string sql = @"
                SELECT
                    UDA_RollNO  AS RegNo,
                    UDA_Name    AS StudentName,
                    UDA_CCode   AS CollegeCode,
                    UDA_CName   AS CollegeName
                FROM [Degree_Data].[dbo].[Degree_All_Data]
                WHERE UDA_RollNO = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<MigrationRecordVM>(
                sql, new { RegNo = regNo });
        }

        public async Task AddRecordAsync(MigrationRecordVM record, DateTime receivedDate)
        {
            const string sql = @"
                INSERT INTO [BPUTLIVE].[dbo].[Migration_Records]
                    (DataSource, RegNo, StudentName, CollegeCode, CollegeName, MigrationReceivedDate, CreatedDate)
                VALUES (@DataSource, @RegNo, @StudentName, @CollegeCode, @CollegeName, @ReceivedDate, GETDATE())";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new
            {
                record.DataSource,
                record.RegNo,
                record.StudentName,
                record.CollegeCode,
                record.CollegeName,
                ReceivedDate = receivedDate
            });
        }

        public async Task<List<MigrationRecordVM>> GetAllRecordsAsync()
        {
            const string sql = @"
                SELECT Id, DataSource, RegNo, StudentName, CollegeCode, CollegeName,
                       MigrationReceivedDate, IsMigrationIssued, MigrationIssueDate, CertificateNo, DuplicateIssueDate
                FROM [BPUTLIVE].[dbo].[Migration_Records]
                ORDER BY Id";

            using var connection = new SqlConnection(_connectionString);
            var results = (await connection.QueryAsync<MigrationRecordVM>(sql)).ToList();

            for (int i = 0; i < results.Count; i++)
                results[i].SNo = i + 1;

            return results;
        }

        public async Task<MigrationRecordVM?> GetRecordByRegNoAsync(string regNo)
        {
            const string sql = @"
                SELECT Id, DataSource, RegNo, StudentName, CollegeCode, CollegeName,
                       MigrationReceivedDate, IsMigrationIssued, MigrationIssueDate, CertificateNo, DuplicateIssueDate
                FROM [BPUTLIVE].[dbo].[Migration_Records]
                WHERE RegNo = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<MigrationRecordVM>(sql, new { RegNo = regNo });
        }

        public async Task IssueMigrationAsync(string regNo, DateTime issueDate, string certificateNo)
        {
            const string sql = @"
                UPDATE [BPUTLIVE].[dbo].[Migration_Records]
                SET IsMigrationIssued = 1,
                    MigrationIssueDate = @IssueDate,
                    CertificateNo = @CertificateNo
                WHERE RegNo = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { RegNo = regNo, IssueDate = issueDate, CertificateNo = certificateNo });
        }

        public async Task<string> GetNextCertificateNoAsync()
        {
            const string sql = "SELECT NEXT VALUE FOR [dbo].[MigrationCertSeq]";

            using var connection = new SqlConnection(_connectionString);
            int nextValue = await connection.ExecuteScalarAsync<int>(sql);

            return nextValue.ToString();
        }

        public async Task<List<CollegeOptionVM>> GetDistinctCollegesAsync()
        {
            const string sql = @"
                SELECT DISTINCT CollegeCode, CollegeName
                FROM [BPUTLIVE].[dbo].[Migration_Records]
                WHERE IsMigrationIssued = 1
                ORDER BY CollegeCode";

            using var connection = new SqlConnection(_connectionString);
            var results = await connection.QueryAsync<CollegeOptionVM>(sql);
            return results.ToList();
        }

        public async Task<List<MigrationRecordVM>> GetReportAsync(DateTime fromDate, DateTime toDate, string collegeCode)
        {
            string sql = @"
                SELECT Id, DataSource, RegNo, StudentName, CollegeCode, CollegeName,
                       MigrationReceivedDate, IsMigrationIssued, MigrationIssueDate, CertificateNo, DuplicateIssueDate
                FROM [BPUTLIVE].[dbo].[Migration_Records]
                WHERE IsMigrationIssued = 1
                  AND MigrationIssueDate BETWEEN @FromDate AND @ToDate";

            if (collegeCode != "ALL")
                sql += " AND CollegeCode = @CollegeCode";

            sql += " ORDER BY CollegeCode, RegNo";

            using var connection = new SqlConnection(_connectionString);
            var results = (await connection.QueryAsync<MigrationRecordVM>(sql,
                new { FromDate = fromDate, ToDate = toDate, CollegeCode = collegeCode })).ToList();

            for (int i = 0; i < results.Count; i++)
                results[i].SNo = i + 1;

            return results;
        }

        public async Task SaveDuplicateIssueDateAsync(string regNo, DateTime duplicateDate)
        {
            const string sql = @"
                UPDATE [BPUTLIVE].[dbo].[Migration_Records]
                SET DuplicateIssueDate = @DuplicateDate
                WHERE RegNo = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { RegNo = regNo, DuplicateDate = duplicateDate });
        }
    }
}
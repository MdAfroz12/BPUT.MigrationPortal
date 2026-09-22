// Data/MigrationRepository.cs
using Dapper;
using Microsoft.Data.SqlClient;
using BPUT.MigrationPortal.Models;

namespace BPUT.MigrationPortal.Data
{
    public class MigrationRepository : IMigrationRepository
    {
        private readonly string _connectionString;

        public MigrationRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("BPUTLive");
        }

        public async Task<MigrationCertificateVM?> GetStudentByRegNoAsync(string regNo)
        {
            const string sql = @"
                SELECT
                    S.FRM_ROLLNO           AS RollNo,
                    S.FRM_NAME             AS StudentName,
                    C.Inst_Title           AS CollegeName,
                    S.IsMigrationIssued    AS IsMigrationIssued,
                    S.MigrationIssueDate   AS MigrationIssueDate,
                    S.CertificateNo        AS CertificateNo
                FROM [BPUTLIVE].[dbo].[Student_Info] S
                INNER JOIN [BPUTLIVE].[dbo].[College] C
                    ON S.FRM_INSTID = C.Inst_ID
                WHERE S.FRM_ROLLNO = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<MigrationCertificateVM>(
                sql, new { RegNo = regNo });
        }

        public async Task SaveIssueDateAsync(string regNo, DateTime issueDate)
        {
            const string sql = @"
                UPDATE [BPUTLIVE].[dbo].[Student_Info]
                SET MigrationIssueDate = @IssueDate
                WHERE FRM_ROLLNO = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { RegNo = regNo, IssueDate = issueDate });
        }

        // TEMPORARY logic — client se format final hone ke baad yahi update hoga
        public async Task<string> GenerateAndSaveCertificateNoAsync(string regNo)
        {
            using var connection = new SqlConnection(_connectionString);

            const string seqSql = "SELECT NEXT VALUE FOR [BPUTLIVE].[dbo].[MigrationCertSeq]";
            int nextValue = await connection.ExecuteScalarAsync<int>(seqSql);
            string newCertNo = nextValue.ToString();

            const string sql = @"
        UPDATE [BPUTLIVE].[dbo].[Student_Info]
        SET CertificateNo = @CertNo
        WHERE FRM_ROLLNO = @RegNo";

            await connection.ExecuteAsync(sql, new { RegNo = regNo, CertNo = newCertNo });

            return newCertNo;
        }

        public async Task ConfirmMigrationIssuedAsync(string regNo)
        {
            const string sql = @"
                UPDATE [BPUTLIVE].[dbo].[Student_Info]
                SET IsMigrationIssued = 1
                WHERE FRM_ROLLNO = @RegNo";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(sql, new { RegNo = regNo });
        }
    }
}
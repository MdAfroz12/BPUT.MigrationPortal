namespace BPUT.MigrationPortal.Models
{
    public class MigrationRecordVM
    {
        public int Id { get; set; }
        public int SNo { get; set; }
        public string DataSource { get; set; } = "";
        public string RegNo { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string CollegeCode { get; set; } = "";
        public string CollegeName { get; set; } = "";

        public DateTime? MigrationReceivedDate { get; set; }
        public bool IsMigrationIssued { get; set; }
        public DateTime? MigrationIssueDate { get; set; }
        public string CertificateNo { get; set; } = "";
        public DateTime? DuplicateIssueDate { get; set; }
    }
}
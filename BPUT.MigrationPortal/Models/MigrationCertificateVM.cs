// Models/MigrationCertificateVM.cs
namespace BPUT.MigrationPortal.Models
{
    public class MigrationCertificateVM
    {
        public string RegNo { get; set; }
        public string RollNo { get; set; }
        public string StudentName { get; set; }
        public string CollegeName { get; set; }

        public bool IsMigrationIssued { get; set; }
        public DateTime? MigrationIssueDate { get; set; }
        public string CertificateNo { get; set; }
    }
}
// Controllers/MigrationController.cs
using Microsoft.AspNetCore.Mvc;
using BPUT.MigrationPortal.Data;
using BPUT.MigrationPortal.Models;

namespace BPUT.MigrationPortal.Controllers
{
    public class MigrationController : Controller
    {
        private readonly IMigrationRepository _repo;
        private readonly IMigrationDataRepository _dataRepo;

        public MigrationController(IMigrationRepository repo, IMigrationDataRepository dataRepo)
        {
            _repo = repo;
            _dataRepo = dataRepo;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new MigrationCertificateVM());
        }

        [HttpPost]
        public async Task<IActionResult> Search(string regNo)
        {
            if (string.IsNullOrWhiteSpace(regNo))
            {
                ModelState.AddModelError("", "Registration No required hai.");
                return View("Index", new MigrationCertificateVM());
            }

            var student = await _repo.GetStudentByRegNoAsync(regNo);

            if (student == null)
            {
                ModelState.AddModelError("", "Is RegNo se koi student nahi mila.");
                return View("Index", new MigrationCertificateVM { RegNo = regNo });
            }

            student.RegNo = regNo;

            if (student.IsMigrationIssued)
                return View("Certificate", student);

            return View("Report", student);
        }

        [HttpPost]
        public async Task<IActionResult> IssueMigration(string regNo, bool migrationIssue, DateTime? issueDate)
        {
            if (!migrationIssue || issueDate == null)
            {
                ModelState.AddModelError("", "Checkbox tick karo aur Date select karo.");
                var studentAgain = await _repo.GetStudentByRegNoAsync(regNo);
                if (studentAgain != null) studentAgain.RegNo = regNo;
                return View("Report", studentAgain);
            }

            await _repo.SaveIssueDateAsync(regNo, issueDate.Value);
            await _repo.GenerateAndSaveCertificateNoAsync(regNo);

            var updatedStudent = await _repo.GetStudentByRegNoAsync(regNo);
            updatedStudent.RegNo = regNo;

            return View("Certificate", updatedStudent);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPrint([FromBody] ConfirmPrintRequest req)
        {
            await _repo.ConfirmMigrationIssuedAsync(req.RegNo);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> PrintOnHardcopy(string regNo)
        {
            var student = await _repo.GetStudentByRegNoAsync(regNo);
            if (student == null) return NotFound();

            student.RegNo = regNo;
            return View("CertificatePrint", student);
        }

        [HttpGet]
        public IActionResult DataEntry()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Fetch(string dataSource, string regNo)
        {
            var record = dataSource == "Degree"
                ? await _dataRepo.GetFromDegreeAsync(regNo)
                : await _dataRepo.GetFromAdmissionAsync(regNo);

            var allRecords = await _dataRepo.GetAllRecordsAsync();

            if (record == null)
            {
                ViewBag.Error = "Is RegNo se koi record nahi mila.";
                return View("DataEntry", allRecords);
            }

            record.DataSource = dataSource;
            ViewBag.FetchedRecord = record;
            return View("DataEntry", allRecords);
        }

        [HttpGet]
        public async Task<IActionResult> FetchJson(string dataSource, string regNo)
        {
            var record = dataSource == "Degree"
                ? await _dataRepo.GetFromDegreeAsync(regNo)
                : await _dataRepo.GetFromAdmissionAsync(regNo);

            if (record == null)
                return Json(new { success = false });

            return Json(new
            {
                success = true,
                studentName = record.StudentName,
                collegeCode = record.CollegeCode,
                collegeName = record.CollegeName
            });
        }

        // === UPDATED — ab duplicateDate bhi return karta hai ===
        [HttpGet]
        public async Task<IActionResult> CheckStatus(string dataSource, string regNo)
        {
            var existing = await _dataRepo.GetRecordByRegNoAsync(regNo);

            if (existing != null && existing.IsMigrationIssued)
            {
                return Json(new
                {
                    exists = true,
                    issued = true,
                    studentName = existing.StudentName,
                    collegeCode = existing.CollegeCode,
                    collegeName = existing.CollegeName,
                    certificateNo = existing.CertificateNo,
                    issueDate = existing.MigrationIssueDate?.ToString("dd-MM-yyyy"),
                    duplicateDate = existing.DuplicateIssueDate?.ToString("dd-MM-yyyy")
                });
            }

            var fetched = dataSource == "Degree"
                ? await _dataRepo.GetFromDegreeAsync(regNo)
                : await _dataRepo.GetFromAdmissionAsync(regNo);

            if (fetched == null)
                return Json(new { exists = false, found = false });

            return Json(new
            {
                exists = false,
                found = true,
                issued = false,
                studentName = fetched.StudentName,
                collegeCode = fetched.CollegeCode,
                collegeName = fetched.CollegeName
            });
        }

        [HttpGet]
        public async Task<IActionResult> GoToPrint(string dataSource, string regNo, string studentName, string collegeCode, string collegeName)
        {
            var existing = await _dataRepo.GetRecordByRegNoAsync(regNo);

            if (existing == null)
            {
                var record = new MigrationRecordVM
                {
                    DataSource = dataSource,
                    RegNo = regNo,
                    StudentName = studentName,
                    CollegeCode = collegeCode,
                    CollegeName = collegeName
                };

                await _dataRepo.AddRecordAsync(record, DateTime.Now);
            }

            return RedirectToAction("IssueForm", new { regNo });
        }

        // === NAYA — duplicate certificate ke liye ===
        [HttpGet]
        public async Task<IActionResult> IssueDuplicate(string regNo, DateTime duplicateDate)
        {
            await _dataRepo.SaveDuplicateIssueDateAsync(regNo, duplicateDate);
            return RedirectToAction("PrintRecord", new { regNo });
        }

        // Bulk Download
        [HttpPost]
        public async Task<IActionResult> BulkPrint(List<string> regNos, List<string> dataSources, DateTime issueDate)
        {
            var printList = new List<MigrationRecordVM>();

            for (int i = 0; i < regNos.Count; i++)
            {
                var regNo = regNos[i];
                if (string.IsNullOrWhiteSpace(regNo)) continue;

                var existing = await _dataRepo.GetRecordByRegNoAsync(regNo);

                if (existing != null && existing.IsMigrationIssued)
                {
                    printList.Add(existing);
                    continue;
                }

                if (existing == null)
                {
                    var dataSource = i < dataSources.Count ? dataSources[i] : "Admission";
                    var fetched = dataSource == "Degree"
                        ? await _dataRepo.GetFromDegreeAsync(regNo)
                        : await _dataRepo.GetFromAdmissionAsync(regNo);

                    if (fetched == null) continue;

                    fetched.DataSource = dataSource;
                    await _dataRepo.AddRecordAsync(fetched, DateTime.Now);
                }

                string certNo = await _dataRepo.GetNextCertificateNoAsync();
                await _dataRepo.IssueMigrationAsync(regNo, issueDate, certNo);

                var updated = await _dataRepo.GetRecordByRegNoAsync(regNo);
                if (updated != null) printList.Add(updated);
            }

            return View("BulkPrint", printList);
        }

        // Report 
        [HttpGet]
        public async Task<IActionResult> MigrationReport()
        {
            ViewBag.Colleges = await _dataRepo.GetDistinctCollegesAsync();
            return View(new List<MigrationRecordVM>());
        }

        [HttpPost]
        public async Task<IActionResult> MigrationReport(DateTime fromDate, DateTime toDate, string collegeCode)
        {
            ViewBag.Colleges = await _dataRepo.GetDistinctCollegesAsync();
            ViewBag.FromDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.ToString("yyyy-MM-dd");
            ViewBag.SelectedCollege = collegeCode;

            var results = await _dataRepo.GetReportAsync(fromDate, toDate, collegeCode);
            return View(results);
        }

        [HttpPost]
        public async Task<IActionResult> AddRecord(string dataSource, string regNo, string studentName,
            string collegeCode, string collegeName, DateTime receivedDate)
        {
            var record = new MigrationRecordVM
            {
                DataSource = dataSource,
                RegNo = regNo,
                StudentName = studentName,
                CollegeCode = collegeCode,
                CollegeName = collegeName
            };

            await _dataRepo.AddRecordAsync(record, receivedDate);
            return RedirectToAction("DataEntry");
        }

        [HttpPost]
        public async Task<IActionResult> SaveRecord(string dataSource, string regNo, string studentName,
            string collegeCode, string collegeName, DateTime receivedDate, bool migrationIssue, DateTime? issueDate)
        {
            var record = new MigrationRecordVM
            {
                DataSource = dataSource,
                RegNo = regNo,
                StudentName = studentName,
                CollegeCode = collegeCode,
                CollegeName = collegeName
            };

            await _dataRepo.AddRecordAsync(record, receivedDate);

            if (migrationIssue && issueDate != null)
            {
                string certNo = await _dataRepo.GetNextCertificateNoAsync();
                await _dataRepo.IssueMigrationAsync(regNo, issueDate.Value, certNo);
            }

            return RedirectToAction("DataEntry");
        }

        [HttpPost]
        public async Task<IActionResult> SaveMultipleRecords(List<MigrationRecordVM> records)
        {
            foreach (var r in records)
            {
                if (string.IsNullOrWhiteSpace(r.RegNo)) continue;

                await _dataRepo.AddRecordAsync(r, r.MigrationReceivedDate ?? DateTime.Now);

                if (r.IsMigrationIssued && r.MigrationIssueDate != null)
                {
                    string certNo = await _dataRepo.GetNextCertificateNoAsync();
                    await _dataRepo.IssueMigrationAsync(r.RegNo, r.MigrationIssueDate.Value, certNo);
                }
            }

            return RedirectToAction("DataEntry");
        }

        [HttpGet]
        public async Task<IActionResult> IssueForm(string regNo)
        {
            var record = await _dataRepo.GetRecordByRegNoAsync(regNo);
            if (record == null) return NotFound();

            if (record.IsMigrationIssued)
                return RedirectToAction("PrintRecord", new { regNo });

            return View(record);
        }

        [HttpPost]
        public async Task<IActionResult> Issue(string regNo, DateTime issueDate)
        {
            string certNo = await _dataRepo.GetNextCertificateNoAsync();
            await _dataRepo.IssueMigrationAsync(regNo, issueDate, certNo);

            return RedirectToAction("PrintRecord", new { regNo });
        }

        [HttpGet]
        public async Task<IActionResult> PrintRecord(string regNo)
        {
            var record = await _dataRepo.GetRecordByRegNoAsync(regNo);
            if (record == null) return NotFound();

            return View(record);
        }
    }

    public class ConfirmPrintRequest
    {
        public string RegNo { get; set; } 
    }
}
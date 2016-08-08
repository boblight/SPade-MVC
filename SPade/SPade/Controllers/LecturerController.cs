﻿using Ionic.Zip;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using SPade.Grading;
using SPade.Models;
using SPade.Models.DAL;
using SPade.ViewModels.Lecturer;
using SPade.ViewModels.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security.AntiXss;

namespace SPade.Controllers
{

    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        //init the db
        private SPadeDBEntities db = new SPadeDBEntities();
        private ApplicationUserManager _userManager;

        public LecturerController()
        {

        }

        public LecturerController(ApplicationUserManager userManager)
        {
            UserManager = userManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        // GET: Lecturer
        public ActionResult Dashboard()
        {
            return View();
        }

        //Manage Students (View all + Class + Update)
        public ActionResult ManageClassesAndStudents()
        {
            List<ManageClassesViewModel> manageClassView = new List<ManageClassesViewModel>();
           

            string loggedInLecturer = User.Identity.GetUserName();

            //get the classes managed by the lecturer 


            List<Class> managedClasses = new List<Class>();

            List<Lec_Class> lec_classes = db.Lec_Class.Where(lc => lc.StaffID == loggedInLecturer).ToList();

            foreach (Lec_Class lc in lec_classes)
            {
                List<Class> temp = db.Classes.Where(c => c.DeletedAt == null).Where(c => c.ClassID == lc.ClassID).ToList();
                managedClasses.AddRange(temp);
            }




            //get the students in that classs
            foreach (Class c in managedClasses)
            {

                ManageClassesViewModel e = new ManageClassesViewModel();
                //match the class ID of student wit hthe class ID of the managed Classes
                var count = db.Students.Where(s => s.ClassID == c.ClassID).Count();

                Course cs = db.Courses.Where(cx => cx.CourseID == c.CourseID).First();

                e.ClassName = cs.CourseAbbr + "/" + c.ClassName;
                e.Id = c.ClassID;
                e.NumberOfStudents = count;

                manageClassView.Add(e);

            }

            return View(manageClassView);

        }

        public ActionResult ViewStudentsByClass(string classID)
        {
            int cID = Int32.Parse(classID);

            List<ViewStudentsByClassViewModel> studList = new List<ViewStudentsByClassViewModel>();
            List<Student> sList = new List<Student>();

            sList = db.Students.Where(s => s.ClassID == cID && s.DeletedAt == null).ToList();

            Class c = db.Classes.Where(cx => cx.ClassID == cID).FirstOrDefault();

            int courseId = c.CourseID;
            string courseAbbr = db.Courses.ToList().Find(cx => cx.CourseID == courseId).CourseAbbr;

            foreach (Student s in sList)
            {
                ViewStudentsByClassViewModel vm = new ViewStudentsByClassViewModel();

                vm.AdminNo = s.AdminNo.ToUpper();
                vm.Name = s.Name;
                vm.Email = s.Email;
                vm.ContactNo = s.ContactNo;

                studList.Add(vm);
            }

            ViewBag.ClassName = courseAbbr + "/" + c.ClassName;
            return View(studList);
        }

        public ActionResult UpdateStudent()
        {
            return View();
        }

        //Add Students (Bulk + Single)
        public FileResult DownloadBulkAddStudentFile()
        {
            string f = Server.MapPath(@"~/BulkUploadFiles/BulkAddStudent.csv");
            byte[] fileBytes = System.IO.File.ReadAllBytes(f);
            string fileName = "BulkAddStudent.csv";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult BulkAddStudent()
        {
            return View();
        }

        [HttpPost]
        public ActionResult BulkAddStudent(HttpPostedFileBase file)
        {
            if ((file != null && Path.GetExtension(file.FileName) == ".csv") && (file.ContentLength > 0))
            {
                //Upload and save the file
                // extract only the filename
                var fileName = Path.GetFileName(file.FileName);
                // store the file inside ~/App_Data/uploads folder
                var path = Path.Combine(Server.MapPath("~/App_Data/Uploads"), fileName);
                file.SaveAs(path);

                string[] lines = System.IO.File.ReadAllLines(path);
                List<Student> slist = new List<Student>();
                for (int i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        Student s = new Student();
                        s.ClassID = Int32.Parse(lines[i].Split(',')[0]);
                        s.AdminNo = lines[i].Split(',')[1];
                        s.Name = lines[i].Split(',')[2];
                        s.Email = lines[i].Split(',')[3];
                        s.ContactNo = Int32.Parse(lines[i].Split(',')[4]);
                        s.CreatedAt = DateTime.Now;
                        s.CreatedBy = User.Identity.GetUserName();
                        s.UpdatedAt = DateTime.Now;
                        s.UpdatedBy = User.Identity.GetUserName();

                        slist.Add(s);
                    }
                }
                db.Students.AddRange(slist);
                db.SaveChanges();
            }
            else
            {
                // Upload file is invalid
                string err = "Uploaded file is invalid ! Please try again!";
                TempData["InputWarning"] = err;
                return View();
            }

            return RedirectToAction("ManageClassesAndStudents");
        }

        public ActionResult AddOneStudent()
        {
            AddStudentViewModel model = new AddStudentViewModel();
            List<Class> allClasses = db.Classes.ToList();
            model.Classes = allClasses;
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> AddOneStudent(AddStudentViewModel model, FormCollection formCollection)
        {
            var user = new ApplicationUser { UserName = model.AdminNo, Email = model.Email };
            user.EmailConfirmed = true;
            var result = await UserManager.CreateAsync(user, "P@ssw0rd"); //default password
            if (result.Succeeded)
            {
                var student = new Student()
                {
                    AdminNo = model.AdminNo.Trim(),
                    Name = model.Name,
                    Email = model.Email,
                    ContactNo = model.ContactNo,
                    ClassID = Int32.Parse(formCollection["ClassID"].ToString()),
                    CreatedBy = User.Identity.Name,
                    UpdatedBy = User.Identity.Name,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                };

                db.Students.Add(student);
                db.SaveChanges();
            }
            else
            {
                //error in registering account
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                return View(model);
            }
            return RedirectToAction("Dashboard");
        }

        //Manage + Update Assignment
        public ActionResult ManageAssignments()
        {
            List<ManageAssignmentViewModel> manageAssgn = new List<ManageAssignmentViewModel>();
            List<Assignment> lecAssgn = new List<Assignment>();
            List<Class> classAssgn = new List<Class>();
            List<Course> courseList = new List<Course>();

            var lecturerID = User.Identity.GetUserName();

            //get the assignments that this lecturer created
            lecAssgn = db.Assignments.ToList().FindAll(a => a.CreateBy == lecturerID && a.DeletedBy == null);
            courseList = db.Courses.ToList();

            //get the details of each assignment and pass to view
            foreach (Assignment a in lecAssgn)
            {
                ManageAssignmentViewModel mmvm = new ManageAssignmentViewModel();

                //get all the classes that are assigned the particular assignment under this lecturer + the classes they manage only ! 
                var query = from c in db.Classes
                            join ca in db.Class_Assgn on c.ClassID equals ca.ClassID
                            where ca.AssignmentID.Equals(a.AssignmentID)
                            join cl in db.Lec_Class on c.ClassID equals cl.ClassID
                            where cl.StaffID.Equals(lecturerID)
                            select c;

                classAssgn = query.ToList();

                string joinedClasses = "";

                //if theres only 1 class assigned that assignment 
                if (classAssgn.Count() == 1)
                {
                    foreach (Course c in courseList)
                    {
                        if (c.CourseID == classAssgn.FirstOrDefault().CourseID)
                        {
                            joinedClasses = c.CourseAbbr + "/" + classAssgn.FirstOrDefault().ClassName;
                        }
                    }
                }

                //if there are more then 1 class assigned that assignment
                if (classAssgn.Count() > 1)
                {
                    //get the last item 
                    var lastItem = classAssgn.LastOrDefault().ClassName;
                    var listSize = classAssgn.Count();
                    int listIndex = 0;

                    foreach (Class c in classAssgn)
                    {
                        listIndex++;

                        if (listIndex != listSize) //not yet the last time
                        {
                            foreach (Course cr in courseList)
                            {
                                if (cr.CourseID == c.CourseID)
                                {
                                    joinedClasses += cr.CourseAbbr + "/" + c.ClassName + ",";
                                }
                            }
                        }

                        if (listIndex == listSize) //reach the last time 
                        {
                            foreach (Course cr in courseList)
                            {
                                if (cr.CourseID == c.CourseID)
                                {
                                    joinedClasses += cr.CourseAbbr + "/" + c.ClassName;
                                }
                            }
                        }
                    }
                }//end of class loop 

                mmvm.Assignment = a;
                mmvm.Classes = joinedClasses;
                manageAssgn.Add(mmvm);
            }

            return View(manageAssgn);
        }

        public ActionResult UpdateAssignment(string assignmentId)
        {
            var i = Int32.Parse(assignmentId);
            var x = User.Identity.GetUserName();

            UpdateAssignmentViewModel model = new UpdateAssignmentViewModel();
            Assignment assgn = new Assignment();
            List<Module> modList = new List<Module>();

            List<Class> classList = new List<Class>();
            List<Class_Assgn> classAssgn = new List<Class_Assgn>();
            List<AssignmentClass> assgnClassList = new List<AssignmentClass>();
            List<Course> courseList = new List<Course>();

            //get the assignment details from the DB
            assgn = db.Assignments.Where(a => a.AssignmentID == i).FirstOrDefault();

            //get all courses
            courseList = db.Courses.ToList();

            //get the classes that this lecturer manages 
            var query = from c in db.Classes join lc in db.Lec_Class on c.ClassID equals lc.ClassID where lc.StaffID.Equals(x) select c;
            classList = query.ToList();

            //now we get the classses that are assigned this assignment 
            classAssgn = db.Class_Assgn.Where(ca => ca.AssignmentID == i).ToList();

            //loop through the classes, check if they are assigned, then give to assignClassList to populate the checkboxes
            foreach (Class cl in classList)
            {
                AssignmentClass ac = new AssignmentClass();

                ac.ClassId = cl.ClassID;

                //string together course abb + class name
                foreach (Course cr in courseList)
                {
                    if (cr.CourseID == cl.CourseID)
                    {
                        ac.ClassName = cr.CourseAbbr + "/" + cl.ClassName;
                    }
                }

                //check which class has been assigned the assignment
                foreach (Class_Assgn ca in classAssgn)
                {
                    if (ca.AssignmentID == i && ca.ClassID == ac.ClassId)
                    {
                        ac.isSelected = true;
                    }
                }

                //add to list
                assgnClassList.Add(ac);
            }

            //get all the modules
            modList = db.Modules.ToList();

            //set the data for the assignment 
            model.AssgnTitle = assgn.AssgnTitle;
            model.ModuleId = assgn.ModuleCode;

            //get the id of the assignment module to set the dropdown
            foreach (Module m in modList)
            {
                if (m.ModuleCode == model.ModuleId)
                {
                    model.SelectedModuleId = m.ModuleCode;
                }
            }

            model.AssignmentId = assgn.AssignmentID;
            model.Describe = assgn.Describe;
            model.StartDate = assgn.StartDate;
            model.DueDate = assgn.DueDate;
            model.MaxAttempt = assgn.MaxAttempt;
            model.Modules = modList;
            model.ClassList = assgnClassList;
            model.UpdateSolution = false;
            model.IsTestCasePresent = true;

            return View(model);
        }

        [HttpPost]
        public ActionResult UpdateAssignment(UpdateAssignmentViewModel uAVM, HttpPostedFileBase solutionsFileUpload, HttpPostedFileBase testCaseUpload)
        {
            //user doesnt wants to update solution
            if (uAVM.UpdateSolution == false)
            {
                if (UpdateAssignmentToDB(uAVM, false, false) == true)
                {
                    //failed to update assignment 
                    TempData["GeneralError"] = "Failed to update assignment to database. Please try again !";
                    return View(uAVM);
                }
            }

            //user wants to update solution 
            if (uAVM.UpdateSolution == true)
            {
                //run solution with testcase
                if (uAVM.IsTestCasePresent == true)
                {
                    if ((solutionsFileUpload != null && Path.GetExtension(solutionsFileUpload.FileName) == ".zip") && (testCaseUpload != null && Path.GetExtension(testCaseUpload.FileName) == ".xml"))
                    {
                        if (solutionsFileUpload.ContentLength > 0 && testCaseUpload.ContentLength > 0)
                        {
                            if (solutionsFileUpload.ContentLength < 104857600)
                            {
                                string slnFilePath = "";
                                string assignmentTitle = (uAVM.AssgnTitle).Replace(" ", ""); //used to name the testcase/solution temporarily until get assignmentID 
                                string fileName = ""; //name of the solution uploaded

                                //save the solution
                                fileName = Path.GetFileNameWithoutExtension(solutionsFileUpload.FileName);
                                var zipLocation = Server.MapPath(@"~/TempSubmissions/" + solutionsFileUpload);
                                solutionsFileUpload.SaveAs(zipLocation);
                                slnFilePath = Server.MapPath(@"~/TempSubmissions/" + fileName);
                                DirectoryInfo fileDirectory = new DirectoryInfo(slnFilePath);

                                if (fileDirectory.Exists)
                                {
                                    foreach (FileInfo files in fileDirectory.GetFiles())
                                    {
                                        files.Delete();
                                    }
                                    foreach (DirectoryInfo dir in fileDirectory.GetDirectories())
                                    {
                                        dir.Delete(true);
                                    }
                                }
                                fileDirectory.Create();
                                System.IO.Compression.ZipFile.ExtractToDirectory(zipLocation, slnFilePath);

                                //access the solution + move the classname.java/.cs into a folder 
                                var toLowerPath = fileName.ToLower();
                                var path = System.IO.Path.Combine(slnFilePath, toLowerPath);
                                Directory.CreateDirectory(path);

                                //get the language and pass into grader
                                ProgLanguage lang = db.ProgLanguages.ToList().Find(l => l.LanguageId == db.Modules.ToList().Find(m => m.ModuleCode == uAVM.ModuleId).LanguageId);

                                var ogPath = "";
                                var newPath = "";

                                if (lang.LangageType.Equals("Java"))
                                {
                                    ogPath = slnFilePath + "/" + fileName + ".java";
                                    newPath = path + "/" + fileName + ".java";
                                }
                                else if (lang.LangageType.Equals("C#"))
                                {
                                    ogPath = slnFilePath + "/" + fileName + ".cs";
                                    newPath = path + "/" + fileName + ".cs";
                                }

                                System.IO.File.Move(ogPath, newPath);

                                //save the testcase
                                var filePath = Server.MapPath(@"~/TestCase/" + assignmentTitle + ".xml");
                                var fileInfo = new FileInfo(filePath);
                                fileInfo.Directory.Create();
                                testCaseUpload.SaveAs(filePath);

                                //get the language and pass into grader
                                Grader g = new Grader(slnFilePath, fileName, assignmentTitle, lang.LangageType, true);

                                //exit codes returned from grader 
                                //1 is successfully done everything
                                //2 is test case submitted could not be read
                                //3 is program has failed to run
                                //4 is program was caught in an infinite loop
                                int exitCode = g.RunLecturerSolution();

                                if (exitCode == 1)
                                {
                                    //update DB + rename solution/testcase
                                    if (UpdateAssignmentToDB(uAVM, true, true) == true)
                                    {
                                        //failed to update DB
                                        DeleteFile(fileName, assignmentTitle, true);
                                        uAVM.Modules = db.Modules.ToList();
                                        TempData["GeneralError"] = "Failed to save assignment to database. Please try again.";
                                        return View(uAVM);
                                    }

                                    //delete the uploaded sln but not test case
                                    DeleteFile(fileName, assignmentTitle, false);

                                }//end of run succesfully method 

                                else if (exitCode == 2)
                                {
                                    DeleteFile(fileName, assignmentTitle, true);
                                    uAVM.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "The test case submitted could not be read properly. Please check your test case file";
                                    return View(uAVM);
                                }

                                else if (exitCode == 3)
                                {
                                    //solution failed to run 
                                    DeleteFile(fileName, assignmentTitle, true);
                                    uAVM.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "The program has failed to run entirely. Please check your program";
                                    return View(uAVM);
                                }
                                else if (exitCode == 4)
                                {
                                    //solution stuck in infinite loop
                                    DeleteFile(fileName, assignmentTitle, true);
                                    uAVM.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "The program uploaded was caught in an infinite loop. Please check your program";
                                    return View(uAVM);
                                }
                            }
                            else
                            {
                                //more than 150MB                     
                                uAVM.Modules = db.Modules.ToList();
                                TempData["SlnWarning"] = "Please make sure that your files is less than 150MB !";
                                return View(uAVM);
                            }
                        }
                        else
                        {
                            //empty file 
                            uAVM.Modules = db.Modules.ToList();
                            string err = "Uploaded file is invalid ! Please try again.";
                            TempData["SlnWarning"] = err;
                            TempData["TcWarning"] = err;
                            return View(uAVM);
                        }
                    }
                    else
                    {
                        //uploaded file is invalid
                        uAVM.Modules = db.Modules.ToList();
                        string err = "Uploaded file is invalid ! Please try again.";
                        TempData["SlnWarning"] = err;
                        TempData["TcWarning"] = err;
                        return View(uAVM);
                    }
                }//end of run with testcase

                //run solution without testcase 
                if (uAVM.IsTestCasePresent == false)
                {
                    if (solutionsFileUpload != null && Path.GetExtension(solutionsFileUpload.FileName) == ".zip")
                    {
                        if (solutionsFileUpload.ContentLength > 0)
                        {
                            if (solutionsFileUpload.ContentLength < 104857600)
                            {
                                string slnFilePath = "";
                                string assignmentTitle = (uAVM.AssgnTitle).Replace(" ", "");
                                string fileName = "";

                                //save the solution
                                fileName = Path.GetFileNameWithoutExtension(solutionsFileUpload.FileName);
                                var zipLocation = Server.MapPath(@"~/TempSubmissions/" + solutionsFileUpload);
                                solutionsFileUpload.SaveAs(zipLocation);
                                slnFilePath = Server.MapPath(@"~/TempSubmissions/" + fileName);
                                DirectoryInfo fileDirectory = new DirectoryInfo(slnFilePath);
                                if (fileDirectory.Exists)
                                {
                                    foreach (FileInfo files in fileDirectory.GetFiles())
                                    {
                                        files.Delete();
                                    }
                                    foreach (DirectoryInfo dir in fileDirectory.GetDirectories())
                                    {
                                        dir.Delete(true);
                                    }
                                }
                                fileDirectory.Create();
                                System.IO.Compression.ZipFile.ExtractToDirectory(zipLocation, slnFilePath);

                                //access the solution + move the classname.java/.cs into a folder 
                                var toLowerPath = fileName.ToLower();
                                var path = System.IO.Path.Combine(slnFilePath, toLowerPath);
                                Directory.CreateDirectory(path);

                                //get the language and pass into grader
                                ProgLanguage lang = db.ProgLanguages.ToList().Find(l => l.LanguageId == db.Modules.ToList().Find(m => m.ModuleCode == uAVM.ModuleId).LanguageId);

                                //get the appropirate file and move the file accordingly
                                var ogPath = "";
                                var newPath = "";

                                if (lang.LangageType.Equals("Java"))
                                {
                                    ogPath = slnFilePath + "/" + fileName + ".java";
                                    newPath = path + "/" + fileName + ".java";
                                }
                                else if (lang.LangageType.Equals("C#"))
                                {
                                    ogPath = slnFilePath + "/" + fileName + ".cs";
                                    newPath = path + "/" + fileName + ".cs";
                                }

                                System.IO.File.Move(ogPath, newPath);

                                Grader g = new Grader(slnFilePath, fileName, assignmentTitle, lang.LangageType, false);

                                int exitCode = g.RunLecturerSolution();

                                if (exitCode == 1)
                                {
                                    //save to DB + rename solution file
                                    if (UpdateAssignmentToDB(uAVM, true, false) == true)
                                    {
                                        //solution has failed to save to DB
                                        DeleteFile(fileName, assignmentTitle, false);
                                        uAVM.Modules = db.Modules.ToList();
                                        TempData["GeneralError"] = "Failed to save assignmet to database ! Please try again.";
                                        return View(uAVM);
                                    }

                                    //delete the uploaded sln
                                    DeleteFile(fileName, assignmentTitle, false);

                                }
                                else if (exitCode == 3)
                                {
                                    //solution failed to run 
                                    DeleteFile(fileName, assignmentTitle, false);
                                    uAVM.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "The program uploaded was caught in an infinite loop. Please check your program";
                                    return View(uAVM);
                                }

                            }
                            else
                            {
                                //file size is more that 150MB
                                uAVM.Modules = db.Modules.ToList();
                                TempData["SlnWarning"] = "Please make sure that your files is less than 150MB !";
                                return View(uAVM);
                            }
                        }
                        else
                        {
                            //empty file 
                            uAVM.Modules = db.Modules.ToList();
                            string err = "Uploaded file is invalid ! Please try again.";
                            TempData["SlnWarning"] = err;
                            TempData["TcWarning"] = err;
                            return View(uAVM);
                        }
                    }
                    else
                    {
                        //invalid file 
                        uAVM.Modules = db.Modules.ToList();
                        string err = "Uploaded file is invalid ! Please try again.";
                        TempData["SlnWarning"] = err;
                        TempData["TcWarning"] = err;
                        return View(uAVM);
                    }

                }  //end of run without testcase 

            }//end of updateSolution

            //successfully updating assignment to DB
            return RedirectToAction("ManageAssignments", "Lecturer");
        }

        [HttpPost]
        public ActionResult DeleteAssignment()
        {
            return View();
        }

        public bool UpdateAssignmentToDB(UpdateAssignmentViewModel uVM, bool updateSln, bool isTestCase)
        {
            Assignment updatedAssignment = new Assignment();
            bool isFailed = false;
            string assignmentTitle = (uVM.AssgnTitle).Replace(" ", "");

            //get the previous asssignment from db
            updatedAssignment = db.Assignments.Where(a => a.AssignmentID == uVM.AssignmentId).FirstOrDefault();

            //update the assignment
            try
            {
                //update the values 
                updatedAssignment.AssgnTitle = uVM.AssgnTitle;
                updatedAssignment.ModuleCode = uVM.ModuleId;
                updatedAssignment.Describe = uVM.Describe;
                updatedAssignment.MaxAttempt = uVM.MaxAttempt;
                updatedAssignment.StartDate = uVM.StartDate;
                updatedAssignment.DueDate = uVM.DueDate;
                updatedAssignment.UpdatedBy = User.Identity.GetUserName();
                updatedAssignment.UpdatedAt = DateTime.Now;

                //users choose to update the assignment solution
                if (updateSln == true)
                {
                    updatedAssignment.Solution = "~/Solutions/" + uVM.AssignmentId + ".xml";
                }

                //delete the old solution that is stored  
                var solutionPath = Server.MapPath(@"~/Solutions/");
                var oldSlnName = uVM.AssignmentId + "solution.xml";

                DirectoryInfo di;
                if ((di = new DirectoryInfo(solutionPath)).Exists)
                {
                    foreach (FileInfo f in di.GetFiles())
                    {
                        //find the original solution 
                        if (f.Name == oldSlnName)
                        {
                            //delete it 
                            f.IsReadOnly = false;
                            f.Delete();
                        }
                    }
                }

                //now that the old solution xml is gone, we reaname the new solution file 
                foreach (FileInfo f in new DirectoryInfo(solutionPath).GetFiles())
                {
                    if (f.Name == assignmentTitle + ".xml")
                    {
                        var sourcePath = solutionPath + f.Name;
                        var destPath = solutionPath + uVM.AssignmentId + "solution.xml";
                        FileInfo info = new FileInfo(sourcePath);
                        info.MoveTo(destPath);
                    }
                }

                //this part only runs IF a testcase is present 
                if (isTestCase == true)
                {
                    var testCasePath = Server.MapPath(@"~/TestCase/");
                    var oldTestCase = uVM.AssignmentId + "TestCase.xml";

                    //delete the old testcase 
                    if ((di = new DirectoryInfo(testCasePath)).Exists)
                    {
                        foreach (FileInfo f in di.GetFiles())
                        {
                            //find the original solution 
                            if (f.Name == oldTestCase)
                            {
                                //delete it 
                                f.IsReadOnly = false;
                                f.Delete();
                            }
                        }
                    }

                    //now that it is deleted, we rename the new testcase uploaded
                    foreach (FileInfo f in new DirectoryInfo(testCasePath).GetFiles())
                    {
                        if (f.Name == assignmentTitle + ".xml")
                        {
                            var sourcePath = testCasePath + f.Name;
                            var destPath = testCasePath + uVM.AssignmentId + "testcase.xml";
                            FileInfo info = new FileInfo(sourcePath);
                            info.MoveTo(destPath);
                        }
                    }
                }

                db.SaveChanges();

            }//end of try 
            catch (Exception ex)
            {
                isFailed = true;
            }

            return isFailed;
        }

        //Add Assignment
        public FileResult DownloadTestCase()
        {
            string f = Server.MapPath(@"~/TestCase/testcase.xml");
            byte[] fileBytes = System.IO.File.ReadAllBytes(f);
            string fileName = "testcase.xml";
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult AddAssignment()
        {
            AddAssignmentViewModel aaVM = new AddAssignmentViewModel();

            List<AssignmentClass> ac = new List<AssignmentClass>();
            List<Class> managedClasses = new List<Class>();

            var x = User.Identity.GetUserName();

            //get the classes managed by the lecturer 
            var query = from c in db.Classes join lc in db.Lec_Class on c.ClassID equals lc.ClassID where lc.StaffID.Equals(x) select c;
            managedClasses = query.ToList();

            //we loop through the managedClasses to fill up the assignmentclass -> which is used to populate checkboxes
            foreach (var c in managedClasses)
            {
                AssignmentClass a = new AssignmentClass();
                a.ClassName = c.ClassName;
                a.ClassId = c.ClassID;
                a.isSelected = false;
                ac.Add(a);
            }

            //get the modules 
            List<Module> allModules = db.Modules.ToList().FindAll(mod => mod.DeletedAt == null);

            aaVM.IsTestCasePresent = true;
            aaVM.ClassList = ac;
            aaVM.Modules = allModules;

            return View(aaVM);
        }

        [HttpPost]
        public ActionResult AddAssignment(AddAssignmentViewModel addAssgn, HttpPostedFileBase solutionsFileUpload, HttpPostedFileBase testCaseUpload)
        {
            //run solution with testcase
            if (addAssgn.IsTestCasePresent == true)
            {
                if ((solutionsFileUpload != null && Path.GetExtension(solutionsFileUpload.FileName) == ".zip") && (testCaseUpload != null && Path.GetExtension(testCaseUpload.FileName) == ".xml"))
                {
                    if (solutionsFileUpload.ContentLength > 0 && testCaseUpload.ContentLength > 0)
                    {
                        if (solutionsFileUpload.ContentLength < 104857600)
                        {
                            string slnFilePath = "";
                            string assignmentTitle = (addAssgn.AssgnTitle).Replace(" ", ""); //used to name the testcase/solution temporarily until get assignmentID 
                            string fileName = ""; //name of the solution uploaded

                            //save the solution
                            fileName = Path.GetFileNameWithoutExtension(solutionsFileUpload.FileName);
                            var zipLocation = Server.MapPath(@"~/TempSubmissions/" + solutionsFileUpload);
                            solutionsFileUpload.SaveAs(zipLocation);
                            slnFilePath = Server.MapPath(@"~/TempSubmissions/" + fileName);
                            DirectoryInfo fileDirectory = new DirectoryInfo(slnFilePath);
                            if (fileDirectory.Exists)
                            {
                                foreach (FileInfo files in fileDirectory.GetFiles())
                                {
                                    files.Delete();
                                }
                                foreach (DirectoryInfo dir in fileDirectory.GetDirectories())
                                {
                                    dir.Delete(true);
                                }
                            }
                            fileDirectory.Create();
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipLocation, slnFilePath);

                            //access the solution + move the classname.java/.cs into a folder 
                            var toLowerPath = fileName.ToLower();
                            var path = System.IO.Path.Combine(slnFilePath, toLowerPath);
                            Directory.CreateDirectory(path);

                            ////get the language and pass into grader
                            ProgLanguage lang = db.ProgLanguages.ToList().Find(l => l.LanguageId == db.Modules.ToList().Find(m => m.ModuleCode == addAssgn.ModuleId).LanguageId);

                            var ogPath = "";
                            var newPath = "";

                            if (lang.LangageType.Equals("Java"))
                            {
                                ogPath = slnFilePath + "/" + fileName + ".java";
                                newPath = path + "/" + fileName + ".java";
                            }
                            else if (lang.LangageType.Equals("C#"))
                            {
                                ogPath = slnFilePath + "/" + fileName + ".cs";
                                newPath = path + "/" + fileName + ".cs";
                            }
                            else
                            {
                                //solution stuck in infinite loop
                                DeleteFile(fileName, assignmentTitle, true);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The program uploaded is unpoorted by the compiler used for this module. Please upload "
                                    + "program coded in the appropriate programming language or ensure you have selected the correct module.";
                                return View(addAssgn);
                            }

                            System.IO.File.Move(ogPath, newPath);

                            //save the testcase
                            var filePath = Server.MapPath(@"~/TestCase/" + assignmentTitle + ".xml");
                            var fileInfo = new FileInfo(filePath);
                            fileInfo.Directory.Create();
                            testCaseUpload.SaveAs(filePath);

                            //get the language and pass into grader
                            Grader g = new Grader(slnFilePath, fileName, assignmentTitle, lang.LangageType, true);

                            //exit codes returned from grader 
                            //1 is successfully done everything
                            //2 is test case submitted could not be read
                            //3 is program has failed to run
                            //4 is program was caught in an infinite loop
                            //5 is Unsupported Language Type
                            int exitCode = g.RunLecturerSolution();

                            if (exitCode == 1)
                            {
                                //save to DB + rename solution/testcase
                                if (AddAssignmentToDB(addAssgn, fileName, true) == true)
                                {
                                    //failed to save to DB
                                    DeleteFile(fileName, assignmentTitle, true);
                                    addAssgn.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "Failed to save assignment to database. Please try again.";
                                    return View(addAssgn);
                                }

                                //delete the uploaded sln
                                DeleteFile(fileName, assignmentTitle, false);

                            }//end of run succesfully method 

                            else if (exitCode == 2)
                            {
                                DeleteFile(fileName, assignmentTitle, true);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The test case submitted could not be read properly. Please check your test case file";
                                return View(addAssgn);
                            }

                            else if (exitCode == 3)
                            {
                                //solution failed to run 
                                DeleteFile(fileName, assignmentTitle, true);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The program has failed to run entirely. Please check your program";
                                return View(addAssgn);
                            }
                            else if (exitCode == 4)
                            {
                                //solution stuck in infinite loop
                                DeleteFile(fileName, assignmentTitle, true);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The program uploaded was caught in an infinite loop. Please check your program";
                                return View(addAssgn);
                            }
                            else if (exitCode == 5)
                            {
                                //solution stuck in infinite loop
                                DeleteFile(fileName, assignmentTitle, true);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The program uploaded is unsupported by the compiler used for this module. Please upload "
                                    + "program coded in the appropriate programming language or ensure you have selected the correct module." +
                                    " Support for that language could also not be added yet.";
                                return View(addAssgn);
                            }
                        }
                        else
                        {
                            //uploaded file is more than 150MB
                            addAssgn.Modules = db.Modules.ToList();
                            TempData["SlnWarning"] = "Please make sure that your files is less than 150MB !";
                            return View(addAssgn);
                        }
                    }
                    else
                    {
                        //uploaded file is empty 
                        addAssgn.Modules = db.Modules.ToList();
                        string err = "Uploaded file is invalid ! Please try again.";
                        TempData["SlnWarning"] = err;
                        TempData["TcWarning"] = err;
                        return View(addAssgn);
                    }
                }
                else
                {
                    //uploaded file is invalid
                    addAssgn.Modules = db.Modules.ToList();
                    string err = "Uploaded file is invalid ! Please try again.";
                    TempData["SlnWarning"] = err;
                    TempData["TcWarning"] = err;
                    return View(addAssgn);
                }
            }//end of run with testcase

            //run without testcase 
            else if (addAssgn.IsTestCasePresent == false)
            {
                if (solutionsFileUpload != null && Path.GetExtension(solutionsFileUpload.FileName) == ".zip")
                {
                    if (solutionsFileUpload.ContentLength > 0)
                    {
                        if (solutionsFileUpload.ContentLength < 104857600)
                        {

                            string slnFilePath = "";
                            string assignmentTitle = (addAssgn.AssgnTitle).Replace(" ", "");
                            string fileName = "";

                            //save the solution
                            fileName = Path.GetFileNameWithoutExtension(solutionsFileUpload.FileName);
                            var zipLocation = Server.MapPath(@"~/TempSubmissions/" + solutionsFileUpload);
                            solutionsFileUpload.SaveAs(zipLocation);
                            slnFilePath = Server.MapPath(@"~/TempSubmissions/" + fileName);
                            DirectoryInfo fileDirectory = new DirectoryInfo(slnFilePath);
                            if (fileDirectory.Exists)
                            {
                                foreach (FileInfo files in fileDirectory.GetFiles())
                                {
                                    files.Delete();
                                }
                                foreach (DirectoryInfo dir in fileDirectory.GetDirectories())
                                {
                                    dir.Delete(true);
                                }
                            }
                            fileDirectory.Create();
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipLocation, slnFilePath);

                            //access the solution + move the classname.java/.cs into a folder 
                            var toLowerPath = fileName.ToLower();
                            var path = System.IO.Path.Combine(slnFilePath, toLowerPath);
                            Directory.CreateDirectory(path);

                            //get the language and pass into grader
                            ProgLanguage lang = db.ProgLanguages.ToList().Find(l => l.LanguageId == db.Modules.ToList().Find(m => m.ModuleCode == addAssgn.ModuleId).LanguageId);

                            //get the appropirate file and move the file accordingly
                            var ogPath = "";
                            var newPath = "";

                            if (lang.LangageType.Equals("Java"))
                            {
                                ogPath = slnFilePath + "/" + fileName + ".java";
                                newPath = path + "/" + fileName + ".java";
                            }
                            else if (lang.LangageType.Equals("C#"))
                            {
                                ogPath = slnFilePath + "/" + fileName + ".cs";
                                newPath = path + "/" + fileName + ".cs";
                            }

                            System.IO.File.Move(ogPath, newPath);

                            Grader g = new Grader(slnFilePath, fileName, assignmentTitle, lang.LangageType, false);

                            int exitCode = g.RunLecturerSolution();

                            if (exitCode == 1)
                            {
                                //save to DB + rename solution file
                                if (AddAssignmentToDB(addAssgn, fileName, false) == true)
                                {
                                    //solution has failed to save to DB
                                    DeleteFile(fileName, assignmentTitle, false);
                                    addAssgn.Modules = db.Modules.ToList();
                                    TempData["GeneralError"] = "Failed to save assignmet to database ! Please try again.";
                                    return View(addAssgn);
                                }

                                //delete the uploaded sln
                                DeleteFile(fileName, assignmentTitle, false);

                            }
                            else if (exitCode == 3)
                            {
                                //solution failed to run 
                                DeleteFile(fileName, assignmentTitle, false);
                                addAssgn.Modules = db.Modules.ToList();
                                TempData["GeneralError"] = "The program uploaded was caught in an infinite loop. Please check your program";
                                return View(addAssgn);
                            }
                        }
                        else
                        {
                            //uploaded file is more than 150MB
                            addAssgn.Modules = db.Modules.ToList();
                            TempData["SlnWarning"] = "Please make sure that your files is less than 150MB !";
                            return View(addAssgn);
                        }
                    }
                    else
                    {
                        //uploaded file is empty 
                        addAssgn.Modules = db.Modules.ToList();
                        TempData["SlnWarning"] = "Uploaded file is empty ! Please try again.";
                        return View(addAssgn);
                    }
                }
                else
                {
                    //uploaded file is invalid 
                    addAssgn.Modules = db.Modules.ToList();
                    TempData["SlnWarning"] = "Uploaded file is invalid ! Please try again.";
                    return View(addAssgn);
                }
            }//end of run without testcase

            //everything all okay 
            return RedirectToAction("ManageAssignments", "Lecturer");
        }

        //used to insert the data into DB. 
        public bool AddAssignmentToDB(AddAssignmentViewModel addAssgn, string fileName, bool isTestCase)
        {
            //now to add into the DB
            Assignment newAssignment = new Assignment();
            Class_Assgn classAssgn = new Class_Assgn();
            List<HttpPostedFileBase> assgnFiles = new List<HttpPostedFileBase>();
            string assignmentTitle = (addAssgn.AssgnTitle).Replace(" ", "");
            bool isFailed = false; //used to tell the user if the assignment has already been successfully save to DB

            try
            {
                //save the main assignment to DB
                addAssgn.Solution = "~/Solutions/" + assignmentTitle + ".xml";
                newAssignment.AssgnTitle = addAssgn.AssgnTitle;
                newAssignment.Describe = addAssgn.Describe;
                newAssignment.MaxAttempt = addAssgn.MaxAttempt;
                newAssignment.StartDate = addAssgn.StartDate;
                newAssignment.DueDate = addAssgn.DueDate;
                newAssignment.Solution = addAssgn.Solution;
                newAssignment.ModuleCode = addAssgn.ModuleId;
                newAssignment.CreateBy = User.Identity.GetUserName();
                newAssignment.CreateAt = DateTime.Now;
                newAssignment.UpdatedBy = User.Identity.GetUserName();
                newAssignment.UpdatedAt = DateTime.Now;
                db.Assignments.Add(newAssignment);
                db.SaveChanges();

                //get the assignment ID 
                var assgnId = db.Assignments.Where(a => a.AssgnTitle == addAssgn.AssgnTitle).Select(s => s.AssignmentID).ToList().First();

                //insert into Class_Assgn
                foreach (AssignmentClass a in addAssgn.ClassList)
                {
                    if (a.isSelected == true)
                    {
                        classAssgn.ClassID = a.ClassId;
                        classAssgn.AssignmentID = assgnId;
                        db.Class_Assgn.Add(classAssgn);
                        db.SaveChanges();
                    }
                }


                //rename the solution here
                var solutionPath = Server.MapPath(@"~/Solutions/");

                foreach (FileInfo f in new DirectoryInfo(solutionPath).GetFiles())
                {
                    if (f.Name == assignmentTitle + ".xml")
                    {
                        var sourcePath = solutionPath + f.Name;
                        var destPath = solutionPath + assgnId + "solution.xml";
                        FileInfo info = new FileInfo(sourcePath);
                        info.MoveTo(destPath);

                        var query = from Assignment in db.Assignments where Assignment.AssignmentID == assgnId select Assignment;

                        //update the DB to reflect the change of name for the solution
                        foreach (Assignment a in query)
                        {
                            a.Solution = "~/Solutions/" + assgnId + "solution.xml";
                        }
                        db.SaveChanges();
                    }
                }

                //rename the testcase IF there is one 
                if (isTestCase == true)
                {
                    var testCasePath = Server.MapPath(@"~/TestCase/");

                    foreach (FileInfo f in new DirectoryInfo(testCasePath).GetFiles())
                    {
                        if (f.Name == assignmentTitle + ".xml")
                        {
                            var sourcePath = testCasePath + f.Name;
                            var destPath = testCasePath + assgnId + "testcase.xml";
                            FileInfo info = new FileInfo(sourcePath);
                            info.MoveTo(destPath);
                        }
                    }
                }

            }//end of try 

            catch (Exception ex)
            {
                //failed to save to DB. will show something to user
                isFailed = true;
            }
            return isFailed;
        }

        //used to delete files
        private void DeleteFile(string fileName, string assgnTitle, bool isTestCase)
        {
            //delete their solution + testcase 
            var tempPath = Server.MapPath(@"~/TempSubmissions/" + fileName);
            var tempPath2 = Server.MapPath(@"~/TestCase/");

            DirectoryInfo di;

            if ((di = new DirectoryInfo(tempPath)).Exists)
            {
                foreach (FileInfo f in di.GetFiles())
                {
                    f.IsReadOnly = false;
                    f.Delete();
                }

                foreach (DirectoryInfo dr in di.GetDirectories())
                {
                    dr.Delete(true);
                }

                di.Delete();
            }

            //delete testcase IF present
            if (isTestCase == true)
            {
                if ((di = new DirectoryInfo(tempPath2)).Exists)
                {
                    foreach (FileInfo f in di.GetFiles())
                    {
                        if (f.Name == assgnTitle + ".xml")
                        {
                            f.IsReadOnly = false;
                            f.Delete();
                        }
                    }
                }
            }
        }

        //View Results
        public ActionResult ViewResults()
        {
            ViewResultsViewModel vrvm = new ViewResultsViewModel();

            string loggedInLecturer = User.Identity.GetUserName(); //temp 


            List<Class> managedClasses = new List<Class>();

            List<Lec_Class> lec_classes = db.Lec_Class.Where(lc => lc.StaffID == loggedInLecturer).ToList();

            foreach(Lec_Class lc in lec_classes)
            {
                List<Class> temp= db.Classes.Where(c => c.DeletedAt == null).Where(c => c.ClassID == lc.ClassID).ToList();
                managedClasses.AddRange(temp);
            }
            
                      
            List<String> classIds = new List<String>();
            List<String> classNames = new List<String>();

            foreach (Class c in managedClasses)
            {
                Course course = db.Courses.Where(courses => courses.CourseID == c.CourseID).FirstOrDefault();

                String cId = c.ClassID.ToString();
                String cName = course.CourseAbbr + "/" + c.ClassName.ToString();

                classIds.Add(cId);
                classNames.Add(cName);
            }

            vrvm.classIds = classIds;
            vrvm.classNames = classNames;

            return View(vrvm);

        }

        [HttpPost]
        public ActionResult GetAssignment(string Class)
        {
            string loggedInLecturer = User.Identity.GetUserName(); //temp 

            var assignments = db.Database.SqlQuery<DBass>("select ca.*, a.AssgnTitle from Class_Assgn ca inner join(select * from Assignment) a on ca.AssignmentID = a.AssignmentID where classid = @inClass and createby = @inCreator and deletedat is null",
        new SqlParameter("@inClass", Class),
        new SqlParameter("@inCreator", loggedInLecturer)).ToList();

            return Json(assignments);
        }

        [HttpPost]
        public ActionResult ViewResults(string Class, string Assignment)
        {
            var results = db.Database.SqlQuery<DBres>("select s1.submissionid, s1.adminno, stud.name, s1.assignmentid, s1.grade, s1.filepath from submission s1 inner join ( select adminno, max(submissionid) submissionid, assignmentid from submission group by adminno, assignmentid) s2 on s1.submissionid = s2.submissionid inner join ( select * from student where classid = @inClass) stud on s1.adminno = stud.adminno where s1.assignmentid = @inAssignment",
        new SqlParameter("@inClass", Class),
        new SqlParameter("@inAssignment", Assignment)).ToList();

            return Json(results);
        }

        [HttpGet]
        public ActionResult Download(string file)
        {

            string path = "~/Submissions/" + file;
            string zipname = file + ".zip";

            var memoryStream = new MemoryStream();
            using (var zip = new ZipFile())
            {

                zip.AddDirectory(Server.MapPath(path));
                zip.Save(memoryStream);
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            return File(memoryStream, "application/zip", zipname);


        }

        class DBass
        {
            public int ClassID { get; set; }
            public int AssignmentID { get; set; }
            public int _id { get; set; }
            public string AssgnTitle { get; set; }
        }

        class DBres
        {
            public int submissionid { get; set; }
            public string adminno { get; set; }
            public string name { get; set; }
            public int assignmentid { get; set; }
            public decimal grade { get; set; }
            public string filepath { get; set; }
        }

    }//end of controller

}
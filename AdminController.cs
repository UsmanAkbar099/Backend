using FinancialAidAllocation.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Http;

namespace FinancialAidAllocation.Controllers
{
    public class AdminController : ApiController
    {
        FAAToolEntities8 db = new FAAToolEntities8();


        [HttpGet]
        public HttpResponseMessage getAdminInfo(int id)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Admins.Where(a => a.AdminID == id).FirstOrDefault());
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddBudget(int amount)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                if (amount > 0)
                {
                    var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                    Budget b;
                    if (paisa != null)
                    {
                        b = new Budget();
                        b.budgetAmount = amount;
                        b.remainingAmount = paisa.remainingAmount + amount;
                        b.status = "A";
                        b.budget_session = session.session1;
                    }
                    else
                    {
                        b = new Budget();
                        b.budgetAmount = amount;
                        b.remainingAmount = amount;
                        b.status = "A";
                        b.budget_session = session.session1;

                    }
                    db.Budgets.Add(b);
                    db.SaveChanges();
                    var Remainbalance = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                    return Request.CreateResponse(HttpStatusCode.OK, Remainbalance.remainingAmount);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "Add Some Ammount");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage FacultyMembers()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Faculties);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage AcceptApplication(int amount, int applicationid)
        {
            try
            {
                var remainingamount = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();

                if (amount > 0 && remainingamount.remainingAmount >= amount)
                {
                    var application = db.FinancialAids.Where(f => f.applicationId == applicationid).FirstOrDefault();
                    if (application.applicationStatus.ToLower() == "pending")
                    {
                        application.amount = amount.ToString();
                        application.applicationStatus = "Accepted";
                        //  var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();
                        remainingamount.remainingAmount -= amount;
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, remainingamount.remainingAmount + "\n" + application.applicationStatus);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotAcceptable, application.applicationStatus);
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "InSufficient Funds");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage RejectApplication(int applicationid)
        {
            try
            {
                var application = db.FinancialAids.Where(f => f.applicationId == applicationid).FirstOrDefault();
                if (application.applicationStatus.ToLower() == "pending")
                {
                    application.applicationStatus = "Rejected";
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, application.applicationStatus);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already : " + application.applicationStatus);
                }

            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        

        [HttpPost]
        public HttpResponseMessage AddPolicies(String description, String val1, String val2, String policyFor, String policy, String strength)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                Criterion c = new Criterion();
                Policy p = new Policy();
                p.policyfor = policyFor;
                p.policy1 = policy;
                p.session = session.session1;
                db.Policies.Add(p);
                db.SaveChanges();
                var pol = db.Policies.OrderByDescending(o => o.id).FirstOrDefault();
                c.val1 = val1;
                c.val2 = val2;
                c.description = description;
                c.policy_id = pol.id;
                c.strength = int.Parse(strength);
                db.Criteria.Add(c);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK,"Added Succesfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage MeritBaseShortListing()
        {
            try
            {
                List<Student> toperStudent = new List<Student>();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var isAlreadyShortListed = db.MeritBases.Where(merit => merit.session == session.session1);
                if (isAlreadyShortListed == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }
                else
                {
                    var amount = db.Amounts.Where(amt => amt.session == session.session1).FirstOrDefault();
                    if (amount != null)
                    {
                        var first = amount.first_position;
                        var second = amount.second_position;
                        var third = amount.third_position;
                        var degree = db.Students.Select(p => new
                        {
                            p.degree,

                        }).Distinct().ToList();

                        foreach (var d in degree)
                        {
                            var semester = db.Students.Where(s => s.degree == d.degree).Select(p => new
                            {
                                p.semester,
                            }).Distinct();
                            foreach (var s in semester)
                            {
                                var section = db.Students.Where(std => std.degree == d.degree && std.semester == s.semester).Select(p => new
                                {
                                    p.section
                                }).Distinct();

                                foreach (var sec in section)
                                {
                                    var count = db.Students.Where(stu => stu.degree == d.degree &&
                                    stu.semester == s.semester && stu.section ==
                                    sec.section).Distinct().Count();
                                    MeritBase m;
                                    FinancialAid fa;

                                    if (count >= 40)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(3)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }

                                    }
                                    else if (count >= 30 && count < 40)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(2)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else if (count < 30)
                                    {
                                        var topers1 = db.Students.Where(st => st.degree == d.degree && st.cgpa >=
                                        3.7 && st.semester == s.semester && st.section == sec.section)
                                            .DistinctBy(st => st.student_id)
                                            .OrderByDescending(od => od.cgpa).Take(1)
                                               .Select(p => new
                                               {
                                                   p.cgpa,
                                                   p.student_id
                                               }).ToList();
                                        foreach (var t in topers1)
                                        {
                                            toperStudent.Add(db.Students.Where(stud => stud.student_id == t.student_id).FirstOrDefault());
                                            for (int index = 0; index < toperStudent.Distinct().ToList().Count; index++)
                                            {
                                                m = new MeritBase();
                                                fa = new FinancialAid();
                                                m.studentId = toperStudent[index].student_id;
                                                m.session = session.session1;
                                                m.position = index + 1;
                                                db.MeritBases.Add(m);
                                                fa.applicationStatus = "Pending";
                                                fa.aidtype = "MeritBase";
                                                fa.applicationId = toperStudent[index].student_id;
                                                if (index == 0)
                                                {
                                                    fa.amount = first.ToString();
                                                }
                                                else if (index == 1)
                                                {
                                                    fa.amount = second.ToString();

                                                }
                                                else
                                                {
                                                    fa.amount = third.ToString();
                                                }
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                }
                            }
                            db.SaveChanges();
                        }
                        /*                    var student = db.Students.Join(
                                                db.MeritBases,
                                                s => s.student_id,
                                                m => m.studentId,
                                                (s, m) => new
                                                {
                                                    s,
                                                    m.position
                                                }
                                                ).Distinct();*/

                        return Request.CreateResponse(HttpStatusCode.OK, toperStudent);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage MeritBase()
        {
            try
            {
                var toperStudent = new List<Student>();

                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var isAlreadyShortListed = db.MeritBases.Any(merit => merit.session == session.session1);

                if (isAlreadyShortListed)
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }
                else
                {
                    var amount = db.Amounts.FirstOrDefault(amt => amt.session == session.session1);
                    if (amount != null)
                    {
                        var first = amount.first_position;
                        var second = amount.second_position;
                        var third = amount.third_position;

                        var degrees = db.Students.Select(p => p.degree).Distinct().ToList();

                        foreach (var degree in degrees)
                        {
                            var semesters = db.Students.Where(s => s.degree == degree).Select(p => p.semester).Distinct().ToList();

                            foreach (var semester in semesters)
                            {
                                var sections = db.Students.Where(std => std.degree == degree && std.semester == semester).Select(p => p.section).Distinct().ToList();

                                foreach (var section in sections)
                                {
                                    var studentQuery = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section)
                                                                 .OrderByDescending(od => od.cgpa).ToList();

                                    if (studentQuery.Count >= 40)
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(3)
                                        .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else if (studentQuery.Count >= 30 && studentQuery.Count < 40)
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(2)
                                           .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var toperCount = db.Students.Where(stu => stu.degree == degree && stu.semester == semester && stu.section == section && stu.cgpa >= 3.7).Take(1)
                                        .OrderByDescending(od => od.cgpa).ToList();
                                        for (int i = 0; i < toperCount.Count; i++)
                                        {
                                            if (!toperStudent.Any(ts => ts.student_id == toperCount[i].student_id))
                                            {
                                                toperStudent.Add(toperCount[i]);
                                                var m = new MeritBase
                                                {
                                                    studentId = toperCount[i].student_id,
                                                    session = session.session1,
                                                    position = i + 1
                                                };

                                                var fa = new FinancialAid
                                                {
                                                    applicationStatus = "Pending",
                                                    aidtype = "MeritBase",
                                                    applicationId = toperCount[i].student_id,
                                                    amount = GetAmount(i + 1, first, second, third)
                                                };

                                                db.MeritBases.Add(m);
                                                db.FinancialAids.Add(fa);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, toperStudent.Select(s => new
                        {
                            s.arid_no,
                            s.name,
                            s.cgpa,
                            s.degree,
                            s.semester,
                            s.student_id,
                            s.section,
                            s.profile_image,
                            s.gender,
                        }));
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetMeritBaseShortListedStudent()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var std = db.MeritBases.Where(mer => mer.session == session.session1).Join
                    (
                    db.FinancialAids.Where(fn => fn.session == session.session1 && fn.applicationStatus.ToLower() == "accepted"),
                    mr => mr.studentId,
                    fa => fa.applicationId,
                    (mr, fa) => new
                    {
                        mr,
                        fa
                    }
                    );

                var info = std.Join(

                    db.Students,
                    s => s.mr.studentId,
                    st => st.student_id,
                    (s, st) => new
                    {
                        s,
                        st,
                    }
                    ).Select(s => new
                    {
                        s.st.student_id,
                        s.st.arid_no,
                        s.st.name,
                        s.st.prev_cgpa,
                        s.st.profile_image,
                        s.st.gender,
                        s.st.degree,
                        s.st.semester,
                        s.st.section,
                        s.st.cgpa,
                        s.s.mr.position,
                        s.s.fa.amount,
                        s.s.fa.aidtype,
                        s.s.fa.applicationStatus,
                    }).ToList();
                /*                var students = db.MeritBases.Where(mr=>mr.session==session.session1).Join(
                                    db.Students,
                                    m => m.studentId,
                                    s => s.student_id,
                                    (m, s) => new
                                    {
                                        s.student_id,
                                        s.arid_no,
                                        s.name,
                                        s.prev_cgpa,
                                        s.profile_image,
                                        s.gender,
                                        s.degree,
                                        s.semester,
                                        s.section,
                                        s.cgpa,
                                        m.position
                                    }
                                    );
                                var info = students.Join(
                                    db.FinancialAids.Where(f=>f.aidtype.ToLower()=="meritbase"),
                                    std=>std.student_id,
                                    fin=>fin.applicationId,
                                    (std,fin) => new 
                                    {
                                        std,fin
                                    }
                                    ).Select(s => new
                                    {
                                        s.std.student_id,
                                        s.std.arid_no,
                                        s.std.name,
                                        s.std.prev_cgpa,
                                        s.std.profile_image,
                                        s.std.gender,
                                        s.std.degree,
                                        s.std.semester,
                                        s.std.section,
                                        s.std.cgpa,
                                        s.std.position,
                                        s.fin.amount,
                                        s.fin.aidtype,
                                        s.fin.applicationStatus,
                                    });*/
                if (std.ToList().Count < 1)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, info);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        private string GetAmount(int position, int first, int second, int third)
        {
            switch (position)
            {
                case 1:
                    return first.ToString();
                case 2:
                    return second.ToString();
                case 3:
                    return third.ToString();
                default:
                    return "0";
            }
        }

        [HttpGet]
        public HttpResponseMessage AcceptedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session == session.session1)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestions) => new
                        {
                            application,
                            suggestions
                        })
                    .Where(ap => ap.suggestions.ToList().Count >= totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
                        student.name,
                        student.student_id,
                        student.father_name,
                        student.gender,
                        student.degree,
                        student.cgpa,
                        student.semester,
                        student.section,
                        student.profile_image,
                        student.prev_cgpa,
                        application.application.applicationDate,
                        application.application.reason,
                        application.application.requiredAmount,
                        application.application.EvidenceDocuments,
                        application.application.applicationID,
                        application.application.session,
                        application.application.father_status,
                        application.application.jobtitle,
                        application.application.salary,
                        application.application.guardian_contact,
                        application.application.house,
                        application.application.guardian_name,
                        application.suggestions
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f
                    })
                    .Where(p => p.f.applicationStatus.ToLower() == "accepted");

                var finalResult = pendingApplications.Select(pa => new
                {
                    pa.f.applicationStatus,
                    pa.re.arid_no,
                    pa.re.name,
                    pa.re.student_id,
                    pa.re.father_name,
                    pa.re.gender,
                    pa.re.degree,
                    pa.re.cgpa,
                    pa.re.semester,
                    pa.re.section,
                    pa.re.profile_image,
                    pa.re.applicationDate,
                    pa.re.reason,
                    pa.re.requiredAmount,
                    pa.re.EvidenceDocuments,
                    pa.re.applicationID,
                    pa.re.session,
                    pa.re.father_status,
                    pa.re.jobtitle,
                    pa.re.salary,
                    pa.re.guardian_contact,
                    pa.re.house,
                    pa.re.prev_cgpa,
                    pa.re.guardian_name,
                    pa.f.amount,
                    Suggestions = pa.re.suggestions.Select(s => new
                    {
                        s.status,
                        s.comment,
                        amount = s.amount,
                        CommitteeMemberName = db.Faculties
                            .Where(fac => fac.facultyId == db.Committees.FirstOrDefault(c => c.committeeId == s.committeeId).facultyId)
                            .Select(fac => fac.name).FirstOrDefault()
                    }).ToList()
                });

                return Request.CreateResponse(HttpStatusCode.OK, finalResult);
                //                return Request.CreateResponse(HttpStatusCode.OK, pendingapplication.Where(p => p.applicationStatus.ToLower().Equals("accepted")));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage AddPreviousCgpa()
        {
            try
            {
                Random rnd = new Random();
                int minValue = 50;
                int maxValue = 100;

                var student = db.Students.ToList();
                for (int i = 0; i < student.Count; i++)
                {
                    int randomNumber = rnd.Next(minValue, maxValue);
                    int id = student[i].student_id;
                    var std = db.Students.Where(st => st.student_id == id).FirstOrDefault();
                    if (std.semester == 1)
                    {
                        std.prev_cgpa = double.Parse(randomNumber.ToString());
                    }
                    else if (std.semester >= 2)
                    {
                        if (std.cgpa > 3.9)
                        {
                            std.prev_cgpa = std.cgpa;
                        }
                        else if (std.cgpa >= 3.7 && std.cgpa < 3.9)
                        {
                            std.prev_cgpa = std.cgpa - 0.0001;

                        }
                        else if (std.cgpa >= 3.5 && std.cgpa < 3.7)
                        {
                            std.prev_cgpa = std.cgpa - 0.0008;
                        }
                        else if (std.cgpa >= 3.3 && std.cgpa < 3.5)
                        {
                            std.prev_cgpa = std.cgpa - 0.010;
                        }
                        else if (std.cgpa >= 3.0 && std.cgpa < 3.3)
                        {
                            std.prev_cgpa = std.cgpa - 0.02;
                        }
                        else if (std.cgpa >= 2.7 && std.cgpa < 3.0)
                        {
                            std.prev_cgpa = std.cgpa - 0.05;
                        }
                        else
                        {
                            std.prev_cgpa = std.cgpa - 0.15;
                        }
                    }
                }
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage MeritBaseRejectedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var rejectedApplication = db.MeritBases.Where(mr => mr.session == session.session1).Join(
                    db.FinancialAids.Where(fn => fn.session == session.session1 && fn.applicationStatus.ToLower() == "rejected"),
                    m => m.studentId,
                    f => f.applicationId,
                    (m, f) => new
                    {
                        m, f
                    }
                    );
                var result = rejectedApplication.Join
                    (
                    db.Students,
                    ra => ra.m.studentId,
                    s => s.student_id,
                    (re, s) => new
                    {
                        re.f.aidtype,
                        re.f.applicationStatus,
                        s.name,
                        s.arid_no,
                        s.profile_image,
                        s.cgpa,
                        s.prev_cgpa,
                        s.degree,
                        s.gender,
                        s.father_name,
                        s.section,
                        s.semester,
                        s.student_id,
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK,result );


            }
            catch(Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage RejectedApplication()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session == session.session1)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestions) => new
                        {
                            application,
                            suggestions
                        })
                    .Where(ap => ap.suggestions.ToList().Count == totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
                        student.name,
                        student.student_id,
                        student.father_name,
                        student.gender,
                        student.degree,
                        student.cgpa,
                        student.semester,
                        student.section,
                        student.profile_image,
                        application.application.applicationDate,
                        application.application.reason,
                        application.application.requiredAmount,
                        application.application.EvidenceDocuments,
                        application.application.applicationID,
                        application.application.session,
                        application.application.father_status,
                        application.application.jobtitle,
                        application.application.salary,
                        application.application.guardian_contact,
                        application.application.house,
                        application.application.guardian_name,
                        application.suggestions
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f
                    })
                    .Where(p => p.f.applicationStatus.ToLower() == "rejected");

                var finalResult = pendingApplications.Select(pa => new
                {
                    pa.f.applicationStatus,
                    pa.re.arid_no,
                    pa.re.name,
                    pa.re.student_id,
                    pa.re.father_name,
                    pa.re.gender,
                    pa.re.degree,
                    pa.re.cgpa,
                    pa.re.semester,
                    pa.re.section,
                    pa.re.profile_image,
                    pa.re.applicationDate,
                    pa.re.reason,
                    pa.re.requiredAmount,
                    pa.re.EvidenceDocuments,
                    pa.re.applicationID,
                    pa.re.session,
                    pa.re.father_status,
                    pa.re.jobtitle,
                    pa.re.salary,
                    pa.re.guardian_contact,
                    pa.re.house,
                    pa.re.guardian_name,

                    Suggestions = pa.re.suggestions.Select(s => new
                    {
                        s.status,
                        s.comment,
                        CommitteeMemberName = db.Faculties
                            .Where(fac => fac.facultyId == db.Committees.FirstOrDefault(c => c.committeeId == s.committeeId).facultyId)
                            .Select(fac => fac.name).FirstOrDefault()
                    }).ToList()
                });

                return Request.CreateResponse(HttpStatusCode.OK, finalResult);

                //                return Request.CreateResponse(HttpStatusCode.OK, pendingapplication.Where(p => p.applicationStatus.ToLower().Equals("rejected")));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage CommitteeMembers()
        {
            try
            {

                var members = db.Committees.Where(com => com.status == "1").Join
                    (
                    db.Faculties,
                    c => c.facultyId,
                    f => f.facultyId,
                    (c, f) => new
                    {
                        c.committeeId,
                        f.name,
                        f.contactNo,
                        f.profilePic,
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddStudent()
        {
            try
            {
                var request = HttpContext.Current.Request;
                String name = request["name"];
                String cgpa = request["cgpa"];
                String semester = request["semester"];
                String aridno = request["aridno"];
                String gender = request["gender"];
                String fathername = request["fathername"];
                String degree = request["degree"];
                String section = request["section"];
                String password = request["password"];
                int Role = 1;
                var photo = request.Files["pic"];
                var provider = new MultipartMemoryStreamProvider();
                String picpath = name + "." + photo.FileName.Split('.')[1];
                photo.SaveAs(HttpContext.Current.Server.MapPath("~/Content/ProfileImages/" + picpath));
                Student s = new Student();
                s.section = section;
                s.name = name;
                s.gender = gender;
                s.arid_no = aridno;
                s.degree = degree;
                s.father_name = fathername;
                s.semester = int.Parse(semester);
                s.cgpa = double.Parse(cgpa);
                s.profile_image = picpath;
                db.Students.Add(s);
                db.SaveChanges();
                var studentId = db.Students.Where(sa => sa.arid_no == aridno).FirstOrDefault();
                AddUser(aridno, password, Role, studentId.student_id);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage AddUser(String username, String password, int role, int? profileId)
        {
            /*
               1 student
               2 faculty
               3 committee
               4 Admin
             */
            try
            {
                var user = db.Users.Where(us => us.userName == username & us.password == password).FirstOrDefault();

                if (user == null)
                {
                    User u = new User();
                    u.userName = username;
                    u.password = password;
                    u.role = role;
                    u.profileId = profileId;
                    db.Users.Add(u);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddFacultyMember()
        {
            try
            {
                var request = HttpContext.Current.Request;
                String name = request["name"];
                String contact = request["contact"];
                String password = request["password"];
                int Role = 3;
                var photo = request.Files["pic"];
                var provider = new MultipartMemoryStreamProvider();
                String picpath = name + "." + photo.FileName.Split('.')[1];
                photo.SaveAs(HttpContext.Current.Server.MapPath("~/Content/ProfileImages/" + picpath));
                Faculty f = new Faculty();
                f.name = name;
                f.contactNo = contact;
                f.profilePic = picpath;
                db.Faculties.Add(f);
                db.SaveChanges();
                var profileId = db.Faculties.Where(fa => fa.name == name & fa.contactNo == contact).FirstOrDefault();
                AddUser(name, password, Role, profileId.facultyId);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Added");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage RemoveFacultyMember(int facultyId)
        {
            try
            {
                // Find the faculty member by ID
                Faculty faculty = db.Faculties.Find(facultyId);
                if (faculty == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Faculty member not found");
                }

                // Remove the user associated with this faculty member
                var user = db.Users.FirstOrDefault(u => u.profileId == facultyId); // Adjusted to use ProfileId
                if (user != null)
                {
                    db.Users.Remove(user);
                }

                // Remove associated records in the graders table
                var graders = db.graders.Where(g => g.facultyId == facultyId).ToList();
                foreach (var grader in graders)
                {
                    db.graders.Remove(grader);
                }

                // Remove or update references in the Committee table
                var committees = db.Committees.Where(c => c.facultyId == facultyId).ToList();
                foreach (var committee in committees)
                {
                    // Option 1: Remove the record if it's safe to do so
                    db.Committees.Remove(committee);

                    // Option 2: Update the reference to null or another value if applicable
                    // committee.facultyId = null; // or set it to another value if needed
                }

                // Remove the faculty member
                db.Faculties.Remove(faculty);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Removed");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddCommitteeMember(int id)
        {
            try
            {
                if (db.Committees.Where(c => c.facultyId == id).FirstOrDefault() == null)
                {
                    Committee committee = new Committee();
                    committee.facultyId = id;
                    committee.status = "1";
                    db.Committees.Add(committee);
                    db.SaveChanges();
                    var user = db.Users.Where(u => u.profileId == id).FirstOrDefault();
                    var comm = db.Committees.Where(cm => cm.facultyId == id).FirstOrDefault();
                    user.role = 4;
                    user.profileId = comm.committeeId;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage RemoveCommitteeMember(int id)
        {
            try
            {
                // Find the committee by facultyId
                var committee = db.Committees.Where(c => c.committeeId == id).FirstOrDefault();

                if (committee != null)
                {
                    // Remove the committee
                    db.Committees.Remove(committee);
                    db.SaveChanges();

                    

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Committee member not found");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage BudgetHistory()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Budgets);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage ApplicationHistory(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session != session.session1 && app.studentId == id)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestions) => new
                        {
                            application,
                            suggestions
                        })
                    .Where(ap => ap.suggestions.ToList().Count == totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
                        student.name,
                        student.student_id,
                        student.father_name,
                        student.gender,
                        student.degree,
                        student.cgpa,
                        student.semester,
                        student.section,
                        student.profile_image,
                        application.application.applicationDate,
                        application.application.reason,
                        application.application.requiredAmount,
                        application.application.EvidenceDocuments,
                        application.application.applicationID,
                        application.application.session,
                        application.application.father_status,
                        application.application.jobtitle,
                        application.application.salary,
                        application.application.guardian_contact,
                        application.application.house,
                        application.application.guardian_name,
                        application.suggestions
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    });

                var finalResult = pendingApplications.Select(pa => new
                {
                    pa.applicationStatus,
                    pa.re.arid_no,
                    pa.re.name,
                    pa.re.student_id,
                    pa.re.father_name,
                    pa.re.gender,
                    pa.re.degree,
                    pa.re.cgpa,
                    pa.re.semester,
                    pa.re.section,
                    pa.re.profile_image,
                    pa.re.applicationDate,
                    pa.re.reason,
                    pa.re.requiredAmount,
                    pa.re.EvidenceDocuments,
                    pa.re.applicationID,
                    pa.re.session,
                    pa.re.father_status,
                    pa.re.jobtitle,
                    pa.re.salary,
                    pa.re.guardian_contact,
                    pa.re.house,
                    pa.re.guardian_name,
                    Suggestions = pa.re.suggestions.Select(s => new
                    {
                        s.comment,
                        amount = s.amount,
                        CommitteeMemberName = db.Faculties
                            .Where(fac => fac.facultyId == db.Committees.FirstOrDefault(c => c.committeeId == s.committeeId).facultyId)
                            .Select(fac => fac.name).FirstOrDefault()
                    }).ToList()
                });

                return Request.CreateResponse(HttpStatusCode.OK, finalResult);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage getStudentApplicationStatus(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var count = db.Applications.Where(c => c.studentId == id && c.session == session.session1).FirstOrDefault();
                if (count != null)
                {
                    var result = db.Applications.Where(ap => ap.studentId == id).Join(
                    db.FinancialAids,
                    a => a.applicationID,
                    f => f.applicationId,
                    (a, f) => new
                    {
                        //                        a.applicationID,
                        f.applicationStatus,
                    }
                    );
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Not Submitted");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AssignGrader(int facultyId, int StudentId)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var student = db.graders.Where(gr => gr.studentId == StudentId & gr.session == session.session1).FirstOrDefault();
                if (student == null)
                {
                    grader g = new grader();
                    g.studentId = StudentId;
                    g.facultyId = facultyId;
                    g.session = session.session1;
                    db.graders.Add(g);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Alredy Assigned ");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage gradersInformation(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var graders = db.Faculties.Where(fal => fal.facultyId == id).Join
                    (
                        db.graders.Where(gr => gr.facultyId == id && gr.session == session.session1),
                        f => f.facultyId,
                        g => g.facultyId,
                        (f, g) => new
                        {
                            g.Student.name,
                            g.Student.arid_no,
                            g.studentId,
                            g.Student.profile_image,
                            g.Student.gender,
                            f.facultyId,
                        }
                    );

                var graderList = graders.ToList();

                if (graderList.Count < 1)
                {
                    var responseMessage = new
                    {
                        Message = "No Grader Assigned"
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, responseMessage);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, graderList);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage Removegrader(int id)
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var graders = db.graders.Where(gr => gr.studentId == id && gr.session == session.session1).FirstOrDefault();
                db.graders.Remove(graders);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        /*[HttpGet]
        public HttpResponseMessage ApplicationWithSuggestion()
        {
            try
            {


                var applications = db.Applications
                            .Join(db.Suggestions,
                                application => application.applicationID,
                                suggestion => suggestion.applicationId,
                                (application, suggestion) => new
                                {
                                    application,
                                    suggestion
                                });
                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (appplication, student) => new
                    {
                        student.arid_no,
                        student.name,
                        student.student_id,
                        student.father_name,
                        student.gender,
                        student.degree,
                        student.cgpa,
                        student.semester,
                        student.section,
                        student.profile_image,
                        appplication.application.applicationDate,
                        appplication.application.reason,
                        appplication.application.requiredAmount,
                        appplication.application.EvidenceDocuments,
                        appplication.application.applicationID,
                        appplication.application.session,
                        appplication.application.father_status,
                        appplication.application.jobtitle,
                        appplication.application.salary,
                        appplication.application.guardian_contact,
                        appplication.application.house,
                        appplication.application.guardian_name,
                        appplication.suggestion.comment,
                        appplication.suggestion.status
                    });

                var pendingapplication = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    }
                    );

                return Request.CreateResponse(HttpStatusCode.OK, pendingapplication.Where(p => p.applicationStatus.ToLower().Equals("pending")));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }*/
        [HttpGet]
        public HttpResponseMessage ApplicationSuggestions()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var totalCommitteeMembers = db.Committees.Where(c => c.status == "1").ToList().Count();

                var applications = db.Applications.Where(app => app.session == session.session1)
                    .GroupJoin(db.Suggestions,
                        application => application.applicationID,
                        suggestion => suggestion.applicationId,
                        (application, suggestions) => new
                        {
                            application,
                            suggestions
                        })
                    .Where(ap => ap.suggestions.ToList().Count == totalCommitteeMembers);

                var result = applications.Join(db.Students,
                    ap => ap.application.studentId,
                    s => s.student_id,
                    (application, student) => new
                    {
                        student.arid_no,
                        student.name,
                        student.student_id,
                        student.father_name,
                        student.gender,
                        student.degree,
                        student.cgpa,
                        student.semester,
                        student.section,
                        student.profile_image,
                        application.application.applicationDate,
                        application.application.reason,
                        application.application.requiredAmount,
                        application.application.EvidenceDocuments,
                        application.application.applicationID,
                        application.application.session,
                        application.application.father_status,
                        application.application.jobtitle,
                        application.application.salary,
                        application.application.guardian_contact,
                        application.application.house,
                        application.application.guardian_name,
                        application.suggestions
                    });

                var pendingApplications = result.Join(
                    db.FinancialAids,
                    re => re.applicationID,
                    f => f.applicationId,
                    (re, f) => new
                    {
                        re,
                        f.applicationStatus
                    })
                    .Where(p => p.applicationStatus.ToLower() == "pending");

                var finalResult = pendingApplications.Select(pa => new
                {
                    pa.re.arid_no,
                    pa.re.name,
                    pa.re.student_id,
                    pa.re.father_name,
                    pa.re.gender,
                    pa.re.degree,
                    pa.re.cgpa,
                    pa.re.semester,
                    pa.re.section,
                    pa.re.profile_image,
                    pa.re.applicationDate,
                    pa.re.reason,
                    pa.re.requiredAmount,
                    pa.re.EvidenceDocuments,
                    pa.re.applicationID,
                    pa.re.session,
                    pa.re.father_status,
                    pa.re.jobtitle,
                    pa.re.salary,
                    pa.re.guardian_contact,
                    pa.re.house,
                    pa.re.guardian_name,
                    Suggestions = pa.re.suggestions.Select(s => new
                    {
                        s.status,
                        s.comment,
                        CommitteeMemberName = db.Faculties
                            .Where(fac => fac.facultyId == db.Committees.FirstOrDefault(c => c.committeeId == s.committeeId).facultyId)
                            .Select(fac => fac.name).FirstOrDefault()
                    }).ToList()
                });

                return Request.CreateResponse(HttpStatusCode.OK, finalResult);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdatePassword(int id, String username, String password)
        {
            try
            {
                var userprofile = db.Users.Where(u => u.userName == username).FirstOrDefault();

                if (userprofile == null)
                {
                    User user = new User();
                    user.role = 1;
                    user.password = password;
                    user.userName = username;
                    user.profileId = id;
                    db.Users.Add(user);
                    db.SaveChanges();
                }
                else
                {
                    userprofile.password = password;
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpPost]
        public HttpResponseMessage UploadFile()
        {
            string pathStr = "";
            var request = HttpContext.Current.Request;
            var enrollmentFile = request.Files["enrollment"];
            var path = HttpContext.Current.Server.MapPath("~/Content/Student_excel_sheet/" + enrollmentFile.FileName.Trim());
            pathStr = path;
            enrollmentFile.SaveAs(path);

            OleDbConnection oleDbConnection = new OleDbConnection("Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Extended Properties='Excel 12.0 Xml;HDR=NO'");

            try
            {
                oleDbConnection.Open();
                OleDbCommand command = new OleDbCommand("select * from [Sheet1$]", oleDbConnection);
                OleDbDataReader reader = command.ExecuteReader();
                List<Student> studentList = new List<Student>();
                Student student;

                while (reader.Read())
                {
                    student = new Student
                    {
                        arid_no = reader[0].ToString(),
                        name = reader[1].ToString(),
                        semester = Convert.ToInt32(reader[2].ToString().Trim()),
                        cgpa = Convert.ToDouble(reader[3].ToString()),
                        section = reader[4].ToString(),
                        degree = reader[5].ToString(),
                        father_name = reader[6].ToString(),
                        gender = reader[7].ToString(),
                        prev_cgpa = Convert.ToDouble(reader[8].ToString().Trim())
                    };
                    studentList.Add(student);
                }
                oleDbConnection.Close();

                var topStudents = new List<Student>();
                var budget = db.Budgets.OrderByDescending(b => b.budgetId).FirstOrDefault();
                var session = db.Sessions.OrderByDescending(s => s.id).FirstOrDefault();

                if (db.MeritBases.Any(m => m.session == session.session1))
                {
                    return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Already Short Listed");
                }

                var cgpaPolicy = db.Policies
                                    .Where(p => p.policyfor == "MeritBase" && p.policy1 == "CGPA")
                                    .Join(db.Criteria, p => p.id, c => c.policy_id, (p, c) => c.val1)
                                    .FirstOrDefault();

                var strengthPolicies = db.Policies
                                        .Where(p => p.policyfor == "MeritBase" && p.policy1 == "STRENGTH")
                                        .Join(db.Criteria, p => p.id, c => c.policy_id, (p, c) => new { c.val1, c.val2, c.strength })
                                        .ToList();

                var amount = db.Amounts.OrderByDescending(a => a.Id).FirstOrDefault();
                if (amount == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                int totalAmount = 0;
                var degrees = studentList.Select(s => s.degree).Distinct().ToList();

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        foreach (var degree in degrees)
                        {
                            var semesters = studentList.Where(s => s.degree == degree).Select(s => s.semester).Distinct().ToList();
                            foreach (var semester in semesters)
                            {
                                var sections = studentList.Where(s => s.degree == degree && s.semester == semester).Select(s => s.section).Distinct().ToList();
                                foreach (var section in sections)
                                {
                                    var studentsInSection = studentList
                                                            .Where(s => s.degree == degree && s.semester == semester && s.section == section)
                                                            .OrderByDescending(s => s.cgpa)
                                                            .ToList();

                                    foreach (var policy in strengthPolicies)
                                    {
                                        int minStrength = int.Parse(policy.val1);
                                        int maxStrength = int.Parse(policy.val2);
                                        double minCgpa = double.Parse(cgpaPolicy);
                                        int topN = int.Parse(policy.strength.ToString());

                                        if (studentsInSection.Count >= minStrength)
                                        {
                                            var topCandidates = studentsInSection.Where(s => s.cgpa >= minCgpa).Take(topN).ToList();

                                            int currentPosition = 1;
                                            double? previousCgpa = null;

                                            for (int i = 0; i < topCandidates.Count; i++)
                                            {
                                                if (!topStudents.Any(ts => ts.arid_no == topCandidates[i].arid_no))
                                                {
                                                    if (previousCgpa != topCandidates[i].cgpa)
                                                    {
                                                        currentPosition = i + 1;
                                                    }

                                                    previousCgpa = topCandidates[i].cgpa;

                                                    topStudents.Add(topCandidates[i]);
                                                    int sems = int.Parse(topCandidates[i].semester.ToString());
                                                    Student s = new Student();

                                                    s.arid_no = topCandidates[i].arid_no;
                                                    s.name = topCandidates[i].name;
                                                    s.semester = sems;
                                                    s.cgpa = topCandidates[i].cgpa;
                                                    s.prev_cgpa = topCandidates[i].prev_cgpa;
                                                    s.section = topCandidates[i].section;
                                                    s.degree = topCandidates[i].degree;
                                                    s.father_name = topCandidates[i].father_name;
                                                    s.gender = topCandidates[i].gender;
                                                    db.Students.Add(s);
                                                    db.SaveChanges();

                                                    String arid = topCandidates[i].arid_no;

                                                    var studentinfo = db.Students.Where(st => st.arid_no == arid).FirstOrDefault();

                                                    var meritBase = new MeritBase
                                                    {
                                                        session = session.session1,
                                                        position = currentPosition,
                                                        studentId = studentinfo.student_id,
                                                    };
                                                    db.MeritBases.Add(meritBase);
                                                    db.SaveChanges();


                                                    //                                                    var meritBaseEntry = db.MeritBases.Where(m => m.session == session.session1 && m.arid_no == arid).FirstOrDefault();
                                                    var financialAid = new FinancialAid
                                                    {
                                                        applicationStatus = "Accepted",
                                                        session = session.session1,
                                                        aidtype = "MeritBase",
                                                        applicationId = studentinfo.student_id,
                                                        amount = GetAmount(currentPosition, amount.first_position, amount.second_position, amount.third_position)
                                                    };
                                                    totalAmount += int.Parse(financialAid.amount);

                                                    db.FinancialAids.Add(financialAid);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        budget.remainingAmount -= totalAmount;
                        db.SaveChanges();
                        transaction.Commit();

                        var response = topStudents.Select(s => new
                        {
                            s.arid_no,
                            s.name,
                            s.cgpa,
                            s.degree,
                            s.semester,
                            s.section,
                            s.gender,
                        }).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, response);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
                    }
                }
            }
            catch (Exception e)
            {
                oleDbConnection.Close();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, e);
            }
        }

        [HttpGet]
        public HttpResponseMessage getAllStudent()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Students);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage getAllBudget()
        {
            try
            {

                return Request.CreateResponse(HttpStatusCode.OK, db.Budgets);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        /*[HttpGet]
        public HttpResponseMessage ToperStudents(double cgpa)
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK, db.Students.Where(s=>s.cgpa>=cgpa));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }*/

        [HttpGet]
        public HttpResponseMessage getPolicies()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();
                var pol = db.Policies.Join(
                    db.Criteria,
                    p => p.id,
                    c => c.policy_id,
                    (p, c) => new
                    {
                        p,
                        c
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, pol.Where(po => po.p.session == session.session1));
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        public HttpResponseMessage UnAssignedFaculty()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var query = from f in db.Faculties
                            join g in db.graders.Where(gr => gr.session == session.session1)
                            on f.facultyId equals g.facultyId into
                            joinedRecord
                            from g in joinedRecord.DefaultIfEmpty()
                            where g == null
                            select f;
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        /*[HttpGet]
        public HttpResponseMessage unAssignedGraders()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                var q = from fa in db.FinancialAids
                        join st in db.Students
                        on fa.applicationId equals st.student_id
                        into combineRecord
                        from st in combineRecord.DefaultIfEmpty()
                        where st != null
                        select st;

                var q2 = from a in q
                         join g in db.graders.Where(gr => gr.session == session.session1)
                         on a.student_id equals g.studentId
                         into result
                         from g in result.DefaultIfEmpty()
                         where g == null
                         select a;

                return Request.CreateResponse(HttpStatusCode.OK, q2);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
*/
        /*
                [HttpGet]
                public HttpResponseMessage unAssignedGraders()
                {
                    try
                    {
                        var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                        var needBaseAcceptedApplications = from fa in db.FinancialAids
                                                           where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "needbase"
                                                           join ap in db.Applications.Where(app => app.session == session.session1)
                                                           on fa.applicationId equals ap.applicationID
                                                           select new { ap.studentId };

                        var needBaseAcceptedStudents = from app in needBaseAcceptedApplications
                                                       join st in db.Students
                                                       on app.studentId equals st.student_id
                                                       select st;

                        var result1 = from student in needBaseAcceptedStudents
                                      join grader in db.graders.Where(gr => gr.session == session.session1)
                                      on student.student_id equals grader.studentId into gradings
                                      from grader in gradings.DefaultIfEmpty()
                                      where grader == null
                                      select student;

                        var meritbaseAcceptedApplication = from fa in db.FinancialAids
                                                           where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "meritbase" && fa.session == session.session1
                                                           join ap in db.MeritBases.Where(app => app.session == session.session1)
                                                           on fa.applicationId equals ap.studentId
                                                           select new { ap.studentId };



                        var meritBaseAcceptedStudents = from fa in meritbaseAcceptedApplication
                                                        join st in db.Students
                                                        on fa.studentId equals st.student_id
                                                        select st;

                        var result2 = from student in meritBaseAcceptedStudents
                                      join grader in db.graders.Where(gr => gr.session == session.session1)
                                      on student.student_id equals grader.studentId into gradings
                                      from grader in gradings.DefaultIfEmpty()
                                      where grader == null
                                      select student;

                        // Combine results
                        var joinedResult = result1.Union(result2).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, joinedResult);
                    }
                    catch (Exception ex)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
                    }
                }*/

        [HttpGet]
        public HttpResponseMessage unAssignedGraders1()
        {
            try
            {
                var session = db.Sessions.OrderByDescending(sess => sess.id).FirstOrDefault();

                // Need-based accepted applications
                var needBaseAcceptedApplications = from fa in db.FinancialAids
                                                   where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "needbase"
                                                   join ap in db.Applications.Where(app => app.session == session.session1)
                                                   on fa.applicationId equals ap.applicationID
                                                   select new { ap.studentId };

                // Need-based accepted students
                var needBaseAcceptedStudents = from app in needBaseAcceptedApplications
                                               join st in db.Students
                                               on app.studentId equals st.student_id
                                               select st;

                // Merit-based accepted applications
                var meritbaseAcceptedApplication = from fa in db.FinancialAids
                                                   where fa.applicationStatus.ToLower() == "accepted" && fa.aidtype.ToLower() == "meritbase" && fa.session == session.session1
                                                   join ap in db.MeritBases.Where(app => app.session == session.session1)
                                                   on fa.applicationId equals ap.studentId
                                                   select new { ap.studentId };

                // Merit-based accepted students
                var meritBaseAcceptedStudents = from fa in meritbaseAcceptedApplication
                                                join st in db.Students
                                                on fa.studentId equals st.student_id
                                                select st;

                // Combine need-based and merit-based accepted students
                var acceptedStudents = needBaseAcceptedStudents.Union(meritBaseAcceptedStudents);

                // Left join with graders to find unassigned graders and calculate average rating
                var result = from student in acceptedStudents
                             join grader in db.graders.Where(gr => gr.session != session.session1)
                             on student.student_id equals grader.studentId into gradings
                             from grader in gradings.DefaultIfEmpty()
                             select new
                             {
                                 student.name,student.arid_no,student.semester,student.cgpa,student.section,student.degree,student.gender,student.father_name,student.student_id,
                                 student.profile_image,student.prev_cgpa,
                                 AverageRating = grader == null ? (double?)null : db.graders.Where(r => r.studentId == grader.studentId).Average(r => (double?)r.feedback)
                             };

                var unassignedGradersWithRatings = result.ToList();

                return Request.CreateResponse(HttpStatusCode.OK, unassignedGradersWithRatings);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

    
[HttpGet]
        public HttpResponseMessage Merit()
        {
            try
            {
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage pendingApplication()
        {
            try
            {

                var query = from s in db.Students
                            join a in db.Applications
                            on s.student_id equals
                            a.studentId into joinedRecords
                            from a in
                                joinedRecords.DefaultIfEmpty()
                            where a != null
                            select a;
                var pendingApplication = from q in query
                                         join f in db.FinancialAids
                                         on q.applicationID equals f.applicationId into
                                         joinedRecords
                                         from f in joinedRecords.DefaultIfEmpty()
                                         where f != null
                                         select q;
                return Request.CreateResponse(HttpStatusCode.OK, query);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GiveRating()
        {
            try
            {

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddSession(String name, String startDate, String EndDate, String lastDate)
        {
            try
            {
                var isExist = db.Sessions.Where(s => s.session1 == name).FirstOrDefault();
                if (isExist == null)
                {
                    Session s = new Session();
                    s.session1 = name;
                    s.start_date = startDate;
                    s.end_date = EndDate;
                    s.submission_date = lastDate;
                    db.Sessions.Add(s);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Added Successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Already Exist");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }
    }
    }


﻿using FinancialAidAllocation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static System.Net.Mime.MediaTypeNames;

namespace FinancialAidAllocation.Controllers
{
    public class CommitteeController : ApiController
    {
        FAAToolEntities8 db = new FAAToolEntities8();

        [HttpGet]
        public HttpResponseMessage GetApplication(int id)
        {
            try
            {

                var result = db.Applications
            .GroupJoin(
                db.Suggestions.Where(s => s.committeeId == id),
        application => application.applicationID,
        suggestion => suggestion.applicationId,
        (application, suggestions) => new
        {
            Application = application,
            Suggestions = suggestions
        }).Where(joinResult => !joinResult.Suggestions.Any()).Select
        (joinResult => joinResult.Application).Distinct().Join(db.Students,
                            application => application.studentId,
                            student => student.student_id,
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
                        application.applicationDate,
                        application.reason,
                        application.requiredAmount,
                        application.EvidenceDocuments,
                        application.applicationID,
                        application.session,
                        application.father_status,
                        application.jobtitle,
                        application.salary,
                        application.guardian_contact,
                        application.house,
                        application.guardian_name,
                    });
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }
        [HttpGet]
        public HttpResponseMessage GetBalance()
        {
            var paisa = db.Budgets.OrderByDescending(bd => bd.budgetId).FirstOrDefault();

            return Request.CreateResponse(HttpStatusCode.OK, paisa.remainingAmount);
        }

        [HttpGet]
        public HttpResponseMessage CommitteeMembers(int id)
        {
            try
            {
                var members = db.Committees.Where(c => c.committeeId == id).Join
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
                    ).FirstOrDefault();
                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage GiveSuggestion(int committeeId, String status, int applicationId, String comment, string amounts)
        {
            try
            {
                var sug = db.Suggestions.Where(su => su.committeeId == committeeId && su.applicationId == applicationId).FirstOrDefault();
                if (sug == null)
                {
                    Suggestion s = new Suggestion();
                    s.comment = comment;
                    s.committeeId = committeeId;
                    s.applicationId = applicationId;
                    s.amount = amounts;
                    s.status = status;
                    
                    db.Suggestions.Add(s);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK,"Success");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}

using System;
using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Services
{
    // Stores and manages all reported issues for the application session.
    // Every issue also becomes a tracked service request (Part 3), so the reference
    // number is issued by ServiceRequestManager to keep numbers unique across both.
    public static class IssueManager
    {
        private static readonly List<Issue> Issues = new List<Issue>();

        // Number of issues reported
        public static int Count
        {
            get { return Issues.Count; }
        }

        public static string AddIssue(Issue issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException("issue");
            }

            ServiceRequest request = ServiceRequestManager.Shared.RegisterIssue(issue);
            issue.ReferenceNumber = request.ReferenceNumber;
            Issues.Add(issue);

            return issue.ReferenceNumber;
        }

        public static IReadOnlyList<Issue> GetAllIssues()
        {
            return Issues.AsReadOnly();
        }

        //Returns issues matching a category
        public static List<Issue> GetIssuesByCategory(string category)
        {
            return Issues.FindAll(i =>
                string.Equals(i.Category, category, StringComparison.OrdinalIgnoreCase));
        }
    }
}
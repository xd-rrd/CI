﻿//-----------------------------------------------------------------------
// <copyright file="Iis7WebsiteTests.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.Tests
{
    using System.Activities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TfsBuildExtensions.Activities.Web;

    /// <summary>
    /// This is a test class for TfsVersionTest and is intended
    /// to contain all TfsVersionTest Unit Tests
    /// </summary>
    [TestClass]
    public class Iis7WebsiteTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }
        
        /// <summary>
        /// A test for GetVersion which makes use of Elapsed time formatting and CombineBuildAndRevision
        /// </summary>
        [TestMethod]
        [DeploymentItem("TfsBuildExtensions.Activities.IIS7.dll")]
        public void CreateWebsite()
        {
            // Initialise Instance
            var target = new Iis7Website { Action = IIS7WebsiteAction.Create, Name = "website1", Path = @"c:\demo" };

            // Create a WorkflowInvoker and add the IBuildDetail Extension
            WorkflowInvoker invoker = new WorkflowInvoker(target);

            var actual = invoker.Invoke();

            ////// Test the result
            ////DateTime d = Convert.ToDateTime("1 Mar 2009");
            ////TimeSpan ts = DateTime.Now - d;
            ////string days = ts.Days.ToString();
            ////Assert.AreEqual("3.1.1" + days + "." + days + "2", actual["Version"].ToString());
        }
    }
}

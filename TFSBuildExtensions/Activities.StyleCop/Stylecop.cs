﻿//-----------------------------------------------------------------------
// <copyright file="StyleCop.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.CodeQuality
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.TeamFoundation.Build.Client;
    using global::StyleCop;

    /// <summary>
    /// Wraps the StyleCopConsole class to provide a mechanism for scanning files for StyleCop compliance.
    /// </summary>
    /// <example>
    /// <code lang="xml"><![CDATA[
    /// <Sequence DisplayName="TFSBuildExtensions StyleCop Sequence" sap:VirtualizedContainerService.HintSize="818,146">
    /// <Sequence.Variables>
    /// <Variable x:TypeArguments="x:Int32" Name="StyleCopViolationCount" />
    /// </Sequence.Variables>
    /// <tac1:StyleCop AdditionalAddInPaths="{x:Null}" CacheResults="{x:Null}" FailBuildOnError="{x:Null}" MaximumViolationCount="{x:Null}" Succeeded="{x:Null}" TreatViolationsErrorsAsWarnings="{x:Null}" TreatWarningsAsErrors="{x:Null}" ForceFullAnalysis="True" sap:VirtualizedContainerService.HintSize="200,22" LogExceptionStack="True" LogFile="[SourcesDirectory + &quot;\StyleCopLog.txt&quot;]" SettingsFile="[SourcesDirectory + &quot;\Settings.StyleCop&quot;]" ShowOutput="True" SourceFiles="[New String() {SourcesDirectory}]" ViolationCount="[StyleCopViolationCount]" XmlOutputFile="[SourcesDirectory + &quot;\StyleCopviolations.xml&quot;]" />
    /// </Sequence>
    /// ]]></code>    
    /// </example>
    [BuildActivity(HostEnvironmentOption.Agent)]
    public sealed class StyleCop : BaseCodeActivity
    {
        /// <summary>
        /// The default maximum number of violations that can be discovered
        /// </summary>
        private const int DefaultViolationLimit = 10000;

        /// <summary>
        /// The status of the analysis
        /// </summary>
        private bool exitCode = true;

        /// <summary>
        /// The files that vioaltions encountered
        /// </summary>
        private List<string> violations = new List<string>();

        /// <summary>
        /// The maximum violation count
        /// </summary>
        private int violationLimit;

        /// <summary>
        /// Sets the maximum violation count before scanning is halted.
        /// </summary>
        [Description("Sets the maximum violation count before scanning is halted")]
        public InArgument<int> MaximumViolationCount { get; set; }

        /// <summary>
        /// Gets the number of violations found.
        /// </summary>
        [Description("Gets the number of violations found.")]
        public OutArgument<int> ViolationCount { get; set; }

        /// <summary>
        /// Sets a value indicating whether to show names of files scanned to the build log
        /// </summary>
        [Description("Sets a value indicating whether to show names of files scanned to the build log")]
        public InArgument<bool> ShowOutput { get; set; }

        /// <summary>
        /// Gets whether the scan succeeded.
        /// </summary>
        [Description("Gets whether the scan succeeded.")]
        public OutArgument<bool> Succeeded { get; set; }

        /// <summary>
        /// Sets a value indicating whether StyleCop should write cache files to disk after performing an analysis. Default is false.
        /// </summary>
        [Description("Sets a value indicating whether StyleCop should write cache files to disk after performing an analysis. Default is false.")]
        public InArgument<bool> CacheResults { get; set; }

        /// <summary>
        /// Sets a value indicating whether StyleCop should ignore cached results and perform a clean analysis. 
        /// </summary>
        [Description("Sets a value indicating whether StyleCop should ignore cached results and perform a clean analysis. ")]
        public InArgument<bool> ForceFullAnalysis { get; set; }

        /// <summary>
        /// Sets the name for the XML log file produced by the StyleCop runner
        /// </summary>
        [Description("Sets the name for the XML log file produced by the StyleCop runner")]
        public InArgument<string> XmlOutputFile { get; set; }

        /// <summary>
        /// Sets the text log file that list the violation 
        /// </summary>
        [Description("Sets the text log file that list the violation ")]
        public InArgument<string> LogFile { get; set; }

        /// <summary>
        /// Sets the source files path or list of specific files
        /// </summary>
        [RequiredArgument]
        [Description("Sets the source files path or list of specific files")]
        public InArgument<string[]> SourceFiles { get; set; }

        /// <summary>
        /// Sets the path to the settings file that defines the rules
        /// </summary>
        [RequiredArgument]
        [Description("Sets the path to the settings file that defines the rules")]
        public InArgument<string> SettingsFile { get; set; }

        /// <summary>
        /// Set the location of any custom addins
        /// </summary>
        [Description("Set the location of any custom addins")]
        public InArgument<string[]> AdditionalAddInPaths { get; set; }

        /// <summary>
        /// Set to true to treat all stylecop violations as warnings
        /// </summary>
        [Description("Set to true to treat all stylecop violations as warnings")]
        public InArgument<bool> TreatViolationsErrorsAsWarnings { get; set; }

        /// <summary>
        /// Executes the logic for this workflow activity
        /// </summary>
        protected override void InternalExecute()
        {
            this.Scan();
        }

        private void Scan()
        {
            // Clear the violation count and set the violation limit for the project.
            this.violations = new List<string>();
            this.violationLimit = 0;

            if (this.MaximumViolationCount.Get(this.ActivityContext) != 0)
            {
                this.violationLimit = this.MaximumViolationCount.Get(this.ActivityContext);
            }

            if (this.violationLimit == 0)
            {
                this.violationLimit = DefaultViolationLimit;
            }

            // Get settings files (if null or empty use null filename so it uses default).
            string settingsFileName = string.Empty;
            if (string.IsNullOrEmpty(this.SettingsFile.Get(this.ActivityContext)) == false)
            {
                settingsFileName = this.SettingsFile.Get(this.ActivityContext);
            }

            // Get addin paths.
            List<string> addinPaths = new List<string>();
            if (this.AdditionalAddInPaths.Get(this.ActivityContext) != null)
            {
                addinPaths.AddRange(this.AdditionalAddInPaths.Get(this.ActivityContext));
            }

            // Create the StyleCop console. But do not initialise the addins as this can cause modal dialogs to be shown on errors
            var console = new StyleCopConsole(settingsFileName, this.CacheResults.Get(this.ActivityContext), this.XmlOutputFile.Get(this.ActivityContext), null, false);

            // make sure the UI is not dispayed on error
            console.Core.DisplayUI = false;

            // declare the add-ins to load
            console.Core.Initialize(addinPaths, true);

            // Create the configuration.
            Configuration configuration = new Configuration(new string[0]);

            // Create a CodeProject object for these files. we use a time stamp for the key and the current directory for the cache location
            CodeProject project = new CodeProject(DateTime.Now.ToLongTimeString().GetHashCode(), @".\", configuration);

            // Add each source file to this project.
            if (this.SourceFiles.Get(this.ActivityContext) != null)
            {
                foreach (var inputSourceLocation in this.SourceFiles.Get(this.ActivityContext))
                {
                    // could be a path or a file
                    if (System.IO.File.Exists(inputSourceLocation))
                    {
                        if (this.ShowOutput.Get(this.ActivityContext))
                        {
                            this.LogBuildMessage(string.Format(CultureInfo.CurrentCulture, "Adding file to check [{0}", inputSourceLocation) + "]", BuildMessageImportance.Low);
                        }

                        console.Core.Environment.AddSourceCode(project, inputSourceLocation, null);
                    }
                    else if (System.IO.Directory.Exists(inputSourceLocation))
                    {
                        foreach (var fileInDirectory in System.IO.Directory.GetFiles(inputSourceLocation, "*.cs", SearchOption.AllDirectories))
                        {
                            if (this.ShowOutput.Get(this.ActivityContext))
                            {
                                this.LogBuildMessage(string.Format(CultureInfo.CurrentCulture, "Adding file to check [{0}", fileInDirectory) + "]", BuildMessageImportance.Low);
                            }

                            console.Core.Environment.AddSourceCode(project, fileInDirectory, null);
                        }
                    }
                    else
                    {
                        this.LogBuildMessage(string.Format(CultureInfo.CurrentCulture, "Cannot add file to check [{0}", inputSourceLocation) + "]", BuildMessageImportance.Low);
                    }
                }

                try
                {
                    // Subscribe to events
                    console.OutputGenerated += this.OnOutputGenerated;
                    console.ViolationEncountered += this.OnViolationEncountered;

                    // Analyze the source files
                    CodeProject[] projects = new[] { project };
                    console.Start(projects, this.ForceFullAnalysis.Get(this.ActivityContext));
                }
                finally
                {
                    // Unsubscribe from events
                    console.OutputGenerated -= this.OnOutputGenerated;
                    console.ViolationEncountered -= this.OnViolationEncountered;
                }
            }

            // log the results to disk as a simple list if there have been failures AND LogFile is specified
            if (string.IsNullOrEmpty(this.LogFile.Get(this.ActivityContext)) == false && this.exitCode == false)
            {
                using (StreamWriter streamWriter = new StreamWriter(this.LogFile.Get(this.ActivityContext), false, Encoding.UTF8))
                {
                    foreach (string i in this.violations)
                    {
                        streamWriter.WriteLine(i);
                    }
                }
            }

            this.Succeeded.Set(this.ActivityContext, this.exitCode);
            this.ViolationCount.Set(this.ActivityContext, this.violations.Count);
        }

        private void OnOutputGenerated(object sender, OutputEventArgs e)
        {
            lock (this)
            {
                this.LogBuildMessage(e.Output.Trim());
            }
        }

        private void OnViolationEncountered(object sender, ViolationEventArgs e)
        {
            if (this.violationLimit < 0 || this.violations.Count < this.violationLimit)
            {
                // Does the violation qualify for breaking the build?
                if (!(e.Warning || this.TreatViolationsErrorsAsWarnings.Get(this.ActivityContext)))
                {
                    this.exitCode = false;
                }

                string file = string.Empty;
                if (e.SourceCode != null && !string.IsNullOrEmpty(e.SourceCode.Path))
                {
                    file = e.SourceCode.Path;
                }
                else if (e.Element != null &&
                    e.Element.Document != null &&
                    e.Element.Document.SourceCode != null &&
                    e.Element.Document.SourceCode.Path != null)
                {
                    file = e.Element.Document.SourceCode.Path;
                }

                file += string.Format(CultureInfo.CurrentUICulture, ". LineNumber: {0}, ", e.LineNumber.ToString(CultureInfo.CurrentCulture));
                file += string.Format(CultureInfo.CurrentUICulture, "CheckId: {0}, ", e.Violation.Rule.CheckId ?? string.Empty);
                file += string.Format(CultureInfo.CurrentUICulture, "Message: {0}, ", e.Message);
                this.violations.Add(file);

                // Prepend the rule check-id to the message.
                string message = string.Concat(e.Violation.Rule.CheckId ?? "NoRuleCheckId", ": ", e.Message);

                lock (this)
                {
                    if (e.Warning || this.TreatViolationsErrorsAsWarnings.Get(this.ActivityContext))
                    {
                        this.LogBuildWarning(string.Format(CultureInfo.CurrentCulture, "{0} [{1}] Line {2}", message, file, e.LineNumber));
                    }
                    else
                    {
                        this.LogBuildError(string.Format(CultureInfo.CurrentCulture, "{0} [{1}] Line {2}", message, file, e.LineNumber));
                    }
                }
            }
        }
    }
}
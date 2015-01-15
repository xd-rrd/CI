﻿//-----------------------------------------------------------------------
// <copyright file="TfsVersion.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.TeamFoundationServer
{
    using System;
    using System.Activities;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.TeamFoundation.Build.Client;
    using TfsBuildExtensions.Activities;

    /// <summary>
    /// TfsVersionAction
    /// </summary>
    public enum TfsVersionAction
    {
        /// <summary>
        /// GetVersion
        /// </summary>
        GetVersion,

        /// <summary>
        /// SetVersion
        /// </summary>
        SetVersion,

        /// <summary>
        /// GetAndSetVersion
        /// </summary>
        GetAndSetVersion
    }

    /// <summary>
    /// TfsVersionVersionFormat
    /// </summary>
    public enum TfsVersionVersionFormat
    {
        /// <summary>
        /// DateTime
        /// </summary>
        DateTime,

        /// <summary>
        /// Elapsed
        /// </summary>
        Elapsed,

        /// <summary>
        /// Synced
        /// </summary>
        Synced
    }

    /// <summary>
    /// TfsVersion
    /// </summary>
    [BuildActivity(HostEnvironmentOption.All)]
    public sealed class TfsVersion : BaseCodeActivity
    {
        private const string AppendAssemblyVersionFormat = "\n[assembly: System.Reflection.AssemblyVersion(\"{0}\")]";
        private const string VBAppendAssemblyVersionFormat = "\n<assembly: System.Reflection.AssemblyVersion(\"{0}\")>";
        private const string AppendAssemblyFileVersionFormat = "\n[assembly: System.Reflection.AssemblyFileVersion(\"{0}\")]";
        private const string VBAppendAssemblyFileVersionFormat = "\n<assembly: System.Reflection.AssemblyFileVersion(\"{0}\")>";
        private const string AppendAssemblyInformationalVersionFormat = "\n[assembly: System.Reflection.AssemblyInformationalVersion(\"{0}\")]";
        private const string VBAppendAssemblyInformationalVersionFormat = "\n<assembly: System.Reflection.AssemblyInformationalVersion(\"{0}\")>";
        private const string AppendAssemblyDescriptionFormat = "\n[assembly: System.Reflection.AssemblyDescription(\"{0}\")]";
        private const string VBAppendAssemblyDescriptionFormat = "\n<assembly: System.Reflection.AssemblyDescription(\"{0}\")>";
        private Regex regexExpression;
        private Regex regexAssemblyVersion;
        private Regex regexAssemblyInfomationalVersion;
        private Regex regexNugetVersion;
        private Regex regexAssemblyDescription;
        private Encoding fileEncoding = Encoding.UTF8;
        private bool setAssemblyFileVersion = true;
        private bool setNuSpecVersion = true;
        private TfsVersionAction action = TfsVersionAction.GetAndSetVersion;
        private InArgument<string> delimiter = ".";
        private TfsVersionVersionFormat versionFormat = TfsVersionVersionFormat.Synced;
        private string buildnumberRegex = @"\d+\.\d+\.\d+\.\d+";

        /// <summary>
        /// Set to True to set the AssemblyDescription when calling SetVersion. Default is false.
        /// </summary>
        public bool SetAssemblyDescription { get; set; }

        /// <summary>
        /// Specifies the action to perform
        /// </summary>
        public TfsVersionAction Action
        {
            get { return this.action; }
            set { this.action = value; }
        }

        /// <summary>
        /// Set to True to set the AssemblyVersion when calling SetVersion. Default is false.
        /// </summary>
        public bool SetAssemblyVersion { get; set; }

        /// <summary>
        /// Set to True to get the elapsed calculation using UTC Date Time. Default is false
        /// </summary>
        public bool UseUtcDate { get; set; }

        /// <summary>
        /// Set to True to set the AssemblyFileVersion when calling SetVersion. Default is true.
        /// </summary>
        public bool SetAssemblyFileVersion
        {
            get { return this.setAssemblyFileVersion; }
            set { this.setAssemblyFileVersion = value; }
        }

        /// <summary>
        /// Set to True to set the AssemblyInformationalVersion when calling SetVersion. Default is false.
        /// </summary>
        public bool SetAssemblyInformationalVersion { get; set; }

        /// <summary>
        /// Set to True to set the SetNuSpecVersion when calling SetVersion. Default is true.
        /// </summary>
        public bool SetNuSpecVersion
        {
            get { return this.setNuSpecVersion; }
            set { this.setNuSpecVersion = value; }
        }

        /// <summary>
        /// Set to true to force SetVersion action to update files that do not have AssemblyVersion | AssemblyFileVersion 
        /// | AssemblyDescription present.  Default is false.  ForceSetVersion does not affect AssemblyVersion when 
        /// SetAssemblyVersion is false or AssemblyDescription when SetAssemblyDescription is false.
        /// </summary>
        public bool ForceSetVersion { get; set; }

        /// <summary>
        /// Sets the file encoding. Default is UTF8
        /// </summary>
        public InArgument<string> TextEncoding { get; set; }

        /// <summary>
        /// Sets the files to version. If Files is not provided, all AssemblyInfo.* files under the SourceDirectory are versioned.
        /// </summary>
        public InArgument<IEnumerable<string>> Files { get; set; }

        /// <summary>
        /// Gets or Sets the Version
        /// </summary>
        public InOutArgument<string> Version { get; set; }

        /// <summary>
        /// Gets or Sets the AssemblyDescription.
        /// </summary>
        public InArgument<string> AssemblyDescription { get; set; }

        /// <summary>
        /// Sets the AssemblyVersion. Defaults to Version if not set.
        /// </summary>
        public InArgument<string> AssemblyVersion { get; set; }

        /// <summary>
        /// Sets the AssemblyInformationalVersion. Defaults to Version if not set.
        /// </summary>
        public InArgument<string> AssemblyInformationalVersion { get; set; }

        /// <summary>
        /// Sets the number of padding digits to use, e.g. 4
        /// </summary>
        public int PaddingCount { get; set; }

        /// <summary>
        /// Sets the padding digit to use, e.g. 0
        /// </summary>
        public char PaddingDigit { get; set; }

        /// <summary>
        /// Sets the start date to use when using VersionFormat="Elapsed"
        /// </summary>
        public InArgument<DateTime> StartDate { get; set; }

        /// <summary>
        /// Sets the date format to use when using VersionFormat="DateTime". e.g. MMdd
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Sets the BuildNumberRegex to determine the verison number from the BuildNumber when using in Synced mode. Default is \d+\.\d+\.\d+\.\d+
        /// </summary>
        public string BuildNumberRegex
        {
            get { return this.buildnumberRegex; }
            set { this.buildnumberRegex = value; }
        }

        /// <summary>
        /// Sets the Version Format. Valid VersionFormats are Elapsed, DateTime, Synced. Default is Synced
        /// </summary>
        public TfsVersionVersionFormat VersionFormat
        {
            get { return this.versionFormat; }
            set { this.versionFormat = value; }
        }

        /// <summary>
        /// Sets the minor version
        /// </summary>
        public InOutArgument<string> Minor { get; set; }

        /// <summary>
        /// Sets the major version
        /// </summary>
        public InOutArgument<string> Major { get; set; }

        /// <summary>
        /// Gets or Sets the Build version
        /// </summary>
        public InOutArgument<string> Build { get; set; }

        /// <summary>
        /// Gets or Sets the Revision version
        /// </summary>
        public InOutArgument<string> Revision { get; set; }

        /// <summary>
        /// Sets whether to make the revision a combination of the Build and Revision. Not applicable for VersionFormat.Synced.
        /// </summary>
        public bool CombineBuildAndRevision { get; set; }

        /// <summary>
        /// Sets the Delimiter to use in the version number. Default is .
        /// </summary>
        public InArgument<string> Delimiter
        {
            get { return this.delimiter; }
            set { this.delimiter = value; }
        }

        /// <summary>
        /// Specify the format of the build number. A format for each part must be specified or left blank, e.g. "00.000.00.000", "..0000.0"
        /// </summary>
        public InArgument<string> VersionTemplateFormat { get; set; }

        /// <summary>
        /// Executes the logic for this workflow activity
        /// </summary>
        protected override void InternalExecute()
        {
            switch (this.Action)
            {
                case TfsVersionAction.GetVersion:
                    this.GetVersion();
                    break;
                case TfsVersionAction.SetVersion:
                    this.SetVersion();
                    break;
                case TfsVersionAction.GetAndSetVersion:
                    this.GetVersion();
                    this.SetVersion();
                    break;
                default:
                    throw new ArgumentException("Action not supported");
            }
        }
        
        private static System.Collections.Generic.IEnumerable<string> Getfiles(FileInfo[] files)
        {
            string[] foundfiles = new string[files.Length];
            int i = 0;
            foreach (FileInfo f in files)
            {
                foundfiles[i] = f.FullName;
                i++;
            }

            return foundfiles;
        }

        private void GetVersion()
        {
            string tfsBuildNumber = this.ActivityContext.GetExtension<IBuildDetail>().BuildNumber;
            this.LogBuildMessage("Getting Version");
            IBuildDetail b = this.ActivityContext.GetExtension<IBuildDetail>();
            if (this.VersionFormat == TfsVersionVersionFormat.Synced)
            {
                Regex r = new Regex(this.BuildNumberRegex, RegexOptions.Compiled);
                var s = r.Match(b.BuildNumber).Value;
                this.ActivityContext.SetValue(this.Version, s);
            }
            else
            {
                if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Major)))
                {
                    this.LogBuildError("Major is required");
                    return;
                }

                if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Minor)))
                {
                    this.LogBuildError("Minor is required");
                    return;
                }

                string buildname = b.BuildDefinition.Name;
                DateTime t = b.StartTime;
                string buildstring = tfsBuildNumber.Replace(buildname + "_", string.Empty);
                string[] buildParts = buildstring.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                DateTime baseTimeToUse = this.UseUtcDate ? DateTime.UtcNow : DateTime.Now;

                if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Revision)))
                {
                    if (this.CombineBuildAndRevision)
                    {
                        switch (this.VersionFormat)
                        {
                            case TfsVersionVersionFormat.Elapsed:
                                TimeSpan elapsed = baseTimeToUse - Convert.ToDateTime(this.ActivityContext.GetValue(this.StartDate));
                                this.ActivityContext.SetValue(this.Revision, elapsed.Days.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit) + buildParts[buildParts.Length - 1]);
                                break;
                            case TfsVersionVersionFormat.DateTime:
                                this.ActivityContext.SetValue(this.Revision, t.ToString(this.DateFormat, CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit) + buildParts[buildParts.Length - 1]);
                                break;
                        }
                    }
                    else
                    {
                        this.ActivityContext.SetValue(this.Revision, buildParts[buildParts.Length - 1]);
                    }
                }

                switch (this.VersionFormat)
                {
                    case TfsVersionVersionFormat.Elapsed:
                        TimeSpan elapsed = baseTimeToUse - Convert.ToDateTime(this.ActivityContext.GetValue(this.StartDate));
                        if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Build)))
                        {
                            this.ActivityContext.SetValue(this.Build, elapsed.Days.ToString(CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit));
                        }

                        this.ActivityContext.SetValue(this.Version, string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", this.ActivityContext.GetValue(this.Major), this.ActivityContext.GetValue(this.Minor), this.ActivityContext.GetValue(this.Build), this.ActivityContext.GetValue(this.Revision), this.ActivityContext.GetValue(this.Delimiter)));
                        break;
                    case TfsVersionVersionFormat.DateTime:
                        if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Build)))
                        {
                            this.ActivityContext.SetValue(this.Build, t.ToString(this.DateFormat, CultureInfo.CurrentCulture).PadLeft(this.PaddingCount, this.PaddingDigit));
                        }

                        this.ActivityContext.SetValue(this.Version, string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", this.ActivityContext.GetValue(this.Major), this.ActivityContext.GetValue(this.Minor), this.ActivityContext.GetValue(this.Build), this.ActivityContext.GetValue(this.Revision), this.ActivityContext.GetValue(this.Delimiter)));
                        break;
                }

                // Check if format is provided
                if (!string.IsNullOrEmpty(this.ActivityContext.GetValue(this.VersionTemplateFormat)))
                {
                    // get the current version number parts
                    int[] buildparts = this.ActivityContext.GetValue(this.Version).Split(char.Parse(this.ActivityContext.GetValue(this.Delimiter))).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();

                    // get the format parts
                    string[] formatparts = this.ActivityContext.GetValue(this.VersionTemplateFormat).Split(char.Parse(this.ActivityContext.GetValue(this.Delimiter)));

                    // format each part
                    string[] newparts = new string[4];
                    for (int i = 0; i <= 3; i++)
                    {
                        newparts[i] = buildparts[i].ToString(formatparts[i], CultureInfo.InvariantCulture);
                    }

                    this.ActivityContext.SetValue(this.Major, newparts[0]);
                    this.ActivityContext.SetValue(this.Minor, newparts[1]);
                    this.ActivityContext.SetValue(this.Build, newparts[2]);
                    this.ActivityContext.SetValue(this.Revision, newparts[3]);

                    // reset the version to the required format);
                    this.ActivityContext.SetValue(this.Version, string.Format(CultureInfo.CurrentCulture, "{0}{4}{1}{4}{2}{4}{3}", newparts[0], newparts[1], newparts[2], newparts[3], this.ActivityContext.GetValue(this.Delimiter)));
                }
            }
        }

        private void SetVersion()
        {
            // Set the file encoding if necessary
            if (!string.IsNullOrEmpty(this.ActivityContext.GetValue(this.TextEncoding)) && !this.SetFileEncoding())
            {
                return;
            }

            if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.Version)))
            {
                this.LogBuildError("Version is required");
                return;
            }

            if (this.ActivityContext.GetValue(this.Files) == null)
            {
                var var1Prop = this.ActivityContext.DataContext.GetProperties()["SourcesDirectory"];
                var var1Text = var1Prop.GetValue(this.ActivityContext.DataContext) as string;
                DirectoryInfo d = new DirectoryInfo(var1Text);
                FileInfo[] tempfiles = d.GetFiles("AssemblyInfo.*", SearchOption.AllDirectories);
                this.ActivityContext.SetValue(this.Files, Getfiles(tempfiles));
            }

            if (string.IsNullOrEmpty(this.ActivityContext.GetValue(this.AssemblyVersion)))
            {
                this.ActivityContext.SetValue(this.AssemblyVersion, this.ActivityContext.GetValue(this.Version));
            }

            // Load the regex to use
            this.regexExpression = new Regex(@"AssemblyFileVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            if (this.SetAssemblyVersion)
            {
                this.regexAssemblyVersion = new Regex(@"AssemblyVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            }

            if (this.SetAssemblyInformationalVersion)
            {
                this.regexAssemblyInfomationalVersion = new Regex(@"AssemblyInformationalVersion.*\(.*""" + ".*" + @""".*\)", RegexOptions.Compiled);
            }

            if (this.SetAssemblyDescription)
            {
                this.regexAssemblyDescription = new Regex(@"AssemblyDescription.*\(.*\)", RegexOptions.Compiled);
            }
            
            if (this.SetNuSpecVersion)
            {
                this.regexNugetVersion = new Regex(@"<version>.*</version>", RegexOptions.Compiled);
            }
            
            foreach (string fullfilename in this.ActivityContext.GetValue(this.Files))
            {
                FileInfo file = new FileInfo(fullfilename);
                this.LogBuildMessage(string.Format(CultureInfo.CurrentCulture, "Versioning {0} at {1}", file.FullName, this.ActivityContext.GetValue(this.Version)));
                bool changedAttribute = false;

                // First make sure the file is writable.
                FileAttributes fileAttributes = File.GetAttributes(file.FullName);

                // If readonly attribute is set, reset it.
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    this.LogBuildMessage("Making file writable", BuildMessageImportance.Low);
                    File.SetAttributes(file.FullName, fileAttributes ^ FileAttributes.ReadOnly);
                    changedAttribute = true;
                }

                // Open the file
                string entireFile;
                using (StreamReader streamReader = new StreamReader(file.FullName, true))
                {
                    entireFile = streamReader.ReadToEnd();
                }

                // Parse the entire file.
                string newFile = this.regexExpression.Replace(entireFile, string.Format(@"AssemblyFileVersion(""{0}"")", this.ActivityContext.GetValue(this.Version)));

                if (this.SetAssemblyFileVersion)
                {
                    if (this.ForceSetVersion && newFile.Equals(entireFile, StringComparison.OrdinalIgnoreCase) && newFile.IndexOf("AssemblyFileVersion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        switch (file.Extension)
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyFileVersionFormat, this.ActivityContext.GetValue(this.Version));
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyFileVersionFormat, this.ActivityContext.GetValue(this.Version));
                                break;
                        }
                    }
                }

                // Replace the version number in the NuSpec File
                if (this.SetNuSpecVersion)
                {
                    switch (file.Extension)
                    {
                        case ".nuspec":
                            newFile = this.regexNugetVersion.Replace(newFile, string.Format(@"<version>{0}</version>", this.ActivityContext.GetValue(this.Version)));
                            break;
                    }
                }

                if (this.SetAssemblyInformationalVersion)
                {
                    string originalFile = newFile;
                    newFile = this.regexAssemblyInfomationalVersion.Replace(newFile, string.Format(@"AssemblyInformationalVersion(""{0}"")", this.ActivityContext.GetValue(this.AssemblyInformationalVersion)));
                    if (this.ForceSetVersion && newFile.Equals(originalFile, StringComparison.OrdinalIgnoreCase) && newFile.IndexOf("AssemblyInformationalVersion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        switch (file.Extension)
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyInformationalVersionFormat, this.ActivityContext.GetValue(this.AssemblyInformationalVersion));
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyInformationalVersionFormat, this.ActivityContext.GetValue(this.AssemblyInformationalVersion));
                                break;
                        }
                    }
                }

                if (this.SetAssemblyVersion)
                {
                    string originalFile = newFile;
                    newFile = this.regexAssemblyVersion.Replace(newFile, string.Format(@"AssemblyVersion(""{0}"")", this.ActivityContext.GetValue(this.AssemblyVersion)));
                    if (this.ForceSetVersion && newFile.Equals(originalFile, StringComparison.OrdinalIgnoreCase) && newFile.IndexOf("AssemblyVersion", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        switch (file.Extension)
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyVersionFormat, this.ActivityContext.GetValue(this.AssemblyVersion));
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyVersionFormat, this.ActivityContext.GetValue(this.AssemblyVersion));
                                break;
                        }
                    }
                }

                if (this.SetAssemblyDescription)
                {
                    string originalFile = newFile;
                    newFile = this.regexAssemblyDescription.Replace(newFile, string.Format(@"AssemblyDescription(""{0}"")", this.ActivityContext.GetValue(this.AssemblyDescription)));
                    if (this.ForceSetVersion && newFile.Equals(originalFile, StringComparison.OrdinalIgnoreCase))
                    {
                        switch (file.Extension)
                        {
                            case ".cs":
                                newFile = newFile.AppendFormat(AppendAssemblyDescriptionFormat, this.ActivityContext.GetValue(this.AssemblyDescription));
                                break;
                            case ".vb":
                                newFile = newFile.AppendFormat(VBAppendAssemblyDescriptionFormat, this.ActivityContext.GetValue(this.AssemblyDescription));
                                break;
                        }
                    }
                }

                // Write out the new contents.
                using (StreamWriter streamWriter = new StreamWriter(file.FullName, false, this.fileEncoding))
                {
                    streamWriter.Write(newFile);
                }

                if (changedAttribute)
                {
                    this.LogBuildMessage("Making file readonly", BuildMessageImportance.Low);
                    File.SetAttributes(file.FullName, FileAttributes.ReadOnly);
                }
            }
        }

        /// <summary>
        /// Sets the file encoding.
        /// </summary>
        /// <returns>bool</returns>
        private bool SetFileEncoding()
        {
            switch (this.ActivityContext.GetValue(this.TextEncoding))
            {
                case "ASCII":
                    this.fileEncoding = System.Text.Encoding.ASCII;
                    break;
                case "Unicode":
                    this.fileEncoding = System.Text.Encoding.Unicode;
                    break;
                case "UTF7":
                    this.fileEncoding = System.Text.Encoding.UTF7;
                    break;
                case "UTF8":
                    this.fileEncoding = System.Text.Encoding.UTF8;
                    break;
                case "BigEndianUnicode":
                    this.fileEncoding = System.Text.Encoding.BigEndianUnicode;
                    break;
                case "UTF32":
                    this.fileEncoding = System.Text.Encoding.UTF32;
                    break;
                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Encoding not supported: {0}", this.ActivityContext.GetValue(this.TextEncoding)));
            }

            return true;
        }
    }
}
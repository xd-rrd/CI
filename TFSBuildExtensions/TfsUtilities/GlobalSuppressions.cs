﻿//-----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.WebAccess.#GetTswaLinkingService(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.WIT.#GetWorkItemById(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.WebAccess.#GetHyperlinkService(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.WorkItems.#GetWorkItemById(Microsoft.TeamFoundation.Client.TfsTeamProjectCollection,System.Int32)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.TfsHttpMessageHandler.#ExpectContinue")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.HttpClientExtensions+DownloadStream.#ValidateHash()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "TfsBuildExtensions.TfsUtilities.HttpClientExtensions+DownloadStream.#Transform(System.Byte[],System.Int32,System.Int32,System.Byte[]&,System.Int32&,System.Int32&)")]
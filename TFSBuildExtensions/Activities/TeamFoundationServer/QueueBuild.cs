﻿//-----------------------------------------------------------------------
// <copyright file="QueueBuild.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.TeamFoundationServer
{
    using System;
    using System.Activities;
    using System.ComponentModel;
    using Microsoft.TeamFoundation.Build.Client;

    /// <summary>
    /// Workflow activity that queues a new build with the specified build definition
    /// and TFS Build Server.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.All)]
    public sealed class QueueBuild : BaseCodeActivity<IQueuedBuild>
    {
        /// <summary>
        /// The <see cref="Microsoft.TeamFoundation.Build.Client.IBuildServer"/>
        /// object for the Team Foundation Server and Team
        /// Project Collection to use that contains the build definition.
        /// </summary>
        [RequiredArgument]
        [Browsable(true)]
        public InArgument<IBuildServer> BuildServer { get; set; }

        /// <summary>
        /// The <see cref="Microsoft.TeamFoundation.Build.Client.IBuildDefinition"/>
        /// object to use to queue a new build.
        /// </summary>
        [RequiredArgument]
        [Browsable(true)]
        public InArgument<IBuildDefinition> BuildDefinition { get; set; }

        /// <summary>
        /// The build priority to use when placing the new build request on the queue.
        /// </summary>
        /// <remarks>The default value is <see cref="Microsoft.TeamFoundation.Build.Client.QueuePriority.Normal"/>.</remarks>
        [Browsable(true)]
        [DefaultValue(QueuePriority.Normal)]
        public InArgument<QueuePriority> Priority { get; set; }

        /// <summary>
        /// The process parameters to use whenever queuing a new build if you would like to change any
        /// of the parameters from their default values specified in the build definition or in the process
        /// template file.
        /// </summary>
        [Browsable(true)]
        public InArgument<string> ProcessParameters { get; set; }

        /// <summary>
        /// The <see cref="Microsoft.TeamFoundation.Build.Client.IBuildController"/>
        /// object to use to queue a new build if different from the default build controller
        /// as specified in the build definition.
        /// </summary>
        [Browsable(true)]
        public InArgument<IBuildController> BuildController { get; set; }

        /// <summary>
        /// Executes the logic for this workflow activity.
        /// </summary>
        /// <returns>The <see cref="Microsoft.TeamFoundation.Build.Client.IQueuedBuild"/>
        /// object that is returned after queueing the new build.</returns>
        protected override IQueuedBuild InternalExecute()
        {
            IBuildServer buildServer = this.BuildServer.Get(this.ActivityContext);

            IBuildDefinition buildDefinition = this.BuildDefinition.Get(this.ActivityContext);
            IBuildRequest buildRequest = buildServer.CreateBuildRequest(buildDefinition.Uri);

            buildRequest.Priority = this.Priority.Get(this.ActivityContext);

            if (Enum.IsDefined(typeof(QueuePriority), buildRequest.Priority) == false)
            {
                // Set default value to normal, if no value has been passed.
                buildRequest.Priority = QueuePriority.Normal;
            }

            string processParameters = this.ProcessParameters.Get(this.ActivityContext);
            if (!string.IsNullOrEmpty(processParameters))
            {
                buildRequest.ProcessParameters = processParameters;
            }

            IBuildController buildController = this.BuildController.Get(this.ActivityContext);
            if (buildController != null)
            {
                buildRequest.BuildController = buildController;
            }

            IQueuedBuild queuedBuild = buildServer.QueueBuild(buildRequest);

            return queuedBuild;
        }
    }
}

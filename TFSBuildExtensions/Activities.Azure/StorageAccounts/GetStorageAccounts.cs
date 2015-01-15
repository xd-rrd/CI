﻿//-----------------------------------------------------------------------
// <copyright file="GetStorageAccounts.cs">(c) http://TfsBuildExtensions.codeplex.com/. This source is subject to the Microsoft Permissive License. See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx. All other rights reserved.</copyright>
//-----------------------------------------------------------------------
namespace TfsBuildExtensions.Activities.Azure.StorageAccounts
{
    using System.Activities;
    using System.ServiceModel;
    using Microsoft.Samples.WindowsAzure.ServiceManagement;
    using Microsoft.TeamFoundation.Build.Client;

    /// <summary>
    /// Get a list of storage services for a subscription.
    /// </summary>
    [BuildActivity(HostEnvironmentOption.All)]
    public class GetStorageAccounts : BaseAzureActivity
    {
        /// <summary>
        /// Gets or sets the storage service list.
        /// </summary>
        public OutArgument<StorageServiceList> StorageServices { get; set; }

        /// <summary>
        /// Connect to an Azure subscription and obtain a list of storage services.
        /// </summary>
        protected override void AzureExecute()
        {
            try
            {
                StorageServiceList storageServices = this.RetryCall(s => this.Channel.ListStorageServices(s));
                this.StorageServices.Set(this.ActivityContext, storageServices);
            }
            catch (EndpointNotFoundException ex)
            {
                this.LogBuildMessage(ex.Message);
                this.StorageServices.Set(this.ActivityContext, null);
            }
        }
    }
}
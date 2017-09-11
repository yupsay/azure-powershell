﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Management.Automation;
using System.Net;
using Microsoft.Azure.Commands.Insights.OutputClasses;
using Microsoft.Azure.Management.Monitor.Management.Models;

namespace Microsoft.Azure.Commands.Insights.ActionGroups
{
    using System.Collections;
    using System.Linq;

    using Microsoft.Azure.Management.Monitor.Management;

    /// <summary>
    /// Gets an Azure Action Group.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "AzureRmActionGroup", SupportsShouldProcess = true), OutputType(typeof(PSActionGroupResource))]
    public class SetAzureRmActionGroupCommand : ManagementCmdletBase
    {
        private const string ByPropertyName = "ByPropertyName";

        private const string ByInputObject = "ByInputObject";

        #region Cmdlet parameters

        /// <summary>
        /// Gets or sets the resource group parameter.
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The resource group name")]
        [ValidateNotNullOrEmpty]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the action group name parameter.
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The action group name")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the action group short name parameter.
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The action group short name")]
        [ValidateNotNullOrEmpty]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the list of email receivers.
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = true, ValueFromPipelineByPropertyName = true, HelpMessage = "The list of receivers")]
        public List<PSActionGroupReceiverBase> Receiver { get; set; }

        /// <summary>
        /// Gets or sets the DisableGroup flag.
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "Whether or not the action group should be enabled")]
        public SwitchParameter DisableGroup { get; set; }

        /// <summary>
        /// Gets or sets the Tags of the activity log alert resource
        /// </summary>
        [Parameter(ParameterSetName = ByPropertyName, Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "The tags of the action group resource")]
        [ValidateNotNullOrEmpty]
        public IDictionary<string, string> Tags { get; set; }

        [Parameter(ParameterSetName = ByInputObject, Mandatory = true, ValueFromPipeline = true, HelpMessage = "The action group resource")]
        public PSActionGroupResource InputObject { get; set; }

        #endregion

        /// <summary>
        /// Executes the cmdlet.
        /// </summary>
        protected override void ProcessRecordInternal()
        {
            if (
                ShouldProcess(
                    target: string.Format("Add/update action group: {0} from resource group: {1}", this.Name, this.ResourceGroup),
                    action: "Add/update action group"))
            {
                if (ParameterSetName == ByInputObject)
                {
                    this.ResourceGroup = this.InputObject.ResourceGroup;
                    this.ShortName = this.InputObject.GroupShortName;
                    this.DisableGroup = !this.InputObject.Enabled;
                    this.Tags = this.InputObject.Tags;
                    this.Receiver = new List<PSActionGroupReceiverBase>();
                    this.Receiver.AddRange(this.InputObject.EmailReceivers);
                    this.Receiver.AddRange(this.InputObject.SmsReceivers);
                    this.Receiver.AddRange(this.InputObject.WebhookReceivers);
                }

                IList<EmailReceiver> emailReceivers =
                    this.Receiver.OfType<PSEmailReceiver>().
                        Select(o => new EmailReceiver(name: o.Name, emailAddress: o.EmailAddress, status: o.Status)).ToList();
                IList<SmsReceiver> smsReceivers =
                    this.Receiver.OfType<PSSmsReceiver>().
                        Select(o => new SmsReceiver(name: o.Name, countryCode: o.CountryCode, phoneNumber: o.PhoneNumber, status: o.Status)).ToList();
                IList<WebhookReceiver> webhookReceivers =
                    this.Receiver.OfType<PSWebhookReceiver>().
                        Select(o => new WebhookReceiver(name: o.Name, serviceUri: o.ServiceUri)).ToList();
                ActionGroupResource actionGroup = new ActionGroupResource
                                                  {
                                                      Location = "Global",
                                                      GroupShortName = this.ShortName,
                                                      Enabled = !this.DisableGroup.IsPresent || !this.DisableGroup,
                                                      Tags = this.Tags,
                                                      EmailReceivers = emailReceivers,
                                                      SmsReceivers = smsReceivers,
                                                      WebhookReceivers = webhookReceivers
                                                  };

                WriteObject(
                    new PSActionGroupResource(
                        this.MonitorManagementClient.ActionGroups.CreateOrUpdate(
                            resourceGroupName: this.ResourceGroup,
                            actionGroupName: this.Name,
                            actionGroup: actionGroup),
                        this.ResourceGroup));
            }
        }
    }
}

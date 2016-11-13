using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuildAutomation.Models.Release
{

    public class ReleaseEvent
    {
        public string subscriptionId { get; set; }
        public int notificationId { get; set; }
        public string id { get; set; }
        public string eventType { get; set; }
        public string publisherId { get; set; }
        public string scope { get; set; }
        public Message message { get; set; }
        public Detailedmessage detailedMessage { get; set; }
        public Resource resource { get; set; }
        public string resourceVersion { get; set; }
        public Resourcecontainers resourceContainers { get; set; }
        public DateTime createdDate { get; set; }
    }

    public class Message
    {
        public string text { get; set; }
        public string html { get; set; }
        public string markdown { get; set; }
    }

    public class Detailedmessage
    {
        public string text { get; set; }
        public string html { get; set; }
        public string markdown { get; set; }
    }

    public class Resource
    {
        public Environment environment { get; set; }
        public Project project { get; set; }
    }

    public class Environment
    {
        public int id { get; set; }
        public int releaseId { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public Variables variables { get; set; }
        public Predeployapproval[] preDeployApprovals { get; set; }
        public Postdeployapproval[] postDeployApprovals { get; set; }
        public Preapprovalssnapshot preApprovalsSnapshot { get; set; }
        public Postapprovalssnapshot postApprovalsSnapshot { get; set; }
        public Deploystep[] deploySteps { get; set; }
        public int rank { get; set; }
        public int definitionEnvironmentId { get; set; }
        public Environmentoptions environmentOptions { get; set; }
        public object[] demands { get; set; }
        public Condition[] conditions { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public object[] workflowTasks { get; set; }
        public Deployphasessnapshot[] deployPhasesSnapshot { get; set; }
        public Owner owner { get; set; }
        public object[] schedules { get; set; }
        public Release release { get; set; }
        public Releasedefinition releaseDefinition { get; set; }
        public Releasecreatedby releaseCreatedBy { get; set; }
        public string triggerReason { get; set; }
        public float timeToDeploy { get; set; }
    }

    public class Variables
    {
    }

    public class Preapprovalssnapshot
    {
        public object[] approvals { get; set; }
    }

    public class Postapprovalssnapshot
    {
        public object[] approvals { get; set; }
    }

    public class Environmentoptions
    {
        public string emailNotificationType { get; set; }
        public string emailRecipients { get; set; }
        public bool skipArtifactsDownload { get; set; }
        public int timeoutInMinutes { get; set; }
        public bool enableAccessToken { get; set; }
    }

    public class Owner
    {
        public string id { get; set; }
    }

    public class Release
    {
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public _Links _links { get; set; }
    }

    public class _Links
    {
        public Web web { get; set; }
        public Self self { get; set; }
    }

    public class Web
    {
        public string href { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Releasedefinition
    {
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public _Links1 _links { get; set; }
    }

    public class _Links1
    {
        public Web1 web { get; set; }
        public Self1 self { get; set; }
    }

    public class Web1
    {
        public string href { get; set; }
    }

    public class Self1
    {
        public string href { get; set; }
    }

    public class Releasecreatedby
    {
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public class Predeployapproval
    {
        public int id { get; set; }
        public int revision { get; set; }
        public Approver approver { get; set; }
        public Approvedby approvedBy { get; set; }
        public string approvalType { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public string status { get; set; }
        public string comments { get; set; }
        public bool isAutomated { get; set; }
        public bool isNotificationOn { get; set; }
        public int trialNumber { get; set; }
        public int attempt { get; set; }
        public int rank { get; set; }
        public Release1 release { get; set; }
        public Releasedefinition1 releaseDefinition { get; set; }
        public Releaseenvironment releaseEnvironment { get; set; }
    }

    public class Approver
    {
        public string id { get; set; }
    }

    public class Approvedby
    {
        public string id { get; set; }
    }

    public class Release1
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links2 _links { get; set; }
    }

    public class _Links2
    {
    }

    public class Releasedefinition1
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links3 _links { get; set; }
    }

    public class _Links3
    {
    }

    public class Releaseenvironment
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links4 _links { get; set; }
    }

    public class _Links4
    {
    }

    public class Postdeployapproval
    {
        public int id { get; set; }
        public int revision { get; set; }
        public Approver1 approver { get; set; }
        public Approvedby1 approvedBy { get; set; }
        public string approvalType { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public string status { get; set; }
        public string comments { get; set; }
        public bool isAutomated { get; set; }
        public bool isNotificationOn { get; set; }
        public int trialNumber { get; set; }
        public int attempt { get; set; }
        public int rank { get; set; }
        public Release2 release { get; set; }
        public Releasedefinition2 releaseDefinition { get; set; }
        public Releaseenvironment1 releaseEnvironment { get; set; }
    }

    public class Approver1
    {
        public string id { get; set; }
    }

    public class Approvedby1
    {
        public string id { get; set; }
    }

    public class Release2
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links5 _links { get; set; }
    }

    public class _Links5
    {
    }

    public class Releasedefinition2
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links6 _links { get; set; }
    }

    public class _Links6
    {
    }

    public class Releaseenvironment1
    {
        public int id { get; set; }
        public string name { get; set; }
        public _Links7 _links { get; set; }
    }

    public class _Links7
    {
    }

    public class Deploystep
    {
        public int id { get; set; }
        public int deploymentId { get; set; }
        public int attempt { get; set; }
        public string reason { get; set; }
        public string status { get; set; }
        public string operationStatus { get; set; }
        public Releasedeployphas[] releaseDeployPhases { get; set; }
        public Requestedby requestedBy { get; set; }
        public DateTime queuedOn { get; set; }
        public Lastmodifiedby lastModifiedBy { get; set; }
        public DateTime lastModifiedOn { get; set; }
        public bool hasStarted { get; set; }
        public object[] tasks { get; set; }
        public string runPlanId { get; set; }
    }

    public class Requestedby
    {
        public string id { get; set; }
    }

    public class Lastmodifiedby
    {
        public string id { get; set; }
    }

    public class Releasedeployphas
    {
        public int id { get; set; }
        public int rank { get; set; }
        public string phaseType { get; set; }
        public string status { get; set; }
        public string runPlanId { get; set; }
        public Deploymentjob[] deploymentJobs { get; set; }
        public object[] manualInterventions { get; set; }
    }

    public class Deploymentjob
    {
        public Job job { get; set; }
        public Task[] tasks { get; set; }
    }

    public class Job
    {
        public int id { get; set; }
        public string timelineRecordId { get; set; }
        public string name { get; set; }
        public DateTime dateStarted { get; set; }
        public DateTime dateEnded { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }
        public string status { get; set; }
        public int rank { get; set; }
        public object[] issues { get; set; }
        public string agentName { get; set; }
    }

    public class Task
    {
        public int id { get; set; }
        public string timelineRecordId { get; set; }
        public string name { get; set; }
        public DateTime dateStarted { get; set; }
        public DateTime dateEnded { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }
        public string status { get; set; }
        public int rank { get; set; }
        public object[] issues { get; set; }
        public string agentName { get; set; }
    }

    public class Condition
    {
        public string name { get; set; }
        public string conditionType { get; set; }
        public string value { get; set; }
    }

    public class Deployphasessnapshot
    {
        public Deploymentinput deploymentInput { get; set; }
        public int rank { get; set; }
        public string phaseType { get; set; }
        public string name { get; set; }
        public Workflowtask[] workflowTasks { get; set; }
    }

    public class Deploymentinput
    {
        public Parallelexecution parallelExecution { get; set; }
        public bool skipArtifactsDownload { get; set; }
        public int timeoutInMinutes { get; set; }
        public int queueId { get; set; }
        public object[] demands { get; set; }
        public bool enableAccessToken { get; set; }
    }

    public class Parallelexecution
    {
        public string multipliers { get; set; }
        public int maxNumberOfAgents { get; set; }
        public bool continueOnError { get; set; }
        public string parallelExecutionType { get; set; }
    }

    public class Workflowtask
    {
        public string taskId { get; set; }
        public string version { get; set; }
        public string name { get; set; }
        public bool enabled { get; set; }
        public bool alwaysRun { get; set; }
        public bool continueOnError { get; set; }
        public int timeoutInMinutes { get; set; }
        public string definitionType { get; set; }
        public Inputs inputs { get; set; }
    }

    public class Inputs
    {
        public string sourcePath { get; set; }
        public string serverName { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string remotePath { get; set; }
        public string useBinary { get; set; }
        public string excludeFilter { get; set; }
        public string deleteOldFiles { get; set; }
        public string deploymentFilesOnly { get; set; }
    }

    public class Project
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class Resourcecontainers
    {
        public Collection collection { get; set; }
        public Account account { get; set; }
    }

    public class Collection
    {
        public string id { get; set; }
        public string baseUrl { get; set; }
    }

    public class Account
    {
        public string id { get; set; }
        public string baseUrl { get; set; }
    }

}
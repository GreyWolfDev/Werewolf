using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuildAutomation.Models.Build
{

    public class BuildEvent
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
        public _Links _links { get; set; }
        public Plan[] plans { get; set; }
        public int id { get; set; }
        public string buildNumber { get; set; }
        public string status { get; set; }
        public string result { get; set; }
        public DateTime queueTime { get; set; }
        public DateTime startTime { get; set; }
        public DateTime finishTime { get; set; }
        public string url { get; set; }
        public Definition definition { get; set; }
        public int buildNumberRevision { get; set; }
        public Project1 project { get; set; }
        public string uri { get; set; }
        public string sourceBranch { get; set; }
        public string sourceVersion { get; set; }
        public Queue queue { get; set; }
        public string priority { get; set; }
        public string reason { get; set; }
        public Requestedfor requestedFor { get; set; }
        public Requestedby requestedBy { get; set; }
        public DateTime lastChangedDate { get; set; }
        public Lastchangedby lastChangedBy { get; set; }
        public Orchestrationplan orchestrationPlan { get; set; }
        public Logs logs { get; set; }
        public Repository repository { get; set; }
        public bool keepForever { get; set; }
        public bool retainedByRelease { get; set; }
    }

    public class _Links
    {
        public Self self { get; set; }
        public Web web { get; set; }
        public Timeline timeline { get; set; }
    }

    public class Self
    {
        public string href { get; set; }
    }

    public class Web
    {
        public string href { get; set; }
    }

    public class Timeline
    {
        public string href { get; set; }
    }

    public class Definition
    {
        public string path { get; set; }
        public string type { get; set; }
        public int revision { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public Project project { get; set; }
    }

    public class Project
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string state { get; set; }
        public int revision { get; set; }
    }

    public class Project1
    {
        public string id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string state { get; set; }
        public int revision { get; set; }
    }

    public class Queue
    {
        public Pool pool { get; set; }
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Pool
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Requestedfor
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
        public string url { get; set; }
        public string imageUrl { get; set; }
    }

    public class Requestedby
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
        public string url { get; set; }
        public string imageUrl { get; set; }
        public bool isContainer { get; set; }
    }

    public class Lastchangedby
    {
        public string id { get; set; }
        public string displayName { get; set; }
        public string uniqueName { get; set; }
        public string url { get; set; }
        public string imageUrl { get; set; }
    }

    public class Orchestrationplan
    {
        public string planId { get; set; }
    }

    public class Logs
    {
        public int id { get; set; }
        public string type { get; set; }
        public string url { get; set; }
    }

    public class Repository
    {
        public string id { get; set; }
        public string type { get; set; }
        public object clean { get; set; }
        public bool checkoutSubmodules { get; set; }
    }

    public class Plan
    {
        public string planId { get; set; }
    }

    public class Resourcecontainers
    {
        public Collection collection { get; set; }
        public Account account { get; set; }
        public Project2 project { get; set; }
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

    public class Project2
    {
        public string id { get; set; }
        public string baseUrl { get; set; }
    }

}
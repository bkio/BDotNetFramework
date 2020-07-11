/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Compute.v1;
using Google.Apis.Compute.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;
using BCommonUtilities;

namespace BCloudServiceUtilities.VMServices
{
    public class BVMServiceGC : IBVMServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        private readonly string ProjectID;
        private readonly string ZoneName;

        private readonly ServiceAccountCredential Credential;

        private readonly string ProgramUniqueID;

        /// <summary>
        /// 
        /// <para>BVMServiceGC: Parametered Constructor for Managed Service by Google</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ProgramUniqueID"/>           Program Unique ID</para>
        /// <para><paramref name="_ProjectID"/>                 GC Project ID</para>
        /// <para><paramref name="_ZoneName"/>                  GC Compute Engine Zone Name</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BVMServiceGC(
            string _ProgramUniqueID,
            string _ProjectID,
            string _ZoneName,
            Action<string> _ErrorMessageAction = null)
        {
            ProgramUniqueID = _ProgramUniqueID;
            ProjectID = _ProjectID;
            ZoneName = _ZoneName;
            try
            {
                string ApplicationCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                string ApplicationCredentialsPlain = Environment.GetEnvironmentVariable("GOOGLE_PLAIN_CREDENTIALS");
                if (ApplicationCredentials == null && ApplicationCredentialsPlain == null)
                {
                    _ErrorMessageAction?.Invoke("BVMServiceGC->Constructor: GOOGLE_APPLICATION_CREDENTIALS (or GOOGLE_PLAIN_CREDENTIALS) environment variable is not defined.");
                    bInitializationSucceed = false;
                }
                else
                {
                    if (ApplicationCredentials == null)
                    {
                        if (!BUtility.HexDecode(out ApplicationCredentialsPlain, ApplicationCredentialsPlain, _ErrorMessageAction))
                        {
                            throw new Exception("Hex decode operation for application credentials plain has failed.");
                        }
                        Credential = GoogleCredential.FromJson(ApplicationCredentialsPlain)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                ComputeService.Scope.Compute,
                                                ComputeService.Scope.CloudPlatform
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                    }
                    else
                    {
                        using (var Stream = new FileStream(ApplicationCredentials, FileMode.Open, FileAccess.Read))
                        {
                            Credential = GoogleCredential.FromStream(Stream)
                                         .CreateScoped(
                                            new string[]
                                            {
                                                ComputeService.Scope.Compute,
                                                ComputeService.Scope.CloudPlatform
                                            })
                                         .UnderlyingCredential as ServiceAccountCredential;
                        }
                    }

                    if (Credential != null)
                    {
                        bInitializationSucceed = true;
                    }
                    else
                    {
                        bInitializationSucceed = false;
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BVMServiceGC->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
        }

        private ComputeService GetService()
        {
            return new ComputeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = Credential,
                ApplicationName = ProgramUniqueID
            });
        }

        private enum EBVMInstanceAction
        {
            Start,
            Stop
        };

        /// <summary>
        ///
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <para>Check <seealso cref="IBTracingServiceInterface.HasInitializationSucceed"/> for detailed documentation</para>
        ///
        /// </summary>
        public bool HasInitializationSucceed()
        {
            return bInitializationSucceed;
        }

        private InstanceList GetInstanceList(Action<string> _ErrorMessageAction = null)
        {
            InstanceList RequestedList = null;
            try
            {
                using (var Service = GetService())
                {
                    var Request = Service.Instances.List(ProjectID, ZoneName);
                    RequestedList = Request.Execute();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BVMServiceGC->GetInstanceList: " + e.Message + ", Trace: " + e.StackTrace);
                return null;
            }
            return RequestedList;
        }

        private Instance FindInstanceByUniqueName(string _UniqueName, Action<string> _ErrorMessageAction = null)
        {
            InstanceList RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Items != null && RequestedList.Items.Count > 0)
            {
                foreach (var Current in RequestedList.Items)
                {
                    if (Current != null && Current.Name == _UniqueName)
                    {
                        return Current;
                    }
                }
            }
            return null;
        }

        public JObject ListInstances(
            Action<string> _ErrorMessageAction = null)
        {
            JObject Result = new JObject();

            InstanceList RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Items != null && RequestedList.Items.Count > 0)
            {
                foreach (var Current in RequestedList.Items)
                {
                    if (Current != null)
                    {
                        Result[Current.Name] = new JObject()
                        {
                            ["UniqueID"] = Current.Id.HasValue ? Current.Id.Value.ToString() : "",
                            ["Status"] = Current.Status,
                            ["Zone"] = Current.Zone,
                            ["Kind"] = Current.Kind,
                            ["bDeletionProtection"] = (Current.DeletionProtection ?? false),
                            ["CreationTimestamp"] = Current.CreationTimestamp,
                            ["Description"] = Current.Description ?? "",
                            ["ETag"] = Current.ETag ?? ""
                        };

                        Result[Current.Name]["Disks"] = new JArray();
                        if (Current.Disks != null)
                        {
                            foreach (var Disk in Current.Disks)
                            {
                                if (Disk != null)
                                {
                                    JObject DiskObject = new JObject()
                                    {
                                        ["bAutoDelete"] = Disk.AutoDelete ?? false,
                                        ["Kind"] = Disk.Kind,
                                        ["DeviceName"] = Disk.DeviceName,
                                        ["bIsBootType"] = Disk.Boot ?? false,
                                        ["bReadOnly"] = Disk.Mode == "READ_ONLY",
                                        ["ETag"] = Disk.ETag ?? ""
                                    };
                                    (Result[Current.Name]["Disks"] as JArray).Add(DiskObject);
                                }
                            }
                        }

                        Result[Current.Name]["Labels"] = new JObject();
                        if (Current.Labels != null)
                        {
                            foreach (var Label in Current.Labels)
                            {
                                (Result[Current.Name]["Labels"] as JObject)[Label.Key] = Label.Value;
                            }
                        }

                        Result[Current.Name]["Tags"] = new JArray();
                        if (Current.Tags != null)
                        {
                            foreach (var Tag in Current.Tags.Items)
                            {
                                (Result[Current.Name]["Tags"] as JArray).Add(Tag);
                            }
                        }

                        Result[Current.Name]["NetworkInterfaces"] = new JArray();
                        if (Current.NetworkInterfaces != null)
                        {
                            foreach (var NetworkInterface in Current.NetworkInterfaces)
                            {
                                if (NetworkInterface != null)
                                {
                                    JObject NetworkInterfaceObject = new JObject()
                                    {
                                        ["UniqueName"] = NetworkInterface.Name,
                                        ["Kind"] = NetworkInterface.Kind,
                                        ["Network"] = NetworkInterface.Network,
                                        ["NetworkIP"] = NetworkInterface.NetworkIP,
                                        ["Subnetwork"] = NetworkInterface.Subnetwork,
                                        ["ETag"] = NetworkInterface.ETag ?? ""
                                    };
                                    if (NetworkInterface.AccessConfigs != null && NetworkInterface.AccessConfigs.Count > 0)
                                    {
                                        string ExternalIP = NetworkInterface.AccessConfigs[0].NatIP;
                                        if (ExternalIP != null)
                                        {
                                            NetworkInterfaceObject["ExternalIP"] = ExternalIP;
                                        }
                                    }
                                    (Result[Current.Name]["NetworkInterfaces"] as JArray).Add(NetworkInterfaceObject);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (RequestedList != null)
                {
                    _ErrorMessageAction?.Invoke("ComputeEngine->ListInstances: Result is empty: " + RequestedList.ToString());
                }
                else
                {
                    _ErrorMessageAction?.Invoke("ComputeEngine->ListInstances: Result is null.");
                }
            }
            return Result;
        }

        public EBVMInstanceStatus GetStatusFromString(string _Status)
        {
            if (_Status == "RUNNING")
            {
                return EBVMInstanceStatus.Running;
            }
            else if (_Status == "STOPPED" || _Status == "TERMINATED" || _Status == "SUSPENDED")
            {
                return EBVMInstanceStatus.Stopped;
            }
            else if (_Status == "PROVISIONING" || _Status == "STAGING")
            {
                return EBVMInstanceStatus.PreparingToRun;
            }
            else if (_Status == "STOPPING" || _Status == "SUSPENDING")
            {
                return EBVMInstanceStatus.Stopping;
            }
            return EBVMInstanceStatus.None;
        }

        public bool GetInstanceStatus(
            string _UniqueInstanceName,
            out EBVMInstanceStatus _Status,
            Action<string> _ErrorMessageAction = null)
        {
            _Status = EBVMInstanceStatus.None;

            Instance FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
            if (FoundInstance != null)
            {
                _Status = GetStatusFromString(FoundInstance.Status);
                if (_Status == EBVMInstanceStatus.None)
                {
                    _Status = EBVMInstanceStatus.None;
                    _ErrorMessageAction?.Invoke("BVMServiceGC->GetInstanceStatus: Unexpected instance status: " + FoundInstance.Status);
                    return false;
                }
                return true;
            }
            return false;
        }

        private int CurrentActionIndex = 0;

        private readonly Dictionary<int, Stack<object>> ProgressStacks = new Dictionary<int, Stack<object>>();
        private readonly object ProgressStacks_Lock = new object();

        //EBVMInstanceStatus is the condition in here
        private int PerformActionOnInstances(
            Tuple<string, EBVMInstanceAction, EBVMInstanceStatus>[] _Operations,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            int ProgressStackIx = Interlocked.Increment(ref CurrentActionIndex);

            var ProgressStack = new Stack<object>();

            if (_Operations != null && _Operations.Length > 0)
            {
                lock (ProgressStacks_Lock)
                {
                    ProgressStacks.Add(ProgressStackIx, ProgressStack);
                }

                var Service = GetService(); //Will be disposed in async methods
                var Request = new BatchRequest(Service);

                foreach (var _Operation in _Operations)
                {
                    var FoundInstance = FindInstanceByUniqueName(_Operation.Item1, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.Status) == _Operation.Item3)
                        {
                            IClientServiceRequest RequestAction = null;
                            if (_Operation.Item2 == EBVMInstanceAction.Start)
                            {
                                RequestAction = Service.Instances.Start(ProjectID, ZoneName, FoundInstance.Name);
                            }
                            else if (_Operation.Item2 == EBVMInstanceAction.Stop)
                            {
                                RequestAction = Service.Instances.Stop(ProjectID, ZoneName, FoundInstance.Name);
                            }

                            if (RequestAction != null)
                            {
                                Request.Queue<Instance>(RequestAction,
                                (Content, Error, i, Message) =>
                                {
                                    lock (ProgressStacks_Lock)
                                    {
                                        if (ProgressStacks.TryGetValue(ProgressStackIx, out Stack<object> FoundStack) && FoundStack.Count > 0)
                                        {
                                            if (Error != null)
                                            {
                                                _ErrorMessageAction?.Invoke("BVMServiceGC->PerformActionOnInstances->Error: " + Error.Message);
                                                FoundStack.Clear();
                                                _OnFailure?.Invoke();
                                            }
                                            else
                                            {
                                                FoundStack.Pop();
                                                if (FoundStack.Count == 0)
                                                {
                                                    ProgressStacks.Remove(ProgressStackIx);
                                                    _OnCompleted?.Invoke();
                                                }
                                            }
                                        }
                                    }
                                });
                                ProgressStack.Push(new object());
                            }
                        }
                    }
                }
                if (ProgressStack.Count > 0)
                {
                    BTaskWrapper.Run(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;

                        try
                        {
                            using (var CreatedTask = Request.ExecuteAsync())
                            {
                                CreatedTask.Wait();
                            }
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke("BVMServiceGC->PerformActionOnInstances->Exception: " + e.Message);
                            _OnFailure?.Invoke();
                        }
                        Service?.Dispose();
                    });
                }
                else
                {
                    lock (ProgressStacks_Lock)
                    {
                        ProgressStacks.Remove(ProgressStackIx);
                    }
                    Service?.Dispose();
                }
            }
            return ProgressStack.Count;
        }

        private bool StartOrStopInstances(
            string[] _UniqueInstanceNames,
            EBVMInstanceAction _Action,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                var Actions = new Tuple<string, EBVMInstanceAction, EBVMInstanceStatus>[_UniqueInstanceNames.Length];

                int i = 0;
                foreach (var _Name in _UniqueInstanceNames)
                {
                    Actions[i++] = new Tuple<string, EBVMInstanceAction, EBVMInstanceStatus>(
                        _Name,
                        _Action,
                        _Action == EBVMInstanceAction.Start ? EBVMInstanceStatus.Stopped : EBVMInstanceStatus.Running);
                }
                return PerformActionOnInstances(Actions, _OnCompleted, _OnFailure, _ErrorMessageAction) > 0;
            }
            return false;
        }

        public bool StartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            return StartOrStopInstances(_UniqueInstanceNames, EBVMInstanceAction.Start, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool StopInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            return StartOrStopInstances(_UniqueInstanceNames, EBVMInstanceAction.Stop, _OnCompleted, _OnFailure, _ErrorMessageAction);
        }

        public bool WaitUntilInstanceStatus(
            string _UniqueInstanceName,
            EBVMInstanceStatus[] _OrStatus,
            Action<string> _ErrorMessageAction = null)
        {
            EBVMInstanceStatus CurrentInstanceStatus = EBVMInstanceStatus.None;

            bool bFirstTime = true;

            List<EBVMInstanceStatus> Conditions = new List<EBVMInstanceStatus>(_OrStatus);

            while (!Conditions.Contains(CurrentInstanceStatus))
            {
                Instance FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
                if (FoundInstance != null)
                {
                    if (GetInstanceStatus(_UniqueInstanceName, out CurrentInstanceStatus, _ErrorMessageAction))
                    {
                        if (bFirstTime)
                        {
                            bFirstTime = false;
                            continue;
                        }
                        Thread.Sleep(2000);
                    }
                    else return false;
                }
                else return false;
            }
            return false;
        }
    }
}
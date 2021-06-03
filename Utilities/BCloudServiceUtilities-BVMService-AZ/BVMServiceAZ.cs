using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using BCommonUtilities;
using System.Linq;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Compute.Fluent;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent.Models;

namespace BCloudServiceUtilities.VMServices
{
    public class BVMServiceAZ : IBVMServiceInterface
    {
        /// <summary>
        /// Holds initialization success
        /// </summary>
        private readonly bool bInitializationSucceed;

        /// <summary>
        /// Azure Manager for managing Azure resources
        /// </summary>
        private readonly IAzure AzureManager;

        private readonly string ResourseGroupName;

        private readonly string ProgramUniqueID;

        /// <summary>
        /// 
        /// <para>BVMServiceAZ: Parametered Constructor for Managed Service by Azure</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_ClientId"/>                                  Azure Client Id</para>
        /// <para><paramref name="_ClientSecret"/>                              Azure Client Secret</para>
        /// <para><paramref name="_TenantId"/>                                  Azure Tenant Id</para>
        /// <para><paramref name="_ResourceGroupName"/>                         Azure Resource Group Name</para>
        /// <para><paramref name="_ProgramUniqueID"/>           Program Unique ID</para>
        /// <para><paramref name="_ErrorMessageAction"/>        Error messages will be pushed to this action</para>
        /// 
        /// </summary>
        public BVMServiceAZ(
            string _ClientId,
            string _ClientSecret,
            string _TenantId, 
            string _ResourceGroupName, 
            string _ProgramUniqueID,
            Action<string> _ErrorMessageAction = null)
        {
            ResourseGroupName = _ResourceGroupName;
            ProgramUniqueID = _ProgramUniqueID;

            try
            {
                var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(_ClientId, _ClientSecret, _TenantId, AzureEnvironment.AzureGlobalCloud);

                AzureManager = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials)
                    .WithDefaultSubscription();

                bInitializationSucceed = true;
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BVMServiceAZ->Constructor: " + e.Message + ", Trace: " + e.StackTrace);
                bInitializationSucceed = false;
            }
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

        public bool CreateInstance(
            string _UniqueInstanceName,
            string _Description,
            string _MachineType,
            long _DiskSizeGB,
            int _GpuCount,
            string _GpuName,
            string _OSSourceImageURL,
            EBVMDiskType _DiskType,
            EBVMOSType _OSType,
            IDictionary<string, string> _Labels,
            BVMNetworkFirewall _FirewallSettings,
            string _OptionalStartupScript,
            out int _ErrorCode,
            Action<string> _ErrorMessageAction = null)
        {
            _ErrorCode = 400;

            return false;
        }

        private List<IVirtualMachine> GetInstanceList(Action<string> _ErrorMessageAction = null)
        {
            try
            {
                using (var GetVirtualMachineTask = AzureManager.VirtualMachines.ListByResourceGroupAsync(ResourseGroupName))
                {
                    GetVirtualMachineTask.Wait();
                    return GetVirtualMachineTask.Result.ToList();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BVMServiceAZ->GetInstanceList: " + e.Message + ", Trace: " + e.StackTrace);
                return null;
            }
        }

        private IVirtualMachine FindInstanceByUniqueName(string _UniqueName, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                using (var GetVirtualMachineTask = AzureManager.VirtualMachines.GetByResourceGroupAsync(ResourseGroupName, _UniqueName))
                {
                    GetVirtualMachineTask.Wait();
                    return GetVirtualMachineTask.Result;
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke("BVMServiceAZ->FindInstanceByUniqueName: " + e.Message + ", Trace: " + e.StackTrace);
                return null;
            }
        }

        public JObject ListInstances(
            Action<string> _ErrorMessageAction = null)
        {
            JObject Result = new JObject();

            List<IVirtualMachine> RequestedList = GetInstanceList(_ErrorMessageAction);
            if (RequestedList != null && RequestedList.Count > 0)
            {
                foreach (var Current in RequestedList)
                {
                    if (Current != null)
                    {
                        Result[Current.Name] = new JObject()
                        {
                            ["UniqueID"] = Current.Id,
                            ["Status"] = Current.PowerState.ToString(),
                            ["Zone"] = Current.AvailabilityZones.ToList().ToString(),
                            ["Kind"] = Current.Type,
                            //["bDeletionProtection"] = (Current.DeletionProtection ?? false),
                            //["CreationTimestamp"] = Current.,
                            //["Description"] = Current.Description ?? "",
                            //["ETag"] = Current.ETag ?? ""
                        };

                        Result[Current.Name]["Disks"] = new JArray();
                        if (Current.DataDisks != null)
                        {
                            //foreach (var Disk in Current.DataDisks)
                            //{
                            //    if (Disk != null)
                            //    {
                            //        JObject DiskObject = new JObject()
                            //        {
                            //            ["bAutoDelete"] = Disk.AutoDelete ?? false,
                            //            ["Kind"] = Disk.Kind,
                            //            ["DeviceName"] = Disk.DeviceName,
                            //            ["bIsBootType"] = Disk.Boot ?? false,
                            //            ["bReadOnly"] = Disk.Mode == "READ_ONLY",
                            //            ["ETag"] = Disk.ETag ?? ""
                            //        };
                            //        (Result[Current.Name]["Disks"] as JArray).Add(DiskObject);
                            //    }
                            //}
                        }

                        //Result[Current.Name]["Labels"] = new JObject();
                        //if (Current.Labels != null)
                        //{
                        //    foreach (var Label in Current.Labels)
                        //    {
                        //        (Result[Current.Name]["Labels"] as JObject)[Label.Key] = Label.Value;
                        //    }
                        //}

                        Result[Current.Name]["Tags"] = new JArray();
                        if (Current.Tags != null)
                        {
                            foreach (var Tag in Current.Tags)
                            {
                                (Result[Current.Name]["Tags"] as JArray).Add(Tag);
                            }
                        }

                        Result[Current.Name]["NetworkInterfaces"] = new JArray();
                        if (Current.NetworkInterfaceIds != null)
                        {
                            foreach (var NetworkInterfaceId in Current.NetworkInterfaceIds)
                            {
                                using (var GetNetworkInterfaceTask = AzureManager.NetworkInterfaces.GetByIdAsync(NetworkInterfaceId))
                                {
                                    GetNetworkInterfaceTask.Wait();
                                    var NetworkInterface = GetNetworkInterfaceTask.Result;
                                    if (NetworkInterface != null)
                                    {
                                        //JObject NetworkInterfaceObject = new JObject()
                                        //{
                                        //    ["UniqueName"] = NetworkInterface.Name,
                                        //    ["Kind"] = NetworkInterface.Type,
                                        //    ["Network"] = NetworkInterface.,
                                        //    ["NetworkIP"] = NetworkInterface.PrimaryIPConfiguration.,
                                        //    ["Subnetwork"] = NetworkInterface.Subnetwork,
                                        //    ["ETag"] = NetworkInterface.ETag ?? ""
                                        //};
                                        //if (NetworkInterface.IPConfigurations != null && NetworkInterface.AccessConfigs.Count > 0)
                                        //{
                                        //    string ExternalIP = NetworkInterface.AccessConfigs[0].NatIP;
                                        //    if (ExternalIP != null)
                                        //    {
                                        //        NetworkInterfaceObject["ExternalIP"] = ExternalIP;
                                        //    }
                                        //}
                                        //(Result[Current.Name]["NetworkInterfaces"] as JArray).Add(NetworkInterfaceObject);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (RequestedList != null) return Result;
                else
                {
                    _ErrorMessageAction?.Invoke("ComputeEngine->ListInstances: Result is null.");
                }
            }
            return Result;
        }

        public EBVMInstanceStatus GetStatusFromString(string _Status)
        {
            if (_Status == "Running")
            {
                return EBVMInstanceStatus.Running;
            }
            else if (_Status == "Stopped" || _Status == "Deallocated")
            {
                return EBVMInstanceStatus.Stopped;
            }
            else if (_Status == "Starting")
            {
                return EBVMInstanceStatus.PreparingToRun;
            }
            else if (_Status == "Stopping" || _Status == "Deallocating")
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

            var FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
            if (FoundInstance != null)
            {
                _Status = GetStatusFromString(FoundInstance.PowerState.ToString());
                if (_Status == EBVMInstanceStatus.None)
                {
                    _Status = EBVMInstanceStatus.None;
                    _ErrorMessageAction?.Invoke("BVMServiceAZ->GetInstanceStatus: Unexpected instance status: " + FoundInstance.PowerState.ToString());
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

                var Request = new ConcurrentQueue<Task>();

                foreach (var _Operation in _Operations)
                {
                    var FoundInstance = FindInstanceByUniqueName(_Operation.Item1, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.PowerState.ToString()) == _Operation.Item3)
                        {
                            Task RequestAction = null;
                            if (_Operation.Item2 == EBVMInstanceAction.Start)
                            {
                                RequestAction = FoundInstance.StartAsync();
                            }
                            else if (_Operation.Item2 == EBVMInstanceAction.Stop)
                            {
                                RequestAction = FoundInstance.DeallocateAsync();
                            }

                            if (RequestAction != null)
                            {
                                Request.Enqueue(RequestAction);
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
                            if (Request.TryDequeue(out Task CreatedTask))
                            {
                                using (CreatedTask)
                                {
                                    CreatedTask.Wait();
                                    lock (ProgressStacks_Lock)
                                    {
                                        if (ProgressStacks.TryGetValue(ProgressStackIx, out Stack<object> FoundStack) && FoundStack.Count > 0)
                                        {
                                            if (CreatedTask.Exception != null)
                                            {
                                                _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformActionOnInstances->Error: " + CreatedTask.Exception.Message);
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
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformActionOnInstances->TryDequeue error occured.");
                                _OnFailure?.Invoke();
                            }
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformActionOnInstances->Exception: " + e.Message);
                            _OnFailure?.Invoke();
                        }
                    });
                }
                else
                {
                    lock (ProgressStacks_Lock)
                    {
                        ProgressStacks.Remove(ProgressStackIx);
                    }
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

        //EBVMInstanceStatus is the condition in here
        private int PerformRunCommandActions(
            string[] _UniqueInstanceNames,
            EBVMOSType _VMOperationSystemType,
            string[] _Commands,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            int ProgressStackIx = Interlocked.Increment(ref CurrentActionIndex);

            var ProgressStack = new Stack<object>();

            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                lock (ProgressStacks_Lock)
                {
                    ProgressStacks.Add(ProgressStackIx, ProgressStack);
                }

                var Request = new ConcurrentQueue<Task>();

                foreach (var _InstanceName in _UniqueInstanceNames)
                {
                    var FoundInstance = FindInstanceByUniqueName(_InstanceName, _ErrorMessageAction);
                    if (FoundInstance != null)
                    {
                        if (GetStatusFromString(FoundInstance.PowerState.ToString()) == EBVMInstanceStatus.Running)
                        {
                            try
                            {
                                var _CommandId = "RunPowerShellScript";
                                if (_VMOperationSystemType == EBVMOSType.Linux)
                                {
                                    _CommandId = "RunShellScript";
                                }

                                var _RunCommandInput = new RunCommandInput()
                                {
                                    CommandId = _CommandId,
                                    Script = _Commands.ToList()
                                };
                                Task RequestAction = FoundInstance.RunCommandAsync(_RunCommandInput);
                                Request.Enqueue(RequestAction);
                                ProgressStack.Push(new object());
                            }
                            catch (System.Exception ex)
                            {
                                _ErrorMessageAction?.Invoke($"BVMServiceAZ->PerformRunCommandActions->An error occurred when RunCommandInput is casting. Error: { ex.Message } - StackTrace: {ex.StackTrace}");
                                _OnFailure?.Invoke();
                            }
                        }
                        else
                        {
                            _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformRunCommandActions->Virtual Machine is not running.");
                            _OnFailure?.Invoke();
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
                            if (Request.TryDequeue(out Task CreatedTask))
                            {
                                using (CreatedTask)
                                {
                                    CreatedTask.Wait();
                                    lock (ProgressStacks_Lock)
                                    {
                                        if (ProgressStacks.TryGetValue(ProgressStackIx, out Stack<object> FoundStack) && FoundStack.Count > 0)
                                        {
                                            if (CreatedTask.Exception != null)
                                            {
                                                _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformRunCommandActions->Error: " + CreatedTask.Exception.Message);
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
                                }
                            }
                            else
                            {
                                _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformRunCommandActions->TryDequeue error occured.");
                                _OnFailure?.Invoke();
                            }
                        }
                        catch (Exception e)
                        {
                            _ErrorMessageAction?.Invoke("BVMServiceAZ->PerformRunCommandActions->Exception: " + e.Message);
                            _OnFailure?.Invoke();
                        }
                    });
                }
                else
                {
                    lock (ProgressStacks_Lock)
                    {
                        ProgressStacks.Remove(ProgressStackIx);
                    }
                }
            }
            return ProgressStack.Count;
        }

        public bool RunCommand(
            string[] _UniqueInstanceNames,
            EBVMOSType _VMOperationSystemType,
            string[] _Commands,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null)
        {
            if (_UniqueInstanceNames != null && _UniqueInstanceNames.Length > 0)
            {
                return PerformRunCommandActions(_UniqueInstanceNames, _VMOperationSystemType, _Commands, _OnCompleted, _OnFailure, _ErrorMessageAction) > 0;
            }
            return false;
        }

        public bool WaitUntilInstanceStatus(
            string _UniqueInstanceName,
            EBVMInstanceStatus[] _OrStatus,
            Action<string> _ErrorMessageAction = null)
        {
            EBVMInstanceStatus CurrentInstanceStatus = EBVMInstanceStatus.None;

            List<EBVMInstanceStatus> Conditions = new List<EBVMInstanceStatus>(_OrStatus);

            int LocalErrorRetryCount = 0;
            do
            {
                var FoundInstance = FindInstanceByUniqueName(_UniqueInstanceName, _ErrorMessageAction);
                if (FoundInstance != null)
                {
                    if (GetInstanceStatus(_UniqueInstanceName, out CurrentInstanceStatus, _ErrorMessageAction))
                    {
                        if (Conditions.Contains(CurrentInstanceStatus)) return true;
                    }
                    else
                    {
                        if (++LocalErrorRetryCount < 5 && ThreadSleep(2000)) continue;
                        return false;
                    }
                }
                else
                {
                    if (++LocalErrorRetryCount < 5 && ThreadSleep(2000)) continue;
                    return false;
                }
            } while (!Conditions.Contains(CurrentInstanceStatus) && ThreadSleep(2000));

            return true;
        }
        private bool ThreadSleep(int _MS) { Thread.Sleep(_MS); return true; }
    }
}

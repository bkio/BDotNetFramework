/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;

namespace BCloudServiceUtilities
{
    public enum EBVMInstanceStatus
    {
        None,
        Stopped,
        Stopping,
        PreparingToRun,
        Running
    };
    public enum EBVMDiskType
    {
        HDD,
        SSD
    }
    public enum EBVMOSType
    {
        Linux,
        Windows
    }
    public class BVMNetworkFirewall
    {
        public enum EVMNetworkFirewallPortProtocol
        {
            TCP,
            UDP,
            Both
        }
        public class BVMNetworkFirewallPortRange
        {
            public ushort FromPortInclusive;
            public ushort ToPortInclusive;
            public EVMNetworkFirewallPortProtocol OpenFor;
        }

        public bool bOpenAll = false;
        public System.Collections.Generic.List<BVMNetworkFirewallPortRange> OpenPorts = new System.Collections.Generic.List<BVMNetworkFirewallPortRange>();
    }

    public interface IBVMServiceInterface
    {
        /// <summary>
        /// 
        /// <para>HasInitializationSucceed:</para>
        /// 
        /// <returns>Returns: Initialization succeed or failed</returns>
        /// 
        /// </summary>
        bool HasInitializationSucceed();

        /// <summary>
        /// 
        /// <para>ListInstances:</para>
        /// 
        /// <para>Lists the VM instances</para>
        /// 
        /// <returns>Returns: List of instances</returns>
        /// 
        /// </summary>
        Newtonsoft.Json.Linq.JObject ListInstances(
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>CreateInstance:</para>
        /// 
        /// <para>Creates a new VM instance</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_UniqueInstanceName"/>            Instance name</para>
        /// <para><paramref name="_Description"/>                   Description of the instance</para>
        /// <para><paramref name="_MachineType"/>                   Machine type</para>
        /// <para><paramref name="_DiskSizeGB"/>                    Disk size In GB</para>
        /// <para><paramref name="_GpuCount"/>                      How many GPUs will be attached</para>
        /// <para><paramref name="_GpuName"/>                       Name (brand-model) of the gpu to be attached</para>
        /// <para><paramref name="_OSSourceImageURL"/>              URL of the OS source image</para>
        /// <para><paramref name="_DiskType"/>                      SSD/HDD</para>
        /// <para><paramref name="_OSType"/>                        Linux/Windows</para>
        /// <para><paramref name="_Labels"/>                        Labels of the instance</para>
        /// <para><paramref name="_FirewallSettings"/>              Firewall settings of the instance</para>
        /// <para><paramref name="_OptionalStartupScript"/>         Startup script: For linux it is a regular shell script. For Windows it is powershell script.</para>
        /// <para><paramref name="_ErrorCode"/>                     Http error code</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns>Returns: Success or not</returns>
        /// 
        /// </summary>
        bool CreateInstance(
            string _UniqueInstanceName,
            string _Description,
            string _MachineType,
            long _DiskSizeGB,
            int _GpuCount,
            string _GpuName,
            string _OSSourceImageURL,
            EBVMDiskType _DiskType,
            EBVMOSType _OSType,
            System.Collections.Generic.IDictionary<string, string> _Labels,
            BVMNetworkFirewall _FirewallSettings,
            string _OptionalStartupScript,
            out int _ErrorCode,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>StartInstances:</para>
        /// 
        /// <para>Starts given VM instances</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_UniqueInstanceNames"/>           Name of the instances to be started</para>
        /// <para><paramref name="_OnCompleted"/>                   This callback will be called upon success</para>
        /// <para><paramref name="_OnFailure"/>                     This callback will be called upon failure</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns>Returns: Success or not</returns>
        /// 
        /// </summary>
        bool StartInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>StopInstances:</para>
        /// 
        /// <para>Stops given VM instances</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_UniqueInstanceNames"/>           Name of the instances to be stopped</para>
        /// <para><paramref name="_OnCompleted"/>                   This callback will be called upon success</para>
        /// <para><paramref name="_OnFailure"/>                     This callback will be called upon failure</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns>Returns: Success or not</returns>
        /// 
        /// </summary>
        bool StopInstances(
            string[] _UniqueInstanceNames,
            Action _OnCompleted,
            Action _OnFailure,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetInstanceStatus:</para>
        /// 
        /// <para>Gets status of the given VM instance</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_UniqueInstanceName"/>            Name of the instance</para>
        /// <para><paramref name="_Status"/>                        Returned status of the vm</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns>Returns: Success or not</returns>
        /// 
        /// </summary>
        bool GetInstanceStatus(
            string _UniqueInstanceName,
            out EBVMInstanceStatus _Status,
            Action<string> _ErrorMessageAction = null);

        /// <summary>
        /// 
        /// <para>GetStatusFromString:</para>
        /// 
        /// <para>Gets status from the given status string</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Status"/>                        Status string</para>
        /// 
        /// <returns>Returns: Enumerated status</returns>
        /// 
        /// </summary>
        EBVMInstanceStatus GetStatusFromString(
            string _Status);

        /// <summary>
        /// 
        /// <para>WaitUntilInstanceStatus:</para>
        /// 
        /// <para>Wait until the given VM instance satisfies one of the given status</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_UniqueInstanceName"/>            Name of the instance</para>
        /// <para><paramref name="_OrStatus"/>                      Returned status of the vm</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns>Returns: Success or not</returns>
        /// 
        /// </summary>
        bool WaitUntilInstanceStatus(
            string _UniqueInstanceName,
            EBVMInstanceStatus[] _OrStatus,
            Action<string> _ErrorMessageAction = null);
    }
}
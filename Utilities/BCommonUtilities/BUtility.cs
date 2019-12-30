/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace BCommonUtilities
{
    public class BTuple<T1, T2>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }

        public BTuple(T1 _Item1, T2 _Item2)
        {
            Item1 = _Item1;
            Item2 = _Item2;
        }
    }
    public class BTuple<T1, T2, T3>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }

        public BTuple(T1 _Item1, T2 _Item2, T3 _Item3)
        {
            Item1 = _Item1;
            Item2 = _Item2;
            Item3 = _Item3;
        }
    }
    public class BTuple<T1, T2, T3, T4>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }

        public BTuple(T1 _Item1, T2 _Item2, T3 _Item3, T4 _Item4)
        {
            Item1 = _Item1;
            Item2 = _Item2;
            Item3 = _Item3;
            Item4 = _Item4;
        }
    }
    public class BTuple<T1, T2, T3, T4, T5>
    {
        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
        public T4 Item4 { get; set; }
        public T5 Item5 { get; set; }

        public BTuple(T1 _Item1, T2 _Item2, T3 _Item3, T4 _Item4, T5 _Item5)
        {
            Item1 = _Item1;
            Item2 = _Item2;
            Item3 = _Item3;
            Item4 = _Item4;
            Item5 = _Item5;
        }
    }

    public enum EBPrimitiveTypeEnum
    {
        String,
        Integer,
        Double,
        ByteArray
    };
    public class BPrimitiveType
    {
        public EBPrimitiveTypeEnum Type { get; }

        public BPrimitiveType(BPrimitiveType _Other)
        {
            Type = _Other.Type;
            switch (Type)
            {
                case EBPrimitiveTypeEnum.Double:
                    AsDouble = _Other.AsDouble;
                    break;
                case EBPrimitiveTypeEnum.Integer:
                    AsInteger = _Other.AsInteger;
                    break;
                case EBPrimitiveTypeEnum.ByteArray:
                    AsByteArray = _Other.AsByteArray;
                    break;
                default:
                    AsString = _Other.AsString;
                    break;
            }
        }

        public string AsString { get; }
        public BPrimitiveType(string _Str)
        {
            Type = EBPrimitiveTypeEnum.String;
            AsString = _Str;
        }

        public int AsInteger { get; }
        public BPrimitiveType(int _Int)
        {
            Type = EBPrimitiveTypeEnum.Integer;
            AsInteger = _Int;
        }

        public double AsDouble { get; }
        public BPrimitiveType(double _Double)
        {
            Type = EBPrimitiveTypeEnum.Double;
            AsDouble = _Double;
        }

        public byte[] AsByteArray { get; }
        public BPrimitiveType(byte[] _ByteArray)
        {
            Type = EBPrimitiveTypeEnum.ByteArray;
            AsByteArray = _ByteArray;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case EBPrimitiveTypeEnum.Double:
                    return AsDouble.ToString();
                case EBPrimitiveTypeEnum.Integer:
                    return AsInteger.ToString();
                case EBPrimitiveTypeEnum.ByteArray:
                    return Convert.ToBase64String(AsByteArray);
                default:
                    return AsString;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                BPrimitiveType Casted = (BPrimitiveType)obj;
                if (Casted != null && Casted.Type == Type)
                {
                    return (Casted.Type == EBPrimitiveTypeEnum.Double && Casted.AsDouble == AsDouble) ||
                        (Casted.Type == EBPrimitiveTypeEnum.Integer && Casted.AsInteger == AsInteger) ||
                        (Casted.Type == EBPrimitiveTypeEnum.String && Casted.AsString == AsString) ||
                        (Casted.Type == EBPrimitiveTypeEnum.ByteArray && Casted.ToString() == ToString());
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            var HashCode = 674144506;
            HashCode = HashCode * -1521134295 + Type.GetHashCode();
            HashCode = HashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AsString);
            HashCode = HashCode * -1521134295 + AsInteger.GetHashCode();
            HashCode = HashCode * -1521134295 + AsDouble.GetHashCode();
            if (AsByteArray != null)
            {
                HashCode = HashCode * -1521134295 + AsByteArray.ToString().GetHashCode();
            }
            return HashCode;
        }
    }

    public enum EBDirectoryTreeNodeType
    {
        File,
        Directory
    }
    public class BDirectoryTreeNode
    {
        private readonly EBDirectoryTreeNodeType NodeType;
        public EBDirectoryTreeNodeType GetNodeType()
        {
            return NodeType;
        }

        private readonly string Name;
        public string GetName()
        {
            return Name;
        }

        private readonly BDirectoryTreeNode Parent;
        public BDirectoryTreeNode GetParent()
        {
            return Parent;
        }

        private readonly List<BDirectoryTreeNode> Children;
        public List<BDirectoryTreeNode> GetChildren()
        {
            return Children;
        }

        public BDirectoryTreeNode(string _Name, BDirectoryTreeNode _Parent, List<BDirectoryTreeNode> _Children, EBDirectoryTreeNodeType _NodeType)
        {
            NodeType = _NodeType;
            Name = _Name;
            Parent = _Parent;
            Children = _Children;
        }
    }

    public static class BUtility
    {
        //Option based; in the second array, it is sufficient for one of the elements to exists in environment variable to succeed.
        public static bool GetEnvironmentVariables(
            out Dictionary<string, string> _ParsedResult,
            IEnumerable<IEnumerable<string>> _VaribleKeysOptions,
            Action<string> _ErrorMessageAction)
        {
            if (_VaribleKeysOptions == null)
            {
                _ParsedResult = null;
                _ErrorMessageAction?.Invoke("BUtility->GetRequiredEnvironmentVariables: Input _VaribleKeysOptions is null.");
                return false;
            }

            var Count = _VaribleKeysOptions.Count();
            if (Count == 0)
            {
                _ParsedResult = null;
                _ErrorMessageAction?.Invoke("BUtility->GetRequiredEnvironmentVariables: Input _VaribleKeysOptions does not have a key.");
                return false;
            }

            /*
            * Getting environment variables
            */
            _ParsedResult = new Dictionary<string, string>(Count);
            try
            {
                foreach (var VarKey in _VaribleKeysOptions)
                {
                    if (VarKey.Count() == 0)
                    {
                        _ParsedResult = null;
                        _ErrorMessageAction?.Invoke("BUtility->GetRequiredEnvironmentVariables: Some required environment variable options are not set.");
                        return false;
                    }

                    bool bFound = false;
                    foreach (var OptionKey in VarKey)
                    {
                        _ParsedResult[OptionKey] = Environment.GetEnvironmentVariable(OptionKey);
                        if (_ParsedResult[OptionKey] != null)
                        {
                            bFound = true;
                        }
                    }
                    if (!bFound)
                    {
                        _ParsedResult = null;
                        _ErrorMessageAction?.Invoke("BUtility->GetRequiredEnvironmentVariables: Some required environment variables are not set.");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                _ParsedResult = null;
                _ErrorMessageAction?.Invoke("BUtility->GetRequiredEnvironmentVariables: Failure during getting required environment variables: " + e.Message);
                return false;
            }
            return true;
        }

        public static void SortJObject(JObject _Object, bool bConvertRoundFloatToInt = false)
        {
            if (_Object == null) return;

            var Props = _Object.Properties().ToList();
            for (var i = 0; i < Props.Count; i++)
            {
                var Prop = Props[i];

                if (bConvertRoundFloatToInt && Prop.Value.Type == JTokenType.Float)
                {
                    var Value = (double)Prop;
                    if (Value == Math.Floor(Value))
                    {
                        Props[i].Value = (long)Value;
                    }
                }

                Prop.Remove();
            }

            foreach (var Prop in Props.OrderBy(P => P.Name))
            {
                _Object.Add(Prop);
                if (Prop.Value is JObject)
                {
                    SortJObject((JObject)Prop.Value, bConvertRoundFloatToInt);
                }
                else if (Prop.Value is JArray)
                {
                    SortJArray((JArray)Prop.Value, bConvertRoundFloatToInt);
                }
            }
        }
        public static void SortJArray(JArray _Array, bool bConvertRoundFloatToInt = false)
        {
            if (_Array == null) return;

            if (_Array.Count > 0)
            {
                var Props = _Array.ToList();
                for (var i = 0; i < Props.Count; i++)
                {
                    var Prop = Props[i];

                    if (bConvertRoundFloatToInt && Prop.Type == JTokenType.Float)
                    {
                        var Value = (double)Prop;
                        if (Value == Math.Floor(Value))
                        {
                            Props[i] = (long)Value;
                        }
                    }

                    Prop.Remove();
                }

                foreach (var Prop in Props.OrderBy(P => P.ToString()))
                {
                    _Array.Add(Prop);
                }
            }
        }

        public static bool GetDirectoryTreeStructure(out BDirectoryTreeNode _ParentNode, string _DirectoryPath, Action<string> _ErrorMessageAction = null)
        {
            _ParentNode = null;
            try
            {
                if (!ConvertDirectoryInfoToTreeNode(out _ParentNode, null, new DirectoryInfo(_DirectoryPath)))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                _ParentNode = null;
                _ErrorMessageAction?.Invoke("BCommonUtilities->GetDirectoryTreeStructure has failed with " + e.Message + ", trace: " + e.StackTrace);
                return false;
            }
            return _ParentNode != null;
        }
        private static bool ConvertDirectoryInfoToTreeNode(out BDirectoryTreeNode _CreatedNode, BDirectoryTreeNode _Parent, DirectoryInfo _DirectoryInfo, Action<string> _ErrorMessageAction = null)
        {
            _CreatedNode = null;
            try
            {
                _CreatedNode = new BDirectoryTreeNode(_DirectoryInfo.Name, _Parent, new List<BDirectoryTreeNode>(), EBDirectoryTreeNodeType.Directory);
                foreach (FileInfo _ChildFile in _DirectoryInfo.GetFiles())
                {
                    _CreatedNode.GetChildren().Add(new BDirectoryTreeNode(_ChildFile.Name, _CreatedNode, null, EBDirectoryTreeNodeType.File));
                }

                foreach (DirectoryInfo _ChildDirectory in _DirectoryInfo.GetDirectories())
                {
                    if (!ConvertDirectoryInfoToTreeNode(out BDirectoryTreeNode ChildDirectoryNode, _CreatedNode, _ChildDirectory))
                    {
                        _CreatedNode = null;
                        return false;
                    }
                    _CreatedNode.GetChildren().Add(ChildDirectoryNode);
                }
            }
            catch (Exception e)
            {
                _CreatedNode = null;
                _ErrorMessageAction?.Invoke("BCommonUtilities->ConvertDirectoryInfoToTreeNode has failed with " + e.Message + ", trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>StringToIPAddress:</para>
        /// 
        /// <para>Converts a string representing a host name or address to its representation</para>
        /// <para>optionally opting to return a IpV6 address (defaults to IpV4)</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_HostNameOrAddress"/>             Host name or address to convert into an IPAddress</para>
        /// <para><paramref name="_Destination"/>                   Destination IPAddress object</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// <para><paramref name="_FavorIpV6"/>                     Optionally opting to return a IpV6 address (defaults to IpV4)</para>
        /// 
        /// <returns> Returns:                                      An IpV4 address instead.</returns>
        /// 
        /// </summary>
        public static bool StringToIPAddress(string _HostNameOrAddress, out IPAddress _Destination, Action<string> _ErrorMessageAction = null, bool _FavorIpV6 = false)
        {
            AddressFamily FavoredFamily = _FavorIpV6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            try
            {
                IPAddress[] Addresses = Dns.GetHostAddresses(_HostNameOrAddress);
                _Destination = Addresses.FirstOrDefault(addr => addr.AddressFamily == FavoredFamily)
                       ??
                       Addresses.FirstOrDefault();
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _Destination = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>CalculateFileMD5:</para>
        /// 
        /// <para>Calculates MD5 hash of a file and returns in lowercase hex-encoded format</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_FileFullPath"/>                  Full path to file</para>
        /// <para><paramref name="_Destination"/>                   Destination MD5 Hash String</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool CalculateFileMD5(string _FileFullPath, out string _Destination, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                using (MD5 MD5Instance = MD5.Create())
                {
                    //Stream will be disposed by GC, thanks to using.
                    using (FileStream Stream = File.OpenRead(_FileFullPath))
                    {
                        byte[] HashBytes = MD5Instance.ComputeHash(Stream);
                        _Destination = BitConverter.ToString(HashBytes).Replace("-", string.Empty).ToLower();
                    }
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _Destination = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>CalculateStreamMD5:</para>
        /// 
        /// <para>Calculates hash of a data stream and returns in lowercase hex-encoded format</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Stream"/>                        Data Stream</para>
        /// <para><paramref name="_Destination"/>                   Destination MD5 Hash String</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool CalculateStreamMD5(Stream _Stream, out string _Destination, Action<string> _ErrorMessageAction = null)
        {
            long InitialStreamPos;
            try
            {
                InitialStreamPos = _Stream.Position;
                _Stream.Position = 0;
            }
            catch (Exception)
            {
                InitialStreamPos = -1;
            }

            try
            {
                using (MD5 MD5Instance = MD5.Create())
                {
                    byte[] HashBytes = MD5Instance.ComputeHash(_Stream);
                    _Destination = BitConverter.ToString(HashBytes).Replace("-", string.Empty).ToLower();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _Destination = null;
                return false;
            }

            if (InitialStreamPos != -1)
            {
                try
                {
                    _Stream.Position = InitialStreamPos;
                }
                catch (Exception) { }
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>CalculateStringMD5:</para>
        /// 
        /// <para>Calculates MD5 hash of a string and returns in lowercase hex-encoded format</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Input"/>                         Input String</para>
        /// <para><paramref name="_Destination"/>                   Destination MD5 Hash String</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool CalculateStringMD5(string _Input, out string _Destination, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                using (MD5 MD5Instance = MD5.Create())
                {
                    byte[] HashBytes = MD5Instance.ComputeHash(System.Text.Encoding.ASCII.GetBytes(_Input));
                    _Destination = BitConverter.ToString(HashBytes).Replace("-", string.Empty).ToLower();
                }
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _Destination = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>CheckURLValidity:</para>
        /// 
        /// <para>Checks if given URL is a valid http or https format</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_URL"/>                         URL Parameter</para>
        /// 
        /// <returns> Returns:                                    Valid or invalid</returns>
        /// 
        /// </summary>
        public static bool CheckURLValidity(string _URL)
        {
            return _URL.Length > 0 && Uri.TryCreate(_URL, UriKind.Absolute, out Uri UriResult) && (UriResult.Scheme == Uri.UriSchemeHttp || UriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// 
        /// <para>CheckIfOnlyHexInString:</para>
        /// 
        /// <para>Checks if given input has only uppercase or lowercase hexedecimal characters and numbers</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Input"/>                       Input Parameter</para>
        /// 
        /// <returns> Returns:                                    Hex-encoded or not</returns>
        /// 
        /// </summary>
        public static bool CheckIfOnlyHexInString(string _Input)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return _Input.Length > 0 && Regex.IsMatch(_Input, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        /// <summary>
        /// 
        /// <para>HexDecode:</para>
        /// 
        /// <para>Decodes Hex String</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Result"/>                        Hex-decoded string</para>
        /// <para><paramref name="_Input"/>                         Input Parameter</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Success or failure</returns>
        /// 
        /// </summary>
        /// 
        public static bool HexDecode(out string _Result, string _Input, Action<string> _ErrorMessageAction = null)
        {
            _Result = null;

            var Result = new byte[_Input.Length / 2];
            try
            {
                for (var i = 0; i < Result.Length; i++)
                {
                    Result[i] = Convert.ToByte(_Input.Substring(i * 2, 2), 16);
                }
                _Result = Encoding.ASCII.GetString(Result);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            
            return _Result != null;
        }

        /// <summary>
        /// 
        /// <para>GetApplicationExePath:</para>
        /// 
        /// <para>Returns full path to this exe</para>
        /// 
        /// <returns> Returns:                                    Ends with \\ by default</returns>
        /// 
        /// </summary>
        public static string GetApplicationExePath(char EndWith = '\\')
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + EndWith;
        }

        /// <summary>
        /// 
        /// <para>GetApplicationDriveLetter:</para>
        /// 
        /// <para>Returns drive's letter that contains this exe</para>
        /// 
        /// <returns> Returns:                     Drive letter</returns>
        /// 
        /// </summary>
        public static string GetApplicationDriveLetter()
        {
            string AppPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            int FirstOccurenceOfColon = AppPath.IndexOf(':');
            if (FirstOccurenceOfColon == -1) return "";
            return AppPath.Substring(0, FirstOccurenceOfColon);
        }

        /// <summary>
        /// 
        /// <para>DoesFileExist:</para>
        /// 
        /// <para>Checks if file exists locally</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_FilePath"/>                      Full file path</para>
        /// <para><paramref name="_bExists"/>                       Destination boolean for existence result</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool DoesFileExist(string _FilePath, out bool _bExists, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                _bExists = File.Exists(_FilePath);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _bExists = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>GetFileSize:</para>
        /// 
        /// <para>Get local file size</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_FilePath"/>                      Full file path</para>
        /// <para><paramref name="_FileSize"/>                      Destination size</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool GetFileSize(string _FilePath, out ulong _FileSize, Action<string> _ErrorMessageAction = null)
        {
            try
            {
                _FileSize = (ulong)((new FileInfo(_FilePath)).Length);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                _FileSize = 0;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>DeleteFile:</para>
        /// 
        /// <para>Delete local file</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_FilePath"/>                      Full file path</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool DeleteFile(
            string _FilePath,
            Action<string> _ErrorMessageAction = null)
        {
            try
            {
                File.Delete(_FilePath);
            }
            catch (Exception e)
            {
                _ErrorMessageAction?.Invoke(e.Message + ", Trace: " + e.StackTrace);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 
        /// <para>GetValueByKeyFromList:</para>
        /// 
        /// <para>Gets value by key from string pairs list</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_SourceList"/>                    List of string pairs</para>
        /// <para><paramref name="_Key"/>                           Key to look for in SourceList</para>
        /// <para><paramref name="_Value"/>                         Destination string to store found value, nulled is not found</para>
        /// <para><paramref name="_ErrorMessageAction"/>            Error messages will be pushed to this action</para>
        /// 
        /// <returns> Returns:                                      Operation success</returns>
        /// 
        /// </summary>
        public static bool GetValueByKeyFromList(
            List<Tuple<string, string>> _SourceList,
            string _Key,
            out string _Value,
            Action<string> _ErrorMessageAction = null)
        {
            _Value = null;

            if (_SourceList == null || _SourceList.Count == 0)
            {
                _ErrorMessageAction?.Invoke("BCommonUtilities->GetValueByKeyFromList: SourceList is null or does not have any element.");
                return false;
            }

            foreach (Tuple<string, string> CurrentElement in _SourceList)
            {
                if (CurrentElement.Item1 == _Key)
                {
                    _Value = CurrentElement.Item2;
                    return true;
                }
            }

            _ErrorMessageAction?.Invoke("BCommonUtilities->GetValueByKeyFromList: SourceList does not contain key: " + _Key);
            return false;
        }

        /// <summary>
        /// 
        /// <para>GetValueFromKeyValueArray:</para>
        /// 
        /// <para>Checks if given array has given key and returns if it has</para>
        /// 
        /// <para>Parameters:</para>
        /// <para><paramref name="_Array"/>                         Array to look for</para>
        /// <para><paramref name="_Key"/>                           Key to be searched</para>
        /// <para><paramref name="_Value"/>                         Found value, null if not found</para>
        /// 
        /// <returns> Returns:                                      Found or not found</returns>
        /// 
        /// </summary>
        public static bool GetValueFromKeyValueArray(Tuple<string, string>[] _Array, string _Key, out string _Value)
        {
            _Value = null;
            if (_Array == null || _Array.Length == 0) return false;

            foreach (Tuple<string, string> _Element in _Array)
            {
                if (_Element != null && _Element.Item1 != null && _Element.Item1 == _Key)
                {
                    _Value = _Element.Item2;
                    return true;
                }
            }
            return false;
        }

        public static string WildCardToRegular(string _Value)
        {
            return "^" + Regex.Escape(_Value).Replace("\\*", ".*") + "$";
        }

        public static string RandomString(int _Size, bool _LowerCase)
        {
            var Builder = new StringBuilder();
            Random Rand = new Random();

            char Char;

            for (int i = 0; i < _Size; i++)
            {
                Char = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * Rand.NextDouble() + 65)));
                Builder.Append(Char);
            }
            if (_LowerCase)
            {
                return Builder.ToString().ToLower();
            }
            return Builder.ToString();
        }

        public static string EncodeStringForTagging(string _Input)
        {
            return WebUtility.UrlEncode(_Input).Replace("%", "@pPp@");
        }
        public static string DecodeStringForTagging(string _Input)
        {
            return WebUtility.UrlDecode(_Input.Replace("@pPp@", "%"));
        }

        public static void DeleteFolderContent(string _Path)
        {
            try
            {
                DirectoryInfo DirInfo = new DirectoryInfo(_Path);

                foreach (FileInfo _File in DirInfo.GetFiles())
                {
                    try
                    {
                        _File.Delete();
                    }
                    catch (Exception) { }
                }
                foreach (DirectoryInfo _Directory in DirInfo.GetDirectories())
                {
                    try
                    {
                        _Directory.Delete(true);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }

    /// <summary>
    /// Multiple producers: thread safety will be ensured when setting the value
    /// </summary>
    public enum EBProducerStatus
    {
        SingleProducer,
        MultipleProducer
    };

    /// <summary>
    /// For passing a primitive value as reference in Action parameters
    /// </summary>
    public class BValue<T>
    {
        private T Value;
        private readonly EBProducerStatus ThreadSafety;
        
        public readonly Object Monitor = new Object();

        public BValue(T _InitialValue, EBProducerStatus _ThreadSafety = EBProducerStatus.SingleProducer)
        {
            Value = _InitialValue;
            ThreadSafety = _ThreadSafety;
        }

        public T Get()
        {
            return Value;
        }

        public void Set(T NewValue)
        {
            if (ThreadSafety == EBProducerStatus.MultipleProducer)
            {
                lock(Monitor)
                {
                    Value = NewValue;
                }
            }
            else
            {
                Value = NewValue;
            }
        }
    }

    /// <summary>
    /// <para>Only allows getting a primitive value, not setting</para>
    /// </summary>
    public class BValueOnlyGetAllowed<T>
    {
        private BValue<T> RelativeValue = null;
        public T Get()
        {
            return RelativeValue.Get();
        }

        public BValueOnlyGetAllowed(BValue<T> _RelativeBoolean)
        {
            RelativeValue = _RelativeBoolean;
        }
    }

    public enum EBStringOrStreamEnum
    {
        String,
        Stream
    };
    public class BStringOrStream
    {
        private readonly Action DestructorAction;

        public Stream Stream { get; }
        public long StreamLength { get; }

        public string String { get; }

        public EBStringOrStreamEnum Type { get; }
        
        public BStringOrStream(Stream _Stream, long _StreamLength)
        {
            Type = EBStringOrStreamEnum.Stream;
            Stream = _Stream;
            StreamLength = _StreamLength;
        }
        public BStringOrStream(string _Str)
        {
            Type = EBStringOrStreamEnum.String;
            String = _Str;
        }

        public BStringOrStream(Stream _Stream, long _StreamLength, Action _DestructorAction)
        {
            Type = EBStringOrStreamEnum.Stream;
            Stream = _Stream;
            StreamLength = _StreamLength;
            DestructorAction = _DestructorAction;
        }
        ~BStringOrStream()
        {
            if (Type == EBStringOrStreamEnum.Stream)
            {
                DestructorAction?.Invoke();
            }
        }
    }

    //Copyright (C) 2016 BravoTango86 (https://gist.github.com/BravoTango86/2a085185c3b9bd8383a1f956600e515f)
    public static class Base32
    {
        private static readonly char[] DIGITS;
        private static readonly int MASK;
        private static readonly int SHIFT;
        private static Dictionary<char, int> CHAR_MAP = new Dictionary<char, int>();
        private const string SEPARATOR = "-";

        static Base32()
        {
            DIGITS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
            MASK = DIGITS.Length - 1;
            SHIFT = numberOfTrailingZeros(DIGITS.Length);
            for (int i = 0; i < DIGITS.Length; i++) CHAR_MAP[DIGITS[i]] = i;
        }

        private static int numberOfTrailingZeros(int i)
        {
            // HD, Figure 5-14
            int y;
            if (i == 0) return 32;
            int n = 31;
            y = i << 16; if (y != 0) { n = n - 16; i = y; }
            y = i << 8; if (y != 0) { n = n - 8; i = y; }
            y = i << 4; if (y != 0) { n = n - 4; i = y; }
            y = i << 2; if (y != 0) { n = n - 2; i = y; }
            return n - (int)((uint)(i << 1) >> 31);
        }

        public static byte[] Decode(string encoded)
        {
            // Remove whitespace and separators
            encoded = encoded.Trim().Replace(SEPARATOR, "");

            // Remove padding. Note: the padding is used as hint to determine how many
            // bits to decode from the last incomplete chunk (which is commented out
            // below, so this may have been wrong to start with).
            encoded = Regex.Replace(encoded, "[=]*$", "");

            // Canonicalize to all upper case
            encoded = encoded.ToUpper();
            if (encoded.Length == 0)
            {
                return new byte[0];
            }
            int encodedLength = encoded.Length;
            int outLength = encodedLength * SHIFT / 8;
            byte[] result = new byte[outLength];
            int buffer = 0;
            int next = 0;
            int bitsLeft = 0;
            foreach (char c in encoded.ToCharArray())
            {
                if (!CHAR_MAP.ContainsKey(c))
                {
                    throw new DecodingException("Illegal character: " + c);
                }
                buffer <<= SHIFT;
                buffer |= CHAR_MAP[c] & MASK;
                bitsLeft += SHIFT;
                if (bitsLeft >= 8)
                {
                    result[next++] = (byte)(buffer >> (bitsLeft - 8));
                    bitsLeft -= 8;
                }
            }
            // We'll ignore leftover bits for now.
            //
            // if (next != outLength || bitsLeft >= SHIFT) {
            //  throw new DecodingException("Bits left: " + bitsLeft);
            // }
            return result;
        }

        public static string Encode(byte[] data, bool padOutput = false)
        {
            if (data.Length == 0)
            {
                return "";
            }

            // SHIFT is the number of bits per output character, so the length of the
            // output is the length of the input multiplied by 8/SHIFT, rounded up.
            if (data.Length >= (1 << 28))
            {
                // The computation below will fail, so don't do it.
                throw new ArgumentOutOfRangeException("data");
            }

            int outputLength = (data.Length * 8 + SHIFT - 1) / SHIFT;
            StringBuilder result = new StringBuilder(outputLength);

            int buffer = data[0];
            int next = 1;
            int bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < SHIFT)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= (data[next++] & 0xff);
                        bitsLeft += 8;
                    }
                    else
                    {
                        int pad = SHIFT - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }
                int index = MASK & (buffer >> (bitsLeft - SHIFT));
                bitsLeft -= SHIFT;
                result.Append(DIGITS[index]);
            }
            if (padOutput)
            {
                int padding = 8 - (result.Length % 8);
                if (padding > 0) result.Append(new string('=', padding == 8 ? 0 : padding));
            }
            return result.ToString();
        }

        private class DecodingException : Exception
        {
            public DecodingException(string message) : base(message)
            {
            }
        }
    }
}
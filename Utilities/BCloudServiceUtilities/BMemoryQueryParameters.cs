/// MIT License, Copyright Burak Kara, burak@burak.io, https://en.wikipedia.org/wiki/MIT_License

using System;

namespace BCloudServiceUtilities
{
    public struct BMemoryQueryParameters
    {
        public string Domain;

        public string SubDomain;

        public string Identifier;

        public static readonly string Delimiter = "[-]";

        public override string ToString()
        {
            return Domain + Delimiter + SubDomain + Delimiter + Identifier;
        }

        public static bool CreateFrom(out BMemoryQueryParameters Result, string SplitFrom)
        {
            if (SplitFrom != null && SplitFrom.Length > 0)
            {
                string[] Splitted = SplitFrom.Split(new string[] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                if (Splitted != null && Splitted.Length >= 3)
                {
                    Result = new BMemoryQueryParameters()
                    {
                        Domain = Splitted[0],
                        SubDomain = Splitted[1],
                        Identifier = Splitted[2]
                    };
                    return true;
                }
            }
            Result = new BMemoryQueryParameters();
            return false;
        }
    }
}
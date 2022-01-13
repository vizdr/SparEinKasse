using System;
using System.Text;
using System.Security.Cryptography;
using System.Management;

namespace SimpleSecurity
{
    public static class Security
    {
        const string crSalt = "VladimirZ";

        public static string GetAuthorizationCode(string authorizationRequest)
        {
            return GetPreCode(authorizationRequest + crSalt).Substring(0, 15);
        }

        public static string GetRequestCode(string Uname)
        {
            string mbsn = GetMotherBoardID1() ?? GetMotherBoardID2();
            string preRes = GetPreCode(mbsn + Uname);
            return preRes.Substring(1, 16);
        }

        private static string GetMotherBoardID1()
        {
            string mbInfo = String.Empty;
            ManagementScope scope = new ManagementScope("\\\\" + Environment.MachineName + "\\root\\cimv2");

            scope.Connect();
            using (ManagementObject wmiClass = new ManagementObject(scope, new ManagementPath("Win32_BaseBoard.Tag=\"Base Board\""), new ObjectGetOptions()))
            {
                foreach (PropertyData propData in wmiClass.Properties)
                {
                    if (propData.Name == "SerialNumber")
                        mbInfo = Convert.ToString(propData.Value); //*{0,-25}* propData.Name,/
                }
            }
            return mbInfo;
        }

        private static string GetMotherBoardID2()
        {
            string mbInfo = String.Empty;

            //Get motherboard's serial number 
            using (ManagementObjectSearcher mbs = new ManagementObjectSearcher("Select * From Win32_BaseBoard"))
            {
                foreach (ManagementObject mo in mbs.Get())
                {
                    mbInfo += mo["SerialNumber"].ToString();
                }
            }
            return mbInfo;
        }

        private static string GetPreCode(string input)
        {
            Byte[] preCode = Encoding.UTF8.GetBytes(input);

            using (MD5 coder = MD5Cng.Create())
            {
                coder.Initialize();
                preCode = coder.ComputeHash(preCode);
            }

            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < preCode.Length; i++)
            {
                sBuilder.Append(preCode[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}

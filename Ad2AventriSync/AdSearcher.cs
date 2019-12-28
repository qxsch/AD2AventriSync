using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ad2AventriSync
{
    class AdSearcher
    {
        public string BaseDn="";

        public List<string> PropertiesToLoad = new List<string> {
            "cn",
            "displayName",
            "mail",
            "Department",
            "physicalDeliveryOfficeName",
            "co",
            "l",
            "employeeType"
        };


        private static object _defaultLdapPathLock = new object();

        private static string _defaultLdapPath = "";
        public static string GetDefaultLdapPath()
        {
            lock(_defaultLdapPathLock)
            {
                if (_defaultLdapPath == "")
                {
                    try
                    {
                        DirectoryEntry rootDse = new DirectoryEntry("LDAP://RootDSE");
                        _defaultLdapPath = rootDse.Properties["defaultNamingContext"][0].ToString();
                    }
                    catch (System.Runtime.InteropServices.COMException)
                    {
                        // .net bug: when the binary is located on a network drive, this will throw an exception
                        // workarround start powershell (that is a new .net process on the local drive)
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.RedirectStandardOutput = true;
                        p.StartInfo.RedirectStandardError = false;
                        p.StartInfo.FileName = @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe";
                        p.StartInfo.WorkingDirectory = @"C:\Windows\System32\WindowsPowerShell\v1.0\";
                        p.StartInfo.UseShellExecute = false;
                        p.StartInfo.Arguments = "-encodedCommand " + System.Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes("([ADSI]'LDAP://RootDSE').Properties['defaultNamingContext'][0].ToString()"), Base64FormattingOptions.None);

                        p.Start();
                        _defaultLdapPath = p.StandardOutput.ReadToEnd().Trim();
                        p.WaitForExit();
                    }
                }
                return _defaultLdapPath;
            }
        }

        protected void SetDefaultsIfEmptyOrNull()
        {
            if (BaseDn == null || BaseDn.Trim() == "")
            {
                BaseDn = GetDefaultLdapPath();
            }

            if (PropertiesToLoad == null || PropertiesToLoad.Count == 0)
            {
                PropertiesToLoad = new List<string> {
                    "cn",
                    "displayName",
                    "mail",
                    "Department",
                    "physicalDeliveryOfficeName",
                    "co",
                    "l",
                    "employeeType"
                };
            }
        }

        private string[] GetLdapPropertiesToLoad()
        {
            string[] properties = new string[PropertiesToLoad.Count + 1];
            properties[0] = "userAccountControl";
            int i = 1;
            foreach(string p in PropertiesToLoad)
            {
                if(p.ToLower() == "useraccountcontrol")
                {
                    return PropertiesToLoad.ToArray();
                }
                properties[i++] = p;
            }
            return properties;
        }

        public List<Dictionary<string, List<string>>> Search(string query)
        {
            List<Dictionary<string, List<string>>> result = new List<Dictionary<string, List<string>>>();

            SetDefaultsIfEmptyOrNull();

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + BaseDn);
            using (DirectorySearcher ds = new DirectorySearcher(entry, query, GetLdapPropertiesToLoad()))
            {
                ds.PageSize = 500;
                ds.SizeLimit = 0;

                SearchResultCollection resultCollection = ds.FindAll();
                foreach(SearchResult adResult in resultCollection)
                {
                    Dictionary<string, List<string>> d = new Dictionary<string, List<string>>();
                    foreach(string key in adResult.Properties.PropertyNames)
                    {
                        List<string> val = new List<string>();
                        for(int i = 0; i < adResult.Properties[key].Count; i++)
                        {
                            val.Add(adResult.Properties[key][i].ToString());
                        }
                        d.Add(key.ToLower(), val);
                    }
                    // just add enabled users....
                    if(adResult.Properties.Contains("userAccountControl") && !Convert.ToBoolean(((int)adResult.Properties["userAccountControl"][0]) & 0x002))
                    {
                        result.Add(d);
                    }
                }
            }
            return result;
        }

        public System.Data.DataTable SearchAsDatatable(string query)
        {
            System.Data.DataTable table = new System.Data.DataTable();

            SetDefaultsIfEmptyOrNull();

            foreach(string key in PropertiesToLoad)
            {
                table.Columns.Add(key);
            }

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + BaseDn);
            using (DirectorySearcher ds = new DirectorySearcher(entry, query, GetLdapPropertiesToLoad()))
            {
                ds.PageSize = 500;
                ds.SizeLimit = 0;

                SearchResultCollection resultCollection = ds.FindAll();
                foreach (SearchResult adResult in resultCollection)
                {
                    System.Data.DataRow row = table.NewRow();
                    int i = 0;
                    foreach (string key in PropertiesToLoad)
                    {
                        if(adResult.Properties[key].Count > 0)
                        {
                            row[i] = adResult.Properties[key][0].ToString();
                        }
                        else
                        {
                            row[i] = "";
                        }
                        i++;
                    }
                    // just add enabled users....
                    if (adResult.Properties.Contains("userAccountControl") && !Convert.ToBoolean(((int)adResult.Properties["userAccountControl"][0]) & 0x002))
                    {
                        table.Rows.Add(row);
                    }
                }
            }

            return table;
        }

    }
}

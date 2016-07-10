using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace DrawerServer
{
    class SettingRoot
    {
        static string RootDirectoryName = "drawer-settings";

        static string root;

        static string getProgramDirectory()
        {
            string progPath = new Uri(Assembly.GetAssembly(typeof(SettingRoot)).CodeBase).LocalPath;
            return Path.GetDirectoryName(progPath);
        }

        public static void Setup()
        {
            root =  Path.Combine(getProgramDirectory(), RootDirectoryName);
            Console.WriteLine("SettingRoot: {0}", root);
        }

        static string getDevmodePath(string name)
        {
            return Path.Combine(root, name + ".devmode");
        }

        static string getDevnamesPath(string name)
        {
            return Path.Combine(root, name + ".devnames");
        }

        static string getAuxPath(string name)
        {
            return Path.Combine(root, name + ".json");
        }

        static public List<string> ListSettingNames()
        {
            if (root == null)
            {
                return new List<string>();
            }
            else
            {
                List<string> settings = new List<string>();
                foreach (string path in Directory.GetFiles(root, "*.devmode"))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    settings.Add(name);
                }
                return settings;
            }
        }

        static public bool SettingExists(string name)
        {
            if (root == null)
            {
                return false;
            }
            else
            {
                return File.Exists(getDevmodePath(name));
            }
        }

        static public void EnsureRoot()
        {
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }

        static public byte[] ReadDevmode(string name)
        {
            return File.ReadAllBytes(getDevmodePath(name));
        }

        static public void WriteDevmode(string name, byte[] data)
        {
            File.WriteAllBytes(getDevmodePath(name), data);
        }

        static public byte[] ReadDevnames(string name)
        {
            return File.ReadAllBytes(getDevnamesPath(name));
        }

        static public void WriteDevnames(string name, byte[] devnames)
        {
            File.WriteAllBytes(getDevnamesPath(name), devnames);
        }

        static public AuxSettings ReadAux(string name)
        {
            AuxSettings auxSettings = new AuxSettings();
            string path = getAuxPath(name);
            if( File.Exists(path))
            {
                using (var sr = new StreamReader(path))
                using (var textReader = new JsonTextReader(sr))
                {
                    JObject auxData = (JObject)JToken.ReadFrom(textReader);
                    if (auxData["dx"] != null)
                    {
                        auxSettings.Dx = auxData["dx"].Value<double>();
                    }
                    if (auxData["dy"] != null)
                    {
                        auxSettings.Dy = auxData["dy"].Value<double>();
                    }
                }
            }
            return auxSettings;
        }

        static public void WriteAux(string name, AuxSettings auxSettings)
        {
            File.WriteAllText(getAuxPath(name), auxSettings.SerializeToJson());
        }

        static public void DeleteSetting(string name)
        {
            File.Delete(getDevmodePath(name));
            File.Delete(getDevnamesPath(name));
            File.Delete(getAuxPath(name));
        }
    }
}

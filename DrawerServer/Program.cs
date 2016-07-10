using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Drawing.Printing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DrawerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            SettingRoot.Setup();
            IPAddress addr = new IPAddress(new byte[] { 127, 0, 0, 1 });
            TcpListener server = new TcpListener(addr, 10000);
            Console.WriteLine(server.ToString());
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                new Thread(() => HandleClient(client)).Start();
            }
        }

        static void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 10000;
                stream.WriteTimeout = 10000;
                Request req = new Request(stream);
                Console.WriteLine("Method: {0}", req.Method);
                Console.WriteLine("Req: {0}", req.Url);
                if (req.Url.StartsWith("/setting/"))
                {
                    string name = req.Url.Substring(9);
                    Console.WriteLine("name: {0}", name);
                    switch (req.Method)
                    {
                        case "POST":
                            {
                                PrintDialog dialog = new PrintDialog();
                                DialogResult result = dialog.ShowDialog();
                                if (result == DialogResult.OK)
                                {
                                    SettingRoot.EnsureRoot();
                                    byte[] devmode = Win32.CopyDevmode(dialog.PrinterSettings);
                                    byte[] devnames = Win32.CopyDevnames(dialog.PrinterSettings);
                                    SettingRoot.WriteDevmode(name, devmode);
                                    SettingRoot.WriteDevnames(name, devnames);
                                    SettingRoot.WriteAux(name, new AuxSettings());
                                    TextPlain(stream, "ok");
                                }
                                else
                                {
                                    TextPlain(stream, "cancel");
                                }
                                break;
                            }
                        default: throw new ArgumentException("Invalid HTTP Method");
                    }
                }
                else if (req.Url.StartsWith("/setting-detail/"))
                {
                    string name = req.Url.Substring(16);
                    HandleSettingDetail(stream, name);
                }
                else {
                    if (req.Method == "GET")
                    {
                        ServeFile(stream, req.Url);
                    }
                    else
                    {
                        BadRequest(stream, "間違い");
                    }
                }
                client.Close();
            }
            catch (Exception ex)
            {
                BadRequest(client.GetStream(), "");
                Console.WriteLine("Exception: {0}", ex.Message);
                Console.WriteLine("Stack Trace: {0}", ex.StackTrace);
            }
        }

        static void TextPlain(NetworkStream stream, string content)
        {
            System.Text.Encoding enc = new System.Text.UTF8Encoding();
            int count = enc.GetByteCount(content);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                writer.WriteLine("HTTP/1.0 200 OK");
                writer.WriteLine("Connection: close");
                writer.WriteLine("Content-Type: text/plain; charset=utf-8");
                writer.WriteLine("Content-Length: {0}", count);
                writer.WriteLine("");
                writer.Write(content);
            }
        }

        static void RespondWithJson(NetworkStream stream, object result)
        {
            string message = JsonConvert.SerializeObject(result);
            System.Text.Encoding enc = new System.Text.UTF8Encoding();
            int count = enc.GetByteCount(message);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                writer.WriteLine("HTTP/1.0 400 Bad Request");
                writer.WriteLine("Connection: close");
                writer.WriteLine("Content-Type: application/json; charset=utf-8");
                writer.WriteLine("Content-Length: {0}", count);
                writer.WriteLine("");
                writer.Write(message);
            }
        }

        static void BadRequest(NetworkStream stream, string message)
        {
            System.Text.Encoding enc = new System.Text.UTF8Encoding();
            int count = enc.GetByteCount(message);
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                writer.WriteLine("HTTP/1.0 400 Bad Request");
                writer.WriteLine("Connection: close");
                writer.WriteLine("Content-Type: text/plain; charset=utf-8");
                writer.WriteLine("Content-Length: {0}", count);
                writer.WriteLine("");
                writer.Write(message);
            }
        }

        static void NotFound(NetworkStream stream)
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(stream))
            {
                writer.WriteLine("HTTP/1.0 400 Not Found");
                writer.WriteLine("Connection: close");
                writer.WriteLine("");
            }
        }

        static Dictionary<string, string> MimeTypes = new Dictionary<string, string>()
        {
            { ".html", "text/html; charset=\"utf-8\"" },
            { ".css", "text/css" },
            { ".js", "text/javascript" }
        };

        static void HandleSettingDetail(NetworkStream stream, string name)
        {
            if (!SettingRoot.SettingExists(name))
            {
                throw new ArgumentException("setting does not exist: " + name);
            }
            PrinterSettings printerSettings = new PrinterSettings();
            byte[] devmodeBytes = SettingRoot.ReadDevmode(name);
            byte[] devnamesBytes = SettingRoot.ReadDevnames(name);
            AuxSettings auxSettings = SettingRoot.ReadAux(name);
            Win32.DEVMODE devmode = Win32.ParseDevmode(devmodeBytes);
            Dictionary<string, object> dict = auxSettings.ConvertToDict();
            Dictionary<string, object> dictDevmode = new Dictionary<string, object>();
            Dictionary<string, object> dictDevnames = new Dictionary<string, object>();
            dictDevmode["deviceName"] = devmode.dmDeviceName;
            dictDevmode["orientation"] = PrinterConsts.OrientationToLabel(devmode.dmOrientation);
            dictDevmode["paperSize"] = PrinterConsts.PaperSizeToLabel(devmode.dmPaperSize);
            dictDevmode["printQuality"] = PrinterConsts.QualityToLabel(devmode.dmPrintQuality);
            dictDevmode["defaultSource"] = PrinterConsts.SourceToLabel(devmode.dmDefaultSource);
            {
                string driver, device, output;
                Win32.ParseDevnames(devnamesBytes, out driver, out device, out output);
                dictDevnames["driver"] = driver;
                dictDevnames["device"] = device;
                dictDevnames["output"] = output;
            }
            dict["devmode"] = dictDevmode;
            dict["devnames"] = dictDevnames;
            RespondWithJson(stream, dict);
        }


        static void ServeFile(NetworkStream stream, String url)
        {
            string docDir = "./static";
            string path = docDir + url;
            if (System.IO.File.Exists(path))
            {
                string ext = Path.GetExtension(url);
                string type = null;
                if (MimeTypes.ContainsKey(ext))
                {
                    type = MimeTypes[ext];
                }
                if (type != null)
                {
                    FileInfo fileInfo = new FileInfo(path);
                    int fsize = (int)fileInfo.Length;
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("HTTP/1.0 200 OK");
                        writer.WriteLine("Content-Type: {0}", type);
                        writer.WriteLine("Content-Length: {0}", fsize);
                        writer.WriteLine("Connection: close");
                        writer.WriteLine("");
                        writer.Flush();
                        using (System.IO.FileStream fstream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                        {
                            fstream.CopyTo(stream);
                        }
                    }
                }
                else
                {
                    NotFound(stream);
                }
            }
            else
            {
                NotFound(stream);
            }
        }
    }
}

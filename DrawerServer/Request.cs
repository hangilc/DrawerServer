using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Web;

namespace DrawerServer
{
    class Request
    {
        public string Method;
        public string Url;
        public Dictionary<string, string> Query;
        public string Body;

        public Request(NetworkStream stream)
        {
            Query = new Dictionary<string, string>();
            byte[] buf = new byte[1024];
            System.Text.Encoding enc = System.Text.Encoding.UTF8;
            string input;
            using (System.IO.MemoryStream memBuffer = new System.IO.MemoryStream())
            {
                do
                {
                    int nRead = stream.Read(buf, 0, buf.Length);
                    memBuffer.Write(buf, 0, nRead);

                } while (stream.DataAvailable);
                input = enc.GetString(memBuffer.GetBuffer(), 0, (int)memBuffer.Length);
            }
            using (System.IO.StringReader inputReader = new System.IO.StringReader(input))
            {
                string line = inputReader.ReadLine();
                Console.WriteLine(line);
                string[] firstTokens = line.Split(new char[] { ' ' });
                this.Method = firstTokens[0].ToUpper();
                string url = firstTokens[1];
                int qIndex = url.IndexOf('?');
                if( qIndex >= 0)
                {
                    var q = url.Substring(qIndex + 1);
                    setupQuery(q);
                    url = url.Substring(0, qIndex);
                }
                this.Url = HttpUtility.UrlDecode(url);
                while (true)
                {
                    string next = inputReader.ReadLine();
                    if (next == "")
                    {
                        if( this.Method == "POST")
                        {
                            this.Body = inputReader.ReadToEnd();
                        }
                        break;
                    }
                    else if( next == null)
                    {
                        break;
                    }
                    else
                    {
                        //Console.WriteLine("NEXT: {0}", next);
                    }
                }
            }
        }

        void setupQuery(string q)
        {
            string[] parts = q.Split('&');
            foreach(string part in parts)
            {
                int index = part.IndexOf('=');
                if( index >= 0)
                {
                    string key = part.Substring(0, index);
                    string val = part.Substring(index + 1);
                    key = HttpUtility.UrlDecode(key);
                    val = HttpUtility.UrlDecode(val);
                    Query[key] = val;
                }
                else
                {
                    string key = HttpUtility.UrlDecode(part);
                    Query[key] = "";
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DrawerServer
{
    class AuxSettings
    {
        public double Dx;
        public double Dy;

        public Dictionary<string, object> ConvertToDict()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>()
            {
                {"dx", Dx},
                {"dy", Dy}
            };
            return dict;
        }

        public string SerializeToJson()
        {
            return JsonConvert.SerializeObject(ConvertToDict());
        }
    }
}

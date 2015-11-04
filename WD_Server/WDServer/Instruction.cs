using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WDServer
{
    class Instruction
    {
        public string Command { get; set; } = "MSG"; // Default to MSG
        public string Arg1 { get; set; } = "";
        public string Arg2 { get; set; } = "";
        public string Arg3 { get; set; } = "";
        public string Arg4 { get; set; } = "";

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

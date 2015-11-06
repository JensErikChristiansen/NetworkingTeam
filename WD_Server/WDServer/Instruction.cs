using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace WDServer
{
    [Serializable()]
    class Instruction
    {
        public Server.InstructionType Command { get; set; } = Server.InstructionType.JOIN;
        public string Arg1 { get; set; } = "";
        public string Arg2 { get; set; } = "";
        public string Arg3 { get; set; } = "";
        public string Arg4 { get; set; } = "";
    }
}

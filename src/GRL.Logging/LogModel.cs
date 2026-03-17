using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRL.Logging
{
    public class LogModel
    {
        public LogType LogType { get; set; }
        public string? Message { get; set; }
        public Exception? MessageException { get; set; }
        public LogModel()
        {
            LogType = LogType.None;
            Message = string.Empty;
            MessageException = null;
        }
    }
}

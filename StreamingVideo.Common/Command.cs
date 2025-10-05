using System;
using System.Collections.Generic;
using System.Text;

namespace StreamingVideo.Common {

    public enum CommandType {
        Play, 
        Stop, 
        Pause, 
        Seek
    }

    public class Command {
        public CommandType Cmd { get; set; }
        //millisecond
        public double TimeSkipMillis { get; set; }
    }
}

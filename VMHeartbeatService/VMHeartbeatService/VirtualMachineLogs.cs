using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VMHeartbeatService
{
    public class VirtualMachineLogVM
    {
        public string roleName { get; set; }
        public string timeStamp { get; set; }
        public string courseID { get; set; }
        public string userID { get; set; }
        public string veProfileID { get; set; }
        public string comment { get; set; }
        public string machineInstance { get; set; }    
        public string machineName { get; set; }
    }

    public class VirtualMachineMapping
    {
        public string RoleName { get; set; }
        public int UserID { get; set; }
        public int VEProfileID { get; set; }
        public int CourseID { get; set; }
        public int MachineInstance { get; set; }
    }
}

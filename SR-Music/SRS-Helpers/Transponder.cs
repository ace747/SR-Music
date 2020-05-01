using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCS_SR_Music.SRS_Helpers
{
    public class Transponder
    {
        public enum IFFControlMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
            DISABLED = 2
        }

        public enum IFFStatus
        {
            OFF = 0,
            NORMAL = 1,
            IDENT = 2
        }

        public IFFControlMode control = IFFControlMode.DISABLED;

        public bool expansion = false;

        public int mode1 = -1;
        public int mode3 = -1;
        public bool mode4 = false;

        public int mic = -1;

        public IFFStatus status = IFFStatus.OFF;
    }
}

namespace DCS_SR_Music.SRS_Helpers
{
    public class DCSPlayerRadioInfo
    {
        // HOTAS or IN COCKPIT controls
        public enum RadioSwitchControls
        {
            HOTAS = 0,
            IN_COCKPIT = 1
        }

        public string name = "";
        private readonly DcsPosition pos = new DcsPosition { x = 0, y = 0, z = 0 };
        public volatile bool ptt = false;


        public readonly short selected = 0;
        public string unit = "";
        public readonly uint unitId = 0;

        public RadioInformation[] radios = new RadioInformation[11]; // 10 + intercom
        public RadioSwitchControls control = RadioSwitchControls.HOTAS;

        // This is where non aircraft "Unit" Ids start from for satcom intercom
        public readonly static uint UnitIdOffset = 100000001;

        // Global toggle enabling simultaneous transmission on multiple radios, activated via the AWACS panel
        public bool simultaneousTransmission = true;

        public DCSPlayerRadioInfo(System.Double frequency, int mod)
        {
            for (var i = 0; i < 11; i++)
            {
                radios[i] = new RadioInformation();
            }

            radios[0].enc = false;
            radios[0].encKey = 0;
            radios[0].encMode = 0;
            radios[0].freqMax = 1.0;
            radios[0].freqMin = 1.0;
            radios[0].freq = frequency;
            radios[0].modulation = (RadioInformation.Modulation) mod;
            radios[0].name = name;
            radios[0].secFreq = 0.0;
            radios[0].volume = 1.0f;
            radios[0].freqMode = 0;
            radios[0].volMode = 0;
            radios[0].expansion = false;
            radios[0].Station = -1;
            radios[0].simul = false;
        }
    }
}

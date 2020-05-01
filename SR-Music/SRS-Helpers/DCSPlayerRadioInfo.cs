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
        private readonly DCSPosition pos = new DCSPosition { x = 0, y = 0, z = 0 };
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

        public Transponder iff = new Transponder();

        public DCSPlayerRadioInfo()
        {
            for (var i = 0; i < radios.Length; i++)
            {
                radios[i] = new RadioInformation();
            }
        }
    }
}

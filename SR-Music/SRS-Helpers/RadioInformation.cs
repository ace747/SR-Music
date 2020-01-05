
namespace DCS_SR_Music.SRS_Helpers
{
    public class RadioInformation
    {
        public enum EncryptionMode
        {
            NO_ENCRYPTION = 0,
            ENCRYPTION_JUST_OVERLAY = 1,
            ENCRYPTION_FULL = 2,
            ENCRYPTION_COCKPIT_TOGGLE_OVERLAY_CODE = 3

            // 0  is no controls
            // 1 is FC3 Gui Toggle + Gui Enc key setting
            // 2 is InCockpit toggle + Incockpit Enc setting
            // 3 is Incockpit toggle + Gui Enc Key setting
        }

        public enum VolumeMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
        }

        public enum FreqMode
        {
            COCKPIT = 0,
            OVERLAY = 1,
        }

        public enum Modulation
        {
            AM = 0,
            FM = 1,
            INTERCOM = 2,
            DISABLED = 3
        }

        public bool enc = false; // encrytion enabled
        public byte encKey = 0;
        public EncryptionMode encMode = EncryptionMode.NO_ENCRYPTION;

        public double freqMax = 1;
        public double freqMin = 1;
        public double freq = 1;
        public Modulation modulation = Modulation.DISABLED;
        public string name = "";
        public double secFreq = 1;
        public float volume = 1.0f;

        public FreqMode freqMode = FreqMode.COCKPIT;
        public VolumeMode volMode = VolumeMode.COCKPIT;

        public bool expansion = false;

        public int Station = -1;

        public bool simul = false;
    }
}

namespace DCS_SR_Music.SRS_Helpers
{
    public struct DcsPosition
    {
        public double x;
        public double y;
        public double z;

        public override string ToString()
        {
            return $"Pos:[{x},{y},{z}]";
        }
    }
}

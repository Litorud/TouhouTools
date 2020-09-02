namespace TouhouTools
{
    public abstract class GameInfo
    {
        public string Code { get; internal set; }
        public string Name { get; internal set; }
        public string StartPath { get; internal set; }
        public string SaveFolder { get; internal set; }
    }
}
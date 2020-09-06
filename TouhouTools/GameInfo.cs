namespace TouhouTools
{
    public interface GameInfo
    {
        string Code { get; set; }
        string Name { get; set; }
        string StartPath { get; set; }
        string SaveFolder { get; set; }
    }
}
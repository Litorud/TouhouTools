namespace TouhouTools
{
    public class ShortcutGameInfo : GameInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string StartPath { get; set; }
        public string SaveFolder { get; set; }

        public ShortcutGameInfo(string code, string name, string startPath, string saveFolder)
        {
            Code = code;
            Name = name;
            StartPath = startPath;
            SaveFolder = saveFolder;
        }
    }
}
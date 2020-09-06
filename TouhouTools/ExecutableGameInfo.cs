namespace TouhouTools
{
    public class ExecutableGameInfo : GameInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string StartPath { get; set; }
        public string SaveFolder { get; set; }
        public string WorkingDirectory { get; set; }

        internal ExecutableGameInfo(string code, string name, string startPath, string saveFolder, string workingDirectory)
        {
            Code = code;
            Name = name;
            StartPath = startPath;
            SaveFolder = saveFolder;
            WorkingDirectory = workingDirectory;
        }
    }
}

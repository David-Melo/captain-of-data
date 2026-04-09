using Mafi;
using Mafi.Core.Prototypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataExtractorMod
{
    public class Reporter
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _success = new List<string>();
        private readonly string _exportDir;

        public Reporter(string exportDir)
        {
            _exportDir = exportDir;
        }
        public void WriteFile(string filename, string data)
        {
            Directory.CreateDirectory(_exportDir);
            File.WriteAllText($"{_exportDir}/{filename}", data);
            _success.Add(filename);
        }
        public void RecordError(Proto item)
        {
            Log.Info("###################################################");
            Log.Info("ERROR" + item.ToString());
            Log.Info("###################################################");
            _errors.Add(item.ToString());
        }
        public void Finish()
        {
            Directory.CreateDirectory(_exportDir);
            System.Text.StringBuilder reportBuilder = new System.Text.StringBuilder();
            reportBuilder.AppendLine("{");
            reportBuilder.AppendLine($"\"game_version\":\"{DataExtractor.GAME_VERSION}\",");
            reportBuilder.AppendLine($"\"date\":\"{DateTime.UtcNow}\",");
            reportBuilder.AppendLine($"\"files\":[{string.Join(", ", _success.Select(file => $"\"{file}\""))}],");
            reportBuilder.AppendLine($"\"errors\":[{string.Join(", ", _errors.Select(error => $"\"{error}\""))}],");
            reportBuilder.AppendLine($"\"success\":{(_errors.Count == 0 ? "true" : "false")}");
            reportBuilder.AppendLine("}");
            File.WriteAllText($"{_exportDir}/summary.json", reportBuilder.ToString());
        }
    }
}

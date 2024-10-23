using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Handler
{
    public class CSVHandler : AbsHandler
    {
        private ProcessHandler _processHandler;
        private RegionHandler _regionHandler;
        
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        private static char[] TRIM_CHARS = { '\"' };

        private int _dialogueIndex;
        private bool _isLoadingDialogue;
        private List<string> _keysToKeep;
        private const string DialoguePath = "Dialogues/";
        public HeaderType CurHeader { get; private set; }
        
        public enum HeaderType
        {
            NULL,
            OBJECT_NAME,
            KOR,
            ENG,
            JPN,
            CHN
        }
        
        public override void OnAwake(ProcessHandler processHandler)
        {
            _processHandler = processHandler;
            
            Debug.Log("CSVHandler Awake");
        }
        
        // (쉼표로 구분된)csv reader
        public List<Dictionary<string, object>> Read(string file)
        {
            var list = new List<Dictionary<string, object>>();
            TextAsset data = Resources.Load(file) as TextAsset;

            var lines = Regex.Split(data.text, LINE_SPLIT_RE);
            if (lines.Length <= 1) return list;

            var header = Regex.Split(lines[0], SPLIT_RE);
            
            for (var i = 1; i < lines.Length; i++)
            {
                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || values[0] == "") continue;

                var entry = new Dictionary<string, object>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j];
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                    object finalvalue = value;
                    int n;
                    float f;
                    if (int.TryParse(value, out n))
                    {
                        finalvalue = n;
                    }
                    else if (float.TryParse(value, out f))
                    {
                        finalvalue = f;
                    }
                    entry[header[j]] = finalvalue;
                }

                // 다이얼로그 불러올 때만 특정 키 제외 나머지 삭제
                if (_isLoadingDialogue)
                {
                    var keysToRemove = entry.Keys.Where(k => !_keysToKeep.Contains(k)).ToList();
                    foreach (var key in keysToRemove)
                    {
                        entry.Remove(key);
                    }
                }

                list.Add(entry);
            }
            return list;
        }

        private HeaderType CheckRegion()
        {
            RegionHandler.RegionType regionType = ProcessHandler.Instance.RegionHandler.CurRegionType;

            switch (regionType)
            {
                case RegionHandler.RegionType.KOR:
                    return HeaderType.KOR;
                
                case RegionHandler.RegionType.ENG:
                    return HeaderType.ENG;
                
                case RegionHandler.RegionType.JPN:
                    return HeaderType.JPN;
                
                case RegionHandler.RegionType.CHN:
                    return HeaderType.CHN;
                
                default:
                    return HeaderType.KOR;
            }
        }

        public List<Dictionary<string, object>> LoadDialogue(LocalizationHandler.DialogueType dialogueType)
        {
            _isLoadingDialogue = true;
            
            CurHeader = CheckRegion();
            _keysToKeep = new List<string> { HeaderType.OBJECT_NAME.ToString(), CurHeader.ToString() };
            
            return Read(DialoguePath + dialogueType.ToString());
        }
    }
}
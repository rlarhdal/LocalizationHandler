using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Handler
{
    public class CSVHandler : AbsHandler
    {
        [Header("CsvRead")]
        private ProcessHandler _processHandler;
        private RegionHandler _regionHandler;
        
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        private static char[] TRIM_CHARS = { '\"' };
        private const string PLACEHOLDER_PATTERN = @"\{(.+?)\}";  // {플레이스홀더} 패턴


        private int _dialogueIndex;
        private bool _isLoadingData;
        public List<string> KeysToKeep;
        private const string DialoguePath = "CSV/";

        public enum HeaderType
        {
            NULL,
            OBJECT_NAME,
            koreana,
            english,
            japanese,
            schinese
        }
        
        public enum FilePath
        {
            Null,
            Dialog,
            Skill,
            Character,
            UI
        }
        
        public override void OnAwake(ProcessHandler processHandler)
        {
            _processHandler = processHandler;
            
            Debug.Log("CSVHandler Awake");
            
            _regionHandler = _processHandler.RegionHandler;
        }
        
        // (쉼표로 구분된)csv reader
        public List<Dictionary<string, string>> Read(string file)
        {
            var list = new List<Dictionary<string, string>>();
            TextAsset data = Resources.Load(file) as TextAsset;

            if (data == null) return null;
            
            var lines = Regex.Split(data.text, LINE_SPLIT_RE);
            if (lines.Length <= 1) return list;

            var header = Regex.Split(lines[0], SPLIT_RE);
            
            for (var i = 1; i < lines.Length; i++)
            {
                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || values[0] == "") continue;

                var entry = new Dictionary<string, string>();
                for (var j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j];
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                    
                    value = value.Replace("<br>", "\n"); // 추가된 부분. 개행문자를 \n대신 <br>로 사용한다.
                    value = value.Replace("<c>", ",");
                    
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
                    entry[header[j]] = Convert.ToString(finalvalue);
                }
                
                // 다이얼로그 불러올 때만 특정 키 제외 나머지 삭제
                if (_isLoadingData)
                {
                    var keysToRemove = entry.Keys.Where(k => !KeysToKeep.Contains(k)).ToList();
                    foreach (var key in keysToRemove)
                    {
                        entry.Remove(key);
                    }
                }
                list.Add(entry);
            }
            return list;
        }
        
        public void SaveDialogue(string fileName, List<Dictionary<string, string>> saveDatas, RegionHandler.RegionType saveType, FilePath path)
        {
            //파일명이 유효한지 확인
            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("파일명이 유효하지 않습니다.");
                return;
            }

            string filePath = Path.Combine(Application.dataPath, "Resources/CSV" , path.ToString(), $"{fileName}.csv");

            //기존 파일이 있는지 확인
            bool fileExists = File.Exists(filePath);

            List<Dictionary<string, string>> existingData = new List<Dictionary<string, string>>();

            //파일이 이미 있으면 데이터를 읽어서 기존 데이터를 로드
            if (fileExists)
            {
                KeysToKeep = new List<string> { HeaderType.OBJECT_NAME.ToString(), saveType.ToString() };
                existingData = Read(DialoguePath+FilePath.Dialog+"/"+fileName); // 기존 데이터를 읽어옴
            }

            //새로 저장할 데이터와 기존 데이터를 합치고 중복을 처리
            var combinedData = new List<Dictionary<string, string>>(existingData);

            foreach (var newEntry in saveDatas)
            {
                var existingEntry = combinedData.FirstOrDefault(e => e["OBJECT_NAME"].ToString() == newEntry["OBJECT_NAME"].ToString());
                if (existingEntry != null)
                {
                    // 중복 데이터가 있다면 덮어씌움
                    existingEntry[saveType.ToString()] = newEntry[saveType.ToString()];
                    
                }
                else
                {
                    // 중복이 없으면 새로 추가
                    combinedData.Add(newEntry);
                }
            }

            //"OBJECT_NAME" 값으로 오름차순 정렬
            combinedData = combinedData.OrderBy(e => e["OBJECT_NAME"].ToString()).ToList();

            //기존 파일이 있으면 헤더 없이 추가, 없으면 헤더 작성 후 저장
            using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8)) // append 모드를 사용하여 기존 파일 아래에 추가
            {
                writer.WriteLine("OBJECT_NAME,koreana,english,japanese,schinese");

                // 기존 파일 아래로 데이터 작성 (전처리 위에서 다 했음으로 그대로 추가)
                foreach (var entry in combinedData)
                {
                    string objectName = entry.ContainsKey("OBJECT_NAME") ? entry["OBJECT_NAME"].ToString() : null;
                    string koreana = entry.ContainsKey("koreana") ? entry["koreana"].ToString() : null;
                    string english = entry.ContainsKey("english") ? entry["english"].ToString() : null;
                    string japanese = entry.ContainsKey("japanese") ? entry["japanese"].ToString() : null;
                    string schinese = entry.ContainsKey("schinese") ? entry["schinese"].ToString() : null;
                    
                    writer.WriteLine($"{objectName},\"{koreana}\",\"{english}\",\"{japanese}\",\"{schinese}\""); //쉼표가 있을 경우를 대비해서 큰따옴표로 감쌈.
                }
            }

            Debug.Log("데이터가 성공적으로 저장되었습니다.");
        }
        
        /// <summary>
        /// 템플릿 문자열에서 {text} 세트를 찾아 순서대로 교체
        /// </summary>
        public string ReplacePlaceholders(string template, string[] replacements)
        {
            // {text} 패턴에 해당하는 모든 매칭 찾기
            MatchCollection matches = Regex.Matches(template, @"\{[^}]+\}");

            // 각 {text}를 replacements 배열의 요소로 교체
            for (int i = 0; i < matches.Count; i++)
            {
                template = template.Replace(matches[i].Value, replacements[i]);
            }

            return template;
        }

        public List<Dictionary<string, string>> LoadLocalizationDatas(FilePath filePath, string localizationType)
        {
            _isLoadingData = true;
            
            KeysToKeep = new List<string> { HeaderType.OBJECT_NAME.ToString(), _regionHandler.CurRegionType.ToString() };
            return Read(DialoguePath + filePath + "/" + localizationType);
        }
    }
}

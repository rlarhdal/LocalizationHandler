using System.Collections.Generic;
using DefaultNamespace.UI;
using DefaultNamespace.UI.Retry;
using Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Handler
{
    public class LocalizationHandler : AbsHandler
    {
        private ProcessHandler _processHandler;
        private RegionHandler _regionHandler;

        public SkillSelectUIHandler SkillSelectUIHandler;
        public RetryUIHandler RetryUIHandler;
        public DialogHandler DialogHandler;

        public enum LocalizationType
        {
            None,
            Intro,
            Title,
            ProcessUI,
            RetryInfo,
            RetryUI,
            Room
        }
        
        public override void OnAwake(ProcessHandler processHandler)
        {
            _processHandler = processHandler;
            
            Debug.Log("LocalizationHandler Awake");

            _regionHandler = _processHandler.RegionHandler;
        }

        public override void OnSceneLoaded()
        {
            SetLocalization();
        }

        public void SetLocalization()
        {
            // 이벤트 형식으로 변경 예정
            DialogHandler.SetData();
            SkillSelectUIHandler.LocalizationSkillInfo(); // skillinfo
            SettingDialog(); // 다이얼로그 
            GetUILocalizationData(); // UI
            RetryUIHandler.LoadRetryData();
        }
        
        public void SettingDialog()
        {
            // 컷씬인지 확인
            if (_processHandler.CurSceneType != SceneHandler.SceneType.CutScene) return;
            
            //리젼에 맞는 폰트 가지고 오기 
            TMP_FontAsset curFont =
                _processHandler.TMPFontAssetHandler.GetDialogFontAssetByRegionType(_regionHandler.CurRegionType);
            
            //다이얼로그 오브젝트 배열로 받아오기
            List<TextHolder> objs = FindDialogueTextHolderObjs();

            //csv데이터 오브젝트에 삽입
            List<Dictionary<string, string>> dialogData = GetDatasWithCurrScene();
            ApplyData(objs, dialogData, curFont);
        }
        
        public void GetUILocalizationData()
        {
            //리젼에 맞는 폰트 가지고 오기 
            TMP_FontAsset curFont =
                _processHandler.TMPFontAssetHandler.GetUIFontAssetByRegionType(_regionHandler.CurRegionType);
            
            //다이얼로그 오브젝트 배열로 받아오기
            List<TextHolder> objs = FindUITextHolderObjs();
            
            //csv데이터 오브젝트에 삽입
            List<Dictionary<string, string>> localizationUIDatas = ProcessHandler.Instance.CSVHandler.LoadLocalizationDatas(CSVHandler.FilePath.UI, LocalizationType.ProcessUI.ToString());
            ApplyData(objs, localizationUIDatas, curFont);
        }

        private void ApplyData(List<TextHolder> objs, List<Dictionary<string, string>> datas, TMP_FontAsset font)
        {
            foreach (var VARIABLE in objs)
            {
                if (datas == null)
                {
                    Debug.LogWarning("LocalizationHandler에서 List<Dictionary<string, string>> 이 Null");
                    continue;
                }
                
                for (int i = 0; i < datas.Count; i++)
                {
                    if(datas[i].Count != 2) continue;
                    
                    string curDialogObjName = datas[i][CSVHandler.HeaderType.OBJECT_NAME.ToString()];
                    string dialog = datas[i][_regionHandler.CurRegionType.ToString()];

                    string identifier = VARIABLE.GetCurrentIdentifier() == null ? VARIABLE.gameObject.name : VARIABLE.GetCurrentIdentifier();
                    if (identifier == curDialogObjName)
                    {
                        VARIABLE.SetText(dialog);
                        VARIABLE.SetFontAsset(font);
                    }
                }
            }
        }


        private List<TextHolder> FindUITextHolderObjs()
        {
            List<TextHolder> textHolders = new List<TextHolder>(GameObject.FindObjectsByType<TextHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            List<TextHolder> uiTextHolders = new List<TextHolder>();
            
            foreach (var VARIABLE in textHolders)
            {
                if (VARIABLE.HolderType != TextHolder.TextHolderType.Dialogue)
                {
                    uiTextHolders.Add(VARIABLE);
                }
            }

            return uiTextHolders;
        }
        
        private List<TextHolder> FindDialogueTextHolderObjs()
        {
            List<TextHolder> textHolders = new List<TextHolder>(GameObject.FindObjectsByType<TextHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            List<TextHolder> dialogueTextHolders = new List<TextHolder>();
            
            foreach (var VARIABLE in textHolders)
            {
                if (VARIABLE.HolderType == TextHolder.TextHolderType.Dialogue)
                {
                    dialogueTextHolders.Add(VARIABLE);
                }
            }

            return dialogueTextHolders;
        }

        //>>임시<< 씬 이름 받아오기
        private List<Dictionary<string, string>> GetDatasWithCurrScene()
        {
            string curScene = SceneManager.GetActiveScene().name;
            
            switch (curScene)
            {
                case "intro":
                    return ProcessHandler.Instance.CSVHandler.LoadLocalizationDatas(CSVHandler.FilePath.Dialog, LocalizationType.Intro.ToString());
                
                case "TitleScreen":
                    return ProcessHandler.Instance.CSVHandler.LoadLocalizationDatas(CSVHandler.FilePath.Dialog, LocalizationType.Title.ToString());
                
                case "Room":
                    return ProcessHandler.Instance.CSVHandler.LoadLocalizationDatas(CSVHandler.FilePath.Dialog, LocalizationType.Room.ToString());
                
                default:
                    return ProcessHandler.Instance.CSVHandler.LoadLocalizationDatas(CSVHandler.FilePath.Dialog, LocalizationType.None.ToString());
            }
        }

    }
}

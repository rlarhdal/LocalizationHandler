using System;
using System.Collections.Generic;
using Dialogue;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = System.Object;

namespace Handler
{
    public class LocalizationHandler : AbsHandler
    {
        private ProcessHandler _processHandler;
        private RegionHandler _regionHandler;

        private List<TextHolder> _objs;
        private List<Dictionary<string, object>> _dialogData;

        public enum DialogueType
        {
            NULL,
            Intro,
            Lab,
            BossEncount,
            Ending,
        }
        
        public override void OnAwake(ProcessHandler processHandler)
        {
            _processHandler = processHandler;
            
            Debug.Log("LocalizationHandler Awake");
        }

        public void OnSceneLoaded()
        {
            // 초기화 신경 써야하는 것들
            if (_objs != null) _objs.Clear();

            SettingDialog();
        }
        public void SettingDialog()
        {
            // 컷씬인지 확인
            if (_processHandler.CurSceneType != SceneHandler.SceneType.CutScene) return;
            
            //리젼에 맞는 폰트 가지고 오기 
            TMP_FontAsset curFont = GetRegionFont();
            
            //다이얼로그 오브젝트 배열로 받아오기
            _objs = FindDialogueObjs();

            //폰트 넣기
            foreach (var VARIABLE in _objs)
            {
                VARIABLE.TextMeshProUGUI.font = curFont;
            }

            //csv데이터 오브젝트에 삽입
            MatchingDialog();
        }

        private void MatchingDialog()
        {
            _dialogData = GetCurrentScene();

            foreach (var VARIABLE in _objs)
            {
                for (int j = 0; j < _dialogData.Count; j++)
                {
                    Object curDialogObjName = _dialogData[j][CSVHandler.HeaderType.OBJECT_NAME.ToString()];

                    if (VARIABLE.gameObject.name == curDialogObjName.ToString())
                    {
                        VARIABLE.TextMeshProUGUI.text =
                            _dialogData[j][ProcessHandler.Instance.CSVHandler.CurHeader.ToString()].ToString();
                    }
                }
            }
        }

        private TMP_FontAsset GetRegionFont()
        {
            // 리젼 갖고 있으면 리젼에 맞는 폰트 가져오기
            return ProcessHandler.Instance.RegionHandler.GetFontAssetByRegionType(ProcessHandler.Instance.RegionHandler.CurRegionType);
        }

        private List<TextHolder> FindDialogueObjs()
        {
            return new List<TextHolder>(GameObject.FindObjectsOfType<TextHolder>());
        }

        //>>임시<< 씬 이름 받아오기
        private List<Dictionary<string, object>> GetCurrentScene()
        {
            string curScene = SceneManager.GetActiveScene().name;
            
            switch (curScene)
            {
                case "intro":
                    return ProcessHandler.Instance.CSVHandler.LoadDialogue(DialogueType.Intro);
                
                default:
                    return ProcessHandler.Instance.CSVHandler.LoadDialogue(DialogueType.Intro);
            }
        }
    }
}
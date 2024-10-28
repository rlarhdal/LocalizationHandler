using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dialogue;
using Handler;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DialogTool : OdinEditorWindow
{
    [InfoBox("다이얼로그 오브젝트 확인용")]
    [ShowInInspector, ReadOnly]
    private static List<TextMeshProUGUI> _textObjs = new();
    [InfoBox("저장 포맷에 맞지 않은 이름")]
    [ShowInInspector, ReadOnly]
    private static List<GameObject> _needToChangeName = new();
    
    private static CSVHandler _csvHandler;
    private static List<Dictionary<string, object>> _saveDatas = new();
    
    private string _csvName = null;
    
    //window바-Tools에 생성
    [MenuItem(("Tools/DialogTool"))]
    private static void OpenWindow()
    {
        _needToChangeName.Clear();
        _saveDatas.Clear();
        _textObjs.Clear();

        _csvHandler = new CSVHandler();
        
        GetWindow<DialogTool>().Show();
    }

    [TitleGroup("Dialog-To-Csv")]
    [InfoBox("다이얼로그 저장 전 저장할 csv 파일 닫기")]
    [Button(ButtonSizes.Large)]
    private void 다이얼로그_저장하기()
    {
        OpenWindow(); //초기화
        FindSaveDatas(); // 저장할 데이터 찾기
        GetCurScene(); // filepath으로 보낼 file 이름 받기
        
        _csvHandler.SaveDialogue("Dialogues/"+_csvName, _saveDatas); // csv파일로 저장하기
    }

    [TitleGroup("Control-TextHolder")]
    [Button(ButtonSizes.Large)]
    private void TextHolder만_붙이기()
    {
        _textObjs = FindTextMeshProUGUI();
        
        foreach (var VARIABLE in _textObjs)
        {
            AttachTextHolder(VARIABLE);
        }
    }

    [Button(ButtonSizes.Large), PropertySpace(spaceBefore:0, spaceAfter:20)]
    private void TextHolder만_삭제()
    {
        _textObjs = FindTextMeshProUGUI();

        foreach (var VARIABLE in _textObjs)
        {
            DeleteTextHolder(VARIABLE);
        }
    }

    private void DeleteTextHolder(TextMeshProUGUI variable)
    {
        GameObject parent = variable.transform.parent.gameObject;

        //부모 없는 다이얼로그일 때,
        if (parent.name == "Canvas")
        {
            DestroyImmediate(variable.gameObject.GetComponent<TextHolder>());
        }
        else
        {
            DestroyImmediate(parent.GetComponent<TextHolder>());
        }
    }

    private void FindSaveDatas()
    {
        //TextMeshProUGUI
        _textObjs = FindTextMeshProUGUI();

        foreach (var VARIABLE in _textObjs)
        {
            //TextHolder 붙이기
            AttachTextHolder(VARIABLE);
            
            Dictionary<string, object> data = new Dictionary<string, object>();
            
            GameObject obj = VARIABLE.transform.parent.gameObject;
            //데이터 전처리
            if (Preprocessing(obj, obj.name, VARIABLE.text))
            {
                VARIABLE.text = Correction(VARIABLE.text);
                data.Add("OBJECT_NAME", obj.name);
                data.Add("KOR", VARIABLE.text);
            
                _saveDatas.Add(data);
            }
        }
    }

    private bool Preprocessing(GameObject obj, string objName, string kor)
    {
        // 1. 이름이나, text 비어있는지 확인
        if (objName == "" || kor == null) 
            return false;
        
        // 2. 이름 포맷에 맞는지 확인
        if (!Regex.IsMatch(objName, @"^\d+_.*_\d+$"))
        {
            Debug.LogError("오브젝트 이름의 형식이 맞지 않습니다.");
            _needToChangeName.Add(obj);
            return false;
        }

        return true;
    }

    private string Correction(string kor)
    {
        // 3. text ',', 공백 찾아서 처리하기
        kor = kor.TrimEnd('\r', '\n', ' '); //공백 제거
        if (!Regex.IsMatch(kor, "^\".*\"$") && kor.Contains(","))
        {
            kor = $"\"{kor}\""; //쉼표 처리
        }

        return kor;
    }

    private void AttachTextHolder(TextMeshProUGUI variable)
    {
        GameObject parent = variable.transform.parent.gameObject;

        if (parent.TryGetComponent(out TextHolder holder)) return;
        
        TextHolder textHolder;
        //부모 없는 다이얼로그일 때,
        if (parent.name == "Canvas")
        {
            textHolder = variable.gameObject.AddComponent<TextHolder>();
        }
        else
        {
            textHolder = parent.AddComponent<TextHolder>();
        }
        textHolder.TextMeshProUGUI = variable;
    }
    
    private void GetCurScene()
    {
        string curScene = SceneManager.GetActiveScene().name;

        switch (curScene)
        {
            //컷씬 추가되면 CASE 추가
            //csv파일은 자동 생성됨
            case "intro":
                _csvName = "Intro"; break;
            
            case "Lab":
                _csvName = "Lab"; break;
            
            case "boss_encounter":
                _csvName = "BossEncounter"; break;
            
            default:
                _csvName = "Intro"; break;
        }
    }
    
    private List<TextMeshProUGUI> FindTextMeshProUGUI()
    {
        if(_textObjs != null)
            _textObjs.Clear();
        
        MonoBehaviour[] monos = FindObjectsOfType<MonoBehaviour>(true);

        foreach (var VARIABLE in monos)
        {
            if (VARIABLE.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
            {
                _textObjs.Add(textMeshProUGUI);
            }
        }

        _textObjs = _textObjs.Distinct().ToList();
         
        return _textObjs;
    }
}

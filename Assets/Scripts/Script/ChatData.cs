using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Events;
using GoogleSheetsToUnity;
#endif

[Serializable]
public struct Chat
{
    public int id;

    [TextArea]
    public string content;

    public Chat(int id, string content)
    {
        this.id = id;
        this.content = content;
    }
}

[CreateAssetMenu(fileName = "ChatData", menuName = "Scriptable Objects/ChatData")]
public class ChatData : DataReaderBase
{
    [Header("Chat Data")]
    public List<Chat> chatDataList = new List<Chat>();

#if UNITY_EDITOR
    internal void UpdateStats(List<GSTU_Cell> list)
    {
        int id = 0;
        string content = "";

        for (int i = 0; i < list.Count; i++)
        {
            switch (list[i].columnId)
            {
                case "id":
                    int.TryParse(list[i].value, out id);
                    break;

                case "content":
                    content = list[i].value;
                    break;
            }
        }

        chatDataList.Add(new Chat(id, content));
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(ChatData))]
public class ChatDataReaderEditor : Editor
{
    private ChatData data;

    private void OnEnable()
    {
        data = (ChatData)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(20);
        GUILayout.Label("스프레드 시트 읽어오기");

        if (GUILayout.Button("데이터 읽기(API 호출)"))
        {
            data.chatDataList.Clear();
            UpdateStats(UpdateMethodOne);
        }
    }

    private void UpdateStats(UnityAction<GstuSpreadSheet> callback, bool mergedcells = false)
    {
        SpreadsheetManager.Read(
            new GSTU_Search(data.sheetURL, data.sheetName),
            callback,
            mergedcells
        );
    }

    private void UpdateMethodOne(GstuSpreadSheet sheet)
    {
        for (int i = data.startRowIndex; i <= data.endRowIndex; i++)
        {
            if (sheet.rows.ContainsKey(i))
            {
                data.UpdateStats(sheet.rows[i]);
            }
        }

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }
}
#endif
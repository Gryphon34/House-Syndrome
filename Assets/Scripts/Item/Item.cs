using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;      // 아이템 이름
    [TextArea]
    public string description;   // 아이템 설명 (일기장 내용 등)
}
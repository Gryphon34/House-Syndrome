using UnityEngine;
using System.Collections;

public class MoveObjectController : MonoBehaviour 
{
	public float reachRange = 1.8f;

	[Tooltip("체크 시 상호작용/레이캐스트 동작을 로그로 출력")]
	public bool debugMode = false;

	private Animator anim;
	private Camera fpsCam;
	private GameObject player;

	private const string animBoolName = "isOpen_Obj_";

	private bool playerEntered;
	private bool showInteractMsg;
	private GUIStyle guiStyle;
	private string msg;

	private int rayLayerMask;
	private bool useInteractLayer;

	void Start()
	{
		player = GameObject.FindGameObjectWithTag("Player");
		if (player == null && debugMode) Debug.LogWarning("[MoveObjectController] Tag 'Player'인 오브젝트가 없습니다.");

		fpsCam = Camera.main;
		if (fpsCam == null)
		{
			Debug.LogError("[MoveObjectController] Tag가 'MainCamera'인 카메라가 없습니다.");
			return;
		}

		anim = GetComponent<Animator>();
		if (anim == null)
		{
			Debug.LogError("[MoveObjectController] 같은 오브젝트에 Animator가 없습니다.", gameObject);
			return;
		}
		anim.enabled = false;

		int iRayLM = LayerMask.NameToLayer("InteractRaycast");
		if (iRayLM >= 0)
		{
			rayLayerMask = 1 << iRayLM;
			useInteractLayer = true;
		}
		else
		{
			rayLayerMask = ~0;
			useInteractLayer = false;
			if (debugMode) Debug.LogWarning("[MoveObjectController] 'InteractRaycast' 레이어가 없어 모든 레이어를 대상으로 레이캐스트합니다.");
		}

		setupGui();
	}

	void OnTriggerEnter(Collider other)
	{		
		if (other.gameObject == player)		//player has collided with trigger
		{			
			playerEntered = true;

		}
	}

	void OnTriggerExit(Collider other)
	{		
		if (other.gameObject == player)		//player has exited trigger
		{			
			playerEntered = false;
			//hide interact message as player may not have been looking at object when they left
			showInteractMsg = false;		
		}
	}



	void Update()
	{
		if (fpsCam == null || anim == null) return;

		if (playerEntered)
		{
			Vector3 rayOrigin = fpsCam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
			RaycastHit hit;

			if (Physics.Raycast(rayOrigin, fpsCam.transform.forward, out hit, reachRange, rayLayerMask, QueryTriggerInteraction.Collide))
			{
				MoveableObject moveableObject = null;
				if (!isEqualToParent(hit.collider, out moveableObject))
					return;

				if (moveableObject != null)
				{
					showInteractMsg = true;
					string animBoolNameNum = animBoolName + moveableObject.objectNumber.ToString();

					bool hasParam = HasAnimatorBool(anim, animBoolNameNum);
					if (debugMode && !hasParam)
						Debug.LogWarning("[MoveObjectController] Animator에 Bool 파라미터 '" + animBoolNameNum + "'가 없습니다. Animator Controller에 추가하세요.", gameObject);

					bool isOpen = hasParam ? anim.GetBool(animBoolNameNum) : false;
					msg = getGuiMsg(isOpen);

					if (Input.GetKeyUp(KeyCode.E) || Input.GetButtonDown("Fire1"))
					{
						if (debugMode) Debug.Log("[MoveObjectController] 상호작용 키 입력, isOpen=" + isOpen + ", param=" + animBoolNameNum);
						anim.enabled = true;
						if (hasParam)
						{
							anim.SetBool(animBoolNameNum, !isOpen);
							msg = getGuiMsg(!isOpen);
						}
					}
				}
			}
			else
			{
				showInteractMsg = false;
				if (debugMode && (Input.GetKeyUp(KeyCode.E) || Input.GetButtonDown("Fire1")))
					Debug.Log("[MoveObjectController] E 키 눌림 but 레이캐스트에 히트 없음 (거리/방향/레이어 확인)");
			}
		}
	}

	static bool HasAnimatorBool(Animator a, string name)
	{
		if (a == null || string.IsNullOrEmpty(name)) return false;
		foreach (var p in a.parameters)
			if (p.type == AnimatorControllerParameterType.Bool && p.name == name) return true;
		return false;
	}

	//is current gameObject equal to the gameObject of other.  check its parents
	private bool isEqualToParent(Collider other, out MoveableObject draw)
	{
		draw = null;
		bool rtnVal = false;
		try
		{
			int maxWalk = 6;
			draw = other.GetComponent<MoveableObject>();

			GameObject currentGO = other.gameObject;
			for(int i=0;i<maxWalk;i++)
			{
				if (currentGO.Equals(this.gameObject))
				{
					rtnVal = true;	
					if (draw== null) draw = currentGO.GetComponentInParent<MoveableObject>();
					break;			//exit loop early.
				}

				//not equal to if reached this far in loop. move to parent if exists.
				if (currentGO.transform.parent != null)		//is there a parent
				{
					currentGO = currentGO.transform.parent.gameObject;
				}
			}
		} 
		catch (System.Exception e)
		{
			Debug.Log(e.Message);
		}
			
		return rtnVal;

	}
		

	#region GUI Config

	//configure the style of the GUI
	private void setupGui()
	{
		guiStyle = new GUIStyle();
		guiStyle.fontSize = 16;
		guiStyle.fontStyle = FontStyle.Bold;
		guiStyle.normal.textColor = Color.white;
		msg = "Press E/Fire1 to Open";
	}

	private string getGuiMsg(bool isOpen)
	{
		string rtnVal;
		if (isOpen)
		{
			rtnVal = "Press E/Fire1 to Close";
		}else
		{
			rtnVal = "Press E/Fire1 to Open";
		}

		return rtnVal;
	}

	void OnGUI()
	{
		if (showInteractMsg)  //show on-screen prompts to user for guide.
		{
			GUI.Label(new Rect (50,Screen.height - 50,200,50), msg,guiStyle);
		}
	}		
	//End of GUI Config --------------
	#endregion
}


using UnityEngine;
using System;
using KBEngine;
using System.Collections.Generic;
using scopely.msgpacksharp;
using grpania_unity3d_demo;

public class GameEntity : MonoBehaviour 
{
	public bool isPlayer = false;
	private Vector3 _position = Vector3.zero;
	private Vector3 _eulerAngles = Vector3.zero;
	private Vector3 _scale = Vector3.zero;
	private Camera playerCamera = null;
	public string entity_name;
	
	public string hp = "100/100";
	
	float npcHeight = 2.0f;
	private CharacterMotor motor;

	private Vector3 targetPosition, pointPosition;
	private float speed = 2.0f;

	private InputControllerData inputControllerData;

	private float mov;
	private long zStamp;

	uint count;
	uint status;
	float beat;

	void Awake ()   
	{
        if (isPlayer)
        {
			count = 0;
			status = 0;
			mov = 0;
			beat = 0;
			zStamp = 0;
			inputControllerData = gameObject.GetComponent<InputControllerData>();
		}
	}

	void Start()
	{
		motor = gameObject.GetComponent<CharacterMotor>();

		if (this.name == "player")
		{
			isPlayer = true;
		}
		else
		{
			//motor.inputMoveDirection = this.transform.position;
		}

		pointPosition = this.transform.position;
		targetPosition = this.transform.position;
	}

	void OnGUI()
	{
		if (!isPlayer)
			return;

		if (!gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().GetComponent<Renderer>().isVisible)
			return;
		
		Vector3 worldPosition = new Vector3 (transform.position.x , transform.position.y + npcHeight, transform.position.z);

		if (playerCamera == null)
			playerCamera = Camera.current;

		//根据NPC头顶的3D坐标换算成它在2D屏幕中的坐标
		Vector2 uiposition = playerCamera.WorldToScreenPoint(worldPosition);
		
		//得到真实NPC头顶的2D坐标
		uiposition = new Vector2 (uiposition.x, Screen.height - uiposition.y);
		
		//计算NPC名称的宽高
		Vector2 nameSize = GUI.skin.label.CalcSize (new GUIContent(entity_name));
		
		//设置显示颜色为黄色
		GUI.color  = Color.yellow;
		
		//绘制NPC名称
		GUI.Label(new Rect(uiposition.x - (nameSize.x / 2), uiposition.y - nameSize.y - 5.0f, nameSize.x, nameSize.y), entity_name);
		
		//计算NPC名称的宽高
		Vector2 hpSize = GUI.skin.label.CalcSize (new GUIContent(hp));

		//设置显示颜色为红
		GUI.color = Color.red;
		
		//绘制HP
		GUI.Label(new Rect(uiposition.x - (hpSize.x / 2), uiposition.y - hpSize.y - 30.0f, hpSize.x, hpSize.y), hp);
	} 
  
    public Vector3 eulerAngles {  
		get
		{
			return _eulerAngles;
		}

		set
		{
			_eulerAngles = value;
			
			if(gameObject != null)
			{
				gameObject.transform.eulerAngles = _eulerAngles;
			}
		}    
    }   

	public void set_state(sbyte v)
	{
		if (v == 3) 
		{
			if(isPlayer)
				gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().material.color = Color.green;
			else
				gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().material.color = Color.red;
		} else if (v == 0) 
		{
			if(isPlayer)
				gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().material.color = Color.blue;
			else
				gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().material.color = Color.white;
		} else if (v == 1) {
			gameObject.transform.Find ("Graphics").GetComponent<MeshRenderer> ().material.color = Color.black;
		}
	}

	public void Pong(uint count)
	{
		status = 1;
	}

	private long GetTime()
	{
		//精确到毫秒
		return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
	}

	void Update () 
	{
        if (!isPlayer)
        {
			if (Math.Round(transform.position.x) != Math.Round(targetPosition.x) && Math.Round(transform.position.z) != Math.Round(targetPosition.z))
			{
				transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
			} else
			{
				targetPosition.x = pointPosition.x + UnityEngine.Random.Range(-100, 100);
				targetPosition.y = pointPosition.y;
				targetPosition.z = pointPosition.z + UnityEngine.Random.Range(-100, 100);
			}
        } else
        {
			if(status == 0)
            {
				if(Time.time - beat > 1)
                {
					PingSend(count++);
					beat = Time.time;
				} 

			} else if(status == 1)
            {

				if (inputControllerData.mov != 0 && inputControllerData.mov != this.mov)
				{
					if (inputControllerData.z != 0)
					{
						long ctiem = GetTime();
						if(ctiem - zStamp > 300)
                        {
							MoveSend(10);
							zStamp = ctiem;
						}
					}else
                    {
						MoveSend(10);
					}
                }
                else
                {
					if (inputControllerData.z != 0)
					{
						long ctiem = GetTime();
						if (ctiem - zStamp > 300)
						{
							MoveSend(10);
							zStamp = ctiem;
						}
					}
					else
					{
						//停止移动
						MoveSend(0);
					}
				}
				
				this.mov = inputControllerData.mov;
            }
		}
	}

	public void MoveSend(float velocity)
	{
		//this.transform.position;
		Vector3 euler = this.transform.rotation.eulerAngles;
		MemoryStream ms = new MemoryStream();

		euler.y = euler.y - 90;

		byte[] mybuf = new byte[512];
		int pos = MsgPackSerializer.SerializeObject("Move", mybuf, 0);
		pos = MsgPackSerializer.SerializeObject(this.transform.position.x, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(this.transform.position.y, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(this.transform.position.z, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(euler.x, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(euler.y, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(euler.z, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(velocity, mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(Tool.TimeStamp(), mybuf, pos);
		pos = MsgPackSerializer.SerializeObject(0, mybuf, pos);

		ms.writeUint32((ushort)(21+ pos));
		ms.writeUint8(4);
		ms.writeUint64(0);
		ms.writeUint64(0);
		ms.writeBuffer(mybuf, pos);

		Main._networkInterface.send(ms);
	}

	public void PingSend(uint count)
	{
		//this.transform.position;
		Vector3 euler = this.transform.rotation.eulerAngles;
		MemoryStream ms = new MemoryStream();

		byte[] mybuf = new byte[512];
		int pos = MsgPackSerializer.SerializeObject("Ping", mybuf, 0);
		pos = MsgPackSerializer.SerializeObject(count, mybuf, pos);

		ms.writeUint32((ushort)(21 + pos));
		ms.writeUint8(4);
		ms.writeUint64(0);
        ms.writeUint64(0);
		ms.writeBuffer(mybuf, pos);

		Main._networkInterface.send(ms);
	}
}


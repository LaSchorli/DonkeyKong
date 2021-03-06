﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using DG.Tweening;
public class Init : MonoBehaviour {

	public PhysicsMaterial2D Metal;
	[SerializeField] private string address;
	[SerializeField] private int port;
	[SerializeField] private int serverPort;
	[SerializeField] private bool sendOSC;
	[SerializeField] private bool receiveOSC;
	[SerializeField] private bool muteMario;

	public static float receivedPitch;
	public static float receivedDur;
	private OSCReceiver mReceiver;
	private int idx;
	private DKThrow DK;
	private Dictionary<string, ServerLog> servers;
	private Dictionary<string, ClientLog> clients;

	public UnityEngine.UI.RawImage bg;
	INotePositions[] cb;
	private static Init instance;

	public static List<float> receivedParams;

	private string GATE_0 = "0";
	private string GATE_1 = "1";
	private string GATE_2 = "2";

	static public Vector2[] gateNotes;
	private NotePositions notePosition;

	void Awake()
	{
		DontDestroyOnLoad(this);

		if (instance == null)
		{
			instance = this;
		}
		else
		{
			DestroyObject(gameObject);
			return;
		}
		
		Application.runInBackground = true;
		foreach (var i in GameObject.FindObjectsOfType<BoxCollider2D>())
		{
			i.GetComponent<BoxCollider2D>().sharedMaterial = Metal;
			// i.GetComponent<BoxCollider2D>().isTrigger = true;
			// BoxCollider2D bc = i.gameObject.AddComponent(typeof(BoxCollider2D)) as BoxCollider2D;
			// i.GetComponent<BoxCollider2D>().isTrigger = true;
			i.size = new Vector2(0.18f,0.1f);
		}


		if (sendOSC)
		{
			OSCSender sender = new OSCSender(address, port);   
		}

		if (receiveOSC)
		{
			if (mReceiver == null){
				Debug.Log("create server");
				mReceiver = new OSCReceiver("dkserver", serverPort);
			}
		}

		if (muteMario)
		{
			
		}

		notePosition = FindObjectOfType<NotePositions>();
		//receivedParams = new List<float>();
		servers = new Dictionary<string, ServerLog>();
		clients = new Dictionary<string,ClientLog> ();

		gateNotes = new Vector2[3];

        
    }

	void Start (){
		
		cb = FindObjectsOfType<NotePositions>();
        gateNotes[0] = new Vector2(70, 200);
        gateNotes[1] = new Vector2(70, 200);
        gateNotes[2] = new Vector2(70, 200);
        int cnt = 0;

        foreach (var item in cb)
        {
            item.PushNote(cnt, gateNotes[cnt]);
            cnt++;
        }


    }
	// Receive OSC
	// TODO: we need to poll this info from the main thread. So
	// it would be better to have a sync queue from the worker thread instead
	void Update()
	{
		servers = OSCHandler.Instance.Servers;
		
		OSCHandler.Instance.UpdateLogs();

		foreach (KeyValuePair<string, ServerLog> item in servers) {
			// If we have received at least one packet,
			// show the last received from the log in the Debug console
			if (item.Value.log.Count > 0) {
				int lastPacketIndex = item.Value.packets.Count - 1;
				
				//if (lastPacketIndex == idx) return;
				// UnityEngine.Debug.Log (String.Format ("SERVER: {0} ADDRESS: {1} VALUE : {2}", 
				// 										item.Key, // Server name
				// 										item.Value.packets [lastPacketIndex].Address, // OSC address
				// 										item.Value.packets [lastPacketIndex].Data [0].ToString ())); //First data value
				ParseOSC(item.Value.packets [lastPacketIndex]);	
			
				//idx = lastPacketIndex;
				//converts the values into MIDI to scale the cube
				// float tempVal = float.Parse (item.Value.packets [lastPacketIndex].Data [0].ToString ());
				// cube.transform.localScale = new Vector3 (tempVal, tempVal, tempVal);
			}
		}

	}
	void ParseOSC(UnityOSC.OSCPacket packet)
	{
		if ( String.Equals( packet.Address, OSCReceiver.throwcmd ) )
		{
			Debug.Log("throw");
			// TODO: FindObjectOfType is costly with each call
			// store reference and destroy on level load 
			FindObjectOfType<DKThrow>().Throw();
		} 

		if ( String.Equals( packet.Address, OSCReceiver.beatcmd ) )
		{
			DOTween.ToAlpha(()=> bg.color, x=> bg.color = x, .6f, .05f).SetEase(Ease.Flash).OnComplete(()=>{
				DOTween.ToAlpha(()=> bg.color, x=> bg.color = x, 0, .05f).SetEase(Ease.Flash);
			});
		}
     
        if ( String.Equals( packet.Address, OSCReceiver.notecmd) ) {
            Debug.Log(packet.Address + " " + OSCReceiver.notecmd + 0);
			receivedPitch = (float)packet.Data[0]; // pitch
			receivedDur = (float)packet.Data[1]; // duration
			gateNotes[0] = new Vector2(receivedPitch, receivedDur);
			foreach (var item in cb)
			{
				item.PushNote(0, gateNotes[0]);
			}
			
		} else if ( String.Equals( packet.Address, OSCReceiver.notecmd + 1) ) {
            Debug.Log(packet.Address + " " + OSCReceiver.notecmd + 1);
            receivedPitch = (float)packet.Data[0]; // pitch
			receivedDur = (float)packet.Data[1]; // duration
			gateNotes[1] = new Vector2(receivedPitch, receivedDur);
			foreach (var item in cb)
			{
				item.PushNote(1, gateNotes[1]);
			}
		} else if ( String.Equals( packet.Address, OSCReceiver.notecmd + 2) ) {
			receivedPitch = (float)packet.Data[0]; // pitch
			receivedDur = (float)packet.Data[1]; // duration
			gateNotes[2] = new Vector2(receivedPitch, receivedDur);
			foreach (var item in cb)
			{
				item.PushNote(2, gateNotes[2]);
			}
		}

		packet.clear();

	}
}

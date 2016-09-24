using UnityEngine;
using System.Collections.Generic;
using RTS;
using Newtonsoft.Json;

public class Building : WorldObject {
	public float maxBuildProgress;
	protected Queue< string > buildQueue;
	private float currentBuildProgress = 0.0f;
	private Vector3 spawnPoint;
	protected Vector3 rallyPoint;
	public Texture2D rallyPointImage;
	public Texture2D sellImage;

	private bool needsBuilding = false;

	public override bool IsActive { get { return !needsBuilding; } }

	protected override void Awake() {
		base.Awake();

		buildQueue = new Queue< string >();
		float spawnX = selectionBounds.center.x + transform.forward.x * selectionBounds.extents.x + transform.forward.x;
		float spawnZ = selectionBounds.center.z + transform.forward.z + selectionBounds.extents.z + transform.forward.z;
		spawnPoint = new Vector3(spawnX, 0.0f, spawnZ);
		rallyPoint = spawnPoint;
	}

	protected override void Start () {
		base.Start();
	}

	protected override void Update () {
		base.Update();
		ProcessBuildQueue();
	}

	protected override void OnGUI() {
		base.OnGUI();
		if(needsBuilding) DrawBuildProgress();
	}

	protected void CreateUnit(string unitName) {
		GameObject unit = ResourceManager.GetUnit(unitName);
		Unit unitObject = unit.GetComponent< Unit >();
		int power = 1;
		if (player.GetResourceAmount (ResourceType.Power) + power <= player.GetResourceLimit (ResourceType.Power) &&
			player.GetResourceAmount(ResourceType.Gold) - unitObject.cost >= 0)  {
			buildQueue.Enqueue (unitName);
			if (player && unitObject)
				player.RemoveResource (ResourceType.Gold, unitObject.cost);
			player.AddResource (ResourceType.Power, power);
		}
	}

	protected void ProcessBuildQueue() {
		if(buildQueue.Count > 0) {
			currentBuildProgress += Time.deltaTime * ResourceManager.BuildSpeed;
			if(currentBuildProgress > maxBuildProgress) {
				if(player) player.AddUnit(buildQueue.Dequeue(), spawnPoint, rallyPoint, transform.rotation, this);
				currentBuildProgress = 0.0f;
			}
		}
	}
	public string[] getBuildQueueValues() {
		string[] values = new string[buildQueue.Count];
		int pos=0;
		foreach(string unit in buildQueue) values[pos++] = unit;
		return values;
	}

	public float getBuildPercentage() {
		return currentBuildProgress / maxBuildProgress;
	}
	public override void SetSelection(bool selected, PlayingArea playingArea) {
		base.SetSelection(selected, playingArea);
		if(player) {
			RallyPoint flag = player.GetComponentInChildren< RallyPoint >();
			if(selected) {
				if(flag && player.human && spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition) {
					Debug.Log (rallyPoint);
					flag.transform.localPosition = rallyPoint;
					flag.transform.forward = transform.forward;
					flag.Enable();
				}
			} else {
				if(flag && player.human) flag.Disable();
			}
		}
	}
	public bool hasSpawnPoint() {
		return spawnPoint != ResourceManager.InvalidPosition && rallyPoint != ResourceManager.InvalidPosition;
	}
	public override void SetHoverState(GameObject hoverObject) {
		base.SetHoverState(hoverObject);
		//only handle input if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			if(WorkManager.ObjectIsGround(hoverObject)) {
				if(player.hud.GetPreviousCursorState() == CursorState.RallyPoint) player.hud.SetCursorState(CursorState.RallyPoint);
			}
		}
	}

	public override void RightMouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		base.RightMouseClick(hitObject, hitPoint, controller);
		//only handle iput if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			if(WorkManager.ObjectIsGround(hitObject)) {
				if((player.hud.GetCursorState() == CursorState.RallyPoint || player.hud.GetPreviousCursorState() == CursorState.RallyPoint) && hitPoint != ResourceManager.InvalidPosition) {
					SetRallyPoint(hitPoint);
				}
			}
		}
	}
	public void SetRallyPoint(Vector3 position) {
		rallyPoint = position;
		if(player && player.human && currentlySelected) {
			RallyPoint flag = player.GetComponentInChildren< RallyPoint >();
			if(flag) flag.transform.localPosition = rallyPoint;
		}
	}
	public void Sell() {
		if(player) player.AddResource(ResourceType.Gold, sellValue);
		if(currentlySelected) SetSelection(false, playingArea);
		Destroy(this.gameObject);
	}
	public void StartConstruction() {
		CalculateBounds();
		needsBuilding = true;
		hitPoints = 0;
	}
	private void DrawBuildProgress() {
		GUI.skin = ResourceManager.SelectBoxSkin;
		Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
		//Draw the selection box around the currently selected object, within the bounds of the main draw area
		GUI.BeginGroup(playingArea.area);
		CalculateCurrentHealth(0.5f, 0.99f);
		DrawHealthBar(selectBox, "Building ...");
		GUI.EndGroup();
	}
	protected override void DrawSelectionBox(Rect selectBox) {
		GUI.Box(selectBox, "");
		CalculateCurrentHealth(0.35f, 0.65f);
		DrawHealthBar(selectBox, "");
	}

	protected override void CalculateCurrentHealth(float lowSplit, float highSplit) {
		healthPercentage = (float)hitPoints / (float)maxHitPoints;
		if(healthPercentage > highSplit) healthStyle.normal.background = ResourceManager.HealthyTexture;
		else if(healthPercentage > lowSplit) healthStyle.normal.background = ResourceManager.DamagedTexture;
		else healthStyle.normal.background = ResourceManager.CriticalTexture;
	}

	protected void DrawHealthBar(Rect selectBox, string label) {
		healthStyle.padding.top = -20;
		healthStyle.fontStyle = FontStyle.Bold;
		GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), label, healthStyle);
	}

	public bool UnderConstruction() {
		return needsBuilding;
	}

	public void Construct(int amount) {
		hitPoints += amount;
		if(hitPoints >= maxHitPoints) {
			hitPoints = maxHitPoints;
			needsBuilding = false;
			RestoreMaterials();
			SetTeamColor ();
		}
	}
	public override void SaveDetails (JsonWriter writer) {
		base.SaveDetails (writer);
		SaveManager.WriteBoolean(writer, "NeedsBuilding", needsBuilding);
		SaveManager.WriteVector(writer, "SpawnPoint", spawnPoint);
		SaveManager.WriteVector(writer, "RallyPoint", rallyPoint);
		SaveManager.WriteFloat(writer, "BuildProgress", currentBuildProgress);
		SaveManager.WriteStringArray(writer, "BuildQueue", buildQueue.ToArray());
		if (needsBuilding) {
			SaveManager.WriteRect (writer, "PlayingArea", playingArea.area);
		}
	}
	protected override void HandleLoadedProperty (JsonTextReader reader, string propertyName, object readValue) {
		base.HandleLoadedProperty (reader, propertyName, readValue);
		switch(propertyName) {
		case "NeedsBuilding": needsBuilding = (bool)readValue; break;
		case "SpawnPoint": spawnPoint = LoadManager.LoadVector(reader); break;
		case "RallyPoint": rallyPoint = LoadManager.LoadVector(reader); break;
		case "BuildProgress": currentBuildProgress = (float)(double)readValue; break;
		case "BuildQueue": buildQueue = new Queue< string >(LoadManager.LoadStringArray(reader)); break;
		case "PlayingArea": playingArea.area = LoadManager.LoadRect(reader); break;
		default: break;
		}
	}
}

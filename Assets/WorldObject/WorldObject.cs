using UnityEngine;
using System.Collections.Generic;
using RTS;
using Newtonsoft.Json;

public class WorldObject : MonoBehaviour {
	public int ObjectId { get; set; }
	protected Bounds selectionBounds;
	public string objectName;
	public Texture2D buildImage;
	public int cost, sellValue, hitPoints, maxHitPoints;

	protected Player player;
	protected string[] actions = {};
	protected bool currentlySelected = false;
	protected PlayingArea playingArea = new PlayingArea ();
	protected GUIStyle healthStyle = new GUIStyle();
	protected float healthPercentage;

	//ATTACKING
	protected WorldObject target = null;
	protected bool attacking = false;
	public float weaponRange = 3.0f;
	protected bool movingIntoPosition = false;
	protected bool aiming = false;
	public float weaponRechargeTime = 1.0f;
	private float currentWeaponChargeTime;

	//LOAD
	protected bool loadedSavedValues = false;
	private int loadedTargetId = -1;

	//AI
	//we want to restrict how many decisions are made to help with game performance
	//the default time at the moment is a tenth of a second
	private float timeSinceLastDecision = 0.0f, timeBetweenDecisions = 0.1f;
	public float detectionRange = 20.0f;
	protected List< WorldObject > nearbyObjects;

	public virtual bool IsActive{
		get{
			return true;
		}
	}

	private List< Material > oldMaterials = new List< Material >();

	public Bounds GetSelectionBounds() {
		return selectionBounds;
	}

	protected virtual void Awake() {
		selectionBounds = ResourceManager.InvalidBounds;
		CalculateBounds();
	}

	protected virtual void Start () {
		SetPlayer();
		if(player) {
			if(loadedSavedValues) {
				if(loadedTargetId >= 0) target = player.GetObjectForId(loadedTargetId);
			} else {
				SetTeamColor();
			}
		}
	}

	protected virtual void Update () {
		if(ShouldMakeDecision()) DecideWhatToDo();
		currentWeaponChargeTime += Time.deltaTime;
		if(attacking && !movingIntoPosition && !aiming) PerformAttack();
	}

	protected virtual void OnGUI() {
		if(currentlySelected) DrawSelection();
	}
	public virtual void SetSelection(bool selected, PlayingArea playingArea) {
		currentlySelected = selected;
		if(selected) this.playingArea = playingArea;
	}
	public string[] GetActions() {
		return actions;
	}

	public virtual void PerformAction(string actionToPerform) {
		//it is up to children with specific actions to determine what to do with each of those actions
	}
	public virtual void RightMouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		//only handle input if currently selected
		if(currentlySelected && hitObject && !WorkManager.ObjectIsGround(hitObject)) {
			WorldObject worldObject = hitObject.transform.parent.GetComponent< WorldObject >();
			//clicked on another selectable object
			if(worldObject) {
				Resource resource = hitObject.transform.parent.GetComponent< Resource >();
				if(resource && resource.isEmpty()) return;
				Player owner = hitObject.transform.root.GetComponent< Player >();
				if(owner) { //the object is controlled by a player
					if(player && player.human) { //this object is controlled by a human player
						//start attack if object is not owned by the same player and this object can attack, else select
						if(player.username != owner.username && CanAttack()) BeginAttack(worldObject);
					}
				}
			}
		}
	}
	public virtual void LeftMouseClick(GameObject hitObject, Vector3 hitPoints, Player controller){
		if (currentlySelected && hitObject && !WorkManager.ObjectIsGround (hitObject)) {
			WorldObject worldObject = hitObject.transform.parent.GetComponent< WorldObject >();
			if (worldObject) {
				ChangeSelection (worldObject, controller);
			}
		}
	}

	private void ChangeSelection(WorldObject worldObject, Player controller) {
		//this should be called by the following line, but there is an outside chance it will not
		SetSelection(false, playingArea);
		if(controller.SelectedObject) controller.SelectedObject.SetSelection(false, playingArea);
		controller.SelectedObject = worldObject;
		worldObject.SetSelection(true, controller.hud.GetPlayingArea());
	}

	private void DrawSelection() {
		GUI.skin = ResourceManager.SelectBoxSkin;
		Rect selectBox = WorkManager.CalculateSelectionBox(selectionBounds, playingArea);
		//Draw the selection box around the currently selected object, within the bounds of the playing area
		GUI.BeginGroup(playingArea.area);
		DrawSelectionBox(selectBox);
		GUI.EndGroup();
	}
	public void CalculateBounds() {
		selectionBounds = new Bounds(transform.position, Vector3.zero);
		foreach(Renderer r in GetComponentsInChildren< Renderer >()) {
			selectionBounds.Encapsulate(r.bounds);
		}
	}
	protected virtual void DrawSelectionBox(Rect selectBox) {
		GUI.Box(selectBox, "");
		CalculateCurrentHealth (0.35f, 0.65f);
		GUI.Label(new Rect(selectBox.x, selectBox.y - 7, selectBox.width * healthPercentage, 5), "", healthStyle);
	}
	public virtual void SetHoverState(GameObject hoverObject) {
		//only handle input if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			//something other than the ground is being hovered over
			if(!WorkManager.ObjectIsGround(hoverObject)) {
				Player owner = hoverObject.transform.root.GetComponent< Player >();
				Unit unit = hoverObject.transform.parent.GetComponent< Unit >();
				Building building = hoverObject.transform.parent.GetComponent< Building >();
				if(owner) { //the object is owned by a player
					if(owner.username == player.username) player.hud.SetCursorState(CursorState.Select);
					else if(CanAttack()) player.hud.SetCursorState(CursorState.Attack);
					else player.hud.SetCursorState(CursorState.Select);
				} else if(unit || building && CanAttack()) player.hud.SetCursorState(CursorState.Attack);
				else player.hud.SetCursorState(CursorState.Select);
			}
		}
	}

	public bool IsOwnedBy(Player owner) {
		if(player && player.Equals(owner)) {
			return true;
		} else {
			return false;
		}
	}
	protected virtual void CalculateCurrentHealth (float lowSplit, float highSplit) {
		healthPercentage = (float)hitPoints / (float)maxHitPoints;
		if (healthPercentage > 0.65f)
			healthStyle.normal.background = ResourceManager.HealthyTexture;
		else if(healthPercentage > 0.35f)
			healthStyle.normal.background = ResourceManager.DamagedTexture;
		else
			healthStyle.normal.background = ResourceManager.CriticalTexture;
	}

	public void SetColliders(bool enabled) {
		Collider[] colliders = GetComponentsInChildren< Collider >();
		foreach(Collider collider in colliders) collider.enabled = enabled;
	}

	public void SetTransparentMaterial(Material material, bool storeExistingMaterial) {
		if(storeExistingMaterial) oldMaterials.Clear();
		Renderer[] renderers = GetComponentsInChildren< Renderer >();
		foreach(Renderer renderer in renderers) {
			if(storeExistingMaterial) oldMaterials.Add(renderer.material);
			renderer.material = material;
		}
	}

	public void RestoreMaterials() {
		Renderer[] renderers = GetComponentsInChildren< Renderer >();
		if(oldMaterials.Count == renderers.Length) {
			for(int i = 0; i < renderers.Length; i++) {
				renderers[i].material = oldMaterials[i];
			}
		}
	}

	public void SetPlayingArea(PlayingArea playingArea) {
		this.playingArea = playingArea;
	}
	public void SetPlayer() {
		player = transform.root.GetComponentInChildren< Player >();
	}
	public virtual bool CanAttack() {
		//default behaviour needs to be overidden by children
		return false;
	}
	public void SetTeamColor() {
		TeamColor[] teamColors = GetComponentsInChildren< TeamColor >();
		foreach (TeamColor teamColor in teamColors) {
			Renderer renderer = teamColor.GetComponentInChildren<Renderer> ();
			renderer.material.color = player.teamColor;
		}
	}
	protected virtual void BeginAttack(WorldObject target) {
		this.target = target;
		if(TargetInRange()) {
			attacking = true;
			PerformAttack();
		} else AdjustPosition();
	}
	private bool TargetInRange() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		if(direction.sqrMagnitude < weaponRange * weaponRange) {
			return true;
		}
		return false;
	}
	private void AdjustPosition() {
		Unit self = this as Unit;
		if(self) {
			movingIntoPosition = true;
			Vector3 attackPosition = FindNearestAttackPosition();
			self.StartMove(attackPosition);
			attacking = true;
		} else attacking = false;
	}
	private Vector3 FindNearestAttackPosition() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		float targetDistance = direction.magnitude;
		float distanceToTravel = targetDistance - (0.9f * weaponRange);
		return Vector3.Lerp(transform.position, targetLocation, distanceToTravel / targetDistance);
	}
	private void PerformAttack() {
		if(!target) {
			attacking = false;
			return;
		}
		if(!TargetInRange()) AdjustPosition();
		else if(!TargetInFrontOfWeapon()) AimAtTarget();
		else if(ReadyToFire()) UseWeapon();
	}
	private bool TargetInFrontOfWeapon() {
		Vector3 targetLocation = target.transform.position;
		Vector3 direction = targetLocation - transform.position;
		if(direction.normalized == transform.forward.normalized) return true;
		else return false;
	}
	protected virtual void AimAtTarget() {
		aiming = true;
		//this behaviour needs to be specified by a specific object
	}
	private bool ReadyToFire() {
		if(currentWeaponChargeTime >= weaponRechargeTime){ 
			return true;
		}
		return false;
	}
	protected virtual void UseWeapon() {
		currentWeaponChargeTime = 0.0f;
		//this behaviour needs to be specified by a specific object
	}
	public void TakeDamage(int damage){
		hitPoints -= damage;
		if (hitPoints <= 0)
			Destroy (gameObject);
	}

	public virtual void SaveDetails(JsonWriter writer) {
		SaveManager.WriteString(writer, "Type", name);
		SaveManager.WriteString(writer, "Name", objectName);
		SaveManager.WriteInt(writer, "Id", ObjectId);
		SaveManager.WriteVector(writer, "Position", transform.position);
		SaveManager.WriteQuaternion(writer, "Rotation", transform.rotation);
		SaveManager.WriteVector(writer, "Scale", transform.localScale);
		SaveManager.WriteInt(writer, "HitPoints", hitPoints);
		SaveManager.WriteBoolean(writer, "Attacking", attacking);
		SaveManager.WriteBoolean(writer, "MovingIntoPosition", movingIntoPosition);
		SaveManager.WriteBoolean(writer, "Aiming", aiming);
		if(attacking) {
			//only save if attacking so that we do not end up storing massive numbers for no reason
			SaveManager.WriteFloat(writer, "CurrentWeaponChargeTime", currentWeaponChargeTime);
		}
		if(target != null) SaveManager.WriteInt(writer, "TargetId", target.ObjectId);
	}
	public void LoadDetails(JsonTextReader reader) {
		while(reader.Read()) {
			if(reader.Value != null) {
				if(reader.TokenType == JsonToken.PropertyName) {
					string propertyName = (string)reader.Value;
					reader.Read();
					HandleLoadedProperty(reader, propertyName, reader.Value);
				}
			} else if(reader.TokenType == JsonToken.EndObject) {
				//loaded position invalidates the selection bounds so they must be recalculated
				selectionBounds = ResourceManager.InvalidBounds;
				CalculateBounds();
				loadedSavedValues = true;
				return;
			}
		}
		//loaded position invalidates the selection bounds so they must be recalculated
		selectionBounds = ResourceManager.InvalidBounds;
		CalculateBounds();
		loadedSavedValues = true;
	}
	protected virtual void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue) {
		switch(propertyName) {
		case "Name": objectName = (string)readValue; break;
		case "Id": ObjectId = (int)(System.Int64)readValue; break;
		case "Position": transform.localPosition = LoadManager.LoadVector(reader); break;
		case "Rotation": transform.localRotation = LoadManager.LoadQuaternion(reader); break;
		case "Scale": transform.localScale = LoadManager.LoadVector(reader); break;
		case "HitPoints": hitPoints = (int)(System.Int64)readValue; break;
		case "Attacking": attacking = (bool)readValue; break;
		case "MovingIntoPosition": movingIntoPosition = (bool)readValue; break;
		case "Aiming": aiming = (bool)readValue; break;
		case "CurrentWeaponChargeTime": currentWeaponChargeTime = (float)(double)readValue; break;
		case "TargetId": loadedTargetId = (int)(System.Int64)readValue; break;
		default: break;
		}
	}
	/**
 * A child class should only determine other conditions under which a decision should
 * not be made. This could be 'harvesting' for a harvester, for example. Alternatively,
 * an object that never has to make decisions could just return false.
 */
	protected virtual bool ShouldMakeDecision() {
		if(!attacking && !movingIntoPosition && !aiming) {
			//we are not doing anything at the moment
			if(timeSinceLastDecision > timeBetweenDecisions) {
				timeSinceLastDecision = 0.0f;
				return true;
			}
			timeSinceLastDecision += Time.deltaTime;
		}
		return false;
	}

	protected virtual void DecideWhatToDo() {
		//determine what should be done by the world object at the current point in time
		Vector3 currentPosition = transform.position;
		nearbyObjects = WorkManager.FindNearbyObjects(currentPosition, detectionRange);
		if(CanAttack()) {
			List< WorldObject > enemyObjects = new List< WorldObject >();
			foreach(WorldObject nearbyObject in nearbyObjects) {
				Resource resource = nearbyObject.GetComponent< Resource >();
				if(resource) continue;
				if(nearbyObject.GetPlayer() != player) enemyObjects.Add(nearbyObject);
			}
			WorldObject closestObject = WorkManager.FindNearestWorldObjectInListToPosition(enemyObjects, currentPosition);
			if(closestObject) BeginAttack(closestObject);
		}
	}
	public Player GetPlayer() {
		return player;
	}
}

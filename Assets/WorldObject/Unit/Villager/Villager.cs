using UnityEngine;
using System.Collections.Generic;
using RTS;
using Newtonsoft.Json;

public class Villager : Unit {

	public float capacity;

	//BUILD
	public int buildSpeed;

	private Building currentProject;
	private bool building = false;
	private float amountBuilt = 0.0f;

	//HARVEST
	private bool harvesting = false, emptying = false;
	private float currentLoad = 0.0f;
	private ResourceType harvestType;
	private Resource resourceDeposit;
	public Building resourceStore;
	public float collectionAmount, depositAmount;

	private float currentDeposit = 0.0f;

	private int loadedDepositId = -1, loadedStoreId = -1;

	private int loadedProjectId = -1;

	/*** Game Engine methods, all can be overridden by subclass ***/
	/* */
	protected override void Start () {
		base.Start();
		actions = new string[] {"House", "Barracks"};
		harvestType = ResourceType.Unknown;
		if(loadedSavedValues) {
			if(player) {
				if(loadedStoreId >= 0) {
					WorldObject obj = player.GetObjectForId(loadedStoreId);
					if(obj.GetType().IsSubclassOf(typeof(Building))) resourceStore = (Building)obj;
				}
				if(loadedDepositId >= 0) {
					WorldObject obj = player.GetObjectForId(loadedDepositId);
					if(obj.GetType().IsSubclassOf(typeof(Resource))) resourceDeposit = (Resource)obj;
				}
				if(loadedProjectId >= 0) {
					WorldObject obj = player.GetObjectForId(loadedProjectId);
					if(obj.GetType().IsSubclassOf(typeof(Building))) currentProject = (Building)obj;
				}
			}
		} else {
			harvestType = ResourceType.Unknown;
		}
	}
		
	protected override void Update () {
		base.Update();
		if(!rotating && !moving) {
			if(harvesting || emptying) {
				Arms[] arms = GetComponentsInChildren< Arms >();
				foreach (Arms arm in arms) {
					Renderer renderer = arm.GetComponent<Renderer> ();
					renderer.enabled = true;
				}
				if(harvesting) {
					Collect();
					if(currentLoad >= capacity) {
						//make sure that we have a whole number to avoid bugs
						//caused by floating point numbers
						currentLoad = Mathf.Floor(currentLoad);
						harvesting = false;
						emptying = true;
						foreach (Arms arm in arms) {
							Renderer renderer = arm.GetComponent<Renderer> ();
							renderer.enabled = false;
						}
						StartMove (resourceStore.transform.position, resourceStore.gameObject);
					}
				} else {
					Deposit();
					if(currentLoad <= 0) {
						emptying = false;
						foreach (Arms arm in arms) {
							Renderer renderer = arm.GetComponent<Renderer> ();
							renderer.enabled = false;
						}
						if(!resourceDeposit.isEmpty()) {
							harvesting = true;
							StartMove (resourceDeposit.transform.position, resourceDeposit.gameObject);
						}
					}
				}
			}
			if(building && currentProject && currentProject.UnderConstruction()) {
				amountBuilt += buildSpeed * Time.deltaTime;
				int amount = Mathf.FloorToInt(amountBuilt);
				if(amount > 0) {
					amountBuilt -= amount;
					currentProject.Construct(amount);
					if(!currentProject.UnderConstruction()) building = false;
				}
			}
		}
	}

	/* Public Methods */

	public override void SetHoverState(GameObject hoverObject) {
		base.SetHoverState(hoverObject);
		//only handle input if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			if(!WorkManager.ObjectIsGround(hoverObject)) {
				Resource resource = hoverObject.transform.parent.GetComponent< Resource >();
				if(resource && !resource.isEmpty()) player.hud.SetCursorState(CursorState.Harvest);
			}
		}
	}

	public override void RightMouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		bool doBase = true;
		//only handle input if owned by a human player
		if(player && player.human) {
			if (currentlySelected && hitObject && !WorkManager.ObjectIsGround(hitObject)) {
				Building building = hitObject.transform.parent.GetComponent< Building >();
				if(building) {
					if(building.UnderConstruction()) {
						SetBuilding(building);
						doBase = false;
					}
				}
			}
			if(!WorkManager.ObjectIsGround(hitObject)) {
				Resource resource = hitObject.transform.parent.GetComponent< Resource >();
				if(resource && !resource.isEmpty()) {
					//make sure that we select harvester remains selected
					if(player.SelectedObject) player.SelectedObject.SetSelection(false, playingArea);
					SetSelection(true, playingArea);
					player.SelectedObject = this;
					StartHarvest(resource);
				}
			} else StopHarvest();
		}
		if(doBase) base.RightMouseClick(hitObject, hitPoint, controller);
	}

	/* Private Methods */

	private void StartHarvest(Resource resource) {
		resourceDeposit = resource;
		StartMove(resource.transform.position, resource.gameObject);
		//we can only collect one resource at a time, other resources are lost
		if(harvestType == ResourceType.Unknown || harvestType != resource.GetResourceType()) {
			harvestType = resource.GetResourceType();
			currentLoad = 0.0f;
		}
		harvesting = true;
		emptying = false;
	}

	private void StopHarvest() {

	}

	private void Collect() {
		float collect = collectionAmount * Time.deltaTime;
		//make sure that the harvester cannot collect more than it can carry
		if(currentLoad + collect > capacity) collect = capacity - currentLoad;
		if(resourceDeposit.isEmpty()) {
			Arms[] arms = GetComponentsInChildren< Arms >();
			foreach (Arms arm in arms) {
				Renderer renderer = arm.GetComponent<Renderer> ();
				renderer.enabled = false;
			}
			DecideWhatToDo();
		} else {
			resourceDeposit.Remove(collect);
		}
		currentLoad += collect;
	}

	private void Deposit() {
		currentLoad = Mathf.Floor(currentLoad);
		currentDeposit += depositAmount * Time.deltaTime;
		int deposit = Mathf.FloorToInt(currentDeposit);
		if(deposit >= 1) {
			if(deposit > currentLoad) deposit = Mathf.FloorToInt(currentLoad);
			currentDeposit -= deposit;
			currentLoad -= deposit;
			ResourceType depositType = harvestType;
			if(harvestType == ResourceType.Ore) depositType = ResourceType.Gold;
			player.AddResource(depositType, deposit);
		}
	}

	protected override void DrawSelectionBox (Rect selectBox) {
		base.DrawSelectionBox(selectBox);
		float percentFull = currentLoad / capacity;
		float maxHeight = selectBox.height - 4;
		float height = maxHeight * percentFull;
		float leftPos = selectBox.x + selectBox.width - 7;
		float topPos = selectBox.y + 2 + (maxHeight - height);
		float width = 5;
		Texture2D resourceBar = ResourceManager.GetResourceHealthBar(harvestType);
		if(resourceBar) GUI.DrawTexture(new Rect(leftPos, topPos, width, height), resourceBar);
	}

	public override void SetBuilding (Building project) {
		base.SetBuilding (project);
		resourceStore = project;
		currentProject = project;
		StartMove(currentProject.transform.position, currentProject.gameObject);
		building = true;
	}

	public override void PerformAction (string actionToPerform) {
		base.PerformAction (actionToPerform);
		CreateBuilding(actionToPerform);
	}

	public override void StartMove(Vector3 destination) {
		base.StartMove(destination);
		amountBuilt = 0.0f;
		building = false;
	}

	private void CreateBuilding(string buildingName) {
		Vector3 buildPoint = new Vector3(transform.position.x, transform.position.y, transform.position.z + 10);
		if(player) player.CreateBuilding(buildingName, buildPoint, this, playingArea);
	}
	public override void SaveDetails (JsonWriter writer) {
		base.SaveDetails (writer);
		SaveManager.WriteBoolean(writer, "Harvesting", harvesting);
		SaveManager.WriteBoolean(writer, "Emptying", emptying);
		SaveManager.WriteFloat(writer, "CurrentLoad", currentLoad);
		SaveManager.WriteFloat(writer, "CurrentDeposit", currentDeposit);
		SaveManager.WriteString(writer, "HarvestType", harvestType.ToString());
		if(resourceDeposit) SaveManager.WriteInt(writer, "ResourceDepositId", resourceDeposit.ObjectId);
		if(resourceStore) SaveManager.WriteInt(writer, "ResourceStoreId", resourceStore.ObjectId);

		SaveManager.WriteBoolean(writer, "Building", building);
		SaveManager.WriteFloat(writer, "AmountBuilt", amountBuilt);
		if(currentProject) SaveManager.WriteInt(writer, "CurrentProjectId", currentProject.ObjectId);
	}
	protected override void HandleLoadedProperty (JsonTextReader reader, string propertyName, object readValue) {
		base.HandleLoadedProperty (reader, propertyName, readValue);
		switch(propertyName) {
		case "Harvesting": harvesting = (bool)readValue; break;
		case "Emptying": emptying = (bool)readValue; break;
		case "CurrentLoad": currentLoad = (float)(double)readValue; break;
		case "CurrentDeposit": currentDeposit = (float)(double)readValue; break;
		case "HarvestType": harvestType = WorkManager.GetResourceType((string)readValue); break;
		case "ResourceDepositId": loadedDepositId = (int)(System.Int64)readValue; break;
		case "ResourceStoreId": loadedStoreId = (int)(System.Int64)readValue; break;
		case "Building": building = (bool)readValue; break;
		case "AmountBuilt": amountBuilt = (float)(double)readValue; break;
		case "CurrentProjectId": loadedProjectId = (int)(System.Int64)readValue; break;
		default: break;
		}
	}
	protected override bool ShouldMakeDecision () {
		if(building) return false;
		return base.ShouldMakeDecision();
	}
	protected override void DecideWhatToDo () {
		base.DecideWhatToDo ();
		List< WorldObject > resources = new List< WorldObject >();
		foreach(WorldObject nearbyObject in nearbyObjects) {
			Resource resource = nearbyObject.GetComponent< Resource >();
			if(resource && !resource.isEmpty()) resources.Add(nearbyObject);
		}
		WorldObject nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(resources, transform.position);
		if(nearestObject) {
			Resource closestResource = nearestObject.GetComponent< Resource >();
			if(closestResource) StartHarvest(closestResource);
		} else if(harvesting) {
			harvesting = false;
			if(currentLoad > 0.0f) {
				//make sure that we have a whole number to avoid bugs
				//caused by floating point numbers
				currentLoad = Mathf.Floor(currentLoad);
				emptying = true;
				Arms[] arms = GetComponentsInChildren< Arms >();
				foreach (Arms arm in arms) {
					Renderer renderer = arm.GetComponent<Renderer> ();
					renderer.enabled = false;
				}
				StartMove (resourceStore.transform.position, resourceStore.gameObject);
			}
		}
	}
}

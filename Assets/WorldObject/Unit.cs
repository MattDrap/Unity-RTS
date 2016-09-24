using UnityEngine;
using System.Collections;
using RTS;
using Newtonsoft.Json;

public class Unit : WorldObject {
	protected bool moving, rotating;
	public float moveSpeed, rotateSpeed;
	private Vector3 destination;
	private Quaternion targetRotation;
	protected Animator animator;
	private GameObject destinationTarget;
	protected NavMeshAgent agent;
	/*** Game Engine methods, all can be overridden by subclass ***/

	private int loadedDestinationTargetId = -1;


	protected override void Awake() {
		base.Awake();
	}

	protected override void Start () {
		base.Start();
		agent = GetComponent<NavMeshAgent> ();
		animator = GetComponent<Animator>();
		if(player && loadedSavedValues && loadedDestinationTargetId >= 0) {
			destinationTarget = player.GetObjectForId(loadedDestinationTargetId).gameObject;
		}
	}

	protected override void Update () {
		base.Update();
		if (agent.velocity.magnitude > 0) {
			CalculateBounds ();
		}
		if(rotating) TurnToTarget();
		else if(moving) MakeMove();
	}

	protected override void OnGUI() {
		base.OnGUI();
	}
	public override void SetHoverState(GameObject hoverObject) {
		base.SetHoverState(hoverObject);
		//only handle input if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			bool moveHover = false;
			if(WorkManager.ObjectIsGround(hoverObject)) {
				moveHover = true;
			} else {
				Resource resource = hoverObject.transform.parent.GetComponent< Resource >();
				if(resource && resource.isEmpty()) moveHover = true;
			}
			if(moveHover) player.hud.SetCursorState(CursorState.Move);
		}
	}
	public override void RightMouseClick(GameObject hitObject, Vector3 hitPoint, Player controller) {
		base.RightMouseClick(hitObject, hitPoint, controller);
		//only handle input if owned by a human player and currently selected
		if(player && player.human && currentlySelected) {
			bool clickedOnEmptyResource = false;
			if(hitObject.transform.parent) {
				Resource resource = hitObject.transform.parent.GetComponent< Resource >();
				if(resource && resource.isEmpty()) clickedOnEmptyResource = true;
			}
			if((WorkManager.ObjectIsGround(hitObject) || clickedOnEmptyResource) && hitPoint != ResourceManager.InvalidPosition) {
				float x = hitPoint.x;
				//makes sure that the unit stays on top of the surface it is on
				float y = hitPoint.y + player.SelectedObject.transform.position.y;
				float z = hitPoint.z;
				Vector3 destination = new Vector3(x, y, z);
				//StartMove(destination);
				agent.SetDestination(destination);
			}
		}
	}
	public void StartMove(Vector3 destination, GameObject destinationTarget) {
		StartMove(destination);
		this.destinationTarget = destinationTarget;
	}
	public virtual void StartMove(Vector3 destination) {
		this.destination = destination;
		targetRotation = Quaternion.LookRotation (destination - transform.position);
		rotating = true;
		moving = false;
		destinationTarget = null;
	}
	private void TurnToTarget() {
		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed);
		//sometimes it gets stuck exactly 180 degrees out in the calculation and does nothing, this check fixes that
		Quaternion inverseTargetRotation = new Quaternion(-targetRotation.x, -targetRotation.y, -targetRotation.z, -targetRotation.w);
		if(transform.rotation == targetRotation || transform.rotation == inverseTargetRotation) {
			rotating = false;
			moving = true;
		}
		if (animator != null && transform.position != destination) {
			if (movingIntoPosition) {
				animator.SetBool ("Charge", true);
			} else {
				animator.SetBool ("Walk", true);
			}
		}
		CalculateBounds();
		if(destinationTarget) CalculateTargetDestination();
	}
	private void MakeMove() {
		transform.position = Vector3.MoveTowards(transform.position, destination, Time.deltaTime * moveSpeed);
		if (transform.position == destination) {
			moving = false;
			movingIntoPosition = false;
		}
		if (animator != null && !moving){
			if(animator.GetBool("Charge")){
				animator.SetBool("Charge", false);
			}else{
				animator.SetBool ("Walk", false);
			}
		}
		CalculateBounds();
	}
	private void CalculateTargetDestination() {
		//calculate number of unit vectors from unit centre to unit edge of bounds
		Vector3 originalExtents = selectionBounds.extents;
		Vector3 normalExtents = originalExtents;
		normalExtents.Normalize();
		float numberOfExtents = originalExtents.x / normalExtents.x;
		int unitShift = Mathf.FloorToInt(numberOfExtents);

		//calculate number of unit vectors from target centre to target edge of bounds
		WorldObject worldObject = destinationTarget.GetComponent< WorldObject >();
		if(worldObject) originalExtents = worldObject.GetSelectionBounds().extents;
		else originalExtents = new Vector3(0.0f, 0.0f, 0.0f);
		normalExtents = originalExtents;
		normalExtents.Normalize();
		numberOfExtents = originalExtents.x / normalExtents.x;
		int targetShift = Mathf.FloorToInt(numberOfExtents);

		//calculate number of unit vectors between unit centre and destination centre with bounds just touching
		int shiftAmount = targetShift + unitShift;

		//calculate direction unit needs to travel to reach destination in straight line and normalize to unit vector
		Vector3 origin = transform.position;
		Vector3 direction = new Vector3(destination.x - origin.x, 0.0f, destination.z - origin.z);
		direction.Normalize();

		//destination = center of destination - number of unit vectors calculated above
		//this should give us a destination where the unit will not quite collide with the target
		//giving the illusion of moving to the edge of the target and then stopping
		for(int i = 0; i < shiftAmount; i++) destination -= direction;
		destination.y = destinationTarget.transform.position.y;

		//BUG FIX - WEIRD
		destinationTarget = null;
	}
	public virtual void SetBuilding(Building creator) {
		//specific initialization for a unit can be specified here
	}
	public override void SaveDetails (JsonWriter writer) {
		base.SaveDetails (writer);
		SaveManager.WriteBoolean(writer, "Moving", moving);
		SaveManager.WriteBoolean(writer, "Rotating", rotating);
		SaveManager.WriteVector(writer, "Destination", destination);
		SaveManager.WriteQuaternion(writer, "TargetRotation", targetRotation);
		if(destinationTarget) {
			WorldObject destinationObject = destinationTarget.GetComponent< WorldObject >();
			if(destinationObject) SaveManager.WriteInt(writer, "DestinationTargetId", destinationObject.ObjectId);
		}
	}
	protected override void HandleLoadedProperty (JsonTextReader reader, string propertyName, object readValue) {
		base.HandleLoadedProperty (reader, propertyName, readValue);
		switch(propertyName) {
		case "Moving": moving = (bool)readValue; break;
		case "Rotating": rotating = (bool)readValue; break;
		case "Destination": destination = LoadManager.LoadVector(reader); break;
		case "TargetRotation": targetRotation = LoadManager.LoadQuaternion(reader); break;
		case "DestinationTargetId": loadedDestinationTargetId = (int)(System.Int64)readValue; break;
		default: break;
		}
	}

	protected override bool ShouldMakeDecision () {
		if(moving || rotating) return false;
		return base.ShouldMakeDecision();
	}
}
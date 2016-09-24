using UnityEngine;
using System.Collections.Generic;
using RTS;
using UnityEngine.UI;

public class UIHUD : MonoBehaviour {
	
	public GameObject PausePanel;
	public Button MenuButton;
	public GameObject ResourcePanel;
	public GameObject DetailPanel;
	public Text GoldText;
	public Text PowerText;
	public Text SelectionText;
	public Text NameText;
	public List<Button> ActionButtons;
	public List<Button> QueueButtons;
	public Slider sliderProgress;

	private Dictionary< ResourceType, int > resourceValues, resourceLimits;
	public Texture2D buttonHover, buttonClick;
	public Texture2D smallButtonHover, smallButtonClick;
	public Texture2D rallyPointCursor;

	private CursorState previousCursorState;
	public CursorState GetPreviousCursorState() {
		return previousCursorState;
	}

	private CursorState activeCursorState;
	private int currentFrame = 0;
	public GUISkin mouseCursorSkin;
	public GUISkin playerDetailsSkin;
	public GUISkin selectBoxSkin;


	private Player player;
	private WorldObject lastSelection;

	public PlayingArea GetPlayingArea() {
		int RESOURCE_BAR_HEIGHT = (int)ResourcePanel.GetComponent<RectTransform> ().rect.height;
		int DETAILS_BAR_HEIGHT = (int)DetailPanel.GetComponent<RectTransform> ().rect.height;
		PlayingArea area = new PlayingArea ();
		area.area = new Rect(0, RESOURCE_BAR_HEIGHT, Screen.width, Screen.height - DETAILS_BAR_HEIGHT - RESOURCE_BAR_HEIGHT);
		area.not_playable_bottom = new Rect(0, Screen.height - DETAILS_BAR_HEIGHT, Screen.width, DETAILS_BAR_HEIGHT);
		area.not_playable_top = new Rect(0, 0, Screen.width, RESOURCE_BAR_HEIGHT);
		return area;
	}

	public Texture2D activeCursor;
	public Texture2D selectCursor, leftCursor, rightCursor, upCursor, downCursor;
	public Texture2D[] moveCursors, attackCursors, harvestCursors;
	public Texture2D healthy, damaged, critical;

	public Texture2D[] resourceHealthBars;

	// Use this for initialization
	void Start () {
		player = transform.root.GetComponent< Player >();
		if (!player.human) {
			this.gameObject.SetActive (false);
		}
		ResourceManager.StoreSelectBoxItems(selectBoxSkin, healthy, damaged, critical);
		SetCursorState(CursorState.Select);
		resourceValues = new Dictionary< ResourceType, int >();
		resourceLimits = new Dictionary< ResourceType, int >();

		resourceValues.Add(ResourceType.Gold, 0);
		resourceValues.Add(ResourceType.Power, 0);
		resourceLimits.Add(ResourceType.Power, 0);

		Dictionary< ResourceType, Texture2D > resourceHealthBarTextures = new Dictionary< ResourceType, Texture2D >();
		for(int i = 0; i < resourceHealthBars.Length; i++) {
			switch(resourceHealthBars[i].name) {
			case "ore":
				resourceHealthBarTextures.Add(ResourceType.Ore, resourceHealthBars[i]);
				break;
			default: break;
			}
		}
		ResourceManager.SetResourceHealthBarTextures(resourceHealthBarTextures);

		MenuButton.onClick.AddListener (delegate {
			OnMenuButtonClick();
		});

		for (int i = 0; i < ActionButtons.Count; i++) {
			int index = i;
			ActionButtons [index].onClick.AddListener (delegate {
				OnActionButton(index);
			});
			ActionButtons [index].gameObject.SetActive (false);
		}
		for (int i = 0; i < QueueButtons.Count; i++) {
			int index = i;
			QueueButtons [index].onClick.AddListener (delegate {
				OnQueueButton(index);
			});
			QueueButtons [index].gameObject.SetActive (false);
		}
	}

	void Destroy(){
		MenuButton.onClick.RemoveAllListeners ();
		for (int i = 0; i < ActionButtons.Count; i++) {
			ActionButtons [i].onClick.RemoveAllListeners ();
		}
		for (int i = 0; i < QueueButtons.Count; i++) {
			QueueButtons [i].onClick.RemoveAllListeners ();
		}
	}

	// Update is called once per frame
	void OnGUI () {
		if(player && player.human) {
			DrawPlayerDetails();
			DrawOrdersBar();
			DrawResourcesBar();
			DrawMouseCursor();
		}
	}
	private void DrawOrdersBar() {
		string selectionName = "";
		ClearButtons ();
		if (player.SelectedObject) {
			selectionName = player.SelectedObject.objectName;

			if (player.SelectedObject.IsOwnedBy (player)) {
				if (player.SelectedObject.IsActive)
					DrawActions (player.SelectedObject.GetActions ());
				//store the current selection
				lastSelection = player.SelectedObject;

				Building selectedBuilding = lastSelection.GetComponent< Building > ();
				if (selectedBuilding) {
					DrawBuildQueue (selectedBuilding.getBuildQueueValues (), selectedBuilding.getBuildPercentage ());
					DrawStandardBuildingOptions (selectedBuilding);
				}
			} 
		} 
		SelectionText.text = selectionName;
	}
	private void ClearButtons(){
		foreach(Button butt in ActionButtons){
			butt.gameObject.SetActive (false);
		}
		foreach(Button butt in QueueButtons){
			butt.gameObject.SetActive (false);
		}
		sliderProgress.gameObject.SetActive (false);
	}
	private void DrawResourcesBar() {
		string restext = resourceValues [ResourceType.Power].ToString () + "/" + resourceLimits[ResourceType.Power].ToString();
		PowerText.text = restext;
		restext = resourceValues [ResourceType.Gold].ToString ();
		GoldText.text = restext;
	}

	void OnMenuButtonClick(){
		Time.timeScale = 0.0f;
		this.gameObject.SetActive(false);
		ResourceManager.MenuOpen = true;
		PausePanel.SetActive (true);
		Cursor.visible = true;
		PlayerUserInput userInput = player.GetComponent< PlayerUserInput >();
		if(userInput) userInput.enabled = false;
	}

	public bool MouseInBounds() {
		//Screen coordinates start in the lower-left corner of the screen
		//not the top-left of the screen like the drawing coordinates do
		Vector3 mousePos = Input.mousePosition;
		int RESOURCE_BAR_HEIGHT = (int)ResourcePanel.GetComponent<RectTransform> ().rect.height;
		int DETAILS_BAR_HEIGHT = (int)DetailPanel.GetComponent<RectTransform> ().rect.height;
		bool insideWidth = true;
		bool insideHeight = mousePos.y >= DETAILS_BAR_HEIGHT && mousePos.y <= Screen.height - RESOURCE_BAR_HEIGHT;
		return insideWidth && insideHeight;
	}

	private void DrawMouseCursor() {
		bool mouseOverHud = !MouseInBounds() && activeCursorState != CursorState.PanRight && activeCursorState != CursorState.PanUp;
		if(mouseOverHud || ResourceManager.MenuOpen) {
			Cursor.visible = true;
		} else {
			Cursor.visible = false;
			if(!player.IsFindingBuildingLocation()) {
				// existing draw cursor code goes here...\
				Cursor.visible = false;
				GUI.skin = mouseCursorSkin;
				GUI.BeginGroup(new Rect(0,0,Screen.width,Screen.height));
				UpdateCursorAnimation();
				Rect cursorPosition = GetCursorDrawPosition();
				GUI.Label(cursorPosition, activeCursor);
				GUI.EndGroup();
			}
		}
	}
	private void UpdateCursorAnimation() {
		//sequence animation for cursor (based on more than one image for the cursor)
		//change once per second, loops through array of images
		if(activeCursorState == CursorState.Move) {
			currentFrame = (int)Time.time % moveCursors.Length;
			activeCursor = moveCursors[currentFrame];
		} else if(activeCursorState == CursorState.Attack) {
			currentFrame = (int)Time.time % attackCursors.Length;
			activeCursor = attackCursors[currentFrame];
		} else if(activeCursorState == CursorState.Harvest) {
			currentFrame = (int)Time.time % harvestCursors.Length;
			activeCursor = harvestCursors[currentFrame];
		}
	}
	private Rect GetCursorDrawPosition() {
		//set base position for custom cursor image
		float leftPos = Input.mousePosition.x;
		float topPos = Screen.height - Input.mousePosition.y; //screen draw coordinates are inverted
		//adjust position base on the type of cursor being shown
		if(activeCursorState == CursorState.PanRight) leftPos = Screen.width - activeCursor.width;
		else if(activeCursorState == CursorState.PanDown) topPos = Screen.height - activeCursor.height;
		else if(activeCursorState == CursorState.Move || activeCursorState == CursorState.Select || activeCursorState == CursorState.Harvest) {
			topPos -= activeCursor.height / 2;
			leftPos -= activeCursor.width / 2;
		}
		else if(activeCursorState == CursorState.RallyPoint) topPos -= activeCursor.height;
		return new Rect(leftPos, topPos, activeCursor.width, activeCursor.height);
	}
	public void SetCursorState(CursorState newState) {
		if(activeCursorState != newState) previousCursorState = activeCursorState;
		activeCursorState = newState;
		switch(newState) {
		case CursorState.Select:
			activeCursor = selectCursor;
			break;
		case CursorState.Attack:
			currentFrame = (int)Time.time % attackCursors.Length;
			activeCursor = attackCursors[currentFrame];
			break;
		case CursorState.Harvest:
			currentFrame = (int)Time.time % harvestCursors.Length;
			activeCursor = harvestCursors[currentFrame];
			break;
		case CursorState.Move:
			currentFrame = (int)Time.time % moveCursors.Length;
			activeCursor = moveCursors[currentFrame];
			break;
		case CursorState.PanLeft:
			activeCursor = leftCursor;
			break;
		case CursorState.PanRight:
			activeCursor = rightCursor;
			break;
		case CursorState.PanUp:
			activeCursor = upCursor;
			break;
		case CursorState.PanDown:
			activeCursor = downCursor;
			break;
		case CursorState.RallyPoint:
			activeCursor = rallyPointCursor;
			break;
		default: break;
		}
	}

	public void SetResourceValues(Dictionary< ResourceType, int > resourceValues, Dictionary< ResourceType, int > resourceLimits) {
		this.resourceValues = resourceValues;
		this.resourceLimits = resourceLimits;
	}
	private void DrawActions(string[] actions) {
		int numActions = actions.Length;

		//display possible actions as buttons and handle the button click for each
		for(int i = 0; i < numActions; i++) {
			Texture2D action = ResourceManager.GetBuildImage(actions[i]);
			if(action) { 
				ActionButtons [i].gameObject.SetActive (true);
				//TODO
				ActionButtons [i].image.sprite = Sprite.Create(action,new Rect(0, 0, action.width, action.height), new Vector2(0.5f, 0.5f));
				//
			}
		}
	}
	private void DrawBuildQueue(string[] buildQueue, float buildPercentage) {
		sliderProgress.gameObject.SetActive (true);
		foreach (Button butt in QueueButtons) {
			butt.gameObject.SetActive (true);
		}
		for (int i = 0; i < buildQueue.Length; i++) {
			//TODO
			Texture2D texture = ResourceManager.GetBuildImage (buildQueue [i]);
			QueueButtons [i].image.sprite = Sprite.Create(texture,new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			if (i == 0) {
				sliderProgress.value = buildPercentage;
			}
		}
	}
	private void DrawStandardBuildingOptions(Building building) {
		if(building.hasSpawnPoint()) {
			ActionButtons [ActionButtons.Count - 1].gameObject.SetActive (true);
			//TODO
			ActionButtons [ActionButtons.Count - 1].image.sprite = Sprite.Create(building.sellImage,new Rect(0, 0, building.sellImage.width, building.sellImage.height), new Vector2(0.5f, 0.5f));

			ActionButtons [ActionButtons.Count - 2].gameObject.SetActive (true);
			//TODO
			ActionButtons [ActionButtons.Count - 2].image.sprite = Sprite.Create(building.rallyPointImage,new Rect(0, 0, building.rallyPointImage.width, building.rallyPointImage.height), new Vector2(0.5f, 0.5f));
		}
	}
	public void OnQueueButton(int i){
	}
	public void OnActionButton(int i){
		lastSelection = player.SelectedObject;

		Building selectedBuilding = lastSelection.GetComponent< Building > ();

		string[] actions = player.SelectedObject.GetActions ();
		if (i == ActionButtons.Count - 1) {
			if (selectedBuilding) {
				selectedBuilding.Sell();
			}
		}
		else if (i == ActionButtons.Count - 2) {
			if(activeCursorState != CursorState.RallyPoint && previousCursorState != CursorState.RallyPoint) SetCursorState(CursorState.RallyPoint);
			else {
				//dirty hack to ensure toggle between RallyPoint and not works ...
				SetCursorState(CursorState.PanRight);
				SetCursorState(CursorState.Select);
			}
		} else {
			if(player.SelectedObject) player.SelectedObject.PerformAction(actions[i]);
		}
	}
	public CursorState GetCursorState() {
		return activeCursorState;
	}
	private void DrawPlayerDetails() {
		string playerName = PlayerManager.GetPlayerName();
		NameText.text = playerName;
	}
}

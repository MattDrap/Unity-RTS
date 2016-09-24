using UnityEngine;

public class House : Building {

	protected override void Start () {
		base.Start();
		actions = new string[] {"Villager"};
	}

	public override void PerformAction(string actionToPerform) {
		base.PerformAction(actionToPerform);
		CreateUnit(actionToPerform);
	}
	protected override bool ShouldMakeDecision () {
		return false;
	}
}

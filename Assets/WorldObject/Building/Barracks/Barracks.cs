using UnityEngine;
using System.Collections;

public class Barracks : Building {
	protected override void Start () {
		base.Start();
		actions = new string[] { "Knight" };
	}
	public override void PerformAction(string actionToPerform) {
		base.PerformAction(actionToPerform);
		CreateUnit(actionToPerform);
	}
	protected override bool ShouldMakeDecision () {
		return false;
	}
}

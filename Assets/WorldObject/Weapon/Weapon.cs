using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {

	public int damage = 1;
	public float weaponAimSpeed = 1;
	protected WorldObject target;
	private bool swinging = false;

	protected virtual void Update () {
		if(HitSomething()) {
			InflictDamage();
		}

	}

	public void SetTarget(WorldObject target) {
		this.target = target;
	}

	protected virtual bool HitSomething() {
		if (target && swinging) {
			swinging = false;
			return true;
		}
		return false;
	}
		
	public void Swing(){
		swinging = true;
	}
	protected void InflictDamage() {
		if(target) target.TakeDamage(damage);
	}
}

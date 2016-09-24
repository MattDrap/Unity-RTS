using UnityEngine;
using System.Collections;

public class Projectile : Weapon {

	protected float range = 1;
	public float velocity = 1;
	public void SetRange(float range) {
		this.range = range;
	}
	// Update is called once per frame
	protected override void Update () {
		if(HitSomething()) {
			InflictDamage();
			Destroy (gameObject);
		}
		if(range>0) {
			float positionChange = Time.deltaTime * velocity;
			range -= positionChange;
			transform.position += (positionChange * transform.forward);
		} else {
			Destroy(gameObject);
		}
	}
	protected override bool HitSomething() {
		if(target && target.GetSelectionBounds().Contains(transform.position)) return true;
		return false;
	}
}

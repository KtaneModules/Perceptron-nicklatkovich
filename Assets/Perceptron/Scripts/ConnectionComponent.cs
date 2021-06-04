using UnityEngine;

public class ConnectionComponent : MonoBehaviour {
	public Renderer MeshRenderer;

	private float _weight;
	public float weight {
		get { return _weight; }
		set {
			MeshRenderer.material.color = Color.HSVToRGB(value * .33f, 1f, .5f + value * .5f);
			transform.localScale = new Vector3(transform.localScale.x, .001f * value + .0005f, transform.localScale.z);
			_weight = value;
		}
	}

	void Start() {
		weight = Random.Range(0f, 1f);
	}
}

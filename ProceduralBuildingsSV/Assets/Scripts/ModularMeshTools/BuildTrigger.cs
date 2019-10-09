using UnityEngine;

namespace Demo {
	public class BuildTrigger : MonoBehaviour {
		public KeyCode BuildKey;

		Shape Root;
		BuildingParameters parameters;

		void Start() {
			Root=GetComponent<Shape>();
			parameters=GetComponent<BuildingParameters>();
		}

		void Update() {
			if (Input.GetKeyDown(BuildKey)) {
				if (parameters!=null) {
					parameters.ResetRandom();
				}
				if (Root!=null) {
					Root.Generate();
				}
			}
		}
	}
}
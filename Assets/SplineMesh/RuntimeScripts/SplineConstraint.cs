using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class SplineConstraint : MonoBehaviour {
		public enum PositionalMode{
			Speed,
			Normalized
		}

        public Spline spline;
        private float rate = 0;
		public PositionalMode Mode = PositionalMode.Speed;
      	[HideInInspector] public float Speed = 0.0f;
		[HideInInspector] public float NormalizedValue = 0.0f;
	// Use this for initialization
        void OnEnable() {
            rate = 0;
#if UNITY_EDITOR
            EditorApplication.update += ConstraintUpdate;
#endif
        }

		void OnDisable() {
#if UNITY_EDITOR
            EditorApplication.update -= ConstraintUpdate;
#endif
        }

		void Update(){
			if(Application.isPlaying){
			#if UNITY_EDITOR
            	EditorApplication.update -= ConstraintUpdate;
			#endif
				ConstraintUpdate();
			}

		}
	
        void ConstraintUpdate() {
			if(spline != null){
				switch(Mode){
					case PositionalMode.Speed:
						rate += Speed*Time.deltaTime;
					break;
					case PositionalMode.Normalized:
						rate = NormalizedValue * (spline.nodes.Count-1);
					break;

				}

				if (rate > spline.nodes.Count - 1){
					rate -= spline.nodes.Count - 1;
				}
				PlaceFollower();
			}

        }

		private void PlaceFollower() {
            if (spline != null) {
                CurveSample sample = spline.GetSample(rate);
                transform.localPosition = sample.location;
                transform.localRotation = sample.Rotation;
            }
        }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplineConstraint))]
public class SplineConstraintEditor : Editor{
	public override void  OnInspectorGUI(){
		SplineConstraint t = (SplineConstraint)target;
		DrawDefaultInspector();
		if(t.Mode == SplineConstraint.PositionalMode.Speed){
			t.Speed = EditorGUILayout.FloatField("Speed",t.Speed);
		}else if(t.Mode == SplineConstraint.PositionalMode.Normalized){
			t.NormalizedValue = EditorGUILayout.Slider("Normalized Value",t.NormalizedValue,0.0f,1.0f);
		}

	}
}
#endif

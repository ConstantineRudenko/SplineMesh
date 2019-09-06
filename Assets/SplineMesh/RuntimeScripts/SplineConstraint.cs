using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[SelectionBase]
[DisallowMultipleComponent]
public class SplineConstraint : MonoBehaviour {
		public enum PositionalMode{
			Speed,
			Normalized
		}

        public Spline m_Spline;
        private float rate = 0;
		public PositionalMode Mode = PositionalMode.Speed;
      	[HideInInspector] public float Speed = 0.0f;
		[HideInInspector] public float m_NormalizedPosition = 0.0f;
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
			if(m_Spline != null){
				switch(Mode){
					case PositionalMode.Speed:
						rate += Speed*Time.deltaTime;
					break;
					case PositionalMode.Normalized:
						rate = m_NormalizedPosition * (m_Spline.nodes.Count-1);
					break;

				}

				if (rate > m_Spline.nodes.Count - 1){
					rate -= m_Spline.nodes.Count - 1;
				}
				PlaceFollower();
			}

        }

		private void PlaceFollower() {
            if (m_Spline != null) {
                CurveSample sample = m_Spline.GetSample(rate);
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
			t.m_NormalizedPosition = EditorGUILayout.Slider("Normalized Position",t.m_NormalizedPosition,0.0f,1.0f);
		}

	}
}
#endif

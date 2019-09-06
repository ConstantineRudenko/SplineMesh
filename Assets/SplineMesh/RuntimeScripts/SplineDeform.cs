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
public class SplineDeform : MonoBehaviour {
		public enum PositionalMode{
			Speed,
			Normalized
		}

        public Spline m_Spline;
        private float rate = 0;
        private MeshBender meshBender;

        [HideInInspector]
        public GameObject m_Generated;

        public Mesh m_Mesh;
        public Material m_Material;
        public Vector3 m_Rotation;
        public Vector3 m_Scale = new Vector3(1.0f,1.0f,1.0f);
		public PositionalMode Mode = PositionalMode.Speed;
      	[HideInInspector] public float m_Speed = 0.0f;
		[HideInInspector] public float m_NormalizedPosition = 0.0f;

		private void OnEnable() {
            rate = 0;
            Init();
#if UNITY_EDITOR
            EditorApplication.update += DeformerUpdate;
#endif
        }

		void OnDisable() {
#if UNITY_EDITOR
            EditorApplication.update -= DeformerUpdate;
#endif
        }

        private void OnValidate() {
            Init();
        }

		void Update(){
			if(Application.isPlaying){
			#if UNITY_EDITOR
            	EditorApplication.update -= DeformerUpdate;
			#endif
				DeformerUpdate();
			}

		}
		void DeformerUpdate() {
			if(m_Spline != null && m_Mesh != null && m_Material != null){
					switch(Mode){
						case PositionalMode.Speed:
							rate += m_Speed*Time.deltaTime;
						break;
						case PositionalMode.Normalized:
							rate = 0.0002f + m_NormalizedPosition - 0.0001f;
						break;

				}

				if (rate > 1) {
					rate --;
				}
				Deform();
			}
		}

        private void Deform() {
            if (m_Generated != null) {
                meshBender.SetInterval(m_Spline, m_Spline.Length * rate);
                meshBender.ComputeIfNeeded();
            }
        }

		private void Init() {
			if(m_Spline != null && m_Mesh != null && m_Material != null){
				string generatedName = "Mesh";
				var generatedTranform = transform.Find(generatedName);
				m_Generated = generatedTranform != null ? generatedTranform.gameObject : UOUtility.Create(generatedName, gameObject,
					typeof(MeshFilter),
					typeof(MeshRenderer),
					typeof(MeshBender));

				m_Generated.GetComponent<MeshRenderer>().material = m_Material;

				meshBender = m_Generated.GetComponent<MeshBender>();
				// m_Spline = GetComponent<Spline>();

				meshBender.Source = SourceMesh.Build(m_Mesh)
					.Rotate(Quaternion.Euler(m_Rotation))
					.Scale(m_Scale);
				meshBender.Mode = MeshBender.FillingMode.Once;
				meshBender.SetInterval(m_Spline, 0);
			}
        }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplineDeform))]
public class SplineDeformEditor : Editor{
	public override void  OnInspectorGUI(){
		SplineDeform t = (SplineDeform)target;
		DrawDefaultInspector();
		if(t.Mode == SplineDeform.PositionalMode.Speed){
			t.m_Speed = EditorGUILayout.FloatField("Speed",t.m_Speed);
		}else if(t.Mode == SplineDeform.PositionalMode.Normalized){
			t.m_NormalizedPosition = EditorGUILayout.Slider("Normalized Position",t.m_NormalizedPosition,0.0f,1.0f);
		}

	}
}
#endif



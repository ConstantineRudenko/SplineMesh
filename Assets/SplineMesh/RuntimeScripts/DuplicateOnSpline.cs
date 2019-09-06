using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SplineMesh;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[SelectionBase]
[DisallowMultipleComponent]
public class DuplicateOnSpline : MonoBehaviour {
		public enum RotationMode {
			FollowSplineNormal,
			CustomRotation,
			Combined,
		}

		public enum OffsetAxis{
			X,Y,
		}

        private GameObject m_Generated;
        public Spline m_Spline = null;
        private bool toUpdate = true;

        public GameObject m_Prefab = null;
        public float m_Scale = 1, m_ScaleRange = 0;
        [Range(0.0f,100.0f)]public float m_Spacing = 1, m_SpacingRange = 0;
		public OffsetAxis m_OffsetAxiz = OffsetAxis.X;
        public float m_Offset = 0, m_OffsetRange = 0;
		public RotationMode m_RotationMode = RotationMode.FollowSplineNormal;
		public Vector3 m_Rotation = new Vector3(0.0f,0.0f,0.0f);
        public bool m_RandomizeRotation = false;
		public float m_MinRange = -180.0f;
		public float m_MaxRange = 180.0f;
        public int m_RandomSeed = 0;


		private void OnEnable() {
			if(m_Spline != null && m_Prefab != null){
				string generatedName = "Generated Objects";
				var generatedTranform = transform.Find(generatedName);
				m_Generated = generatedTranform != null ? generatedTranform.gameObject : UOUtility.Create(generatedName, gameObject);

				m_Spline.NodeListChanged += (s, e) => {
					toUpdate = true;
					foreach (CubicBezierCurve curve in m_Spline.GetCurves()) {
						curve.Changed.AddListener(() => toUpdate = true);
					}
				};
				foreach (CubicBezierCurve curve in m_Spline.GetCurves()) {
					curve.Changed.AddListener(() => toUpdate = true);
				}
			}
        }

		private void OnValidate() {
            toUpdate = true;
        }

        private void Update() {
            if (toUpdate) {
                DuplicateOP();
                toUpdate = false;
            }
        }

		void DuplicateOP() {
			if(m_Spline != null & m_Prefab !=null){
			UOUtility.DestroyChildren(m_Generated);

            UnityEngine.Random.InitState(m_RandomSeed);
            if (m_Spacing + m_SpacingRange <= 0 ||
                m_Prefab == null)
                return;

            float distance = 0;
				while (distance <= m_Spline.Length) {
					CurveSample sample = m_Spline.GetSampleAtDistance(distance);

					GameObject go;
					go = Instantiate(m_Prefab, m_Generated.transform);
					go.transform.localRotation = Quaternion.identity;
					go.transform.localPosition = Vector3.zero;
					go.transform.localScale = Vector3.one;

					// move along spline, according to spacing + random
					go.transform.localPosition = sample.location;
					// apply scale + random
					float rangedScale = m_Scale + UnityEngine.Random.Range(0, m_ScaleRange);
					go.transform.localScale = new Vector3(rangedScale, rangedScale, rangedScale);
					switch(m_RotationMode){
						case (RotationMode.FollowSplineNormal):
							go.transform.rotation = sample.Rotation;

						break;

						case (RotationMode.CustomRotation):
							go.transform.Rotate(m_Rotation);
						break;

						case(RotationMode.Combined):
							go.transform.rotation = sample.Rotation;
							go.transform.rotation *= Quaternion.Euler(m_Rotation);
						break;
					}
					// // rotate with random yaw
					if (m_RandomizeRotation) {
						float RandomRange = UnityEngine.Random.Range(m_MinRange, m_MaxRange);
						go.transform.rotation *= Quaternion.Euler(RandomRange,RandomRange,RandomRange);
					}

					// move orthogonaly to the spline, according to offset + random
					Vector3 binormal = Vector3.zero;
					float localOffset = 1.0f;
					switch(m_OffsetAxiz){
						case OffsetAxis.X:
							binormal = (Quaternion.LookRotation(sample.tangent, sample.up) * Vector3.right).normalized;
							localOffset = m_Offset + UnityEngine.Random.Range(0, m_OffsetRange * Math.Sign(m_Offset));
							localOffset *=  sample.scale.x;
						break;
						case OffsetAxis.Y:
							binormal = (Quaternion.LookRotation(sample.tangent, sample.up) * Vector3.up).normalized;
							localOffset = m_Offset + UnityEngine.Random.Range(m_OffsetRange * Math.Sign(m_Offset),0);
							localOffset *=  sample.scale.y;
						break;
					}


					binormal *= localOffset;
					go.transform.position += binormal;

					distance += m_Spacing + UnityEngine.Random.Range(0, m_SpacingRange);
				}
			}
            
        }

}


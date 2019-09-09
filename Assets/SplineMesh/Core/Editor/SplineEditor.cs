using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SplineMesh {
    [CustomEditor(typeof(Spline))]
    public class SplineEditor : Editor {

        private const int QUAD_SIZE = 30;
        private Color CURVE_COLOR = new Color(0.8f, 0.8f, 0.8f);
        private Color CURVE_BUTTON_COLOR = new Color(0.8f, 0.8f, 0.8f);
        //private Color DIRECTION_COLOR = Color.red;
        //private Color DIRECTION_BUTTON_COLOR = Color.red;
        //private Color UP_BUTTON_COLOR = Color.green;

        private static bool LockSelectionToSpline = true;
        private static bool showUpVector = true;

        private enum SelectionType
        {
            Node,
            Up
        }

        private static SplineNode selection;
        private static SelectionType selectionType;
        private static bool mustCreateNewNode = false;
        private SerializedProperty nodesProp;
        private static Spline spline;

        private GUIStyle nodeButtonStyle, directionButtonStyle, upButtonStyle;

        //Custom Handles 
        public Texture2D HandlesTexture;
        public Event e { get { return Event.current; } }
        public float distance { get;private set;}
        public int controlID { get; private set;}
        public Color color;
        public Color hoveredColor;
        public Color selectedColor;
        public bool faceCamera = true;


        private void OnEnable() {
            spline = (Spline)target;
            nodesProp = serializedObject.FindProperty("nodes");

            //Texture2D t = EditorGUIUtility.whiteTexture;
            Texture2D t = HandlesTexture;

            nodeButtonStyle = new GUIStyle();
            nodeButtonStyle.normal.background = t;


            directionButtonStyle = new GUIStyle();
            directionButtonStyle.normal.background = t;

            upButtonStyle = new GUIStyle();
            //upButtonStyle.normal.background = t;
            selection = null;
        }

        SplineNode AddClonedNode(SplineNode node) {
            int index = spline.nodes.IndexOf(node);
            SplineNode res = new SplineNode(node.Position, node.Direction);
            if (index == spline.nodes.Count - 1) {
                spline.AddNode(res);
            } else {
                spline.InsertNode(index + 1, res);
            }
            return res;
        }

        void OnSceneGUI() {

            if (e.type == EventType.MouseDown) {
                Undo.RegisterCompleteObjectUndo(spline, "change spline topography");
                // if alt key pressed, we will have to create a new node if node position is changed
                if (e.alt) {
                    mustCreateNewNode = true;
                }
            }
            if (e.type == EventType.MouseUp) {
                mustCreateNewNode = false;
            }

            // disable game object transform gyzmo
            if (Selection.activeGameObject == spline.gameObject) {
                //Tools.current = Tool.None;
                if (selection == null && spline.nodes.Count > 0)
                    selection = spline.nodes[0];
            }

            // draw a bezier curve for each curve in the spline
            foreach (CubicBezierCurve curve in spline.GetCurves()) {
                Handles.DrawBezier(spline.transform.TransformPoint(curve.n1.Position),
                    spline.transform.TransformPoint(curve.n2.Position),
                    spline.transform.TransformPoint(curve.n1.Direction),
                    spline.transform.TransformPoint(curve.GetInverseDirection()),
                    CURVE_COLOR,
                    null,
                    3);
            }

            // draw the selection handles
            // place a handle on the node and manage position change       
            float HandleSizeMult = 0.2f;
            var HandlePos = spline.transform.TransformPoint(selection.Position);
            var HandleSize = HandleUtility.GetHandleSize(HandlePos) * HandleSizeMult;
            Color defaultColor = Handles.color;
            Vector3 sceneCamPos = SceneView.currentDrawingSceneView.camera.transform.position;
            Vector3 newPosition;
            Vector3 discNormal = sceneCamPos - spline.transform.TransformPoint(selection.Position);
            if (selectionType==SelectionType.Node)
            {
                newPosition = Handles.PositionHandle(HandlePos, Quaternion.identity);
                if (newPosition != selection.Position)
                {
                    // position handle has been moved
                    if (mustCreateNewNode)
                    {
                        mustCreateNewNode = false;
                        selection = AddClonedNode(selection);
                        selection.Direction += newPosition - selection.Position;
                        selection.Position = newPosition;
                    }
                    else
                    {
                        selection.Direction += newPosition - selection.Position;
                        selection.Position = newPosition;
                    }
                }
            }

            Handles.color = new Color(1.0f, .65f, .26f, 1.0f);
            HandlePos = spline.transform.TransformPoint(selection.Direction);
            HandleSize = HandleUtility.GetHandleSize(HandlePos) * HandleSizeMult;
            var Dirresult = Handles.FreeMoveHandle(HandlePos, Quaternion.identity, HandleSize,Vector3.zero,Handles.RectangleHandleCap);
            if (e.type == EventType.Repaint)
            {
                Handles.DrawSolidDisc(HandlePos, discNormal, HandleSize);
            }
            selection.Direction = spline.transform.InverseTransformPoint(Dirresult);

            HandlePos = 2 * spline.transform.TransformPoint(selection.Position) - spline.transform.TransformPoint(selection.Direction);
            HandleSize = HandleUtility.GetHandleSize(HandlePos)* HandleSizeMult;
            var Invresult = Handles.FreeMoveHandle(HandlePos, Quaternion.identity, HandleSize, Vector3.zero, Handles.RectangleHandleCap);
            if (e.type == EventType.Repaint)
            {
                Handles.DrawSolidDisc(HandlePos, discNormal, HandleSize);
            }
            selection.Direction = 2 * selection.Position - spline.transform.InverseTransformPoint(Invresult);

            if (showUpVector)
            {
                Handles.color = new Color(0.15f,0.87f,0.24f,1f);
                HandlePos = spline.transform.TransformPoint(selection.Position + selection.Up);
                HandleSize = HandleUtility.GetHandleSize(HandlePos) * HandleSizeMult;
                if (e.type == EventType.Repaint)
                {
                    Handles.DrawSolidDisc(HandlePos, discNormal, HandleSize);
                }
                if (selectionType == SelectionType.Up)
                {
                    var Upresult = Handles.FreeMoveHandle(HandlePos, Quaternion.LookRotation(selection.Direction - selection.Position), HandleSize, Vector3.zero, Handles.RectangleHandleCap);
                    selection.Up = (spline.transform.InverseTransformPoint(Upresult) - selection.Position).normalized;
                }

            }

            // draw the handles of all nodes, and manage selection motion
            Handles.BeginGUI();
            foreach (SplineNode n in spline.nodes) {
                var dir = spline.transform.TransformPoint(n.Direction);
                var pos = spline.transform.TransformPoint(n.Position);
                var invDir = spline.transform.TransformPoint(2 * n.Position - n.Direction);
                var up = spline.transform.TransformPoint(n.Position + n.Up);
                
                // first we check if at least one thing is in the camera field of view
                if (!(CameraUtility.IsOnScreen(pos) ||
                    CameraUtility.IsOnScreen(dir) ||
                    CameraUtility.IsOnScreen(invDir) ||
                    (showUpVector && CameraUtility.IsOnScreen(up)))) {
                    continue;
                }

                Vector3 guiPos = HandleUtility.WorldToGUIPoint(pos);
                //HandleSize = HandleUtility.GetHandleSize(guiPos) ;
                //discNormal = sceneCamPos - pos;

                //if (e.type == EventType.Repaint)
                //{
                //    Handles.color = new Color(0.04f, .54f, .93f, 1f);
                //    Handles.DrawSolidDisc(guiPos, discNormal, HandleSize);
                //}

                Color defaultCol = GUI.color;
                if (n == selection)
                {

                    Vector3 guiDir = HandleUtility.WorldToGUIPoint(dir);
                    Vector3 guiInvDir = HandleUtility.WorldToGUIPoint(invDir);
                    Vector3 guiUp = HandleUtility.WorldToGUIPoint(up);

                    // for the selected node, we also draw a line and place two buttons for directions
                    Handles.color = Color.red;
                    //Handles.DrawLine(guiDir, guiInvDir);
                    Handles.DrawBezier(
                        guiDir,guiInvDir,
                        guiDir, guiInvDir,
                        Color.red,null,3
                        );


                    if (showUpVector)
                    {
                        Handles.color = Color.green;
                        Handles.DrawLine(guiPos, guiUp);
                        if (selectionType != SelectionType.Up)
                        {
                            if (Button(guiUp, upButtonStyle))
                            {
                                selectionType = SelectionType.Up;
                            }
                        }
                    }
                }

                GUI.color = CURVE_BUTTON_COLOR;
                if (Button(guiPos, nodeButtonStyle))
                {
                    selection = n;
                    selectionType = SelectionType.Node;
                }

            }
            Handles.EndGUI();

            // Don't allow clicking over empty space to deselect the object
            if(LockSelectionToSpline==true){
                if (e.type == EventType.Layout) {
                    HandleUtility.AddDefaultControl (0);
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(target);
        }

        bool Button(Vector2 position, GUIStyle style) {
            return GUI.Button(new Rect(position - new Vector2(QUAD_SIZE / 2, QUAD_SIZE / 2), new Vector2(QUAD_SIZE, QUAD_SIZE)), GUIContent.none, style);
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            // hint
            EditorGUILayout.HelpBox("Hold Alt and drag a node to create a new one.", MessageType.Info);

            // add button
            if (selection == null) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Add node after selected")) {
                Undo.RegisterCompleteObjectUndo(spline, "add spline node");
                SplineNode newNode = new SplineNode(selection.Direction, selection.Direction + selection.Direction - selection.Position);
                var index = spline.nodes.IndexOf(selection);
                if(index == spline.nodes.Count - 1) {
                    spline.AddNode(newNode);
                } else {
                    spline.InsertNode(index + 1, newNode);
                }
                selection = newNode;
            }
            GUI.enabled = true;

            // delete button
            if (selection == null || spline.nodes.Count <= 2) {
                GUI.enabled = false;
            }
            if (GUILayout.Button("Delete selected node")) {
                Undo.RegisterCompleteObjectUndo(spline, "delete spline node");
                spline.RemoveNode(selection);
                selection = null;
            }
            GUI.enabled = true;

            LockSelectionToSpline = GUILayout.Toggle(LockSelectionToSpline, "Lock Selection To Spline");
            showUpVector = GUILayout.Toggle(showUpVector, "Show up vector");
            spline.IsLoop = GUILayout.Toggle(spline.IsLoop, "Is loop (experimental)");

            // nodes
            EditorGUILayout.PropertyField(nodesProp);
            EditorGUI.indentLevel++;
            if (nodesProp.isExpanded) {
                for (int i = 0; i < nodesProp.arraySize; i++) {
                    SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(nodeProp);
                    EditorGUI.indentLevel++;
                    if (nodeProp.isExpanded) {
                        drawNodeData(nodeProp, spline.nodes[i]);
                    }
                    EditorGUI.indentLevel--;
                }
            }
            EditorGUI.indentLevel--;

            if (selection != null) {
                int index = spline.nodes.IndexOf(selection);
                SerializedProperty nodeProp = nodesProp.GetArrayElementAtIndex(index);
                EditorGUILayout.LabelField("Selected node (node " + index + ")");
                EditorGUI.indentLevel++;
                drawNodeData(nodeProp, selection);
                EditorGUI.indentLevel--;
            } else {
                EditorGUILayout.LabelField("No selected node");
            }
        }

        private void drawNodeData(SerializedProperty nodeProperty, SplineNode node) {
            using (var check = new EditorGUI.ChangeCheckScope()) {
                var positionProp = nodeProperty.FindPropertyRelative("position");
                EditorGUILayout.PropertyField(positionProp, new GUIContent("Position"));
                if (check.changed) {
                    node.Position = positionProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var directionProp = nodeProperty.FindPropertyRelative("direction");
                EditorGUILayout.PropertyField(directionProp, new GUIContent("Direction"));
                if (check.changed) {
                    node.Direction = directionProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var upProp = nodeProperty.FindPropertyRelative("up");
                EditorGUILayout.PropertyField(upProp, new GUIContent("Up"));
                if (check.changed) {
                    node.Up = upProp.vector3Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var scaleProp = nodeProperty.FindPropertyRelative("scale");
                EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale"));
                if (check.changed) {
                    node.Scale = scaleProp.vector2Value;
                }
            }

            using (var check = new EditorGUI.ChangeCheckScope()) {
                var rollProp = nodeProperty.FindPropertyRelative("roll");
                EditorGUILayout.PropertyField(rollProp, new GUIContent("Roll"));
                if (check.changed) {
                    node.Roll = rollProp.floatValue;
                }
            }
        }

        [MenuItem("GameObject/3D Object/Spline")]
        public static void CreateSpline() {
            new GameObject("Spline", typeof(Spline));
        }
    }
}

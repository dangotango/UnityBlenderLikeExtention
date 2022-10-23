
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using System;
using System.Linq;
using System.IO;

//分からない
//SceneViewのワールド座標上に大きさ一定でカメラを向く形でテクスチャを描画させる方法。
//移動モードで描画するHandleにパースがかかっている（透視投影時）。
//UIElementでSerializedObjectにバインドする方法。(UIElement上の値がグレイアウトしていて入力できない)

namespace BlenderLikeExtentions
{
    [EditorTool("BlenderLikeExtension/Edit Transform")]
    public class EditTransform : EditorTool
    {
        static Locks Lock = Locks.None;
        static Modes Mode = Modes.None;
        static LockCoordinateModifyStates LockCoordinateModifyState = LockCoordinateModifyStates.None;

        static Vector3 normal = Vector3.zero;
        static Vector3 prevHitPos = Vector3.zero;
        static Vector3 center = Vector3.zero;
        static TransformChache[] transformChaches;
        static Vector3 axis = Vector3.one;

        static string inputValue = "";
        static bool inputHasMinus = false;
        static bool isInputEditing = false;

        static Vector3 movedAmount = Vector3.zero;
        static float rotateAmount = 0f;
        static Vector3 scaleAmount = Vector3.zero;

        static bool isFirstUpdate = true;

        static Coordinates Coordinate
        {
            get
            {
                switch (LockCoordinateModifyState)
                {
                    case LockCoordinateModifyStates.GrobalNoLock: return Coordinates.Global;
                    case LockCoordinateModifyStates.ToggleCoordinate: return (Coordinates)(((int)ScriptableSingleton<EditTransformScriptableData>.instance.Coordinate + 1) % 2);
                    default: return ScriptableSingleton<EditTransformScriptableData>.instance.Coordinate;
                }

            }
        }
        static TransformPivots TransformPivot => ScriptableSingleton<EditTransformScriptableData>.instance.TransformPivot;

        static float snapMoveAmount => ScriptableSingleton<EditTransformScriptableData>.instance.snapMoveAmount;
        static float snapRotateAmount => ScriptableSingleton<EditTransformScriptableData>.instance.snapRotateAmount;
        static float snapScaleAmount => ScriptableSingleton<EditTransformScriptableData>.instance.snapScaleAmount - 1;
        static bool useSnap => Event.current.control || ScriptableSingleton<EditTransformScriptableData>.instance.useSnap;
        static bool useCursor3D => ScriptableSingleton<EditTransformScriptableData>.instance.useCursor3D;
        static Vector3 cursor3DPosition => ScriptableSingleton<EditTransformScriptableData>.instance.cursor3DPosition;

        static Transform activeTransform;

        //キーダウン入力に対してコールバックを登録する辞書,OnSceneGUIでチェックして実行
        static KeydownActions keydownActions = new KeydownActions();

        void OnEnable()
        {
            //x,y,zの複数押下時に押下のたびに座標軸をトグルさせる。　現在の座標軸→現在設定していないほうの軸→グローバル軸
            void toggleLockCoordinateModify()
            {
                LockCoordinateModifyState = (LockCoordinateModifyStates)(((int)LockCoordinateModifyState + 1) % 3);
                if (LockCoordinateModifyState == LockCoordinateModifyStates.GrobalNoLock)
                {
                    Lock = Locks.None;
                    axis = Vector3.one;
                }
                OnLockOrCordinateChange();
            }

            //G=移動　R＝回転　S=スケール　モードを変更する
            keydownActions.Add(new KeyInfo(KeyCode.G, false, false, false), () =>
            {
                ChangeMode(Modes.Move);
            });
            keydownActions.Add(new KeyInfo(KeyCode.R, false, false, false), () =>
            {
                ChangeMode(Modes.Rotate);
            });
            keydownActions.Add(new KeyInfo(KeyCode.S, false, false, false), () =>
            {
                ChangeMode(Modes.Scale);
            });

            //使用する軸を変更する
            keydownActions.Add(new KeyInfo(KeyCode.X, false, false, false), () =>
             {
                 if (Mode == Modes.None) return;

                 if (Lock != Locks.X)
                 {
                     Lock = Locks.X;
                     axis = Vector3.right;
                     LockCoordinateModifyState = LockCoordinateModifyStates.None;
                     OnLockOrCordinateChange();
                 }
                 else
                     toggleLockCoordinateModify();
             });
            keydownActions.Add(new KeyInfo(KeyCode.Y, false, false, false), () =>
            {
                if (Mode == Modes.None) return;

                if (Lock != Locks.Y)
                {
                    Lock = Locks.Y;
                    axis = Vector3.up;
                    LockCoordinateModifyState = LockCoordinateModifyStates.None;
                    OnLockOrCordinateChange();
                }
                else
                    toggleLockCoordinateModify();
            });
            keydownActions.Add(new KeyInfo(KeyCode.Z, false, false, false), () =>
            {
                if (Mode != Modes.None && Lock != Locks.Z)
                {
                    Lock = Locks.Z;
                    axis = Vector3.forward;
                    LockCoordinateModifyState = LockCoordinateModifyStates.None;
                    OnLockOrCordinateChange();
                }
                else
                    toggleLockCoordinateModify();
            });
            keydownActions.Add(new KeyInfo(KeyCode.X, false, true, false), () =>
            {
                if (Mode != Modes.None && Lock != Locks.YZ)
                {
                    Lock = Locks.YZ;
                    axis = new Vector3(0, 1, 1);
                    LockCoordinateModifyState = LockCoordinateModifyStates.None;
                    OnLockOrCordinateChange();
                }
                else
                    toggleLockCoordinateModify();
            });
            keydownActions.Add(new KeyInfo(KeyCode.Y, false, true, false), () =>
            {
                if (Mode != Modes.None && Lock != Locks.XZ)
                {
                    Lock = Locks.XZ;
                    axis = new Vector3(1, 0, 1);
                    LockCoordinateModifyState = LockCoordinateModifyStates.None;
                    OnLockOrCordinateChange();
                }
                else
                    toggleLockCoordinateModify();
            });
            keydownActions.Add(new KeyInfo(KeyCode.Z, false, true, false), () =>
            {
                if (Mode != Modes.None && Lock != Locks.XY)
                {
                    Lock = Locks.XY;
                    axis = new Vector3(1, 1, 0);
                    LockCoordinateModifyState = LockCoordinateModifyStates.None;
                    OnLockOrCordinateChange();
                }
                else
                    toggleLockCoordinateModify();
            });

            //3Dカーソル位置を(0,0)にリセット
            keydownActions.Add(new KeyInfo(KeyCode.C, false, true, false), () =>
            {
                ScriptableSingleton<EditTransformScriptableData>.instance.setCursor3DPosition(Vector3.zero);
            });
        }

        //ツールエディターを起動するショートカット
        [Shortcut("BlenderLike/Move Mode", typeof(SceneView), KeyCode.G)]
        static public void ActivateMoveMode()
        {
            if (ToolManager.activeToolType != typeof(EditTransform) && Selection.count != 0)
            {
                EditTransformScriptableData.instance.ModeOnActivate = Modes.Move;
                ToolManager.SetActiveTool<EditTransform>();
            }
        }
        [Shortcut("BlenderLike/Rotate Mode", typeof(SceneView), KeyCode.R)]
        static public void ActivateRotateMode()
        {
            if (ToolManager.activeToolType != typeof(EditTransform) && Selection.count != 0)
            {
                EditTransformScriptableData.instance.ModeOnActivate = Modes.Rotate;
                ToolManager.SetActiveTool<EditTransform>();
            }
        }
        [Shortcut("BlenderLike/Scale Mode", typeof(SceneView), KeyCode.S)]
        static public void ActivateScaleMode()
        {
            if (ToolManager.activeToolType != typeof(EditTransform) && Selection.count != 0)
            {
                EditTransformScriptableData.instance.ModeOnActivate = Modes.Scale;
                ToolManager.SetActiveTool<EditTransform>();
            }
        }

        //ツールエディタ起動時の処理
        public override void OnActivated()
        {
            Selection.selectionChanged += EditEnd;
            SceneView.lastActiveSceneView.TryGetOverlay("EditTransform Overlay", out var overlay);
            overlay.displayed = true;
            isFirstUpdate = true;

        }

        //ツールエディタ終了時の処理
        public override void OnWillBeDeactivated()
        {
            EditEnd();
            Selection.selectionChanged -= EditEnd;
            SceneView.lastActiveSceneView.TryGetOverlay("EditTransform Overlay", out var overlay);
            overlay.displayed = false;
            EditTransformScriptableData.instance.ModeOnActivate = Modes.None;
        }

        void ChangeMode(Modes nextMode)
        {
            if (Mode == nextMode || Selection.count == 0) return;

            //最初の一回目
            if (Mode == Modes.None)
            {
                Undo.RegisterCompleteObjectUndo(Selection.transforms, "edit transform");
                transformChaches = Selection.transforms.Select(t => new TransformChache(t)).ToArray();
                SceneViewCameraOperation.isActive = false;
            }
            else //他のモードから切り替え
                foreach (var chache in transformChaches) chache.Undo();
            if (TransformPivot == TransformPivots.ActiveObject) center = activeTransform.position;
            else if (TransformPivot == TransformPivots.Cursor3D) center = cursor3DPosition;
            else center = Selection.transforms.Select(t => t.position).Aggregate((p, p2) => p + p2) / Selection.transforms.Length;

            normal = SceneView.lastActiveSceneView.rotation * Vector3.back;
            var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            new Plane(normal, center).Raycast(ray, out var distance);
            prevHitPos = ray.GetPoint(distance);

            Lock = Locks.None;
            LockCoordinateModifyState = LockCoordinateModifyStates.None;
            axis = Vector3.one;
            inputValue = "";
            inputHasMinus = false;
            isInputEditing = false;
            Mode = nextMode;

            movedAmount = Vector3.zero;
            rotateAmount = 0f;
            scaleAmount = Vector3.zero;
            return;
        }

        void EditEnd()
        {
            Mode = Modes.None;
            transformChaches = null;
            Lock = Locks.None;
            LockCoordinateModifyState = LockCoordinateModifyStates.None;
            axis = Vector3.one;
            SceneViewCameraOperation.isActive = true;

            inputValue = "";
            inputHasMinus = false;
            isInputEditing = false;

            movedAmount = Vector3.zero;
            rotateAmount = 0f;
            scaleAmount = Vector3.zero;

            activeTransform = Selection.activeTransform;
        }
        public override void OnToolGUI(EditorWindow window)
        {
            var currentEvent = Event.current;

            //キーダウン入力に対するコールバックがあれば実行
            keydownActions.TryExecute();

            //3Dカーソル描画
            if (useCursor3D)
            {
                var cameraRotation = SceneView.lastActiveSceneView.rotation;
                var cursorPos = cursor3DPosition;
                var size = HandleUtility.GetHandleSize(cursor3DPosition);
                Handles.color = Color.red;
                Handles.DrawWireDisc(cursor3DPosition, cameraRotation * Vector3.back, size / 10, 2);
                Handles.color = Color.black;

                var p1 = cameraRotation * (Vector3.left * (size / 8)) + cursorPos;
                var p2 = cameraRotation * (Vector3.right * (size / 8)) + cursorPos;
                var p3 = cameraRotation * (Vector3.down * (size / 8)) + cursorPos;
                var p4 = cameraRotation * (Vector3.up * (size / 8)) + cursorPos;
                Handles.color = Color.black;
                Handles.DrawLine(p1, p2, 2f);
                Handles.DrawLine(p3, p4, 2f);

                //3Dカーソル移動　shift+右マウス
                if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && currentEvent.shift && currentEvent.button == 1)
                {
                    var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                    new Plane(cameraRotation * Vector3.back, SceneView.lastActiveSceneView.pivot).Raycast(ray, out var distance);
                    var pos = ray.GetPoint(distance);
                    ScriptableSingleton<EditTransformScriptableData>.instance.setCursor3DPosition(pos);
                }
            }

            if (Selection.count <= 0) return;
            if (SceneView.currentDrawingSceneView != SceneView.lastActiveSceneView) return;

            //ToolEditorをショットカットキーから起動したときの値を反映させる
            if (isFirstUpdate)
            {
                isFirstUpdate = false;
                ChangeMode(EditTransformScriptableData.instance.ModeOnActivate);
            }

            if (Mode == Modes.None) return;

            //操作中の視点操作無効
            if (currentEvent.type == EventType.MouseDown)
                if (currentEvent.button == 1 || currentEvent.button == 2) currentEvent.Use();
            if (currentEvent.type == EventType.ScrollWheel) currentEvent.Use();
            if (currentEvent.button == 2 && currentEvent.type == EventType.MouseDrag) currentEvent.Use();

            //終了時に選択が切れないようにする
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            //操作軸の表示
            if (Lock != Locks.None)
            {
                float lineLength = 65535f;//適当
                if (Coordinate == Coordinates.Global && Mode != Modes.Scale)
                {
                    if (axis.x == 1)
                    {
                        Handles.color = Color.red;
                        Handles.DrawLine(center + Vector3.right * lineLength, center + Vector3.left * lineLength);
                    }
                    if (axis.y == 1)
                    {
                        Handles.color = Color.green;
                        Handles.DrawLine(center + Vector3.down * lineLength, center + Vector3.up * lineLength);

                    }
                    if (axis.z == 1)
                    {
                        Handles.color = Color.blue;
                        Handles.DrawLine(center + Vector3.forward * lineLength, center + Vector3.back * lineLength);
                    }
                }
                else
                {
                    for (int i = 0; i < Selection.count; i++)
                    {
                        var chache = transformChaches[i];
                        if (axis.x == 1)
                        {
                            Handles.color = Color.red;
                            Handles.DrawLine(chache.position + chache.right * lineLength, chache.position + chache.right * -lineLength);
                        }
                        if (axis.y == 1)
                        {
                            Handles.color = Color.green;
                            Handles.DrawLine(chache.position + chache.up * lineLength, chache.position + chache.up * -lineLength);
                        }
                        if (axis.z == 1)
                        {
                            Handles.color = Color.blue;
                            Handles.DrawLine(chache.position + chache.forward * lineLength, chache.position + chache.forward * -lineLength);
                        }
                    }
                }
            }

            //数字入力受付
            if (currentEvent.isKey && currentEvent.type == EventType.KeyDown)
            {
                bool validInput = false;

                if (currentEvent.keyCode == KeyCode.Alpha0)
                {
                    if (inputValue != "0") { inputValue += "0"; validInput = true; }
                }
                else if (49 <= (int)currentEvent.keyCode && (int)currentEvent.keyCode < 58)
                {
                    var number = (int)currentEvent.keyCode - 48;
                    inputValue += number.ToString();
                    validInput = true;
                }
                else if (currentEvent.keyCode == KeyCode.Period)
                {
                    var hasComma = inputValue.Contains(".");
                    if (!hasComma)
                    {
                        if (inputValue.Length == 0) inputValue = "0.";
                        else inputValue += ".";
                        validInput = true;
                    }
                }
                else if (currentEvent.keyCode == KeyCode.Backspace)
                {
                    if (inputValue.Length <= 1) inputValue = "0";
                    else { inputValue = inputValue.Remove(inputValue.Length - 1); }
                    validInput = true;
                }
                else if (currentEvent.keyCode == KeyCode.Minus)
                {
                    inputHasMinus = !inputHasMinus; validInput = true;
                }
                if (validInput)
                {
                    ApplyInputValue();
                    isInputEditing = true;
                }
                currentEvent.Use();
            }

            //エンターキーと左クリックで操作終了
            if (currentEvent.keyCode == KeyCode.Return || currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                EditEnd();
                return;
            }

            if (isInputEditing) return;

            //マウス入力受付
            var inputStrength = currentEvent.shift ? .2f : 1f;//shiftを押しているときは入力値を抑える。
            var data = ScriptableSingleton<EditTransformScriptableData>.instance;
            var snap = useSnap;

            //初期値＋変更量の累計＝現在値
            //移動モード    
            if (Mode == Modes.Move)
            {
                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                new Plane(normal, center).Raycast(ray, out var distance);
                Vector3 hitPos = ray.GetPoint(distance);

                Vector3 delta = Vector3.ProjectOnPlane((hitPos - prevHitPos), normal) * inputStrength;
                Vector3 forward = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.forward : transformChaches[0].forward;
                Vector3 up = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.up : transformChaches[0].up;
                Vector3 right = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.right : transformChaches[0].right;

                movedAmount += delta * inputStrength;
                float x = 0; float y = 0; float z = 0;
                if (axis.x > 0) x = Vector3.Dot(movedAmount, right);
                if (axis.y > 0) y = Vector3.Dot(movedAmount, up);
                if (axis.z > 0) z = Vector3.Dot(movedAmount, forward);
                var _movedAmount = snap ? new Vector3(Mathf.Floor(x / snapMoveAmount) * snapMoveAmount, Mathf.Floor(y / snapMoveAmount) * snapMoveAmount, Mathf.Floor(z / snapMoveAmount) * snapMoveAmount) : new Vector3(x, y, z);

                Selection.transforms[0].position = transformChaches[0].position + forward * _movedAmount.z + up * _movedAmount.y + right * _movedAmount.x;
                for (int i = 1; i < Selection.transforms.Length; i++)
                {
                    forward = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.forward : transformChaches[i].forward;
                    up = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.up : transformChaches[i].up;
                    right = Coordinate == Coordinates.Global || Lock == Locks.None ? Vector3.right : transformChaches[i].right;
                    Selection.transforms[i].position = transformChaches[i].position + forward * _movedAmount.z + up * _movedAmount.y + right * _movedAmount.x;
                }
                prevHitPos = hitPos;

                //移動ハンドル描画
                var size = HandleUtility.GetHandleSize(hitPos) * .1f;
                Handles.color = Color.black;
                var offset = new Vector3(0, .26f, 0) * HandleUtility.GetHandleSize(hitPos);
                var v1 = SceneView.lastActiveSceneView.rotation * (new Vector3(0, 1, 1) * size + offset);
                var v2 = SceneView.lastActiveSceneView.rotation * (new Vector3(0.87f, -0.50f, 0) * size + offset);
                var v3 = SceneView.lastActiveSceneView.rotation * (new Vector3(-0.87f, -0.50f, 0) * size + offset);
                var _normal = SceneView.lastActiveSceneView.rotation * Vector3.back;
                for (int i = 0; i < 4; i++)
                {
                    var _v1 = Quaternion.AngleAxis(90 * i, _normal) * v1;
                    var _v2 = Quaternion.AngleAxis(90 * i, _normal) * v2;
                    var _v3 = Quaternion.AngleAxis(90 * i, _normal) * v3;
                    Handles.DrawLine(_v1 + hitPos, _v2 + hitPos, 2);
                    Handles.DrawLine(_v2 + hitPos, _v3 + hitPos, 2);
                    Handles.DrawLine(_v3 + hitPos, _v1 + hitPos, 2);
                }
            }
            //回転モード
            else if (Mode == Modes.Rotate)
            {
                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                new Plane(normal, center).Raycast(ray, out var distance);
                Vector3 hitPos = ray.GetPoint(distance);

                var a = prevHitPos - center;
                var b = hitPos - center;
                var deltaAngle = Vector3.SignedAngle(a, b, normal) * inputStrength;
                rotateAmount += deltaAngle;
                var _rotateAmount = snap ? Mathf.Floor(rotateAmount / snapRotateAmount) * snapRotateAmount : rotateAmount;

                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    Quaternion deltaRotation;
                    if (Lock == Locks.None)
                        deltaRotation = Quaternion.AngleAxis(_rotateAmount, normal);
                    else
                    {
                        if (Coordinate == Coordinates.Global)
                            deltaRotation = Quaternion.AngleAxis(_rotateAmount, axis);
                        else
                            deltaRotation = Quaternion.AngleAxis(_rotateAmount, transformChaches[i].rotation * axis);
                    }
                    Selection.transforms[i].rotation = deltaRotation * transformChaches[i].rotation;

                    if (TransformPivot == TransformPivots.Center || TransformPivot == TransformPivots.Cursor3D || TransformPivot == TransformPivots.ActiveObject && activeTransform != null)
                    {
                        var offsetPos = deltaRotation * (transformChaches[i].position - center);
                        Selection.transforms[i].position = center + offsetPos;
                    }
                }
                prevHitPos = hitPos;

                //回転ハンドル描画
                var dir = Quaternion.LookRotation((hitPos - center), normal);
                var perpendDir1 = Quaternion.AngleAxis(90, normal) * dir;
                var perpendDir2 = Quaternion.AngleAxis(-90, normal) * dir;

                var size = HandleUtility.GetHandleSize(hitPos);

                var offset = perpendDir1 * Vector3.forward * size * .4f;

                var v1 = Vector3.back * size * .2f;
                var v2 = Quaternion.AngleAxis(45, Vector3.up) * Vector3.back * size * .15f;
                var v3 = Quaternion.AngleAxis(-45, Vector3.up) * Vector3.back * size * .15f;
                var v00 = hitPos + offset;
                var v01 = hitPos - offset;

                Handles.color = Color.black;
                Handles.DrawLine(v00, v00 + perpendDir1 * v1, 3f);
                Handles.DrawLine(v00, v00 + perpendDir1 * v2, 3f);
                Handles.DrawLine(v00, v00 + perpendDir1 * v3, 3f);

                Handles.DrawLine(v01, v01 + perpendDir2 * v1, 3f);
                Handles.DrawLine(v01, v01 + perpendDir2 * v2, 3f);
                Handles.DrawLine(v01, v01 + perpendDir2 * v3, 3f);

                Handles.DrawDottedLine(center, hitPos, 4f);
            }
            //拡大モード
            else if (Mode == Modes.Scale)
            {
                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                new Plane(normal, center).Raycast(ray, out var distance);
                Vector3 hitPos = ray.GetPoint(distance);

                var a = (prevHitPos - center).magnitude;
                var b = (hitPos - center).magnitude;
                var deltaSize = (b - a) / a * axis * inputStrength;

                scaleAmount += deltaSize;
                var _scaleAmount = snap ? new Vector3(Mathf.Floor(scaleAmount.x / snapScaleAmount) * snapScaleAmount, Mathf.Floor(scaleAmount.y / snapScaleAmount) * snapScaleAmount, Mathf.Floor(scaleAmount.z / snapScaleAmount) * snapScaleAmount) : scaleAmount;

                var nextSize = _scaleAmount + Vector3.one;
                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    Selection.transforms[i].transform.localScale = Vector3.Scale(transformChaches[i].scale, nextSize);

                    if (TransformPivot == TransformPivots.Center || TransformPivot == TransformPivots.Cursor3D || TransformPivot == TransformPivots.ActiveObject && activeTransform != null)
                    {
                        var offsetPos = Vector3.Scale((transformChaches[i].position - center), nextSize);
                        Selection.transforms[i].position = center + offsetPos;
                    }
                }
                prevHitPos = hitPos;

                //スケールハンドル描画
                var dir = Quaternion.LookRotation((hitPos - center), normal);
                var perpendDir1 = dir;
                var perpendDir2 = Quaternion.AngleAxis(180, normal) * dir;

                var size = HandleUtility.GetHandleSize(hitPos);

                var offset = perpendDir1 * Vector3.forward * size * .4f;

                var v1 = Vector3.back * size * .2f; ;
                var v2 = Quaternion.AngleAxis(45, Vector3.up) * Vector3.back * size * .15f; ;
                var v3 = Quaternion.AngleAxis(-45, Vector3.up) * Vector3.back * size * .15f; ;
                var v00 = hitPos + offset;
                var v01 = hitPos - offset;

                Handles.color = Color.black;
                Handles.DrawLine(v00, v00 + perpendDir1 * v1, 3f);
                Handles.DrawLine(v00, v00 + perpendDir1 * v2, 3f);
                Handles.DrawLine(v00, v00 + perpendDir1 * v3, 3f);

                Handles.DrawLine(v01, v01 + perpendDir2 * v1, 3f);
                Handles.DrawLine(v01, v01 + perpendDir2 * v2, 3f);
                Handles.DrawLine(v01, v01 + perpendDir2 * v3, 3f);

                Handles.DrawDottedLine(center, hitPos, 3f);
            }
            SceneView.lastActiveSceneView.Repaint();
        }

        void OnLockOrCordinateChange()
        {
            //移動モードの時は再計算する
            if (Mode == Modes.Move)
            {
                if (Lock == Locks.XY) normal = Coordinate == Coordinates.Global ? Vector3.forward : transformChaches[0].forward;
                else if (Lock == Locks.YZ) normal = Coordinate == Coordinates.Global ? Vector3.right : transformChaches[0].right;
                else if (Lock == Locks.XZ) normal = Coordinate == Coordinates.Global ? Vector3.up : transformChaches[0].up;
                else
                {
                    var toCamera = SceneView.lastActiveSceneView.camera.transform.position - transformChaches[0].position;
                    var right = Coordinate == Coordinates.Global ? Vector3.right : transformChaches[0].right;
                    var up = Coordinate == Coordinates.Global ? Vector3.up : transformChaches[0].up;
                    var forward = Coordinate == Coordinates.Global ? Vector3.forward : transformChaches[0].forward;

                    Vector3 v1 = Vector3.zero, v2 = Vector3.zero;
                    if (Lock == Locks.X) { v1 = up; v2 = forward; }
                    if (Lock == Locks.Y) { v1 = right; v2 = forward; }
                    if (Lock == Locks.Z) { v1 = up; v2 = right; }

                    var a = Mathf.Abs(Vector3.Dot(toCamera, v1));
                    var b = Mathf.Abs(Vector3.Dot(toCamera, v2));
                    if (b > a) normal = v2;
                    else normal = v1;
                }

                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                new Plane(normal, center).Raycast(ray, out var distance);
                prevHitPos = ray.GetPoint(distance);
            }
        }

        //数字打ち込み時に入力値を反映させる
        void ApplyInputValue()
        {
            if (inputValue.Length == 0) return;
            var value = float.Parse(inputValue) * (inputHasMinus ? -1 : 1);
            if (Mode == Modes.Move)
            {
                Vector3 delta = axis * value;
                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    if (Coordinate == Coordinates.Global) Selection.transforms[i].position = transformChaches[i].position + delta;
                    else Selection.transforms[i].position = transformChaches[i].position + transformChaches[i].rotation * delta;
                }
            }
            else if (Mode == Modes.Rotate)
            {
                var deltaAngle = value;

                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    Quaternion deltaRotation;
                    if (Lock == Locks.None)
                        deltaRotation = Quaternion.AngleAxis(deltaAngle, normal);
                    else
                    if (Coordinate == Coordinates.Global) deltaRotation = Quaternion.AngleAxis(deltaAngle, axis);
                    else deltaRotation = Quaternion.AngleAxis(deltaAngle, transformChaches[i].rotation * axis);
                    Selection.transforms[i].rotation = deltaRotation * transformChaches[i].rotation;

                    if (TransformPivot == TransformPivots.Center || TransformPivot == TransformPivots.Cursor3D || TransformPivot == TransformPivots.ActiveObject && activeTransform != null)
                    {
                        var offsetPos = deltaRotation * (transformChaches[i].position - center);
                        Selection.transforms[i].position = center + offsetPos;
                    }
                }
            }
            else if (Mode == Modes.Scale)
            {
                Vector3 nextSize = new Vector3(1, 1, 1);
                if (axis.x == 1) nextSize.x *= value;
                if (axis.y == 1) nextSize.y *= value;
                if (axis.z == 1) nextSize.z *= value;
                for (int i = 0; i < Selection.transforms.Length; i++)
                {
                    Selection.transforms[i].transform.localScale = Vector3.Scale(transformChaches[i].scale, nextSize);

                    if (TransformPivot == TransformPivots.Center || TransformPivot == TransformPivots.Cursor3D || TransformPivot == TransformPivots.ActiveObject && activeTransform != null)
                    {
                        var offsetPos = Vector3.Scale((transformChaches[i].position - center), nextSize);
                        Selection.transforms[i].position = center + offsetPos;
                    }
                }
            }
            SceneView.lastActiveSceneView.Repaint();
        }
    }
    public enum Modes { Move, Rotate, Scale, None };
    public enum Locks { X, Y, Z, YZ, XZ, XY, None };
    public enum Coordinates { Global, Local };
    public enum LockCoordinateModifyStates { None, ToggleCoordinate, GrobalNoLock }
    public enum TransformPivots { Center, EachOrigin, ActiveObject, Cursor3D }

    //モード変更時に都度リセットさせるためのキャッシュ
    public class TransformChache
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion rotation;
        public Transform transform;

        public TransformChache(Transform transform)
        {
            position = transform.position;
            scale = transform.localScale;
            rotation = transform.rotation;
            this.transform = transform;
        }
        public Vector3 right => rotation * Vector3.right;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 forward => rotation * Vector3.forward;
        public void Undo()
        {
            transform.position = this.position;
            transform.localScale = this.scale;
            transform.rotation = this.rotation;
        }
    }

    //EditTransform、起動の都度忘れてほしくないデータ
    class EditTransformScriptableData : ScriptableSingleton<EditTransformScriptableData>
    {
        public Modes ModeOnActivate = Modes.None;

        public float snapMoveAmount = 1f;
        public float snapRotateAmount = 5f;
        public float snapScaleAmount = 1.1f;
        public Coordinates Coordinate = Coordinates.Global;
        public TransformPivots TransformPivot = TransformPivots.Center;
        public bool useSnap = false;

        public bool useCursor3D = true;
        public Vector3 cursor3DPosition = Vector3.zero;

        public void setCursor3DPosition(Vector3 pos)
        {
            cursor3DPosition = pos;
            if (OnChangeCursor3DPosition != null) OnChangeCursor3DPosition.Invoke(pos);
        }
        public event Action<Vector3> OnChangeCursor3DPosition;
    }

    //EditTransform用のオーバーレイUI
    [Overlay(typeof(SceneView), "EditTransform Overlay", "BlenderLike Overlay")]
    public class EditTransformOverlay : Overlay
    {

        public override VisualElement CreatePanelContent()
        {
            var guids = AssetDatabase.FindAssets("t:BlenderLikeConfig");
            var assetPath = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(guids[0]));
            var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Path.Combine(assetPath, "EditTransformOverlay.uxml"));
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.Combine(assetPath, "EditTransformOverlay.uss"));
            var root = treeAsset.Instantiate();
            root.styleSheets.Add(uss);

            VisualElement currentSelected = null;

            Coordinates[] coordinates = new Coordinates[] { Coordinates.Global, Coordinates.Local };
            var data = ScriptableSingleton<EditTransformScriptableData>.instance;

            var entries = root.Q<VisualElement>("tabs").Children().Select((VisualElement elm, int i) => (elm, coordinates[i]));
            foreach (var (tab, coordinate) in entries)
            {
                if (data.Coordinate == coordinate)
                {
                    tab.RemoveFromClassList("tab");
                    tab.AddToClassList("tab_selected");
                    currentSelected = tab;
                }

                tab.RegisterCallback<ClickEvent>((ClickEvent e) =>
                {
                    var target = e.currentTarget as VisualElement;
                    if (target == currentSelected) return;

                    currentSelected.RemoveFromClassList("tab_selected");
                    currentSelected.AddToClassList("tab");
                    target.AddToClassList("tab_selected");
                    target.RemoveFromClassList("tab");

                    data.Coordinate = coordinate;
                    currentSelected = target;
                });
            }

            var snapMove = root.Q<FloatField>("snapMoveAmount");
            snapMove.value = data.snapMoveAmount;
            snapMove.RegisterCallback<ChangeEvent<float>>((ChangeEvent<float> e) =>
            {
                data.snapMoveAmount = Mathf.Clamp(e.newValue, 1f, Mathf.Infinity);
            });

            var snapRotate = root.Q<FloatField>("snapRotateAmount");
            snapRotate.value = data.snapRotateAmount;
            snapRotate.RegisterCallback<ChangeEvent<float>>((ChangeEvent<float> e) =>
             {
                 data.snapRotateAmount = Mathf.Clamp(e.newValue, 0f, Mathf.Infinity);
             });

            var snapScale = root.Q<FloatField>("snapScaleAmount");
            snapScale.value = data.snapScaleAmount;
            snapScale.RegisterCallback<ChangeEvent<float>>((ChangeEvent<float> e) =>
             {
                 data.snapScaleAmount = Mathf.Clamp(e.newValue, 1f, Mathf.Infinity);
             });

            var useSnap = root.Q<Toggle>("useSnap");
            useSnap.value = data.useSnap;
            useSnap.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> e) =>
             {
                 data.useSnap = e.newValue;
             });

            var transformPivot = root.Q<EnumField>("transformPivot");
            transformPivot.value = data.TransformPivot;
            transformPivot.RegisterCallback<ChangeEvent<Enum>>((ChangeEvent<Enum> e) =>
             {
                 data.TransformPivot = (TransformPivots)e.newValue;
             });

            var useCursor3D = root.Q<Toggle>("useCursor3D");
            useCursor3D.value = data.useCursor3D;
            useCursor3D.RegisterCallback<ChangeEvent<bool>>((ChangeEvent<bool> e) =>
             {
                 data.useCursor3D = e.newValue;
             });

            var cursor3DPosition = root.Q<Vector3Field>("cursor3DPosition");
            cursor3DPosition.value = data.cursor3DPosition;
            cursor3DPosition.RegisterCallback<ChangeEvent<Vector3>>((ChangeEvent<Vector3> e) =>
             {
                 data.cursor3DPosition = e.newValue;
             });
            data.OnChangeCursor3DPosition += (Vector3 pos) => { cursor3DPosition.value = pos; };

            var objectToCursor3DButton = root.Q<Button>("objectToCursor3DButton");
            objectToCursor3DButton.RegisterCallback<ClickEvent>((ClickEvent e) =>
             {
                 foreach (var t in Selection.transforms)
                 {
                     t.position = data.cursor3DPosition;
                 }
             });

            var cursor3DToObjectButton = root.Q<Button>("Cursor3DToObjectButton");
            cursor3DToObjectButton.RegisterCallback<ClickEvent>((ClickEvent e) =>
             {
                 if (Selection.count > 0)
                 {
                     var pos = Selection.transforms.Select(_ => _.position).Aggregate((a, b) => a + b) / Selection.count;
                     data.setCursor3DPosition(pos);
                 }
             });

            return root;
        }

    }
}


using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.ShortcutManagement;

namespace BlenderLikeExtentions
{
    [InitializeOnLoad]
    public static class SceneViewCameraOperation
    {
        static public bool isActive = true;
        static SceneViewCameraOperation()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        //camera pan hensuu
        // static bool isPan = false;
        // static Vector2 preMousePos = Vector2.zero;
        // static Vector2 currentPos = Vector2.zero;
        // static Vector3 preHitPos = Vector3.zero;
        // static Vector3 normal = Vector3.zero;
        // static Vector2 preMoveDirection = Vector2.zero;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isActive) return;
            //アクティブなSceneView以外を無視
            if (SceneView.currentDrawingSceneView != SceneView.lastActiveSceneView) return;

            var currentEvent = Event.current;
            var lastActiveSceneView = SceneView.lastActiveSceneView;

            //ホイールスクロール ズームインアウト
            if (currentEvent.isScrollWheel)
            {
                var delta = Mathf.Sign(Event.current.delta.y) == 1 ? 1.2f : .8f;

                lastActiveSceneView.size = lastActiveSceneView.size * delta;
                currentEvent.Use();
            }

            //カメラスライド移動
            //var isMouseOver = EditorWindow.mouseOverWindow == lastActiveSceneView;
            // if (isPan)
            // {
            //     //ドラッグ終了を検知
            //     if (Event.current.type == EventType.MouseUp || currentEvent.type == EventType.KeyUp || !currentEvent.shift)
            //     {
            //         isPan = false;
            //         return;
            //     };
            //     if (Event.current.type == EventType.MouseMove && isMouseOver && Event.current.button != 2)
            //     {
            //         isPan = false;
            //         return;
            //     };
            //     //ドラッグ中
            //     if (currentEvent.type == EventType.MouseDrag && isMouseOver
            //         || currentEvent.type == EventType.KeyDown && !isMouseOver)
            //     {
            //         //マウス位置が上下左右にワープした時はスキップする
            //         var xDirection = Mathf.Sign(currentEvent.mousePosition.x - preMousePos.x);
            //         var yDirection = Mathf.Sign(currentEvent.mousePosition.y - preMousePos.y);
            //         if (xDirection == preMoveDirection.x && yDirection == preMoveDirection.y)
            //         {
            //             //現在のマウス位置はドラッグ開始時のマウス位置に都度マウス移動量を加算して計算。
            //             currentPos.x += currentEvent.mousePosition.x - preMousePos.x;
            //             currentPos.y += currentEvent.mousePosition.y - preMousePos.y;
            //             //焦点位置を含むカメラに平行なPlaneにカメラからレイを飛ばす。前回の衝突位置との差分を焦点位置に加算する。
            //             var ray = HandleUtility.GUIPointToWorldRay(currentPos);
            //             new Plane(normal, lastActiveSceneView.pivot).Raycast(ray, out var distance);
            //             var hitPos = ray.GetPoint(distance);
            //             lastActiveSceneView.pivot += (preHitPos - hitPos);
            //         }
            //         preMoveDirection = new Vector2(xDirection, yDirection);
            //         preMousePos = currentEvent.mousePosition;
            //         Event.current.Use();
            //     }
            // }
            // //ドラッグ開始を検知
            // else
            // {
            //     if (currentEvent.button == 2 && Event.current.shift && Event.current.type == EventType.MouseDown)
            //     {
            //         isPan = true;
            //         var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
            //         new Plane(normal, lastActiveSceneView.pivot).Raycast(ray, out var distance);
            //         preHitPos = ray.GetPoint(distance);
            //         preMousePos = currentEvent.mousePosition;
            //         currentPos = currentEvent.mousePosition;
            //     }
            // }
        }
        //カメラのスライド移動
        [Shortcut("BlenderLike/Pan SceneView", typeof(SceneView), KeyCode.Mouse2, ShortcutModifiers.Shift)]
        static void Pan()
        {
            if (!isActive) return;
            bool isFist = true;
            var lastActiveSceneView = SceneView.lastActiveSceneView;
            var preMousePos = Vector2.zero;
            var preMoveDirection = Vector2.one;
            var currentPos = Vector3.zero;
            var normal = lastActiveSceneView.rotation * Vector3.back;
            var preHitPos = Vector3.zero;
            Action<SceneView> act = null;

            act = (SceneView view) =>
                {
                    var currentEvent = Event.current;
                    var isMouseOver = EditorWindow.mouseOverWindow == lastActiveSceneView;

                    //アクティブな SceneView以外は無視
                    if (SceneView.currentDrawingSceneView != lastActiveSceneView) { return; }

                    //ドラッグ終了を検出
                    if (Event.current.type == EventType.MouseUp || currentEvent.type == EventType.KeyUp || !currentEvent.shift)
                    {
                        SceneView.duringSceneGui -= act;
                        return;
                    };
                    if (Event.current.type == EventType.MouseMove && isMouseOver && Event.current.button != 2)
                    {
                        SceneView.duringSceneGui -= act;
                        return;
                    };

                    //初期化
                    if (isFist)
                    {
                        isFist = false;
                        var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                        new Plane(normal, view.pivot).Raycast(ray, out var distance);
                        preHitPos = ray.GetPoint(distance);
                        preMousePos = currentEvent.mousePosition;
                        currentPos = currentEvent.mousePosition;
                    }
                    //ドラッグ中
                    if (currentEvent.type == EventType.MouseDrag && isMouseOver
                         || currentEvent.type == EventType.KeyDown && !isMouseOver)
                    {
                        //マウス位置が上下左右にワープした時はスキップする
                        var xDirection = Mathf.Sign(currentEvent.mousePosition.x - preMousePos.x);
                        var yDirection = Mathf.Sign(currentEvent.mousePosition.y - preMousePos.y);
                        if (xDirection == preMoveDirection.x && yDirection == preMoveDirection.y)
                        {
                            //現在のマウス位置はドラッグ開始時のマウス位置に都度マウス移動量を加算して計算。
                            currentPos.x += currentEvent.mousePosition.x - preMousePos.x;
                            currentPos.y += currentEvent.mousePosition.y - preMousePos.y;
                            //焦点位置を含むカメラに平行なPlaneにカメラからレイを飛ばす。前回の衝突位置との差分を焦点位置に加算する。
                            var ray = HandleUtility.GUIPointToWorldRay(currentPos);
                            new Plane(normal, view.pivot).Raycast(ray, out var distance);
                            var hitPos = ray.GetPoint(distance);
                            view.pivot += (preHitPos - hitPos);
                        }
                        preMoveDirection = new Vector2(xDirection, yDirection);
                        preMousePos = currentEvent.mousePosition;
                    }
                };
            SceneView.duringSceneGui += act;
        }
        //左へ15度回転
        [Shortcut("BlenderLike/SceneView leftRotate 15", typeof(SceneView), KeyCode.Keypad4)]
        static void LeftRotate15()
        {
            var rotation = SceneView.lastActiveSceneView.rotation;
            RotateSceneCamera(Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y + 15f, rotation.eulerAngles.z))); ;
        }
        //右へ15度回転
        [Shortcut("BlenderLike/SceneView rightRotate 15", typeof(SceneView), KeyCode.Keypad6)]
        static void RightRotate15()
        {
            var rotation = SceneView.lastActiveSceneView.rotation;
            RotateSceneCamera(Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y - 15f, rotation.eulerAngles.z))); ;
        }
        //下へ15度回転
        [Shortcut("BlenderLike/SceneView bottomRotate 15", typeof(SceneView), KeyCode.Keypad2)]
        static void BottomRotate15()
        {
            RotateSceneCamera(SceneView.lastActiveSceneView.rotation * Quaternion.Euler(new Vector3(-15f, 0f, 0f)));
        }
        //上へ15度回転
        [Shortcut("BlenderLike/SceneView UpRotate 15", typeof(SceneView), KeyCode.Keypad8)]
        static void UpRotate15()
        {
            RotateSceneCamera(SceneView.lastActiveSceneView.rotation * Quaternion.Euler(new Vector3(15f, 0f, 0f)));
        }
        //正面へ回転
        [Shortcut("BlenderLike/SceneView Rotate Forward", typeof(SceneView), KeyCode.Keypad1)]
        static void RotateForward() { RotateSceneCamera(Vector3.back); }
        //後ろへ回転
        [Shortcut("BlenderLike/SceneView Rotate Back", typeof(SceneView), KeyCode.Keypad1, ShortcutModifiers.Control)]
        static void RotateBack() { RotateSceneCamera(Vector3.forward); }
        //右へ回転
        [Shortcut("BlenderLike/SceneView Rotate Right", typeof(SceneView), KeyCode.Keypad3)]
        static void RotateRight() { RotateSceneCamera(Vector3.left); }
        //左へ回転
        [Shortcut("BlenderLike/SceneView Rotate Left", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Control)]
        static void RotateLeft() { RotateSceneCamera(Vector3.right); }
        //上へ回転
        [Shortcut("BlenderLike/SceneView Rotate Up", typeof(SceneView), KeyCode.Keypad7)]
        static void RotateUp() { RotateSceneCamera(Vector3.down); }
        //下へ回転
        [Shortcut("BlenderLike/SceneView Rotate Bootm", typeof(SceneView), KeyCode.Keypad7, ShortcutModifiers.Control)]
        static void RotateBottom() { RotateSceneCamera(Vector3.up); }
        //透視投影と並行投影のトグル
        [Shortcut("BlenderLike/Toggle Perspective", typeof(SceneView), KeyCode.Keypad5)]
        static void TogglePerspective()
        {
            var activeSceneView = SceneView.lastActiveSceneView;
            activeSceneView.in2DMode = false;
            activeSceneView.LookAt(activeSceneView.pivot, activeSceneView.rotation, activeSceneView.size, !activeSceneView.orthographic);
        }
        //選択物にフォーカス
        [Shortcut("BlenderLike/Focus SelectedObject", typeof(SceneView), KeyCode.KeypadPeriod)]
        static void FocusSelectObjects() { SceneView.lastActiveSceneView.FrameSelected(false, false); }

        private static void RotateSceneCamera(Quaternion direction)
        {
            var activeSceneView = SceneView.lastActiveSceneView;
            activeSceneView.in2DMode = false;
            activeSceneView.LookAt(activeSceneView.pivot, direction, activeSceneView.size, activeSceneView.orthographic);
        }

        private static void RotateSceneCamera(Vector3 direction)
        {
            var activeSceneView = SceneView.lastActiveSceneView;
            activeSceneView.in2DMode = false;
            activeSceneView.LookAt(activeSceneView.pivot, Quaternion.LookRotation(direction), activeSceneView.size, activeSceneView.orthographic);
        }
    }
    // パン
    // [DllImport("user32.dll")]
    // public static extern bool SetCursorPos(int X, int Y);
    // [DllImport("user32.dll")]
    // private extern static bool GetCursorPos(out POINT lpPoint);

    // private delegate void SetCursorPosDelegate(POINT point);
    // public struct POINT
    // {
    //     public int X { get; set; }
    //     public int Y { get; set; }
    //     public float Xfloat => (float)X;
    //     public float Yfloat => (float)Y;
    // }
    // [Shortcut("BlenderLike/Pan SceneView", typeof(SceneView), KeyCode.Mouse2, ShortcutModifiers.Shift)]
    // static void Pan()
    // {
    //     bool isFist = true;
    //     var lastActiveSceneView = SceneView.lastActiveSceneView;
    //     var rect = lastActiveSceneView.position;
    //     Action<SceneView> act = null;

    //     act = (SceneView view) =>
    //         {
    //             var currentEvent = Event.current;
    //             var isMouseOver = EditorWindow.mouseOverWindow == lastActiveSceneView;

    //             if (SceneView.currentDrawingSceneView != lastActiveSceneView) { return; }

    //             if (Event.current.type == EventType.MouseUp || currentEvent.type == EventType.KeyUp || !currentEvent.shift)
    //             {
    //                 SceneView.duringSceneGui -= act;
    //                 return;
    //             };

    //             //初期化
    //             if (isFist)
    //             {
    //                 isFist = false;
    //                 var normal = view.rotation * Vector3.back;
    //                 var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
    //                 new Plane(normal, view.pivot).Raycast(ray, out var distance);
    //                 preHitPos = ray.GetPoint(distance);
    //             }
    //             //ドラッグによるカメラ移動
    //             if (currentEvent.type == EventType.MouseDrag && isMouseOver
    //              || currentEvent.type == EventType.KeyDown && !isMouseOver)
    //             {
    //                 // var normal = view.rotation * Vector3.back;
    //                 // var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
    //                 // new Plane(normal, view.pivot).Raycast(ray, out var distance);
    //                 // var hitPos = ray.GetPoint(distance);
    //                 // view.pivot += (preHitPos - hitPos);
    //                 var scale = EditorPrefs.GetInt("CustomEditorUIScale") / 100;
    //                 GetCursorPos(out var point);
    //                 var x = rect.x / scale;
    //                 var y = rect.y / scale;
    //                 var width = rect.width / scale;
    //                 var height = rect.height / scale;
    //                 float nextX = Mathf.Repeat((point.Xfloat - x) % width, width) + x;
    //                 float nextY = Mathf.Repeat((point.Yfloat - y) % height, height) + y;
    //                 Debug.Log(width + x);
    //                 Debug.Log(height + y);
    //                 Debug.Log(nextX);
    //                 Debug.Log(nextY);
    //             }
    //         };
    //     SceneView.duringSceneGui += act;
    // }


}







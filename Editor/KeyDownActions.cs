using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System;
namespace BlenderLikeExtentions
{
    public class KeydownActions
    {
        public Dictionary<KeyInfo, Action> dict = new Dictionary<KeyInfo, Action>();
        KeyCode[] usedKeys;

        public bool TryExecute()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown) return false;
            if (!CheckContaineKey()) return false;
            this.dict.TryGetValue(new KeyInfo(currentEvent.keyCode, currentEvent.control, currentEvent.shift, currentEvent.alt), out var action);
            if (action != null)
            {
                action();
                currentEvent.Use();
                SceneView.lastActiveSceneView.Repaint();
                return true;
            }
            else return false;
        }

        public void Add(KeyInfo keyInfo, Action action)
        {
            this.dict.TryGetValue(keyInfo, out var _action);
            if (_action == null)
            {
                this.dict[keyInfo] = action;
            }
            else
            {
                _action += action;
            }
        }

        bool CheckContaineKey()
        {
            if (usedKeys == null) usedKeys = this.dict.Select(_ => _.Key.keyCode).Distinct().ToArray();
            return (usedKeys.Contains(Event.current.keyCode));
        }
    }
    public struct KeyInfo : IEquatable<KeyInfo>
    {
        public KeyCode keyCode;
        bool shift;
        bool control;
        bool alt;

        public KeyInfo(KeyCode key, bool control, bool shift, bool alt)
        {
            this.keyCode = key;
            this.shift = shift;
            this.control = control;
            this.alt = alt;
        }

        public bool Equals(KeyInfo obj)
        {
            return keyCode == obj.keyCode && shift == obj.shift && control == obj.control && alt == obj.alt;
        }

        public override bool Equals(object obj) => Equals(obj is KeyInfo);

        public override int GetHashCode()
        {
            return HashCode.Combine(keyCode, shift, alt, control);
            // return (keyCode, shift, alt, control).GetHashCode();
        }
    }
}
using UnityEngine;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;

namespace Higo.Animation.Controller
{
    public enum AnimationStateType
    {
        Clip, BlendTree
    }

    [Serializable]
    public class AnimationState
    {
        public AnimationStateType Type;
        public AnimationClip Motion;
        public BlendTree Tree;
        public float Speed = 1;
    }

    [Serializable]
    public class AnimationLayer
    {
        public string name;
        public List<AnimationState> states;
    }

    public class AnimatorAuthoringComponent : MonoBehaviour
    {
        public List<AnimationLayer> Layers;
    }
}

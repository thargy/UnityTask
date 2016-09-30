using System;

namespace UnityEngine
{
    #region Component Shell
    /// <summary>
    /// 
    /// <para>
    /// Base class for everything attached to GameObjects.
    /// </para>
    /// 
    /// </summary>
    public class Component 
    {
    }

    /// <summary>
    /// 
    /// <para>
    /// Behaviours are Components that can be enabled or disabled.
    /// </para>
    /// 
    /// </summary>
    public class Behaviour : Component
    {
        /// <summary>
        /// 
        /// <para>
        /// Enabled Behaviours are Updated, disabled Behaviours are not.
        /// </para>
        /// 
        /// </summary>
        public bool enabled { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// Has the Behaviour had enabled called.
        /// </para>
        /// 
        /// </summary>
        public bool isActiveAndEnabled { get; set; }
    }

    /// <summary>
    /// 
    /// <para>
    /// MonoBehaviour is the base class every script derives from.
    /// </para>
    /// 
    /// </summary>
    public class MonoBehaviour : Behaviour
    {
    }
    #endregion

    #region Unity Editor Attributes
    /// <summary>
    /// 
    /// <para>
    /// Base class to derive custom property attributes from. Use this to create custom attributes for script variables.
    /// </para>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public abstract class PropertyAttribute : Attribute
    {
        /// <summary>
        /// 
        /// <para>
        /// Optional field to specify the order that multiple DecorationDrawers should be drawn in.
        /// </para>
        /// 
        /// </summary>
        public int order { get; set; }
    }

    /// <summary>
    /// 
    /// <para>
    /// Use this PropertyAttribute to add a header above some fields in the Inspector.
    /// </para>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HeaderAttribute : PropertyAttribute
    {
        /// <summary>
        /// 
        /// <para>
        /// The header text.
        /// </para>
        /// 
        /// </summary>
        public readonly string header;

        /// <summary>
        /// 
        /// <para>
        /// Add a header above some fields in the Inspector.
        /// </para>
        /// 
        /// </summary>
        /// <param name="header">The header text.</param>
        public HeaderAttribute(string header)
        {
            this.header = header;
        }
    }

    /// <summary>
    /// 
    /// <para>
    /// Specify a tooltip for a field.
    /// </para>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class TooltipAttribute : PropertyAttribute
    {
        /// <summary>
        /// 
        /// <para>
        /// The tooltip text.
        /// </para>
        /// 
        /// </summary>
        public readonly string tooltip;

        /// <summary>
        /// 
        /// <para>
        /// Specify a tooltip for a field.
        /// </para>
        /// 
        /// </summary>
        /// <param name="tooltip">The tooltip text.</param>
        public TooltipAttribute(string tooltip)
        {
            this.tooltip = tooltip;
        }
    }

    /// <summary>
    /// 
    /// <para>
    /// Attribute used to make a float or int variable in a script be restricted to a specific range.
    /// </para>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class RangeAttribute : PropertyAttribute
    {
        public readonly float min;
        public readonly float max;

        /// <summary>
        /// 
        /// <para>
        /// Attribute used to make a float or int variable in a script be restricted to a specific range.
        /// </para>
        /// 
        /// </summary>
        /// <param name="min">The minimum allowed value.</param><param name="max">The maximum allowed value.</param>
        public RangeAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
    #endregion

    /// <summary>
    /// 
    /// <para>
    /// A collection of common math functions.
    /// </para>
    /// 
    /// </summary>
    public struct Mathf
    {
        /// <summary>
        /// 
        /// <para>
        /// Clamps a value between a minimum float and maximum float value.
        /// </para>
        /// 
        /// </summary>
        /// <param name="value"/><param name="min"/><param name="max"/>
        public static float Clamp(float value, float min, float max)
        {
            if ((double)value < (double)min)
                value = min;
            else if ((double)value > (double)max)
                value = max;
            return value;
        }

        /// <summary>
        /// 
        /// <para>
        /// Clamps value between min and max and returns value.
        /// </para>
        /// 
        /// </summary>
        /// <param name="value"/><param name="min"/><param name="max"/>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

    }


    /// <summary>
    /// 
    /// <para>
    /// The interface to get time information from Unity.
    /// </para>
    /// 
    /// </summary>
    public sealed class Time
    {
        /// <summary>
        /// 
        /// <para>
        /// The time at the beginning of this frame (Read Only). This is the time in seconds since the start of the game.
        /// </para>
        /// 
        /// </summary>
        public static  float time { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The time this frame has started (Read Only). This is the time in seconds since the last level has been loaded.
        /// </para>
        /// 
        /// </summary>
        public static  float timeSinceLevelLoad { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The time in seconds it took to complete the last frame (Read Only).
        /// </para>
        /// 
        /// </summary>
        public static  float deltaTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The time the latest MonoBehaviour.FixedUpdate has started (Read Only). This is the time in seconds since the start of the game.
        /// </para>
        /// 
        /// </summary>
        public static  float fixedTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The timeScale-independant time at the beginning of this frame (Read Only). This is the time in seconds since the start of the game.
        /// </para>
        /// 
        /// </summary>
        public static  float unscaledTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The timeScale-independent time in seconds it took to complete the last frame (Read Only).
        /// </para>
        /// 
        /// </summary>
        public static  float unscaledDeltaTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The interval in seconds at which physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour.FixedUpdate) are performed.
        /// </para>
        /// 
        /// </summary>
        public static  float fixedDeltaTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The maximum time a frame can take. Physics and other fixed frame rate updates (like MonoBehaviour's MonoBehaviour.FixedUpdate).
        /// </para>
        /// 
        /// </summary>
        public static  float maximumDeltaTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// A smoothed out Time.deltaTime (Read Only).
        /// </para>
        /// 
        /// </summary>
        public static  float smoothDeltaTime { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The scale at which the time is passing. This can be used for slow motion effects.
        /// </para>
        /// 
        /// </summary>
        public static  float timeScale { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The total number of frames that have passed (Read Only).
        /// </para>
        /// 
        /// </summary>
        public static  int frameCount { get; set; }

        public static  int renderedFrameCount { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// The real time in seconds since the game started (Read Only).
        /// </para>
        /// 
        /// </summary>
        public static  float realtimeSinceStartup { get; set; }

        /// <summary>
        /// 
        /// <para>
        /// Slows game playback time to allow screenshots to be saved between frames.
        /// </para>
        /// 
        /// </summary>
        public static  int captureFramerate { get; set; }
    }
}

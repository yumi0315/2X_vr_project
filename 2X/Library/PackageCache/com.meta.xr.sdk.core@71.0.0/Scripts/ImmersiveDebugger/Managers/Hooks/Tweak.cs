/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using Meta.XR.ImmersiveDebugger.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.ImmersiveDebugger.Manager
{
    internal static class TweakUtils
    {
        private static readonly Dictionary<Type, Type> _types = new Dictionary<Type, Type>();
        private static readonly HashSet<Type> _supportsValueRange = new();

        private const string Min = "min";
        private const string Max = "max";

        static TweakUtils()
        {
            _types?.Clear(); // reset static fields in case of domain reload disabled

            _supportsValueRange?.Clear();
            _supportsValueRange?.Add(typeof(int));
            _supportsValueRange?.Add(typeof(float));

            Register<float>(Mathf.InverseLerp, Mathf.Lerp, f => f);
            Register<int>(
                (start, end, value) => Mathf.InverseLerp((float)start, (float)end, (float)value),
                (start, end, tween) => Mathf.RoundToInt(Mathf.Lerp((float)start, (float)end, tween)),
                f => (int)f);
            Register<bool>((_, _, value) => value ? 1.0f : 0.0f, (_, _, tween) => (tween > 0.0f), f => f > 0.0f);
        }

        public static bool IsTypeSupported(Type t)
        {
            return t != null && _types.ContainsKey(t);
        }

        public static bool IsTypeSupportsValueRange(Type t) => t != null && _supportsValueRange.Contains(t);

        public static Tweak Create(MemberInfo memberInfo, DebugMember attribute, object instance)
        {
            var type = memberInfo.GetDataType();
            if (!_types.TryGetValue(type, out var createdType))
            {
                return null;
            }

            return Activator.CreateInstance(createdType, memberInfo, instance, attribute) as Tweak;
        }

        private static void Register<T>(Func<T, T, T, float> inverseLerp, Func<T, T, float, T> lerp, Func<float, T> fromFloat)
        {
            _types.Add(typeof(T), typeof(Tweak<T>));
            Tweak<T>.InverseLerp = inverseLerp;
            Tweak<T>.Lerp = lerp;
            Tweak<T>.FromFloat = fromFloat;
        }

        public static bool IsMemberTypeValidForTweak(MemberInfo member) =>
            member.MemberType == MemberTypes.Field && IsTypeSupported((member as FieldInfo)?.FieldType) ||
            member.MemberType == MemberTypes.Property && IsTypeSupported((member as PropertyInfo)?.PropertyType);

        public static void ProcessMinMaxRange(MemberInfo member, DebugMember attribute, Object instance)
        {
            var memberType = member.GetDataType();
            double value = 0;

            // This is to avoid InvalidCastException
            if (memberType == typeof(float)) value = (float)member.GetValue(instance);
            else if (memberType == typeof(int)) value = (int)member.GetValue(instance);
            else if (memberType == typeof(double)) value = (double)member.GetValue(instance);

            if (attribute.Min <= value && value <= attribute.Max)
            {
                return;
            }

            // 50% min, max range
            var spread = Mathf.Abs((float)(value * 0.5f));
            attribute.Min = RoundToNearest((float)(value - spread), Min);
            attribute.Max = RoundToNearest((float)(value + spread), Max);
        }

        internal static float RoundToNearest(float value, string op)
        {
            var scale = 0f;
            if (value >= 0)
            {
                switch (value)
                {
                    case < 1:
                        return op == Min ? 0 : 1; // Explicitly set for small values
                    case < 10:
                        return Mathf.Round(value);
                    default:
                        scale = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(value)));
                        return Mathf.Ceil(value / scale) * scale;
                }
            }

            switch (value)
            {
                case > -1:
                    return op == Min ? -1.0f : 0; // Explicitly set to -1 for small negative values
                case > -10:
                    return Mathf.Round(value);
                default:
                    scale = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(-value)));
                    var scaledToTen = op == Min ? Mathf.Floor(value / scale) : Mathf.Ceil(value / scale);
                    return scaledToTen * scale;
            }
        }
    }

    internal abstract class Tweak : Hook
    {
        public abstract float Tween { get; set; }

        protected Tweak(MemberInfo memberInfo, object instance, DebugMember attribute) : base(memberInfo, instance, attribute) { }
    }

    internal class Tweak<T> : Tweak
    {
        public static Func<T, T, T, float> InverseLerp;
        public static Func<T, T, float, T> Lerp;
        public static Func<float, T> FromFloat;

        private readonly Func<T> _getter;
        private readonly Action<T> _setter;
        private readonly T _min;
        private readonly T _max;

        public override float Tween
        {
            get => InverseLerp(_min, _max, _getter.Invoke());
            set => _setter.Invoke(Lerp(_min, _max, value));
        }

        public Tweak(MemberInfo memberInfo, object instance, DebugMember attribute) : base(memberInfo, instance, attribute)
        {
            _min = FromFloat(attribute.Min);
            _max = FromFloat(attribute.Max);
            _getter = () => (T)memberInfo.GetValue(instance);
            _setter = (value => memberInfo.SetValue(instance, value));
        }
    }
}

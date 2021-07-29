using System;
using System.Collections.Generic;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class EnableProperty : Property, IEquatable<EnableProperty>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ EnableValue;
            }
        }

        public int EnableValue;

        public EnableProperty(string name, string displayName, int enableValue)
        {
            Name = name;
            DisplayName = displayName;
            Type = "Float";
            DefaultValue = "0.1";
            Attributes = new List<string>();

            EnableValue = enableValue;
        }

        public EnableProperty(string name, int enableValue) : this(name, name, enableValue){}

        public override bool Equals(object obj)
        {
            return Equals(obj as EnableProperty);
        }

        public bool Equals(EnableProperty other)
        {
            return other != null &&
                   base.Equals(other) &&
                   Name == other.Name;
        }

        public static bool operator ==(EnableProperty left, EnableProperty right)
        {
            return EqualityComparer<EnableProperty>.Default.Equals(left, right);
        }

        public static bool operator !=(EnableProperty left, EnableProperty right)
        {
            return !(left == right);
        }
    }
}
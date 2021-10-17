using System;
using System.Collections.Generic;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Property used to define if a module should be enabled or not.
    /// </summary>
    [Serializable]
    public class EnableProperty : Property, IEquatable<EnableProperty>
    {
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
        
        public override Variable ToVariable()
        {
            Variable variable = new Variable();
            variable.Name = Name;
            variable.Type = VariableType.Float;
            return variable;
        }

        public EnableProperty(string name, int enableValue) : this(name, name, enableValue){}

        bool IEquatable<EnableProperty>.Equals(EnableProperty other)
        {
            return Equals(other);
        }

        public static bool operator == (EnableProperty left, EnableProperty right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(EnableProperty left, EnableProperty right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
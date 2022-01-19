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
        /// <summary>
        /// Value to enable the module that uses this enable property.
        /// </summary>
        public int EnableValue;

        /// <summary>
        /// Main constructor.
        /// </summary>
        /// <param name="name">name of the property.</param>
        /// <param name="displayName">Display name of the property.</param>
        /// <param name="enableValue">Value to enable the module that uses this enable property.</param>
        public EnableProperty(string name, string displayName, int enableValue)
        {
            Name = name;
            DisplayName = displayName;
            Type = "Float";
            DefaultValue = "0.1";
            Attributes = new List<string>();

            EnableValue = enableValue;
        }
        
        /// <summary>
        /// Convert the property to its variable implementation.
        /// </summary>
        /// <returns>Shader variable referring to this property.</returns>
        public override Variable ToVariable()
        {
            Variable variable = new Variable();
            variable.Name = Name;
            variable.Type = VariableType.Float;
            return variable;
        }

        /// <summary>
        /// Simpler constructor where the name and display name are the same.
        /// </summary>
        /// <param name="name">Name of the property, used also as display name.</param>
        /// <param name="enableValue">Value to enable the module that uses this enable property.</param>
        public EnableProperty(string name, int enableValue) : this(name, name, enableValue){}

        bool IEquatable<EnableProperty>.Equals(EnableProperty other)
        {
            return Equals(other);
        }
        
        public override bool Equals(object obj)
        {
            if (obj is Property other)
                return Name == other.Name;

            return false;
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
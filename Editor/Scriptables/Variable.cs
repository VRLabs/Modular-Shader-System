using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class Variable : IEquatable<Variable>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                //int hashCode = base.GetHashCode();
                int hashCode =  (Name != null ? Name.GetHashCode() : 0);
                return hashCode;
            }
        }

        public string Name;
        public string Type;

        public override bool Equals(object obj)
        {
            return Equals(obj as Variable);
        }

        public bool Equals(Variable other)
        {
            return other != null &&
                   Name.Equals(other.Name);
        }

        public static bool operator ==(Variable left, Variable right)
        {
            return EqualityComparer<Variable>.Default.Equals(left, right);
        }

        public static bool operator !=(Variable left, Variable right)
        {
            return !(left == right);
        }
    }
}
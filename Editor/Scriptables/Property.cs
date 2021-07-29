using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class Property : IEquatable<Property>
    {
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DisplayName != null ? DisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DefaultValue != null ? DefaultValue.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Attributes != null ? Attributes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public string Name;
        public string DisplayName;
        public string Type; //Check if the content is valid
        public string DefaultValue; //Check if the content is right for the type
        public List<string> Attributes;

        public Variable ToVariable()
        {
            Variable variable = new Variable();
            variable.Name = Name;

            switch(Type)
            {
                case "Float": variable.Name = "float"; break;
                case "Int": variable.Name = "float"; break;
                case "Color": variable.Name = "float4"; break;
                case "Vector": variable.Name = "float4"; break;
                case "2D": variable.Name = "Texture2D"; break;
                case "3D": variable.Name = "Texture3D"; break;
                case "Cube": variable.Name = "TextureCube"; break;
                case "2DArray": variable.Name = "Texture2DArray"; break;
                case "CubeArray": variable.Name = "TextureCubeArray "; break;
                default: variable.Type = Type.StartsWith("Range") ? "float" : Type; break;
            }

            return variable;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Property);
        }

        public bool Equals(Property other)
        {
            return other != null &&
                   Name == other.Name;
        }

        public static bool operator ==(Property left, Property right)
        {
            return EqualityComparer<Property>.Default.Equals(left, right);
        }

        public static bool operator !=(Property left, Property right)
        {
            return !(left == right);
        }
    }
}
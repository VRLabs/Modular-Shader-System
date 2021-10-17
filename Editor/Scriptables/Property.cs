using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRLabs.ModularShaderSystem
{
    [Serializable]
    public class Property : IEquatable<Property>
    {
        public string Name;
        public string DisplayName;
        public string Type; //Check if the content is valid
        public string DefaultValue; //Check if the content is right for the type
        public List<string> Attributes;

        public virtual Variable ToVariable()
        {
            Variable variable = new Variable();
            variable.Name = Name;

            switch(Type)
            {
                case "Float": variable.Type = VariableType.Float; break;
                case "Int": variable.Type = VariableType.Float; break;
                case "Color": variable.Type = VariableType.Float4; break;
                case "Vector": variable.Type = VariableType.Float4; break;
                case "2D": variable.Type = VariableType.Texture2D; break;
                case "3D": variable.Type = VariableType.Texture3D; break;
                case "Cube": variable.Type = VariableType.TextureCube; break;
                case "2DArray": variable.Type = VariableType.Texture2DArray; break;
                case "CubeArray": variable.Type = VariableType.TextureCubeArray; break;
                default: variable.Type = Type.StartsWith("Range") ? VariableType.Float : VariableType.Custom; break;
            }

            return variable;
        }

        public override bool Equals(object obj)
        {
            if (obj is Property other)
                return Name == other.Name;

            return false;
        }
        
        bool IEquatable<Property>.Equals(Property other)
        {
            return Equals(other);
        }

        public static bool operator == (Property left, Property right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(Property left, Property right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            int hashCode = (Name != null ? Name.GetHashCode() : 0);
            return hashCode;
        }
    }
}
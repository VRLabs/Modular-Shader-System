using System;
using System.Collections.Generic;
using UnityEngine;
using VRLabs.ModularShaderSystem.UI;

namespace VRLabs.ModularShaderSystem
{
    /// <summary>
    /// Typed of shader properties.
    /// </summary>
    public enum PropertyType
    {
        Float,
        Int,
        Range,
        Vector,
        Color,
        Texture2D,
        Texture2DArray,
        Cube,
        CubeArray,
        Texture3D
    }
    
    /// <summary>
    /// Shader property information.
    /// </summary>
    [Serializable]
    public class Property : IEquatable<Property>
    {
        /// <summary>
        /// Name of the shader property.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// Display name of the shader property.
        /// </summary>
        public string DisplayName;
        
        /// <summary>
        /// Type of the shader property.
        /// </summary>
        public string Type;
        
        /// <summary>
        /// Default value of the shader property.
        /// </summary>
        public string DefaultValue;

        /// <summary>
        /// Default texture asset assigned to the property if it's a Texture2D or Texture3D.
        ///
        /// Obsolete as of 1.1.0, use the relative list in the <see cref="ShaderModule"/> or <see cref="ModularShader"/> assets.
        /// </summary>
        [Obsolete]
        public Texture DefaultTextureAsset;
        
        /// <summary>
        /// List of attributes for the shader property.
        /// </summary>
        [PropertyAttribute]
        public List<string> Attributes;

        /// <summary>
        /// Convert the property to a shader variable.
        /// </summary>
        /// <returns>A variable representing the the property in shader code.</returns>
        public virtual Variable ToVariable()
        {
            var variable = new Variable();
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